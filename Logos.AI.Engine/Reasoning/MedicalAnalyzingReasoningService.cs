using System.Text.Json;
using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.Reasoning;
using Logos.AI.Engine.Configuration;
using Logos.AI.Engine.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Logos.AI.Engine.Reasoning;

public class MedicalAnalyzingReasoningService : IMedicalAnalyzingReasoningService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<MedicalAnalyzingReasoningService> _logger;
    private readonly string _systemPrompt;
    private readonly LlmOptions _options;

    public MedicalAnalyzingReasoningService(
        IOptions<OpenAiOptions> options,
        ChatClient chatClient,
        IHostEnvironment env,
        ILogger<MedicalAnalyzingReasoningService> logger) 
    {
        _chatClient = chatClient;
        _logger = logger;
        _options = options.Value.MedicalAnalyzing;

        var promptPath = Path.Combine(env.ContentRootPath, "PromptKnowledgeBase", _options.PromptFile);
        
        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException($"Critical error: Prompt file not found at {promptPath}");
        }
        _systemPrompt = File.ReadAllText(promptPath);
    }

    public async Task<ReasoningResult<MedicalAnalyzingLLmResponse>> AnalyzeAsync(PatientAnalyzeLlmRequest request, CancellationToken ct = default)
    {
        // 1. Серіалізуємо весь запит (Пацієнт + RAG Augmentations)
        var userMessageContent = request.SerializeToJson();
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(_systemPrompt),
            new UserChatMessage(userMessageContent) 
        };

        var options = new ChatCompletionOptions
        {
            Temperature = _options.Temperature,
            TopP = _options.TopP,
            MaxOutputTokenCount = _options.MaxTokens,
            IncludeLogProbabilities = true,
            TopLogProbabilityCount = _options.TopLogProbabilityCount, 
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "medical_analysis",
                jsonSchema: LogosJsonExtensions.GetSchemaFromType<MedicalAnalyzingLLmResponse>(false),
                jsonSchemaFormatDescription: "Detailed clinical analysis and recommendations",
                jsonSchemaIsStrict: true 
            ),

            // Параметри "на майбутнє":
            // FrequencyPenalty = 0, //Штрафує за часте повторення слів.
            // PresencePenalty = 0,//Штрафує за те, що слово взагалі вже було в тексті.
            // ReasoningEffortLevel = ChatReasoningEffortLevel.High //Experimental option
            // WebSearchOptions //Experimental
            // Stream Відповідь приходить по словах.
            // StopSequences Стоп-слова.
            // Seed = "Зерно" випадковості. [Experimental("OPENAI001")]
            // AllowParallelToolCalls = true
            // Tools = { } всякі задачі типу "запиши до лікаря" 
        };

        try
        {
            _logger.LogInformation("Sending request to LLM for Clinical Reasoning (Structured Output)...");
            
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, ct);
            
            var content = completion.Content[0].Text;
            var usage = completion.Usage;
            
            var data = JsonSerializer.Deserialize<MedicalAnalyzingLLmResponse>(content) 
                ?? throw new InvalidOperationException("Failed to deserialize LLM response.");

            // Мапимо LogProbs
            var logProbs = new List<LogProbToken>();
            if (completion.ContentTokenLogProbabilities != null)
            {
                logProbs = completion.ContentTokenLogProbabilities
                    .Select(t => new LogProbToken
                    {
                        Token = t.Token,
                        LogProb = t.LogProbability,
                        LinearProbability = Math.Exp(t.LogProbability)
                    })
                    .ToList();
            }
            _logger.LogInformation("Clinical Reasoning (Structured Output) completed successfully");
            return new ReasoningResult<MedicalAnalyzingLLmResponse>
            {
                Data = data,
                TokenUsage = new TokenUsageInfo(usage.InputTokenCount, usage.OutputTokenCount),
                LogProbs = logProbs,
                RawContent = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Clinical Reasoning generation");
            throw; 
        }
    }
}