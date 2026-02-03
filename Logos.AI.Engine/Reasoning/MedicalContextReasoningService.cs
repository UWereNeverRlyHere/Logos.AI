using Logos.AI.Abstractions.PatientAnalysis;
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
	IOptions<OpenAiOptions>                 options,
	ILogger<MedicalContextReasoningService> logger) : IMedicalContextReasoningService
{
	private readonly LlmOptions _options = options.Value.MedicalContext;

	public async Task<ReasoningResult<MedicalContextLlmResponse>> AnalyzeAsync(PatientAnalyzeLlmRequest request, CancellationToken ct = default)
	{
		try
		{
			logger.LogInformation("Sending request to LLM for Medical Context Analysis...");

			var reqData = new LlmRequestDto
			{
				LlmOptions = _options,
				UserMessageContent = request, // Врапер сам серіалізує це в JSON
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
			throw; // Викидаємо помилку далі, щоб контролер або тесты її побачили
		}
	}
}
