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

		if (tokens.Count == 0)
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

		var tokenData = tokens.Select(t => (t.Token, t.LogProb)).ToList();
		var calculatedMetrics = LogProbMetricsCalculator.Calculate(tokenData);
		var uncertainty = DefineUncertainty(tokens);
		// ------------------
		// Risk penalties
		// ------------------
		double riskPenalty = 1.0;

		if (calculatedMetrics.Perplexity > 5.0) riskPenalty *= 0.6;
		if (calculatedMetrics.Entropy > 1.2) riskPenalty *= 0.75;
		if (calculatedMetrics.WeakestTokenProbability < 0.15) riskPenalty *= 0.8;
		if (uncertainty.Type == UncertaintyType.Diffuse) riskPenalty *= 0.5; 
		
		// ------------------
		// Length factor (dampened Wu)
		// ------------------
		double lengthFactor = Math.Exp(-0.15 * Math.Log(calculatedMetrics.LengthPenalty));
		// ------------------
		// Final score
		// ------------------
		double finalScore = calculatedMetrics.IntrinsicConfidence * lengthFactor * riskPenalty;
		finalScore = Math.Clamp(finalScore, 0.0, 1.0);

		var metrics = new ValidationMetrics
		{
			TokenConfidence = new MetricItem
			{
				Value = calculatedMetrics.IntrinsicConfidence,
				Description = $"{calculatedMetrics.IntrinsicConfidence:P1}",
				Rating = LogProbMetricsCalculator.GetLevel(calculatedMetrics.IntrinsicConfidence)
			},
			Perplexity = new MetricItem
			{
				Value = calculatedMetrics.Perplexity,
				Description = calculatedMetrics.Perplexity.ToString("F2"),
				Rating = LogProbMetricsCalculator.GetPerplexityLevel(calculatedMetrics.Perplexity)
			},
			Entropy = new MetricItem
			{
				Value = calculatedMetrics.Entropy,
				Description = calculatedMetrics.Entropy.ToString("F2"),
				Rating = LogProbMetricsCalculator.GetEntropyLevel(calculatedMetrics.Entropy)
			},
			WuScore = new MetricItem
			{
				Value = calculatedMetrics.LengthPenalty,
				Description = "Length normalization factor (Wu et al.)",
				Rating = ConfidenceLevel.Medium
			}
		};

		return Task.FromResult(new ConfidenceValidationResult
		{
			Score = finalScore,
			IsValid = finalScore >= 0.55 && metrics.Perplexity.Rating != ConfidenceLevel.Uncertain,
			ConfidenceLevel = LogProbMetricsCalculator.GetFinalLevel(finalScore, calculatedMetrics),
			Metrics = metrics,
			Details =
			[
				$"Average Token Confidence: {calculatedMetrics.IntrinsicConfidence:P1}",
				$"Perplexity: {calculatedMetrics.Perplexity:F2}",
				$"Token Entropy: {calculatedMetrics.Entropy:F2}",
				$"Weakest Link (Min Confidence): {calculatedMetrics.WeakestTokenProbability:P1} (Token: '{calculatedMetrics.WeakestToken}')",
				"Length normalization applied following Wu et al. (2016)."
			],
			Uncertainty = uncertainty
		});
	}

	private static UncertaintyResult DefineUncertainty(IReadOnlyList<LogProbToken> logProb)
	{
		// --- АНАЛИЗ ТИПА НЕУВЕРЕННОСТИ ---
		// 1. Определяем, какие токены считать "проблемными"
		// Порог 0.6 (60%) - это достаточно строгий критерий.
		// Если уверенность < 60%, токен считается "подозрительным".
		var lowConfidenceTokens = logProb
			.Where(t => Math.Exp(t.LogProb) < 0.6)
			.ToList();
		UncertaintyType uncertaintyType = lowConfidenceTokens.Count switch
		{
			0 => UncertaintyType.None,
			// Фокальная: Мало плохих токенов (1-2 штуки), и они составляют малую часть текста (< 5%)
			<= 2 when (double)lowConfidenceTokens.Count / logProb.Count < 0.05 => UncertaintyType.Focal,
			_ => UncertaintyType.Diffuse
		};

		// Топ-10 слабых для details (как мы делали раньше)
		var weakestTokens = logProb
			.OrderBy(t => t.LogProb)
			.Take(10)
			.Select(t => $"'{t.Token}' ({Math.Exp(t.LogProb):P0})")
			.ToList();
		return new UncertaintyResult(uncertaintyType, weakestTokens);
		
	}
}
