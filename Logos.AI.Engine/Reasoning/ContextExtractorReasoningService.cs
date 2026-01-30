using System.Text.Json;
using Logos.AI.Abstractions.Features.PatientAnalysis;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
namespace Logos.AI.Engine.Reasoning;

public class ContextExtractorService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<ContextExtractorService> _logger;
    private readonly string _systemPrompt; 
    private readonly LlmOptions _options;
    public ContextExtractorService(
        IOptions<OpenAiOptions> options,
        ChatClient chatClient, 
        IHostEnvironment env, 
        ILogger<ContextExtractorService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
        _options = options.Value.ContextExtractionReasoning;
        var promptPath = Path.Combine(env.ContentRootPath, "PromptKnowledgeBase", _options.PromptFile);
        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException($"Critical error: Prompt file not found at {promptPath}");
        }

        _systemPrompt = File.ReadAllText(promptPath);
    }
    public async Task<List<string>> ExtractAsync(AnalyzePatientRequest patientJson)
    {
        return await ExtractAsync(JsonSerializer.Serialize(patientJson));
    }

    public async Task<List<string>> ExtractAsync(string patientJson)
    {
        var options = new ChatCompletionOptions
        {
            Temperature = _options.Temperature,
            TopP = _options.TopP,
            MaxOutputTokenCount = _options.MaxTokens
        };

        var messages = new List<ChatMessage>
        {
            // 1. Спочатку йде "Закон" (System Message)
            new SystemChatMessage(_systemPrompt),
            
            // 2. Потім йдуть "Дані" (User Message)
            new UserChatMessage($"ВХІДНІ ДАНІ:\n{patientJson}")
        };

        try
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);
            var responseText = completion.Content[0].Text;
            return ParseResponse(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting context");
            return new List<string>();
        }
    }
    private List<string> ParseResponse(string responseText)
    {
        var cleanJson = responseText.Replace("```json", "").Replace("```", "").Trim();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(cleanJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}