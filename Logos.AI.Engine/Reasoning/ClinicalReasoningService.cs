using System.Text;
using Logos.AI.Abstractions.Features.Knowledge;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Logos.AI.Engine.Reasoning;

public class ClinicalReasoningService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<ClinicalReasoningService> _logger;
    private readonly string _systemPrompt;
    private readonly LlmOptions _options;

    public ClinicalReasoningService(
        IOptions<OpenAiOptions> options,
        ChatClient chatClient,
        IHostEnvironment env,
        ILogger<ClinicalReasoningService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
        _options = options.Value.ClinicalReasoning;
        // Завантажуємо чистий System Prompt
        var promptPath = Path.Combine(env.ContentRootPath, "PromptKnowledgeBase", _options.PromptFile);
        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException($"Critical error: Prompt file not found at {promptPath}");
        }
        _systemPrompt = File.ReadAllText(promptPath);
    }
    /*public async Task<List<string>> AnalyzeMultipleAsync(string message, List<KnowledgeChunk> protocols, int n = 3)
    {
        // ... формування contextBuilder та messages як раніше ...

        var options = new ChatCompletionOptions
        {
            Temperature = 0.7f, // Підвищуємо температуру для різноманітності варіантів
            MaxOutputTokenCount = _options.MaxTokens
        };

        // Викликаємо модель n разів (або через параметр n, якщо бібліотека підтримує)
        var tasks = Enumerable.Range(0, n).Select(_ => _chatClient.CompleteChatAsync(message, options));
        var results = await Task.WhenAll(tasks);
    
        return results.Select(r => r.Content[0].Text).ToList();
    }*/
    public async Task<string> AnalyzeAsync(string patientJson, List<KnowledgeChunk> protocols, CancellationToken ct = default)
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
        // 4. Налаштування 
        var options = new ChatCompletionOptions
        {
            Temperature = _options.Temperature,
            TopP = _options.TopP,
            MaxOutputTokenCount = _options.MaxTokens,
           // ReasoningEffortLevel = ChatReasoningEffortLevel.High //Experimental option
           FrequencyPenalty = 0, //Штрафує за часте повторення слів.
           PresencePenalty = 0,//Штрафує за те, що слово взагалі вже було в тексті. Змушує постійно змінювати тему
           //WebSearchOptions //Experimental
           ResponseFormat = ChatResponseFormat.CreateTextFormat(),
           //Stream Відповідь приходить по словах (як друкарська машинка), а не вся одразу через 10 секунд.
           //StopSequences Стоп-слова, побачивши які модель замовкає.
           //Seed = "Зерно" випадковості. Якщо передати одне й те саме число (наприклад 12345), модель буде відповідати майже однаково щоразу. [Experimental("OPENAI001")]
           //AllowParallelToolCalls = true //Function Calling (виклик функцій). Модель може сказати: "Виклич функцію GetAnalysisDate()".
          //  Tools = {  } Це наступний рівень після RAG. Якщо ти захочеш, щоб бот не просто писав текст, а, наприклад, сам записував пацієнта до лікаря або рахував ШКФ за формулою, ти описуєш ці функції в Tools. Модель не виконує код, вона просто каже: "Я хочу викликати калькулятор з параметрами А і Б", а твій C# код це виконує.
          IncludeLogProbabilities = true,
          TopLogProbabilityCount = 10
        };
        try
        {
            _logger.LogInformation("Sending request to LLM for Clinical Reasoning...");
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options,ct);
            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Clinical Reasoning generation");
            return "Вибачте, сталася помилка при генерації клінічного висновку.";
        }
    }
}