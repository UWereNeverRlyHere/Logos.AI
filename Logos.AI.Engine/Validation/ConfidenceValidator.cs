using Logos.AI.Abstractions.Reasoning;
using Logos.AI.Abstractions.Validation;
using Logos.AI.Abstractions.Validation.Contracts;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.Validation;

/// <summary>
/// Валідатор впевненості, який використовує статистичні метрики логарифмічних ймовірностей токенів.
/// </summary>
public sealed class ConfidenceValidator(ILogger<ConfidenceValidator> logger) : IConfidenceValidator
{
	// --- Налаштування порогів (Thresholds) ---
    // Було 0.6 (60%). Це занадто суворо.
    // Тепер 0.35 (35%). Тобто, якщо модель впевнена хоча б на 35%, ми не вважаємо це "невизначеністю".
    // Це трохи вище за поріг штрафу (0.2), щоб створити "буферну зону".
	private const double TokenLogProbThreshold = 0.35; // Поріг впевненості для окремого токена (exp)
	
	/// <summary>
	/// Проводить аналіз впевненості результату міркування.
	/// </summary>
	/// <param name="reasoningResult">Результат міркування моделі, що містить logprobs.</param>
	/// <returns>Результат валідації з фінальним балом та детальними метриками.</returns>
	public Task<ConfidenceValidationResult> ValidateAsync(IReasoningResult reasoningResult)
    {
        if (reasoningResult.LogProbs.Count == 0)
        {
            return Task.FromResult(new ConfidenceValidationResult
            {
                Score = 0.5,
                IsValid = false,
                ConfidenceLevel = ConfidenceLevel.Uncertain,
                Details = ["No token-level probabilities returned by the model."]
            });
        }

        // Ми відбираємо тільки "змістовні" токени для розрахунку математики.
        // Це покращить Perplexity та Entropy, бо ми не оцінюємо коми.
        var meaningfulLogProbs = reasoningResult.LogProbs
            .Where(t => IsMeaningfulToken(t.Token))
            .ToList();
        // Якщо після фільтрації нічого не лишилося (рідкісний кейс), повертаємо Fail
        if (meaningfulLogProbs.Count == 0)
        {
            return Task.FromResult(new ConfidenceValidationResult
            {
                Score = 0.0, IsValid = false, ConfidenceLevel = ConfidenceLevel.Uncertain,
                Details = ["All tokens were filtered out as noise."]
            });
        }
        var meaningfulTokenData = meaningfulLogProbs
            .Select(t => (t.Token, t.LogProb))
            .ToList();
        
        // 1. Розрахунок базових метрик
        var metrics = LogProbMetricsCalculator.Calculate(meaningfulTokenData);
        var uncertainty = LogProbMetricsCalculator.AnalyzeUncertainty(meaningfulTokenData);
        // 2. Розрахунок штрафів за ризики (Risk Penalties)
        var (riskPenalty, details) = ConfidencePenaltyCalculator.Calculate(
            meaningfulTokenData, 
            metrics, 
            uncertainty);

        // 3. Фактор довжини (Wu et al.)
        // Використовуємо пом'якшену логарифмічну формулу для штрафу
        double lengthFactor = Math.Exp(-0.15 * Math.Log(metrics.LengthPenalty));
        
        // 4. Фінальний бал
        double finalScore = metrics.IntrinsicConfidence * lengthFactor * riskPenalty;
        finalScore = Math.Clamp(finalScore, 0.0, 1.0);

        // Формуємо об'єкт метрик для відповіді
        var validationMetrics = new ValidationMetrics
        {
            TokenConfidence = new MetricItem
            {
                Value = metrics.IntrinsicConfidence,
                Description = $"{metrics.IntrinsicConfidence:P1}",
                Rating = LogProbMetricsCalculator.GetLevel(metrics.IntrinsicConfidence)
            },
            Perplexity = new MetricItem
            {
                Value = metrics.Perplexity,
                Description = metrics.Perplexity.ToString("F2"),
                Rating = LogProbMetricsCalculator.GetPerplexityLevel(metrics.Perplexity)
            },
            Entropy = new MetricItem
            {
                Value = metrics.Entropy,
                Description = metrics.Entropy.ToString("F2"),
                Rating = LogProbMetricsCalculator.GetEntropyLevel(metrics.Entropy)
            },
            WuScore = new MetricItem
            {
                Value = metrics.LengthPenalty, // Зберігаємо оригінальне значення Wu
                Description = $"Length normalization factor (Wu et al.) applied: {lengthFactor:F3}",
                // Тепер рейтинг розраховується динамічно на основі сили впливу
                Rating = LogProbMetricsCalculator.GetPenaltyLevel(lengthFactor)
            }
        };

        // Базові деталі
        details.Insert(0, $"Average Token Confidence: {metrics.IntrinsicConfidence:P1}");
        details.Insert(1, $"Perplexity: {metrics.Perplexity:F2}");
        details.Insert(2, $"Token Entropy: {metrics.Entropy:F2}");
        details.Insert(3, $"Weakest Link (Min Confidence): {metrics.WeakestTokenProbability:P1} (Token: '{metrics.WeakestToken}')");
        details.Insert(4, $"Risk Penalty: {riskPenalty}");
        details.Add("Length normalization applied following Wu et al. (2016).");

        return Task.FromResult(new ConfidenceValidationResult
        {
            Score = finalScore,
            // Валідно, якщо бал >= 0.55 І перплексія не "сміттєва"
            IsValid = finalScore >= 0.55 && validationMetrics.Perplexity.Rating != ConfidenceLevel.Uncertain,
            ConfidenceLevel = LogProbMetricsCalculator.GetFinalLevel(finalScore, metrics),
            Metrics = validationMetrics,
            Details = details,
            Uncertainty = uncertainty
        });
    }
    
    private static bool IsMeaningfulToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        // Перевіряємо, чи складається токен ТІЛЬКИ зі знаків пунктуації/символів
        // Наприклад: " - " або "." або "://" -> False
        // Але: "COVID-19" -> True (бо є букви/цифри)
        return !token.All(c => char.IsPunctuation(c) || char.IsSeparator(c) || char.IsSymbol(c) || char.IsWhiteSpace(c));
    }
}
