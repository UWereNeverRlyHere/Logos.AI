namespace Logos.AI.Abstractions.Common;

public record TokenUsageInfo
{
	public int InputTokenCount { get; set; }
	public int TotalTokenCount { get; set; }

	public TokenUsageInfo(int inputTokenCount, int totalTokenCount)
	{
		InputTokenCount = inputTokenCount;
		TotalTokenCount = totalTokenCount;
	}
	public TokenUsageInfo()
	{
	}

}