using System.Collections.Concurrent;
using Logos.AI.Abstractions.Features.Knowledge;
using Logos.AI.Engine.RAG;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.Knowledge;

public class KnowledgeService(PdfChunkService pdfService,
	OpenAiEmbeddingService embeddingService,
	QdrantService qdrantService,
	ILogger<KnowledgeService> logger) : IKnowledgeService
{
	//private readonly SqlChunkService _sqlService; 
	private readonly ILogger<KnowledgeService> _logger = logger;
	public async Task<IngestionResult> IngestFileAsync(IngestionUploadData uploadData, CancellationToken ct = default)
	{
		if (!pdfService.TryChunkDocument(uploadData, out var chunkResult, out var error))
		{
			return await Task.FromResult(new IngestionResult(false, uploadData.FileName, 0, $"Parsing failed: {error}"));
		}
		var docId = Guid.NewGuid();
		// await _sqlService.SaveDocumentAsync(docId, fileName, ...); 
		await qdrantService.EnsureCollectionAsync(ct);
		int count = 0;
		foreach (var chunk in chunkResult.Chunks)
		{
			var vectorEnum = await embeddingService.GetEmbeddingAsync(chunk.Content, ct);
			var vector = vectorEnum.ToArray();
			var pointId = $"{docId}-{count}";
			var payload = new Dictionary<string, object>
			{
				["documentId"] = docId.ToString(),
				["fileName"] = uploadData.FileName,
				["documentTitle"] = chunkResult.DocumentTitle, 
				["documentDescription"] = uploadData.Description, // Можливо додам генерацію опису з ШІ
				["pageNumber"] = chunk.PageNumber,
				["fullText"] = chunk.Content,
				["indexedAt"] = DateTime.UtcNow.ToString("O") 
			};

			await qdrantService.UpsertChunkAsync(pointId, vector, payload, ct);
			count++;
		}

		return new IngestionResult(true, chunkResult.FileName, count, "Success");
	}
	public async Task<ICollection<IngestionResult>> IngestFilesAsync(ICollection<IngestionUploadData> uploadData, CancellationToken ct = default)
	{
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
		return results.ToList();
	}
}
