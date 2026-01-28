using System.Text;
using Logos.AI.Abstractions.Features.Knowledge;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Logos.AI.Engine.Reasoning;

public class ClinicalReasoningService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<ClinicalReasoningService> _logger;
    private readonly string _systemPrompt;

    public ClinicalReasoningService(
        ChatClient chatClient,
        IHostEnvironment env,
        ILogger<ClinicalReasoningService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;

        // Завантажуємо чистий System Prompt
        var promptPath = Path.Combine(env.ContentRootPath, "PromptKnowledgeBase", "ClinicalReasoningPrompt.txt");
        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException($"Critical error: Prompt file not found at {promptPath}");
        }

        _systemPrompt = File.ReadAllText(promptPath);
    }

    public async Task<string> AnalyzeAsync(string patientJson, List<KnowledgeChunk> protocols)
    {
        // 1. Формуємо читабельний контекст із знайдених шматків (RAG Context)
        var contextBuilder = new StringBuilder();
        
        if (protocols.Count > 0)
        {
            foreach (var doc in protocols)
            {
                contextBuilder.AppendLine($"--- ДЖЕРЕЛО: {doc.FileName} (стор. {doc.PageNumber}) ---");
                contextBuilder.AppendLine(doc.Content);
                contextBuilder.AppendLine(); 
            }
        }
        else
        {
            contextBuilder.AppendLine("Релевантних медичних протоколів у базі знань не знайдено.");
        }

        // 2. Формуємо повідомлення користувача (User Message)
        // Саме тут ми підставляємо змінні, як ти й хотів.
        var userMessageContent = $"""
            ДАНІ ПАЦІЄНТА (JSON):
            {patientJson}

            БАЗА ЗНАНЬ (Знайдені фрагменти):
            {contextBuilder}
            """;

        // 3. Збираємо діалог
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(_systemPrompt), // Інструкція (Закон)
            new UserChatMessage(userMessageContent) // Дані (Контекст)
        };

        // 4. Налаштування (температура 0.2 для точності)
        var options = new ChatCompletionOptions
        {
            Temperature = 0.2f,
            TopP = 0.9f,
            MaxOutputTokenCount = 2000 // Висновок може бути довгим
        };

        try
        {
            _logger.LogInformation("Sending request to LLM for Clinical Reasoning...");
            
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);
            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Clinical Reasoning generation");
            return "Вибачте, сталася помилка при генерації клінічного висновку.";
        }
    }
}