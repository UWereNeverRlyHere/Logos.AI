using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Reasoning.Contracts;
using Logos.AI.Abstractions.Validation.Contracts;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.RAG;

//Retrieval Augmented Generation — генерация ответа пользователю с учетом дополнительно найденной релевантной информации.
public class RagOrchestrator(
	IRetrievalAugmentationService         retrievalAugmentationService,
	IMedicalAnalyzingReasoningService     reasoningService,
	IConfidenceValidator                  confidenceValidator,
	ILogger<RetrievalAugmentationService> logger)
{
	public async Task<PatientAnalyzeRagResponse> GenerateResponseAsync(PatientAnalyzeRagRequest request, CancellationToken ct = default)
	{
		RetrievalAugmentationResult augmentationRes = await retrievalAugmentationService.AugmentAsync(request, ct);
		var reasoningRes = await reasoningService.AnalyzeAsync(request, ct);
		//var confidenceRes = await confidenceValidator.ValidateAsync(reasoningRes);
		var generationResult = new RagResult
		{
			TotalProcessingTimeSeconds = 0,
			ReasoningTokensSpent = reasoningRes.TokenUsage,
			MedicalAnalyzingLLmResponse = reasoningRes.Data
		};
		return new PatientAnalyzeRagResponse(generationResult, augmentationRes);
	}
}
