using System.Text;
using Logos.AI.Abstractions.Features.Knowledge;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logos.AI.Engine.RAG;

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
	
	public async Task<(string Answer, List<string> FileNames)> AnswerAsync(string query)
	{
		var chunks = await SearchAsync(query);
		var fileNames = chunks.Select(c => c.FileName).Distinct().ToList();
        
		var sb = new StringBuilder();
		if (chunks.Count == 0)
		{
			sb.AppendLine("На жаль, у базі знань не знайдено релевантної інформації.");
		}
		else
		{
			sb.AppendLine($"Знайдено {chunks.Count} фрагментів:");
			foreach(var c in chunks)
			{
				sb.AppendLine($"- {c.FileName} (стор. {c.PageNumber}, точність: {c.Score:F2}): {c.Content.Substring(0, Math.Min(50, c.Content.Length))}...");
			}
		}
        
		return (sb.ToString(), fileNames);
	}
}
