using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Reasoning;
namespace Logos.AI.Abstractions.PatientAnalysis;

public record PatientAnalyzeRagResponse
{
	public MedicalAnalyzingLLmResponse GenerationResult { get; init; }
	public RetrievalAugmentationResult AugmentationResult { get; init; }
}
