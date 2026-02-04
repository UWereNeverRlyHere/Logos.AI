using System.Collections.Concurrent;
using System.Diagnostics;
using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.Knowledge;
using Logos.AI.Abstractions.Knowledge.Contracts;
using Logos.AI.Engine.Knowledge.Qdrant;
using Logos.AI.Engine.RAG;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.Knowledge;
/// <summary>
/// Сервіс для наповнення "бази знань"
/// </summary>
public class IngestionService(
	PdfChunkService           pdfService,
	OpenAIEmbeddingService    embeddingService,
	QdrantService             qdrantService,
	SqlChunkService           sqlChunkService,
	ILogger<IngestionService> logger) : IIngestionService
{
	public async Task<IngestionResult> IngestFileAsync(IngestionUploadData uploadData, CancellationToken ct = default)
	{
		var stopwatch = Stopwatch.StartNew();
		var docId = uploadData.DocumentId;		
		var existingDoc = await sqlChunkService.GetDocumentByIdAsync(docId, ct);
		if (existingDoc != null)
		{
			stopwatch.Stop();
			return IngestionResult.CreateExists(stopwatch, existingDoc);
		}
		if (!pdfService.TryChunkDocument(uploadData, out SimpleDocumentChunk chunkResult, out var error))
		{
			stopwatch.Stop();
			return IngestionResult.CreateFail(stopwatch, $"Parsing failed: {error}");
		}
		await sqlChunkService.SaveDocumentAsync(uploadData.FileName, uploadData.FilePath, chunkResult, ct);
		var texts = chunkResult.Chunks.Select(c => c.Content);
		var embeddingResults = await embeddingService.GetEmbeddingsAsync(texts, ct);
		int count = 0;
		var tokensRes = new List<IngestionTokenUsageDetails>();
		var pointsToUpsert = new List<QdrantUpsertData>();
		foreach (var chunk in chunkResult.Chunks)
		{
			var embeddingResult = embeddingResults[count];
			var vector = embeddingResult.Vector;
			var pointId = $"{docId}-{count}";
			var payload = KnowledgeDictionary.Create()
				.SetDocumentId(docId)
				.SetFileName(chunkResult.FileName)
				.SetDocumentTitle(chunkResult.DocumentTitle)
				.SetDocumentDescription(chunkResult.DocumentDescription)
				.SetPageNumber(chunk.PageNumber)
				.SetFullText(chunk.Content)
				.SetIndexedAt(chunkResult.IndexedAt)
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
			await qdrantService.UpsertChunksAsync(pointsToUpsert, ct);
		}
		stopwatch.Stop();
		return IngestionResult.CreateSuccess(stopwatch,chunkResult, tokensRes);
	}
	public async Task<BulkIngestionResult> IngestFilesAsync(ICollection<IngestionUploadData> uploadData, CancellationToken ct = default)
	{
		await qdrantService.EnsureCollectionAsync(ct);
		var stopwatch = Stopwatch.StartNew();
		var results = new ConcurrentBag<IngestionResult>();
		var parallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = 5,
			CancellationToken = ct
		};
		await Parallel.ForEachAsync(uploadData, parallelOptions, async (item, token) =>
		{
			var result = await IngestFileAsync(item, token);
			results.Add(result);
		});
		stopwatch.Stop();
		
		return new BulkIngestionResult(stopwatch.Elapsed.TotalSeconds, results.ToList());
	}
}
