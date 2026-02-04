using System.Collections.Concurrent;
using System.Diagnostics;
using Logos.AI.Abstractions.Exceptions;
using Logos.AI.Abstractions.Knowledge;
using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Validation.Contracts;
using Logos.AI.Engine.Extensions;
using Logos.AI.Engine.Knowledge.Qdrant;
using Logos.AI.Engine.Reasoning; 
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
		// Глобальний таймер для всієї операції
		var globalStopwatch = Stopwatch.StartNew();
		// 1. Аналіз контексту (Medical Context Analysis)
		var medicalContext = await reasoningService.AnalyzeAsync(request, ct);
		if (!medicalContext.Data.IsMedical)  RagException.ThrowForNotMedical(medicalContext);
		// 2. Валідація впевненості моделі (Confidence Check)
		var validationRes = await confidenceValidator.ValidateAsync(medicalContext);
		if (!validationRes.IsValid) RagException.ThrowForConfidanceValidationFailed(validationRes);
		// 3. Виконання пошуку (Delegation to Core Retrieval)
		var retrieveRes= await RetrieveContextAsync(medicalContext.Data.Queries, ct);
		globalStopwatch.Stop();

		// 4. Формуємо фінальний результат
		// Токени та середній Score порахуються автоматично всередині RetrievalAugmentationResult
		var result = new RetrievalAugmentationResult(globalStopwatch.Elapsed.TotalSeconds, validationRes, retrieveRes);
		
		logger.LogInformation(
			"Retrieval finished in {Time:F2}s. Total queries processed: {Count}. Unique chunks found: {UniqueCount}",
			result.TotalProcessingTimeSeconds,
			result.RetrievalResults.Count,
			result.GetUniqueChunks().Count);
		return result;
	}

	// Перевантаження для JSON-рядка
	public async Task<RetrievalAugmentationResult> AugmentAsync(string jsonRequest, CancellationToken ct = default)
	{
		try
		{
			var request = jsonRequest.DeserializeFromJson<PatientAnalyzeLlmRequest>();
			if (request == null) throw new ArgumentException("Invalid JSON format");

			return await AugmentAsync(request, ct);
		}
		catch (Exception)
		{
			var medicalContext = await reasoningService.AnalyzeAsync(jsonRequest, ct);
			if (!medicalContext.Data.IsMedical)  RagException.ThrowForNotMedical(medicalContext);

			var validationRes = await confidenceValidator.ValidateAsync(medicalContext);
			if (!validationRes.IsValid) RagException.ThrowForConfidanceValidationFailed(validationRes);

			return await RetrieveContextAsync(medicalContext.Data.Queries, ct);
		}
	}

	/// <summary>
    /// Повний цикл: Аналіз пацієнта -> Пошук -> Групування по документах -> ШІ-Валідація -> Фільтрація.
    /// </summary>
    public async Task<RetrievalAugmentationResult> AugmentValidatedAsync(PatientAnalyzeLlmRequest request, CancellationToken ct = default)
    {
        // 1. Спочатку робимо базову розумну аугментацію (отримуємо "сирі" результати пошуку)
        // Це вже включає в себе MedicalContextReasoningService + Confidence Check контексту
        var rawResult = await AugmentAsync(request, ct);

        var validatedRetrievalResults = new ConcurrentBag<RetrievalResult>();

        // Глобальний таймер продовжує рахувати, але ми хочемо заміряти час саме валідації
        var validationStopwatch = Stopwatch.StartNew();
        
        // 2. Паралельна обробка кожного пошукового запиту (Query)
        var parallelOptions = new ParallelOptions 
        { 
            MaxDegreeOfParallelism = 5, 
            CancellationToken = ct 
        };

        await Parallel.ForEachAsync(rawResult.RetrievalResults, parallelOptions, async (retrievalResult, token) =>
        {
            var verifiedChunks = new List<KnowledgeChunk>();
            var evaluations = new List<RelevanceEvaluationResult>();

            // 3. Групуємо чанки за DocumentId
            // Це ключовий момент: ми хочемо, щоб модель бачила всі знайдені шматки одного документа разом.
            var documentGroups = retrievalResult.FoundChunks.GroupBy(c => c.DocumentId);

            foreach (var docGroup in documentGroups)
            {
                var docChunks = docGroup.ToList();
                
                // 4. Викликаємо Reasoning Service для групи чанків
                // Передаємо запит користувача (Query) і всі шматки цього документа
                var relevanceReasoning = await reasoningService.EvaluateRelevanceAsync(retrievalResult, token);

                // 5. Оцінюємо впевненість моделі у своїй оцінці (LogProbs)
                // Ми перевіряємо, чи не галюцинувала модель, коли ставила оцінку "High"
                var confidence = await confidenceValidator.ValidateAsync(relevanceReasoning);
                // Дані оцінки від LLM
                var evalData = relevanceReasoning.Data;

                // --- ЛОГІКА ФІЛЬТРАЦІЇ ---
                bool passValidation = false;

                // Умова 1: Модель впевнена у своїй відповіді (Confidence > Low/Medium)
                if (confidence.IsValid)
                {
                    // Умова 2: Модель сказала, що документ релевантний (Score >= 0.5 або High/Medium)
                    // Ми використовуємо Score, бо він точніший
                    if (evalData.Score >= 0.5)
                    {
                        passValidation = true;
                    }
                }
                else
                {
                    logger.LogWarning("LLM confidence validation failed for document check. Query: {Query}. Reason: {Details}", 
                        retrievalResult.Query, string.Join(", ", confidence.Details));
                    
                    // Fallback політика: Якщо модель "невпевнена", чи відкидати документ?
                    // Для медицини безпечніше відкинути, або помітити як "Uncertain".
                    // Зараз відкидаємо, але зберігаємо в Evaluations з низьким скором.
                }

                if (passValidation)
                {
                    // 6. Вибираємо тільки ті чанки, ID яких повернула модель
                    // Модель могла сказати: "Документ супер, але тільки чанк №2 підходить, а чанк №1 - це вода"
                    var relevantFromDoc = docChunks
                        .Where(c => evalData.RelevantChunkIds.Contains(c.Id))
                        .ToList();

                    if (relevantFromDoc.Count > 0)
                    {
                        verifiedChunks.AddRange(relevantFromDoc);
                    }
                }

                // 7. Зберігаємо результат оцінки (для історії та UI)
                // Нам треба додати сюди інформацію про Confidence самої моделі
                // Тому трохи розширимо Reasoning, додавши інфу про впевненість
                var enrichedEvaluation = evalData with 
                { 
                    Reasoning = $"[Confidence: {confidence.Level}] {evalData.Reasoning}",
                    ConfidenceValidationResult = confidence
                };
                
                evaluations.Add(enrichedEvaluation);
            }

            // Якщо після фільтрації щось залишилось (або якщо ми хочемо повернути пустий список, але з оцінками)
            // Формуємо новий RetrievalResult
            if (evaluations.Count > 0) // Повертаємо, навіть якщо чанків 0, щоб показати "Ми перевірили, нічого не підійшло"
            {
                // Тут ми використовуємо `with`, припускаючи, що ти додав поле Evaluations в RetrievalResult
                var newResult = retrievalResult with 
                { 
                    FoundChunks = verifiedChunks,
                    RelevanceEvaluations = evaluations,
                };
                validatedRetrievalResults.Add(newResult);
            }
        });

        validationStopwatch.Stop();
        
        // Додаємо час валідації до загального часу
        var totalTime = rawResult.TotalProcessingTimeSeconds + validationStopwatch.Elapsed.TotalSeconds;

        return new RetrievalAugmentationResult(totalTime, rawResult.MedicalContextConfidence,validatedRetrievalResults.ToList());
    }

	public async Task<ICollection<RetrievalResult>> RetrieveContextAsync(ICollection<string> queries, CancellationToken ct = default)
	{
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
		return searchResults.ToList();
	}
}
