using System.Diagnostics;
using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Reasoning.Contracts;
using Logos.AI.Abstractions.Validation;
using Logos.AI.Abstractions.Validation.Contracts;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Logos.AI.Engine.RAG;

//Retrieval Augmented Generation — генерация ответа пользователю с учетом дополнительно найденной релевантной информации.
public class RagOrchestrator(
	IRetrievalAugmentationService         retrievalAugmentationService,
	IMedicalAnalyzingReasoningService     reasoningService,
	IConfidenceValidator                  confidenceValidator,
	IOptionsSnapshot<OpenAiOptions>       options,
	ILogger<RetrievalAugmentationService> logger)
{
	public async Task<PatientAnalyzeRagResponse> GenerateResponseAsync(PatientAnalyzeRagRequest request, CancellationToken ct = default)
	{
		var globalStopwatch = Stopwatch.StartNew();
		double augmentationTime = 0;
		// 1. Поиск (тяжелые данные)
		RetrievalAugmentationResult augmentationRes = await retrievalAugmentationService.AugmentAsync(request, ct);
		augmentationTime = augmentationRes.TotalProcessingTimeSeconds;
		// 3. Сборка DTO (вся логика маппинга скрыта внутри DTO)
		var augmentedData = new AugmentedPatientAnalyze
		{
			PatientAnalyzeData = request,
			PreliminaryDiagnosticHypothesis = PreliminaryHypothesisDto.FromResponse(augmentationRes.PreliminaryDiagnosticHypothesis),
			RetrievalResults = ContextRetrievalDto.CreateFromRetrievalResults(augmentationRes.RetrievalResults)
		};
		var reasoningStopWatch = Stopwatch.StartNew();
		var reasoningRes = await reasoningService.AnalyzeAsync(augmentedData, ct);
		reasoningStopWatch.Stop();
		var generationResult = new RagResult
		{
			TotalProcessingTimeSeconds = reasoningStopWatch.Elapsed.TotalSeconds,
			ReasoningTokensSpent = reasoningRes.TokenUsage,
			MedicalAnalyzingLLmResponse = reasoningRes.Data,

		};
		var model = augmentationRes.PreliminaryDiagnosticHypothesis.RequiresComplexAnalysis 
			? $"Reasoning: {options.Value.ReasoningMedicalAnalyzing.Model}"
			: $"Non-Reasoning: {options.Value.NonReasoningMedicalAnalyzing.Model}";
		ConfidenceValidationResult? validationRes = null;
		if (!augmentationRes.PreliminaryDiagnosticHypothesis.RequiresComplexAnalysis)
		{
			 validationRes = await confidenceValidator.ValidateAsync(reasoningRes);
		}
		globalStopwatch.Stop();
		return new PatientAnalyzeRagResponse(model,generationResult, augmentationRes)
		{
			NonReasoningConfidenceValidation = validationRes,
			TotalProcessingTimeSeconds = globalStopwatch.Elapsed.TotalSeconds,
			TotalAugmentationProcessingTimeSeconds = augmentationTime,
			TotalGenerationProcessingTimeSeconds = reasoningStopWatch.Elapsed.TotalSeconds
		};
	}
}
