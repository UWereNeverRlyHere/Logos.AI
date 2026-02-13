using Logos.AI.Abstractions.Exceptions;
using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Reasoning;
using Logos.AI.Abstractions.Reasoning.Contracts; // Перевірь, чи є цей неймспейс у тебе
using Logos.AI.Engine.Configuration;
using Logos.AI.Engine.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Logos.AI.Engine.Reasoning;

public class MedicalContextReasoningService(
	LlmClientWrapper                        llmClientWrapper,
	IOptionsSnapshot<OpenAiOptions>         options,
	ILogger<MedicalContextReasoningService> logger) : IMedicalContextReasoningService
{
	private readonly LlmOptions _contextOptions = options.Value.MedicalContext;
	private readonly LlmOptions _relevanceOptions = options.Value.MedicalRelevance;

	public async Task<ReasoningResult<MedicalContextLlmResponse>> AnalyzeAsync(string jsonRequest, CancellationToken ct = default)
	{
		try
		{
			logger.LogInformation("Sending request to LLM for Medical Context Analysis...");
			var reqData = new LlmRequestDto
			{
				LlmOptions = _contextOptions,
				Content = jsonRequest,
				ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
					jsonSchemaFormatName: "medical_context_analysis",
					jsonSchema: LogosJsonExtensions.GetSchemaFromType<MedicalContextLlmResponse>(false),
					jsonSchemaFormatDescription: "Analyzes text for medical context and generates search queries",
					jsonSchemaIsStrict: true
				)
			};
			return await llmClientWrapper.GenerateAsync<MedicalContextLlmResponse>(reqData, ct);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error during Medical Context Analysis");
			throw new ReasoningException("Failed to analyze medical context due to an AI service error.", ex);		}
	}
	public async Task<ReasoningResult<MedicalContextLlmResponse>> AnalyzeAsync(PatientAnalyzeRagRequest request, CancellationToken ct = default)
	{
		return await AnalyzeAsync(request.SerializeToJson(), ct);
	}

	public async Task<ReasoningResult<RelevanceEvaluationResult>> EvaluateRelevanceAsync(RetrievalResult retrievalResult, CancellationToken ct = default)
	{
		try
		{
			// Формуємо легкий payload, щоб не ганяти весь JSON пацієнта
			var evaluationPayload = new
			{
				UserQuery = retrievalResult.Query,
				FoundChunks = retrievalResult.FoundChunks
			};

			var reqData = new LlmRequestDto
			{
				LlmOptions = _relevanceOptions,
				Content = evaluationPayload,
				ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
					"relevance_evaluation",
					LogosJsonExtensions.GetSchemaFromType<RelevanceEvaluationResult>(false),
					jsonSchemaIsStrict: true
				)
			};

			return await llmClientWrapper.GenerateAsync<RelevanceEvaluationResult>(reqData, ct);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error during Relevance Evaluation");
			// У випадку помилки повертаємо "безпечний" дефолт, щоб не ламати пошук
			return new ReasoningResult<RelevanceEvaluationResult>
			{
				Data = new RelevanceEvaluationResult
				{
					Score = 0.5,
					RelevanceLevel = "Unchecked",
					Reasoning = "Error during AI validation",
					RelevantChunkIds = new ()
				},
				TokenUsage = new Abstractions.Common.TokenUsageInfo(),
				LogProbs = []
			};
		}
	}
}
