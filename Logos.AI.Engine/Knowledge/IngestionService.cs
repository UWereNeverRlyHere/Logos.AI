using System.Collections.Concurrent;
using System.Diagnostics;
using Logos.AI.Abstractions.Knowledge;
using Logos.AI.Abstractions.Knowledge._Contracts;
using Logos.AI.Abstractions.Knowledge.VectorStorage;
using Logos.AI.Engine.RAG;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.Knowledge;
/// <summary>
/// Сервіс для наповнення "бази знань"
/// </summary>
public class IngestionService(
	IDocumentChunkService     pdfService,
	OpenAIEmbeddingService    embeddingService,
	IVectorStorageService qdrantService,
	IStorageService  storageService,
	IServiceScopeFactory scopeFactory,
	ILogger<IngestionService> logger) : IIngestionService
{
	public async Task<IngestionResult> IngestFileAsync(IngestionUploadData uploadData, CancellationToken ct = default)
	{
		var stopwatch = Stopwatch.StartNew();
		var docId = uploadData.DocumentId;
		logger.LogInformation("Starting ingestion for file: {FileName} (ID: {DocId})", uploadData.FileName, docId);

		// Перевіряємо, чи такий документ вже є в базі (за хешем контенту)
		var existingDoc = await storageService.GetDocumentByIdAsync(docId, ct);
		if (existingDoc != null)
		{
			logger.LogInformation("Document {DocId} already exists in SQL database. Skipping ingestion", docId);
			stopwatch.Stop();
			return IngestionResult.CreateExists(stopwatch, existingDoc);
		}

		// Розбиваємо PDF на чанки
		if (!pdfService.TryChunkDocument(uploadData, out SimpleDocumentChunk simpleDocChunk, out var error))
		{
			logger.LogError("Failed to parse PDF {FileName}: {Error}", uploadData.FileName, error);
			stopwatch.Stop();
			return IngestionResult.CreateFail(stopwatch, $"Parsing failed: {error}");
		}

		logger.LogInformation("PDF parsed successfully. Chunks count: {Count}", simpleDocChunk.Chunks.Count);

		var texts = simpleDocChunk.Chunks.Select(c => c.Content).ToList();
		
		// Отримуємо ембеддінги для всіх чанків одним запитом
		logger.LogInformation("Generating embeddings for {Count} chunks...", texts.Count);
		//треба буде додати перевірку лімітів по розміру документів  можна передавати максимум пачками по 2048
		var embeddingResults = await embeddingService.GetEmbeddingsAsync(texts, ct);
		logger.LogDebug("Embeddings generated successfully");

		int count = 0;
		var tokensRes = new List<IngestionTokenUsageDetails>();
		var pointsToUpsert = new List<QdrantUpsertData>();

		foreach (var chunk in simpleDocChunk.Chunks)
		{
			var embeddingResult = embeddingResults[count];
			var vector = embeddingResult.Vector;
			var pointId = $"{docId}-{count}";
			var payload = KnowledgeDictionary.Create()
				.SetDocumentId(docId)
				.SetFileName(simpleDocChunk.FileName)
				.SetDocumentTitle(simpleDocChunk.DocumentTitle)
				.SetDocumentDescription(simpleDocChunk.DocumentDescription)
				.SetPageNumber(chunk.PageNumber)
				.SetFullText(chunk.Content)
				.SetIndexedAt(simpleDocChunk.IndexedAt)
				.GetPayload();

			pointsToUpsert.Add(new QdrantUpsertData()
			{
				PointId = pointId,
				Vector = vector.ToArray(),
				Payload = payload
			});

			tokensRes.Add(new IngestionTokenUsageDetails
			{
				Content = chunk.Content,
				TokenUsageInfo = embeddingResult.EmbeddingTokensSpent
			});
			count++;
		}
		if (pointsToUpsert.Count > 0)
		{
			// Відправляємо дані в Qdrant
			logger.LogInformation("Upserting {Count} points to Qdrant...", pointsToUpsert.Count);
			await qdrantService.UpsertChunksAsync(pointsToUpsert, ct);
			logger.LogDebug("Qdrant upsert completed");
			// Зберігаємо метадані в SQL
			await storageService.SaveDocumentAsync(uploadData, simpleDocChunk, ct);
			logger.LogDebug("Document metadata and chunks saved to SQL database");
		}
		stopwatch.Stop();
		logger.LogInformation("Ingestion completed for {FileName} in {Elapsed}s. Total chunks: {Chunks}", 
			uploadData.FileName, stopwatch.Elapsed.TotalSeconds, simpleDocChunk.Chunks.Count);
		
		return IngestionResult.CreateSuccess(stopwatch, simpleDocChunk, tokensRes);
	}
	public async Task<BulkIngestionResult> IngestFilesAsync(ICollection<IngestionUploadData> uploadData, CancellationToken ct = default)
	{
		logger.LogInformation("Starting bulk ingestion for {Count} files", uploadData.Count);
		var stopwatch = Stopwatch.StartNew();
		var results = new ConcurrentBag<IngestionResult>();
		var parallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = 5,
			CancellationToken = ct
		};

		await Parallel.ForEachAsync(uploadData, parallelOptions, async (item, token) =>
		{
			using var scope = scopeFactory.CreateScope();
			var scopedIngestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
			var result = await scopedIngestionService.IngestFileAsync(item, token);
			results.Add(result);
		});
		
		stopwatch.Stop();
		logger.LogInformation("Bulk ingestion completed in {Elapsed}s. Success: {SuccessCount}, Failed: {FailCount}", 
			stopwatch.Elapsed.TotalSeconds, results.Count(r => r.IsSuccess), results.Count(r => !r.IsSuccess));
		
		return new BulkIngestionResult(stopwatch.Elapsed.TotalSeconds, results.ToList());
	}
}
