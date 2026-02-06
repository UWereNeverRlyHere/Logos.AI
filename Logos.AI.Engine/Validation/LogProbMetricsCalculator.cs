using Logos.AI.Abstractions.Validation;
namespace Logos.AI.Engine.Validation;

public static class LogProbMetricsCalculator
{
	private const double Alpha = 0.65;

	public static LogProbMetrics Calculate(
		IReadOnlyList<(string Token, double LogProb)> tokens)
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
		double entropy = 0.0;
		foreach (var p in probs)
		{
			if (p > 0)
				entropy += -p * Math.Log(p);
		}
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
	public static ConfidenceLevel GetFinalLevel(double finalScore, LogProbMetrics m)
	{
		if (finalScore >= 0.85 && m.Perplexity < 1.3 && m.Entropy < 0.3 && m.WeakestTokenProbability > 0.4)
		{
			return ConfidenceLevel.Certain;
		}

		if (finalScore >= 0.7)
			return ConfidenceLevel.High;

		if (finalScore >= 0.55)
			return ConfidenceLevel.Medium;

		if (finalScore >= 0.4)
			return ConfidenceLevel.Low;

		return ConfidenceLevel.Uncertain;
	}
	public static ConfidenceLevel GetLevel(double score) => score switch
	{
		>= 0.85 => ConfidenceLevel.Certain,
		>= 0.7 => ConfidenceLevel.High,
		>= 0.55 => ConfidenceLevel.Medium,
		>= 0.4 => ConfidenceLevel.Low,
		_ => ConfidenceLevel.Uncertain
	};

	public static ConfidenceLevel GetPerplexityLevel(double ppl) => ppl switch
	{
		< 1.5 => ConfidenceLevel.High,
		< 3.0 => ConfidenceLevel.Medium,
		< 5.0 => ConfidenceLevel.Low,
		_ => ConfidenceLevel.Uncertain
	};

	public static ConfidenceLevel GetEntropyLevel(double entropy) => entropy switch
	{
		< 0.5 => ConfidenceLevel.High,
		< 1.0 => ConfidenceLevel.Medium,
		< 1.5 => ConfidenceLevel.Low,
		_ => ConfidenceLevel.Uncertain
	};
}