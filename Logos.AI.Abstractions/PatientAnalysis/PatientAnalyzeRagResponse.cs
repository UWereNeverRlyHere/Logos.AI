using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.RAG;
namespace Logos.AI.Abstractions.PatientAnalysis;

public record PatientAnalyzeRagResponse(RagResult RagResult, RetrievalAugmentationResult AugmentationResult)
{
	public TokenUsageInfo TotalTokenUsage { get; private init; } = new(RagResult.ReasoningTokensSpent.TotalTokenCount + AugmentationResult.AugmentationTokensSpent.TotalTokenCount, RagResult.ReasoningTokensSpent.InputTokenCount + AugmentationResult.AugmentationTokensSpent.InputTokenCount);
	public RagResult RagResult { get; init; } = RagResult;
	public RetrievalAugmentationResult AugmentationResult { get; init; } = AugmentationResult;
}