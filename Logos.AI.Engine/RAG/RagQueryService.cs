using System.Text;
using Logos.AI.Abstractions.Features.Knowledge;
using Microsoft.Extensions.Logging;

namespace Logos.AI.Engine.RAG;

public class RagQueryService(
	OpenAIEmbeddingService   embedding, 
	QdrantService            qdrant, 
	ILogger<RagQueryService> logger)
{
	// === НОВИЙ МЕТОД (Те, що ти просив: Пошук документів) ===
	public async Task<List<KnowledgeChunk>> SearchAsync(string query, int topK = 5, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(query)) return new List<KnowledgeChunk>();

		// 1. Текст -> Вектор
		var vectorEnum = await embedding.GetEmbeddingAsync(query);
		var vector = vectorEnum.ToArray();

		// 2. Вектор -> Qdrant (повертає KnowledgeChunk з метаданими)
		var results = await qdrant.SearchAsync(vector, topK, ct);
        
		return results;
	}
	
	public async Task<(string Answer, List<string> FileNames)> AnswerAsync(string query, int topK = 5)
	{
		// Використовуємо нову логіку пошуку
		var chunks = await SearchAsync(query, topK);
        
		var fileNames = chunks.Select(c => c.FileName).Distinct().ToList();
        
		// Повертаємо заглушку або простий текст, бо зараз ми фокусуємось на пошуку, а не генерації
		var sb = new StringBuilder();
		sb.AppendLine($"Found information in {chunks.Count} fragments:");
		foreach(var c in chunks)
		{
			sb.AppendLine($"- {c.FileName} (p. {c.PageNumber}): {c.Content.Substring(0, Math.Min(50, c.Content.Length))}...");
		}
        
		return (sb.ToString(), fileNames);
	}
}
