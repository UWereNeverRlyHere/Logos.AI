namespace Logos.AI.Abstractions.Validation;

public readonly record struct LogProbMetrics(
	double IntrinsicConfidence,
	double Perplexity,
	double Entropy,
	string WeakestToken,
	double WeakestTokenProbability,
	double LengthPenalty)
{
	public static LogProbMetrics Empty => new(0, double.PositiveInfinity, double.PositiveInfinity, string.Empty, 0, 1);
}
