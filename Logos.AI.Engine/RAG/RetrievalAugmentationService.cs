using System.Collections.Concurrent;
using System.Diagnostics;
using Logos.AI.Abstractions.RAG;
using Logos.AI.Engine.Knowledge.Qdrant; // Переконайтеся, що неймспейс правильний для QdrantService
using Microsoft.Extensions.Logging;

namespace Logos.AI.Engine.RAG;

public class RetrievalAugmentationService(
    OpenAIEmbeddingService embeddingService,
    QdrantService qdrantService,
    ILogger<RetrievalAugmentationService> logger) : IRetrievalAugmentationService
{
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
                var retrievalResult = new RetrievalResult(query,embedResult, chunks, stepStopwatch.Elapsed.TotalSeconds);

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

    public async Task<RetrievalAugmentationResult> RetrieveWithRelevanceEvaluteAsync(ICollection<string> queries, CancellationToken ct = default)
    {
        
    }
}