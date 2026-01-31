using System.Text.Json;
using Logos.AI.Abstractions.Features.PatientAnalysis;
using Logos.AI.Abstractions.Features.Reasoning;
using Logos.AI.Engine.Configuration;
using Logos.AI.Engine.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
namespace Logos.AI.Engine.Reasoning;

public class MedicalContextReasoningService
{
	private readonly ChatClient _chatClient;
	private readonly ILogger<MedicalContextReasoningService> _logger;
	private readonly string _systemPrompt;
	private readonly LlmOptions _options;
	public MedicalContextReasoningService(
		IOptions<OpenAiOptions>                 options,
		ChatClient                              chatClient,
		IHostEnvironment                        env,
		ILogger<MedicalContextReasoningService> logger)
	{
		_chatClient = chatClient;
		_logger = logger;
		_options = options.Value.MedicalContextReasoning;
		var promptPath = Path.Combine(env.ContentRootPath, "PromptKnowledgeBase", _options.PromptFile);
		if (!File.Exists(promptPath))
		{
			throw new FileNotFoundException($"Critical error: Prompt file not found at {promptPath}");
		}

		_systemPrompt = File.ReadAllText(promptPath);
	}
	public async Task<MedicalContextReasoningResult> ProcessAsync(AnalyzePatientRequest patientJson)
	{
		return await ProcessAsync(JsonSerializer.Serialize(patientJson));
	}

	public async Task<MedicalContextReasoningResult> ProcessAsync(string patientJson)
	{
		try
		{
			var options = new ChatCompletionOptions
			{
				Temperature = _options.Temperature,
				TopP = _options.TopP,
				MaxOutputTokenCount = _options.MaxTokens,
				// ReasoningEffortLevel = ChatReasoningEffortLevel.High //Experimental option
				FrequencyPenalty = 0, //Штрафує за часте повторення слів.
				PresencePenalty = 0, //Штрафує за те, що слово взагалі вже було в тексті. Змушує постійно змінювати тему
				//WebSearchOptions //Experimental
				ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
					jsonSchemaFormatName: "medical_context",
					jsonSchema: LogosJsonExtensions.GetSchemaFromType<MedicalContextReasoningResult>(false),
					jsonSchemaFormatDescription: "Результат аналізу медичного контексту",
					jsonSchemaIsStrict: true // Це змусить модель суворо дотримуватися схеми
				),
				//Stream Відповідь приходить по словах (як друкарська машинка), а не вся одразу через 10 секунд.
				//StopSequences Стоп-слова, побачивши які модель замовкає.
				//Seed = 45,//"Зерно" випадковості. Якщо передати одне й те саме число (наприклад 12345), модель буде відповідати майже однаково щоразу. [Experimental("OPENAI001")]
				//AllowParallelToolCalls = true //Function Calling (виклик функцій). Модель може сказати: "Виклич функцію GetAnalysisDate()".
				//  Tools = {  } Це наступний рівень після RAG. Якщо ти захочеш, щоб бот не просто писав текст, а, наприклад, сам записував пацієнта до лікаря або рахував ШКФ за формулою, ти описуєш ці функції в Tools. Модель не виконує код, вона просто каже: "Я хочу викликати калькулятор з параметрами А і Б", а твій C# код це виконує.
				IncludeLogProbabilities = true,
				TopLogProbabilityCount = 5,
				/*StopSequences =
				{
					"}",
					" ]"
				} */
				//// Стоп-послідовність, якщо модель почне писати пояснення після JSON
			};
			var messages = new List<ChatMessage>
			{
				// 1. Спочатку йде "Закон" (System Message)
				new SystemChatMessage(_systemPrompt),
				// 2. Потім йдуть "Дані" (User Message)
				new UserChatMessage($"ВХІДНІ ДАНІ:\n{patientJson}")
			};

			ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);
			var responseText = completion.Content[0].Text; //Зазвичай текстова відповідь знаходиться в першому елементі контенту 

			var result = responseText.DeserializeFromJson<MedicalContextReasoningResult>();
			_logger.LogInformation("Medical Context Result: {Json}", result);
			return result ?? new MedicalContextReasoningResult
			{
				IsMedical = false,
				Reason = "Виникла невідома помилка під час генерації відповіді.",
				Queries = new List<string>()
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error extracting context");
			return new MedicalContextReasoningResult()
			{
				IsMedical = false,
				Reason = $"Виникла помилка під час генерації відповіді: {ex.Message}.",
				Queries = new List<string>()
			};
		}
	}
}
