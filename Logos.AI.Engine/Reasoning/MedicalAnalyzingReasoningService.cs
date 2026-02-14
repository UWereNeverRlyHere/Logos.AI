using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.Reasoning;
using Logos.AI.Abstractions.Reasoning.Contracts;
using Logos.AI.Engine.Configuration;
using Logos.AI.Engine.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
namespace Logos.AI.Engine.Reasoning;

public class MedicalAnalyzingReasoningService(
	LlmClientWrapper                          llmClientWrapper,
	IOptionsSnapshot<OpenAiOptions>           options,
	ILogger<MedicalAnalyzingReasoningService> logger) : IMedicalAnalyzingReasoningService
{
	private readonly LlmOptions _reasoningPptions = options.Value.ReasoningMedicalAnalyzing;
	private readonly LlmOptions _nonReasoningOptions = options.Value.NonReasoningMedicalAnalyzing;

	public async Task<ReasoningResult<MedicalAnalyzingLLmResponse>> AnalyzeAsync(AugmentedPatientAnalyze request, CancellationToken ct = default)
	{
		try
		{
			logger.LogInformation("Sending request to LLM for Clinical Reasoning (Structured Output)...");
			var opt = request.PreliminaryDiagnosticHypothesis.RequiresComplexAnalysis ? _reasoningPptions : _nonReasoningOptions;
			var reqData = new LlmRequestDto
			{
				LlmOptions = opt,
				Content = request,
				ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
					jsonSchemaFormatName: "medical_analysis",
					jsonSchema: LogosJsonExtensions.GetSchemaFromType<MedicalAnalyzingLLmResponse>(false),
					jsonSchemaFormatDescription: "Detailed clinical analysis and recommendations",
					jsonSchemaIsStrict: true
				)
			};
			return await llmClientWrapper.GenerateAsync<MedicalAnalyzingLLmResponse>(reqData, ct);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error during Clinical Reasoning generation");
			throw;
		}
	}
}
