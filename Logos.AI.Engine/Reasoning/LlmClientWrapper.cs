using System.Text.Json;
using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.Reasoning;
using Logos.AI.Engine.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
namespace Logos.AI.Engine.Reasoning;

public class LlmClientWrapper(IChatClientFactory chatClientFactory, IHostEnvironment environment, ILogger<LlmClientWrapper> logger)
{
	public async Task<ReasoningResult<TResponse>> GenerateAsync<TResponse>(LlmRequestDto requestDto, CancellationToken ct = default)
	{
		try
		{
			logger.LogDebug("Sending LLM request. Temp: {LlmOptionsTemperature}, TopP: {LlmOptionsTopP}, MaxTokens: {LlmOptionsMaxTokens}", requestDto.LlmOptions.Temperature, requestDto.LlmOptions.TopP, requestDto.LlmOptions.MaxTokens);

			var chatMessages = GetChatMessages(requestDto);
			var options = GetChatCompletionOptions(requestDto);
			var chatClient = chatClientFactory.GetClient(requestDto.LlmOptions.Model);
			ChatCompletion completion = await chatClient.CompleteChatAsync(chatMessages, options, ct);
			var content = completion.Content[0].Text;
			var usage = completion.Usage;

			TResponse? data;
			try
			{
				data = JsonSerializer.Deserialize<TResponse>(content);
			}
			catch (JsonException ex)
			{
				logger.LogError(ex, "Failed to deserialize LLM response. Raw content: {Content}", content);
				throw new InvalidOperationException("LLM returned invalid JSON structure.", ex);
			}

			if (Equals(data, default(TResponse))) throw new InvalidOperationException("Deserialized data is null.");

			var logProbs = new List<LogProbToken>();
			//В ContentTokenLogProbabilities  находится логарифмическая вероятность того токена,
			//который модель уже выбрала и вставила в текст.
			//Это прямой показатель: "Насколько модель уверена в том, что она написала".
			
			//TopLogProbabilities возвращает список альтернатив, которые модель рассматривала, но не выбрала (или выбрала, если это топ-1). Это полезно только для очень глубокой аналитики, например:
			//Расчет энтропии (неопределенности): Если у выбранного токена вероятность 40%, а у второго места 39% — модель сильно колебалась.
			//Self-Correction: Чтобы увидеть, не было ли среди альтернатив более "фактического" ответа.
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

			return new ReasoningResult<TResponse>
			{
				Data = data,
				TokenUsage = new TokenUsageInfo(usage.InputTokenCount, usage.OutputTokenCount),
				LogProbs = logProbs,
				RawContent = content
			};
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error inside LlmClientWrapper");
			throw;
		}
	}


	private ICollection<ChatMessage> GetChatMessages(LlmRequestDto requestDto)
	{
		var promptPath = Path.Combine(environment.ContentRootPath, "PromptKnowledgeBase", requestDto.LlmOptions.PromptFile);
		if (!File.Exists(promptPath))
		{
			throw new FileNotFoundException($"Critical error: Prompt file not found at {promptPath}");
		}
		var messages = new List<ChatMessage>
		{
			new SystemChatMessage(File.ReadAllText(promptPath)),
			new UserChatMessage(requestDto.UserMessageJsonContent)
		};
		return messages;
	}

	private ChatCompletionOptions GetChatCompletionOptions(LlmRequestDto requestDto)
	{
		var options = new ChatCompletionOptions
		{
			Temperature = requestDto.LlmOptions.Temperature,
			TopP = requestDto.LlmOptions.TopP,
			MaxOutputTokenCount = requestDto.LlmOptions.MaxTokens,
			IncludeLogProbabilities = true,
			TopLogProbabilityCount = requestDto.LlmOptions.TopLogProbabilityCount, 
			ResponseFormat = requestDto.ResponseFormat,
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
		return options;
	}
}
