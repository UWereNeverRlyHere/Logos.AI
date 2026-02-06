using Logos.AI.Abstractions.Reasoning;
using Logos.AI.Abstractions.Validation;
using Logos.AI.Abstractions.Validation.Contracts;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.Validation;

public sealed class ConfidenceValidator(ILogger<ConfidenceValidator> logger) : IConfidenceValidator
{
    public Task<ConfidenceValidationResult> ValidateAsync(
        IReasoningResult reasoningResult)
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
            WuScore = new MetricItem
            {
                Value = m.LengthPenalty,
                Description = "Length normalization factor (Wu et al.)",
                Rating = ConfidenceLevel.Medium
            }
        };

        return Task.FromResult(new ConfidenceValidationResult
        {
            Score = finalScore,
            IsValid =
                finalScore >= 0.5 &&
                metrics.Perplexity.Rating != ConfidenceLevel.Uncertain,

            ConfidenceLevel = LogProbMetricsCalculator.GetFinalLevel(finalScore,m),

            Metrics = metrics,

            Details =
            [
                $"Average Token Confidence: {m.IntrinsicConfidence:P1}",
                $"Perplexity: {m.Perplexity:F2}",
                $"Token Entropy: {m.Entropy:F2}",
                $"Weakest Link (Min Confidence): {m.WeakestTokenProbability:P1} (Token: '{m.WeakestToken}')",
                "Length normalization applied following Wu et al. (2016)."
            ]
        });
    }
}