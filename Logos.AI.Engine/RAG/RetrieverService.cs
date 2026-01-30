using Logos.AI.Abstractions.Features.Knowledge;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logos.AI.Engine.RAG;
//Retrieval - поиск и извлечение релевантной информации.
public class RagQueryService(
	IOptions<RagOptions> options,
	OpenAiEmbeddingService   embedding, 
	QdrantService            qdrant, 
	ILogger<RagQueryService> logger)
{
	private readonly RagOptions _options = options.Value;
	// === НОВИЙ МЕТОД (Те, що ти просив: Пошук документів) ===
	public async Task<List<KnowledgeChunk>> SearchAsync(string query, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(query)) return new List<KnowledgeChunk>();
		logger.LogInformation("Embedding query: {Query}", query);
		// 1. Векторизація
		var vectorEnum = await embedding.GetEmbeddingAsync(query,ct);
		var vector = vectorEnum.ToArray();
		// 2. Пошук у Qdrant
		float optionsMinScore = _options.MinScore;
		logger.LogInformation("Searching Qdrant with threshold {OptionsMinScore}...", optionsMinScore);
		var results = await qdrant.SearchAsync(vector, ct: ct);
		logger.LogInformation("Found {Count} chunks above threshold", results.Count);
		return results;
	}
	
}
