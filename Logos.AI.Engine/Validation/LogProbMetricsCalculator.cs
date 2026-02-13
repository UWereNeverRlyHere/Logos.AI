using Logos.AI.Abstractions.Validation;
namespace Logos.AI.Engine.Validation;

/// <summary>
/// Калькулятор метрик впевненості на основі логарифмічних імовірностей токенів (logprobs).
/// </summary>
public static class LogProbMetricsCalculator
{
	private const double Alpha = 0.65;
	private const double UncertaintyTokenThreshold = 0.35;
	/// <summary>
	/// Розраховує набір статистичних метрик для оцінки впевненості моделі.
	/// </summary>
	/// <param name="tokens">Список токенів та їх логарифмічних ймовірностей.</param>
	/// <returns>Об'єкт з розрахованими метриками.</returns>
	public static LogProbMetrics Calculate(IReadOnlyList<(string Token, double LogProb)> tokens)
	{
		if (tokens.Count == 0) return LogProbMetrics.Empty;

		int n = tokens.Count;
		var logProbs = tokens.Select(t => t.LogProb).ToList();

		double sumLogProb = logProbs.Sum();
		double avgLogProb = sumLogProb / n;

		// 1. Intrinsic probabilistic confidence (Внутрішня впевненість)
		double intrinsicConfidence = Math.Exp(avgLogProb);

		// 2. Perplexity (Перплексія - міра "здивування" моделі)
		double perplexity = Math.Exp(-avgLogProb);

		// 3. Token entropy (Ентропія - міра невизначеності)
		// Використовуємо ймовірності p = exp(logProb)
		double entropy = tokens
			.Select(t => Math.Exp(t.LogProb))
			.Where(p => p > 0)
			.Sum(p => -p * Math.Log(p));
		entropy /= n;

		// 4. Weakest link (Найслабша ланка)
		var weakest = tokens.MinBy(t => t.LogProb);
		double weakestProb = weakest != default ? Math.Exp(weakest.LogProb) : 0.0;

		// 5. Length normalization (Wu et al. 2016)
		// Формула: (5 + |Y|)^α / (5 + 1)^α
		double lengthPenalty = Math.Pow(5.0 + n, Alpha) / Math.Pow(6.0, Alpha);

		return new LogProbMetrics(
			intrinsicConfidence,
			perplexity,
			entropy,
			weakest.Token ?? string.Empty,
			weakestProb,
			lengthPenalty
		);
	}
	/// <summary>
	/// Аналізує розподіл невпевненості по тексту (Focal vs Diffuse).
	/// </summary>
	public static UncertaintyResult AnalyzeUncertainty(IReadOnlyList<(string Token, double LogProb)> tokens)
	{
		// 1. Фільтруємо токени, що нижче порогу "комфорту" (0.35)
		var lowConfidenceTokens = tokens
			.Where(t => Math.Exp(t.LogProb) < UncertaintyTokenThreshold)
			.ToList();

		// 2. Визначаємо тип
		UncertaintyType type = lowConfidenceTokens.Count switch
		{
			0 => UncertaintyType.None,
            
			// СИНХРОНІЗАЦІЯ: <= 7 токенів (і < 15% тексту) -> Focal
			<= 7 when (double)lowConfidenceTokens.Count / tokens.Count < 0.15 => UncertaintyType.Focal,
            
			// Інакше -> Diffuse
			_ => UncertaintyType.Diffuse
		};

		// 3. Збираємо топ найслабших для логів (щоб людина бачила, де проблема)
		var weakestTokens = tokens
			.OrderBy(t => t.LogProb)
			.Take(10)
			.Select(t => $"'{t.Token}' ({Math.Exp(t.LogProb):P0})")
			.ToList();

		return new UncertaintyResult(type, weakestTokens);
	}
    /// <summary>
    /// Оцінка Score (вже після штрафів)
    /// </summary>
    public static ConfidenceLevel GetLevel(double score) => score switch
    {
        >= 0.85 => ConfidenceLevel.Certain, // Майже ідеал
        >= 0.65 => ConfidenceLevel.High,    // Дуже добре
        >= 0.52 => ConfidenceLevel.Medium,  // Робочий варіант
        >= 0.30 => ConfidenceLevel.Low,     // Сумнівно
        _ => ConfidenceLevel.Uncertain      // Сміття
    };
    
    /// <summary>
    /// Оцінка Перплексії (чим менше, тим краще)
    /// 1.0 - ідеал (модель точно знала кожне слово)
    /// </summary>
    public static ConfidenceLevel GetPerplexityLevel(double ppl) => ppl switch
    {
        < 1.2 => ConfidenceLevel.Certain, // Текст дуже передбачуваний і чіткий
        < 2.5 => ConfidenceLevel.High,    
        < 6.0 => ConfidenceLevel.Medium,  // Трохи "творчий" або складний текст
        < 15.0 => ConfidenceLevel.Low,    // Модель плуталася
        _ => ConfidenceLevel.Uncertain
    };
    /// <summary>
    /// Оцінка Ентропії (чим менше, тим краще)
    /// 0 - повна детермінованість
    /// </summary>
    public static ConfidenceLevel GetEntropyLevel(double entropy) => entropy switch
    {
        < 0.15 => ConfidenceLevel.Certain, // Модель не вагалася між варіантами
        < 0.6 => ConfidenceLevel.High,
        < 1.2 => ConfidenceLevel.Medium,
        < 2.0 => ConfidenceLevel.Low,
        _ => ConfidenceLevel.Uncertain
    };
    /// <summary>
    /// Оцінює вплив штрафного коефіцієнта (наприклад, Wu Score або Risk Penalty).
    /// </summary>
    /// <param name="factor">Множник (0.0 - 1.0), на який множиться фінальний бал.</param>
    public static ConfidenceLevel GetPenaltyLevel(double factor) => factor switch
    {
	    >= 0.90 => ConfidenceLevel.Certain, // Штраф майже відсутній
	    >= 0.75 => ConfidenceLevel.High,    // Незначний вплив
	    >= 0.55 => ConfidenceLevel.Medium,  // Помірний вплив
	    _ => ConfidenceLevel.Low            // Критичний вплив (обвалює оцінку)
    };
    /// <summary>
    /// Визначає фінальний рівень впевненості з урахуванням принципу "вето".
    /// </summary>
    public static ConfidenceLevel GetFinalLevel(double finalScore, LogProbMetrics m)
    {
	    var scoreLevel = GetLevel(finalScore);

	    // Принцип вето: якщо перплексія або ентропія критичні, ми не можемо дати високу оцінку,
	    // навіть якщо середній бал (finalScore) дивом виявився високим.
        
	    if (m.Perplexity > 6.0 || m.Entropy > 1.5)
	    {
		    // Downgrade: якщо метрики "шумні", максимум Medium
		    return scoreLevel > ConfidenceLevel.Medium ? ConfidenceLevel.Medium : scoreLevel;
	    }

	    if (m.Perplexity > 15.0 || m.Entropy > 2.5)
	    {
		    // Критичні показники -> Low/Uncertain
		    return ConfidenceLevel.Low;
	    }

	    // Перевірка на "слабку ланку": якщо є токени з майже нульовою ймовірністю,
	    // це може свідчити про локальну галюцинацію, навіть у хорошому тексті.
	    if (scoreLevel == ConfidenceLevel.Certain && m.WeakestTokenProbability < 0.15)
	    {
		    return ConfidenceLevel.High;
	    }

	    return scoreLevel;
    }
}

