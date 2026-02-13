using System.Collections.Concurrent;
using System.Diagnostics;
using Logos.AI.Abstractions.Exceptions;
using Logos.AI.Abstractions.Knowledge;
using Logos.AI.Abstractions.Knowledge.Contracts;
using Logos.AI.Abstractions.Knowledge.Retrieval;
using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Reasoning.Contracts;
using Logos.AI.Abstractions.Validation.Contracts;
using Logos.AI.Engine.Extensions;
using Microsoft.Extensions.Logging;

namespace Logos.AI.Engine.RAG;

public class RetrievalAugmentationService(
	OpenAIEmbeddingService                embeddingService,
	IVectorStorageService qdrantService,
	IMedicalContextReasoningService       contextReasoningService,
	IConfidenceValidator                  confidenceValidator,
	ILogger<RetrievalAugmentationService> logger) : IRetrievalAugmentationService
{
	public async Task<RetrievalAugmentationResult> AugmentAsync(PatientAnalyzeRagRequest request, CancellationToken ct = default)
	{
		// Глобальний таймер для вимірювання загального часу операції
		var globalStopwatch = Stopwatch.StartNew();
		logger.LogInformation("Starting retrieval augmentation for patient analysis request");
		// 1. Аналіз медичного контексту запиту
		logger.LogDebug("Step 1: Analyzing medical context...");
		var medicalContext = await contextReasoningService.AnalyzeAsync(request, ct);
		if (!medicalContext.Data.IsMedical)
		{
			logger.LogWarning("Request identified as non-medical. Aborting augmentation");
			RagException.ThrowForNotMedical(medicalContext);
		}
		logger.LogInformation("Medical context analysis completed. Found {QueryCount} search queries", medicalContext.Data.Queries.Count);
		// 2. Валідація впевненості моделі в аналізі контексту
		logger.LogDebug("Step 2: Validating LLM confidence for medical context...");
		var validationRes = await confidenceValidator.ValidateAsync(medicalContext);
		if (!validationRes.IsValid)
		{
			logger.LogWarning("Confidence validation failed (Score: {Score:F2}, Level: {Level}). Aborting augmentation", 
				validationRes.Score, validationRes.ConfidenceLevel);
			RagException.ThrowForConfidenceValidationFailed(medicalContext.Data,validationRes);
		}
		logger.LogInformation("Confidence validation passed (Score: {Score:F2}, Level: {Level})", validationRes.Score, validationRes.ConfidenceLevel);
		// 3. Виконання пошуку в базі знань
		logger.LogDebug("Step 3: Executing core retrieval...");
		var retrieveRes = await RetrieveContextAsync(medicalContext.Data.Queries, ct);
		globalStopwatch.Stop();
		// 4. Формування фінального результату
		var result = new RetrievalAugmentationResult(globalStopwatch.Elapsed.TotalSeconds, validationRes, retrieveRes)
		{
			MedicalContextLlmResponse = medicalContext.Data
		};
			
		logger.LogInformation(
			"Retrieval augmentation finished in {Time:F2}s. Total queries: {Count}. Unique chunks: {UniqueCount}",
			result.TotalProcessingTimeSeconds,
			result.RetrievalResults.Count,
			result.GetUniqueChunks().Count);
			
		return result;
	}

	// Перевантаження для обробки JSON-рядка запиту
	public async Task<RetrievalAugmentationResult> AugmentAsync(string jsonRequest, CancellationToken ct = default)
	{
		var globalStopwatch = Stopwatch.StartNew();
		logger.LogInformation("Starting retrieval augmentation from JSON request");
		try
		{
			// Спроба десеріалізації JSON у об'єкт запиту
			var request = jsonRequest.DeserializeFromJson<PatientAnalyzeRagRequest>();
			if (request == null)
			{
				logger.LogError("Failed to deserialize JSON request: Result is null");
				throw new ArgumentException("Invalid JSON format or empty content.");
			}

			logger.LogDebug("JSON successfully deserialized to PatientAnalyzeLlmRequest");
			return await AugmentAsync(request, ct);
		}
		catch (Exception ex)
		{
			logger.LogWarning("Standard JSON augmentation failed or request is in alternative format. Attempting direct reasoning analysis. Error: {Message}", ex.Message);
			
			// 1. Аналіз контексту безпосередньо з тексту
			var medicalContext = await contextReasoningService.AnalyzeAsync(jsonRequest, ct);
			if (!medicalContext.Data.IsMedical)
			{
				logger.LogWarning("Direct reasoning identified content as non-medical");
				RagException.ThrowForNotMedical(medicalContext);
			}
			// 2. Валідація впевненості
			var validationRes = await confidenceValidator.ValidateAsync(medicalContext);
			if (!validationRes.IsValid)
			{
				logger.LogWarning("Confidence validation failed for direct reasoning (Score: {Score:F2})", validationRes.Score);
				RagException.ThrowForConfidenceValidationFailed(medicalContext.Data,validationRes);
			}
			// 3. Пошук контексту
			var retrieveRes = await RetrieveContextAsync(medicalContext.Data.Queries, ct);
			globalStopwatch.Stop();
			logger.LogInformation("Direct reasoning augmentation completed in {Time:F2}s", globalStopwatch.Elapsed.TotalSeconds);
			// Повертаємо повноцінний результат, а не просто колекцію чанків
			return new RetrievalAugmentationResult(globalStopwatch.Elapsed.TotalSeconds, validationRes, retrieveRes);
		}
	}

	/// <summary>
    /// Повний цикл: Аналіз пацієнта -> Пошук -> Групування по документах -> ШІ-Валідація -> Фільтрація.
    /// </summary>
    public async Task<RetrievalAugmentationResult> AugmentValidatedAsync(PatientAnalyzeRagRequest request, CancellationToken ct = default)
    {
        // 1. Початкова аугментація: отримання "сирих" результатів пошуку
        // Включає MedicalContextReasoningService та перевірку впевненості контексту
        logger.LogInformation("Starting validated augmentation process");
        var rawResult = await AugmentAsync(request, ct);
        var validatedRetrievalResults = new ConcurrentBag<RetrievalResult>();
        // Вимірювання часу саме для етапу ШІ-валідації
        var validationStopwatch = Stopwatch.StartNew();
        // 2. Паралельна обробка кожного пошукового запиту
        var parallelOptions = new ParallelOptions 
        { 
            MaxDegreeOfParallelism = 5, 
            CancellationToken = ct 
        };
        logger.LogInformation("Starting parallel relevance evaluation for {QueryCount} retrieval results", rawResult.RetrievalResults.Count);
        await Parallel.ForEachAsync(rawResult.RetrievalResults, parallelOptions, async (retrievalResult, token) =>
        {
            logger.LogDebug("Evaluating relevance for query: '{Query}'", retrievalResult.Query);
            // Якщо для запиту нічого не знайдено, просто зберігаємо його для історії
            if (retrievalResult.FoundChunks.Count == 0)
            {
                validatedRetrievalResults.Add(retrievalResult);
                return;
            }
            var verifiedChunks = new List<KnowledgeChunk>();
            var evaluations = new List<RelevanceEvaluationResult>();
            // 3. Групування знайдених фрагментів за ID документа для поблокової оцінки
            var documentGroups = retrievalResult.FoundChunks.GroupBy(c => c.DocumentId).ToList();
            logger.LogDebug("Found {DocCount} documents for query '{Query}'", documentGroups.Count, retrievalResult.Query);
            foreach (var docGroup in documentGroups)
            {
                var docChunks = docGroup.ToList();
                
                // 4. Оцінка релевантності конкретного документа
                // Створюємо тимчасовий результат тільки з чанками цього документа
                var docRetrieval = retrievalResult with { FoundChunks = docChunks };
                var relevanceReasoning = await contextReasoningService.EvaluateRelevanceAsync(docRetrieval, token);
                // 5. Валідація впевненості ШІ
                var confidence = await confidenceValidator.ValidateAsync(relevanceReasoning);
                var evalData = relevanceReasoning.Data;
                // --- ЛОГІКА ФІЛЬТРАЦІЇ ---
                if (!confidence.IsValid)
                {
                    logger.LogWarning("LLM confidence validation failed for document {DocId}. Query: '{Query}'. Reason: {Details}", 
                        docGroup.Key, retrievalResult.Query, string.Join(", ", confidence.Details));
                }
                else if (evalData.Score < 0.5)
                {
                    logger.LogDebug("Document {DocId} rejected for query '{Query}'. Score: {Score:F2}", 
                        docGroup.Key, retrievalResult.Query, evalData.Score);
                }
                else
                {
                    // 6. Вибір релевантних чанків, підтверджених моделлю
                    var relevantFromDoc = docChunks.Where(c => evalData.RelevantChunkIds.Contains(c.Id)).ToList();
                    if (relevantFromDoc.Count > 0)
                    {
                        logger.LogDebug("Selected {ChunkCount} relevant chunks from document {DocId} for query '{Query}'", 
                            relevantFromDoc.Count, docGroup.Key, retrievalResult.Query);
                        verifiedChunks.AddRange(relevantFromDoc);
                    }
                }
                // 7. Збереження результату оцінки з даними про впевненість
                evaluations.Add(evalData with 
                { 
                    Reasoning = $"[Confidence: {confidence.ConfidenceLevel}] {evalData.Reasoning}",
                    ConfidenceValidationResult = confidence
                });
            }
            // Формуємо оновлений RetrievalResult
            validatedRetrievalResults.Add(retrievalResult with 
            { 
                FoundChunks = verifiedChunks,
                RelevanceEvaluations = evaluations,
            });
            logger.LogInformation("Completed relevance evaluation for query '{Query}'. Validated chunks: {Count}", 
                retrievalResult.Query, verifiedChunks.Count);
        });

        validationStopwatch.Stop();
        
        // Розрахунок загального часу виконання з урахуванням валідації
        var totalTime = rawResult.TotalProcessingTimeSeconds + validationStopwatch.Elapsed.TotalSeconds;
        var finalResult = new RetrievalAugmentationResult(totalTime, rawResult.MedicalContextConfidence, validatedRetrievalResults.ToList());
        logger.LogInformation("Validated augmentation finished. Total time: {Time:F2}s. Validated chunks: {UniqueCount}", 
            totalTime, finalResult.GetUniqueChunks().Count);

        return finalResult;
    }

	public async Task<ICollection<RetrievalResult>> RetrieveContextAsync(ICollection<string> queries, CancellationToken ct = default)
	{
		if (queries.Count == 0)
			return Array.Empty<RetrievalResult>();

		logger.LogInformation("Starting knowledge retrieval for {Count} queries", queries.Count);
		
		var queryList = queries.ToList();
		
		// 1. Векторизація всіх запитів одночасно (Embedding)
		logger.LogDebug("Generating embeddings for {Count} queries in bulk", queryList.Count);
		var embeddingStopwatch = Stopwatch.StartNew();
		var embeddings = await embeddingService.GetEmbeddingsAsync(queryList, ct);
		embeddingStopwatch.Stop();
		logger.LogInformation("Generated {Count} embeddings in {Time:F2}s", embeddings.Count, embeddingStopwatch.Elapsed.TotalSeconds);

		// Потокобезпечна колекція для збору результатів пошуку
		var searchResults = new ConcurrentBag<RetrievalResult>();
		
		// Конфігурація паралельного виконання (обмеження до 5 запитів одночасно для пошуку)
		var parallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = 5,
			CancellationToken = ct
		};

		// 2. Пошук схожих фрагментів у векторній БД Qdrant для кожного запиту
		await Parallel.ForEachAsync(Enumerable.Range(0, queryList.Count), parallelOptions, async (index, token) =>
		{
			var query = queryList[index];
			var embedResult = embeddings[index];
			
			var stepStopwatch = Stopwatch.StartNew();
			try
			{
				logger.LogDebug("Processing query: '{Query}'", query);
				
				// Логування інформації про отриманий ембеддінг (збережено для сумісності з попередніми логами)
				logger.LogInformation("Embedding for '{Query}' retrieved from bulk result. Tokens: {Tokens}", 
					query, embedResult.EmbeddingTokensSpent.TotalTokenCount);

				// Пошук у Qdrant
				logger.LogDebug("Searching Qdrant for query: '{Query}'", query);
				var chunks = await qdrantService.SearchAsync(embedResult.Vector.ToArray(), token);
				stepStopwatch.Stop();
				
				logger.LogInformation("Search for '{Query}' completed. Chunks found: {Count}. Time: {Time:F2}s", 
					query, chunks.Count, stepStopwatch.Elapsed.TotalSeconds);
				
				var retrievalResult = new RetrievalResult(query, embedResult, chunks, stepStopwatch.Elapsed.TotalSeconds);
				searchResults.Add(retrievalResult);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during retrieval for query '{Query}': {Message}", query, ex.Message);
			}
		});

		logger.LogInformation("Retrieval cycle completed. Total results collected: {Count}", searchResults.Count);
		return searchResults.ToList();
	}
}
