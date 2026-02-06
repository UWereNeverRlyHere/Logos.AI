using Logos.AI.Abstractions.Validation;
namespace Logos.AI.Engine.Validation;

/// <summary>
/// Калькулятор метрик впевненості на основі логарифмічних ймовірностей токенів (logprobs).
/// </summary>
public static class LogProbMetricsCalculator
{
	private const double Alpha = 0.65;

	/// <summary>
	/// Розраховує набір статистичних метрик для оцінки впевненості моделі.
	/// </summary>
	/// <param name="tokens">Список токенів та їх логарифмічних ймовірностей.</param>
	/// <returns>Об'єкт з розрахованими метриками.</returns>
	public static LogProbMetrics Calculate(IReadOnlyList<(string Token, double LogProb)> tokens)
	{
		if (tokens.Count == 0)
			return LogProbMetrics.Empty;

		int n = tokens.Count;

		var probs = tokens.Select(t => Math.Exp(t.LogProb)).ToList();
		var logProbs = tokens.Select(t => t.LogProb).ToList();

		double sumLogProb = logProbs.Sum();
		double avgLogProb = sumLogProb / n;

		// 1. Intrinsic probabilistic confidence
		double intrinsicConfidence = Math.Exp(avgLogProb);

		// 2. Perplexity (risk indicator)
		double perplexity = Math.Exp(-avgLogProb);

		// 3. Token entropy
		double entropy = probs.Where(p => p > 0).Sum(p => -p * Math.Log(p));
		entropy /= n;

		// 4. Weakest link
		var weakest = tokens.MinBy(t => t.LogProb)!;
		double weakestProb = Math.Exp(weakest.LogProb);

		// 5. Length normalization (Wu et al.)
		double lengthPenalty =
			Math.Pow(5.0 + n, Alpha) /
			Math.Pow(5.0 + 1.0, Alpha);

		return new LogProbMetrics(
			intrinsicConfidence,
			perplexity,
			entropy,
			weakest.Token,
			weakestProb,
			lengthPenalty
		);
	}
    // 1. Оцінка Score (вже після штрафів)
    public static ConfidenceLevel GetLevel(double score) => score switch
    {
        >= 0.85 => ConfidenceLevel.Certain, // Майже ідеал
        >= 0.65 => ConfidenceLevel.High,    // Дуже добре
        >= 0.50 => ConfidenceLevel.Medium,  // Робочий варіант
        >= 0.30 => ConfidenceLevel.Low,     // Сумнівно
        _ => ConfidenceLevel.Uncertain      // Сміття
    };

    // 2. Оцінка Перплексії (чим менше, тим краще)
    // 1.0 - ідеал (модель точно знала кожне слово)
    public static ConfidenceLevel GetPerplexityLevel(double ppl) => ppl switch
    {
        < 1.5 => ConfidenceLevel.Certain, // Текст дуже передбачуваний і чіткий
        < 3.0 => ConfidenceLevel.High,    
        < 6.0 => ConfidenceLevel.Medium,  // Трохи "творчий" або складний текст
        < 15.0 => ConfidenceLevel.Low,    // Модель плуталася
        _ => ConfidenceLevel.Uncertain
    };

    // 3. Оцінка Ентропії (чим менше, тим краще)
    // 0 - повна детермінованість
    public static ConfidenceLevel GetEntropyLevel(double entropy) => entropy switch
    {
        < 0.2 => ConfidenceLevel.Certain, // Модель не вагалася між варіантами
        < 0.6 => ConfidenceLevel.High,
        < 1.2 => ConfidenceLevel.Medium,
        < 2.0 => ConfidenceLevel.Low,
        _ => ConfidenceLevel.Uncertain
    };

    // 4. Фінальний рівень (Агрегація)
    // Тут ми діємо консервативно: якщо хоч один показник критичний - знижуємо оцінку.
    public static ConfidenceLevel GetFinalLevel(double finalScore, LogProbMetrics m)
    {
        var scoreLevel = GetLevel(finalScore);
        
        // Якщо Score високий, але Перплексія або Ентропія жахливі -> це прихована галюцинація
        if (m.Perplexity > 10.0 || m.Entropy > 2.0)
        {
            // Навіть якщо Score був High, ми його обвалюємо до Low
            return ConfidenceLevel.Low; 
        }

        // Щоб отримати Certain, все має бути ідеальним
        if (scoreLevel == ConfidenceLevel.Certain)
        {
            if (m.Perplexity > 2.0 || m.Entropy > 0.4 || m.WeakestTokenProbability < 0.2)
            {
                return ConfidenceLevel.High; // Downgrade to High
            }
        }

        return scoreLevel;
    }
}