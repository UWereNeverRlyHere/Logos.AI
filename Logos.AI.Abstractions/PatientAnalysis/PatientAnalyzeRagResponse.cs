using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Validation;
namespace Logos.AI.Abstractions.PatientAnalysis;

public record PatientAnalyzeRagResponse(string AnalyzeModel, RagResult RagResult, RetrievalAugmentationResult AugmentationResult)
{
	public required double TotalProcessingTimeSeconds { get; init; }
	public required double TotalAugmentationProcessingTimeSeconds { get; init; }
	public required double TotalGenerationProcessingTimeSeconds { get; init; }
	public TokenUsageInfo TotalTokenUsage { get; private init; } = new(RagResult.ReasoningTokensSpent.TotalTokenCount + AugmentationResult.AugmentationTokensSpent.TotalTokenCount, RagResult.ReasoningTokensSpent.InputTokenCount + AugmentationResult.AugmentationTokensSpent.InputTokenCount);
	public RagResult RagResult { get; init; } = RagResult;
	public RetrievalAugmentationResult AugmentationResult { get; init; } = AugmentationResult;
	public ConfidenceValidationResult? NonReasoningConfidenceValidation { get; init; }
}