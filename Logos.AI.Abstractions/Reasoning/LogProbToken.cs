namespace Logos.AI.Abstractions.Reasoning;

public record LogProbToken
{
	public string Token { get; init; } = string.Empty;
	public double LogProb { get; init; }
	public double? LinearProbability { get; init; } = null;
}
