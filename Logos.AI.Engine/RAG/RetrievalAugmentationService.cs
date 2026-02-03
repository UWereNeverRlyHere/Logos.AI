using System.Collections.Concurrent;
using System.Diagnostics;
using Logos.AI.Abstractions.Knowledge;
using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Validation.Contracts;
using Logos.AI.Engine.Extensions;
using Logos.AI.Engine.Knowledge.Qdrant;
using Logos.AI.Engine.Reasoning; // Переконайтеся, що неймспейс правильний для QdrantService
using Microsoft.Extensions.Logging;

namespace Logos.AI.Engine.RAG;

public class RetrievalAugmentationService(
	OpenAIEmbeddingService                embeddingService,
	QdrantService                         qdrantService,
	MedicalContextReasoningService        reasoningService,
	IConfidenceValidator                  confidenceValidator,
	ILogger<RetrievalAugmentationService> logger) : IRetrievalAugmentationService
{
	public async Task<RetrievalAugmentationResult> AugmentAsync(PatientAnalyzeLlmRequest request, CancellationToken ct = default)
	{
		// 1. Аналіз контексту (Medical Context Analysis)
		var medicalContext = await reasoningService.AnalyzeAsync(request, ct);

		if (!medicalContext.Data.IsMedical)
		{
			// Можна кидати виключення, або повертати пустий результат з флажком
			throw new InvalidOperationException("Medical context is not valid: Data is not medical.");
		}

		// 2. Валідація впевненості моделі (Confidence Check)
		var validationRes = await confidenceValidator.ValidateAsync(medicalContext);
		if (!validationRes.IsValid)
		{
			throw new InvalidOperationException($"Medical context confidence validation failed: {validationRes.SerializeToJson()}");
		}

		// 3. Виконання пошуку (Delegation to Core Retrieval)
		return await RetrieveContextAsync(medicalContext.Data.Queries, ct);
	}

	// Перевантаження для JSON-рядка
	public async Task<RetrievalAugmentationResult> AugmentAsync(string jsonRequest, CancellationToken ct = default)
	{
		// Тут краще десеріалізувати і викликати типізований метод, щоб уникнути дублювання логіки
		try
		{
			var request = jsonRequest.DeserializeFromJson<PatientAnalyzeLlmRequest>();
			if (request == null) throw new ArgumentException("Invalid JSON format");

			return await AugmentAsync(request, ct);
		}
		catch (Exception)
		{
			
			var medicalContext = await reasoningService.AnalyzeAsync(jsonRequest, ct);
			if (!medicalContext.Data.IsMedical) throw new InvalidOperationException("Not medical data");

			var validationRes = await confidenceValidator.ValidateAsync(medicalContext);
			if (!validationRes.IsValid) throw new InvalidOperationException("Low confidence context");

			return await RetrieveContextAsync(medicalContext.Data.Queries, ct);
		}
	}

	// ------------------------------------------------------------
	// 3. AUGMENTATION WITH VALIDATION (Аугментація + Перевірка релевантності)
	// ------------------------------------------------------------

	/// <summary>
	/// Повний цикл: Аналіз пацієнта -> Пошук -> ШІ-Валідація знайденого.
	/// Це найдорожчий, але найнадійніший метод.
	/// </summary>
	public async Task<RetrievalAugmentationResult> AugmentValidatedAsync(PatientAnalyzeLlmRequest request, CancellationToken ct = default)
	{
		// 1. Спочатку робимо звичайну аугментацію (отримуємо "сирі" результати пошуку)
		var rawResult = await AugmentAsync(request, ct);

		// 2. Тепер фільтруємо знайдене через "AI Re-ranking"
		var validatedRetrievalResults = new List<RetrievalResult>();

		// Обробляємо кожну групу результатів (по кожному пошуковому запиту)
		foreach (var group in rawResult.RetrievalResults)
		{
			var verifiedChunks = new List<KnowledgeChunk>();

			// Паралельна перевірка кожного чанку в групі може бути занадто дорогою,
			// тому робимо послідовно або обмежуємо паралелізм.
			// Тут for-each для простоти та контролю токенів.
			foreach (var chunk in group.FoundChunks)
			{
				// Викликаємо наш новий метод оцінки в Reasoning Service
				var relevanceCheck = await reasoningService.EvaluateRelevanceAsync(group.Query, chunk, ct);

				// Логіка фільтрації:
				// Пропускаємо тільки якщо AI сказав "Relevant" І впевненість > 0.6
				if (relevanceCheck.Data.IsRelevant && relevanceCheck.Data.Score >= 0.6)
				{
					// Можна навіть замінити Score чанку на Score релевантності від AI,
					// але краще залишити оригінальний векторний Score.
					verifiedChunks.Add(chunk);
				}
				else
				{
					logger.LogDebug("Chunk rejected by AI Validator. Query: {Query}. Reason: {Reason}",
						group.Query, relevanceCheck.Data.Reasoning);
				}
			}

			// Якщо після фільтрації щось залишилось - додаємо в результат
			if (verifiedChunks.Count > 0)
			{
				// Використовуємо 'with' (record copy), щоб створити оновлену групу
				validatedRetrievalResults.Add(group with
				{
					FoundChunks = verifiedChunks
				});
			}
		}

		// Повертаємо новий результат з відфільтрованими даними
		return new RetrievalAugmentationResult(rawResult.TotalProcessingTimeSeconds, validatedRetrievalResults);
	}

	public async Task<RetrievalAugmentationResult> RetrieveContextAsync(ICollection<string> queries, CancellationToken ct = default)
	{
		// Глобальний таймер для всієї операції
		var globalStopwatch = Stopwatch.StartNew();
		// Потокобезпечна колекція для результатів
		var searchResults = new ConcurrentBag<RetrievalResult>();
		// Налаштування паралелізму (не більше 5 запитів одночасно)
		var parallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = 5,
			CancellationToken = ct
		};

		logger.LogInformation("Starting retrieval for {Count} queries...", queries.Count);
		await qdrantService.EnsureCollectionAsync(ct);
		await Parallel.ForEachAsync(queries, parallelOptions, async (query, token) =>
		{
			// Таймер для ОДНОГО конкретного запиту
			var stepStopwatch = Stopwatch.StartNew();
			try
			{
				logger.LogInformation("Embedding query: '{Query}'", query);
				// 1. Векторизація
				var embedResult = await embeddingService.GetEmbeddingAsync(query, token);
				logger.LogInformation("Embedding query '{Query}' completed. Tokens spent: {Tokens}", query, embedResult.EmbeddingTokensSpent.TotalTokenCount);
				// 2. Пошук у Qdrant
				logger.LogInformation("Searching Qdrant for query: '{Query}'", query);

				var chunks = await qdrantService.SearchAsync(embedResult.Vector.ToArray(), token);

				logger.LogInformation("Search for query '{Query}' completed. Found {Count} chunks", query, chunks.Count);
				stepStopwatch.Stop();
				// 3. Зберігаємо результат
				// Час конвертуємо в секунди
				var retrievalResult = new RetrievalResult(query, embedResult, chunks, stepStopwatch.Elapsed.TotalSeconds);

				searchResults.Add(retrievalResult);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during retrieval for query '{Query}'", query);
			}
		});

		globalStopwatch.Stop();

		// 4. Формуємо фінальний результат
		// Токени та середній Score порахуються автоматично всередині RetrievalAugmentationResult
		var result = new RetrievalAugmentationResult(globalStopwatch.Elapsed.TotalSeconds, searchResults.ToList());


		logger.LogInformation(
			"Retrieval finished in {Time:F2}s. Total queries processed: {Count}. Unique chunks found: {UniqueCount}",
			result.TotalProcessingTimeSeconds,
			result.RetrievalResults.Count,
			result.GetUniqueChunks().Count);

		return result;
	}
}
