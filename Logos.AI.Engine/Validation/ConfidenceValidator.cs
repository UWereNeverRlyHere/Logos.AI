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
	/// <summary>
	/// Проводить аналіз впевненості результату міркування.
	/// </summary>
	/// <param name="reasoningResult">Результат міркування моделі, що містить logprobs.</param>
	/// <returns>Результат валідації з фінальним балом та детальними метриками.</returns>
	public Task<ConfidenceValidationResult> ValidateAsync(IReasoningResult reasoningResult)
	{
		var tokens = reasoningResult.LogProbs;

		if (tokens is null || tokens.Count == 0)
		{
			return Task.FromResult(new ConfidenceValidationResult
			{
				Score = 0.5,
				IsValid = false,
				ConfidenceLevel = ConfidenceLevel.Uncertain,
				Details =
				[
					"No token-level probabilities returned by the model."
				]
			});
		}

		var tokenData =
			tokens.Select(t => (t.Token, t.LogProb)).ToList();

		var m = LogProbMetricsCalculator.Calculate(tokenData);

		// ------------------
		// Risk penalties
		// ------------------
		double riskPenalty = 1.0;

		if (m.Perplexity > 5.0)
			riskPenalty *= 0.6;

		if (m.Entropy > 1.2)
			riskPenalty *= 0.75;

		if (m.WeakestTokenProbability < 0.15)
			riskPenalty *= 0.8;

		// ------------------
		// Length factor (dampened Wu)
		// ------------------
		double lengthFactor =
			Math.Exp(-0.15 * Math.Log(m.LengthPenalty));

		// ------------------
		// Final score
		// ------------------
		double finalScore =
			m.IntrinsicConfidence *
			lengthFactor *
			riskPenalty;

		finalScore = Math.Clamp(finalScore, 0.0, 1.0);

		var metrics = new ValidationMetrics
		{
			TokenConfidence = new MetricItem
			{
				Value = m.IntrinsicConfidence,
				Description = $"{m.IntrinsicConfidence:P1}",
				Rating =
					LogProbMetricsCalculator.GetLevel(
						m.IntrinsicConfidence)
			},
			Perplexity = new MetricItem
			{
				Value = m.Perplexity,
				Description = m.Perplexity.ToString("F2"),
				Rating =
					LogProbMetricsCalculator.GetPerplexityLevel(
						m.Perplexity)
			},
			Entropy = new MetricItem
			{
				Value = m.Entropy,
				Description = m.Entropy.ToString("F2"),
				Rating = LogProbMetricsCalculator.GetEntropyLevel(m.Entropy)
			},
			WuScore = new MetricItem
			{
				Value = m.LengthPenalty,
				Description = "Length normalization factor (Wu et al.)",
				Rating = ConfidenceLevel.Medium
			}
		};
		// --- НОВАЯ ЛОГИКА: АНАЛИЗ ТИПА НЕУВЕРЕННОСТИ ---

		// 1. Определяем, какие токены считать "проблемными"
		// Порог 0.6 (60%) - это достаточно строгий критерий.
		// Если уверенность < 60%, токен считается "подозрительным".
		var lowConfidenceTokens = reasoningResult.LogProbs
			.Where(t => Math.Exp(t.LogProb) < 0.6)
			.ToList();

		string uncertaintyType;
		string uncertaintyReason;

		if (lowConfidenceTokens.Count == 0)
		{
			uncertaintyType = "None";
			uncertaintyReason = "High confidence across all tokens.";
		}
		// Фокальная: Мало плохих токенов (1-2 штуки), и они составляют малую часть текста (< 5%)
		else if (lowConfidenceTokens.Count <= 2 && (double)lowConfidenceTokens.Count / reasoningResult.LogProbs.Count < 0.05)
		{
			//Если в списке только одно слово с низкой вероятностью (например, название редкой болезни), а остальные > 90% — значит, модель не знает конкретного термина.
			uncertaintyType = "Focal"; // Точечная
			uncertaintyReason = "Model is confident globally but stumbled on specific terms (rare names or typos).";
		}
		// Диффузная: Плохих токенов много
		else
		{
			//Если в списке куча обычных слов ('и', 'на', 'что', 'быть') с низкой вероятностью — значит, модель потеряла нить разговора целиком.
			uncertaintyType = "Diffuse"; // Размытая
			uncertaintyReason = "Multiple low-confidence tokens detected. Model likely lost context or is hallucinating broadly.";
		}

		// --- ФОРМИРОВАНИЕ ОТВЕТА ---

		// Топ-10 слабых для details (как мы делали раньше)
		var weakestTokens = reasoningResult.LogProbs
			.OrderBy(t => t.LogProb)
			.Take(10)
			.Select(t => $"'{t.Token}' ({Math.Exp(t.LogProb):P0})")
			.ToList();
		return Task.FromResult(new ConfidenceValidationResult
		{
			Score = finalScore,
			IsValid = finalScore >= 0.55 && metrics.Perplexity.Rating != ConfidenceLevel.Uncertain,
			ConfidenceLevel = LogProbMetricsCalculator.GetFinalLevel(finalScore, m),
			Metrics = metrics,
			Details =
			[
				$"Average Token Confidence: {m.IntrinsicConfidence:P1}",
				$"Perplexity: {m.Perplexity:F2}",
				$"Token Entropy: {m.Entropy:F2}",
				$"Weakest Link (Min Confidence): {m.WeakestTokenProbability:P1} (Token: '{m.WeakestToken}')",
				"Length normalization applied following Wu et al. (2016)."
			],
			UncertaintyType = uncertaintyType,
			UncertaintyReason = uncertaintyReason,
			TopWeakestLinks = weakestTokens
		});
	}
}
