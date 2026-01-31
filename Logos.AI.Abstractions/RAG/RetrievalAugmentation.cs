using System.ComponentModel;
using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.Knowledge;
namespace Logos.AI.Abstractions.RAG;

public record RetrievalAugmentationResult
{
	[Description("Загальний час виконання всієї операції (в секундах)")]
	public double TotalProcessingTimeSeconds { get; private set;}
	[Description("Сумарна кількість токенів, витрачених на ембеддінг всіх запитів")]
	public TokenUsageInfo FullEmbeddingTokensSpent{ get; private set;}
	
	[Description("Середній Score релевантності по всім унікальним чанкам")]
	public float GlobalAverageScore { get; private set;}
	[Description("Детальна історія: який запит -> який вектор -> які чанки знайшли")]
	public ICollection<RetrievalResult> RetrievalResults { get; init; } = new List<RetrievalResult>();
	/// <summary>
	/// Отримати всі знайдені чанки "як є" (можуть бути дублікати, якщо різні запити знайшли одне й те саме).
	/// </summary>
	public IEnumerable<KnowledgeChunk> GetChunks() =>RetrievalResults.SelectMany(r => r.FoundChunks);
	
	/// <summary>
	/// Отримати унікальні чанки (без дублікатів).
	/// Дедуплікація відбувається за DocumentId та PageNumber.
	/// Результат відсортовано за релевантністю (Score).
	/// </summary>
	public ICollection<KnowledgeChunk> GetUniqueChunks()
	{
		return GetChunks()
			.DistinctBy(c => new { c.DocumentId, c.PageNumber }) // Унікальність за документом і сторінкою
			.OrderByDescending(c => c.Score)                     // Спочатку найбільш релевантні
			.ToList();
	}
	public RetrievalAugmentationResult(double totalProcessingTimeSeconds, ICollection<RetrievalResult> retrievalResults )
	{
		TotalProcessingTimeSeconds = totalProcessingTimeSeconds;
		RetrievalResults = retrievalResults;
		
		var totalInput = RetrievalResults.Sum(s => s.Embedding.GetInputTokenCount());
		var totalTotal = RetrievalResults.Sum(s => s.Embedding.GetTotalTokenCount());
		FullEmbeddingTokensSpent =  new TokenUsageInfo(totalInput, totalTotal);
		var unique = GetUniqueChunks();
		GlobalAverageScore = unique.Any() ? unique.Average(c => c.Score) : 0f;
	}
}
/// <summary>
/// Результат пошуку для ОДНОГО конкретного запиту.
/// </summary>
public record RetrievalResult
{
	[Description("Час виконання цього конкретного пошуку (в секундах)")]
	public double DurationSeconds { get; init; }
	[Description("Текст запиту, за яким виконувався пошук")]
	public string Query { get; init; } = string.Empty;
	[Description("Чанки, знайдені саме для цього запиту (сирі дані)")]
	public EmbeddingResult Embedding { get; init; }
	public ICollection<KnowledgeChunk> FoundChunks { get; init; } = new List<KnowledgeChunk>();
	[Description("Середній Score для цього конкретного запиту")]
	public float AverageScore => FoundChunks.Any() ? FoundChunks.Average(c => c.Score) : 0f;
	public RetrievalResult(string query,EmbeddingResult embedding, ICollection<KnowledgeChunk> chunks, double durationSeconds)
	{
		Query = query;
		Embedding = embedding;
		FoundChunks = chunks;
		DurationSeconds = durationSeconds;
	}
}

public record EmbeddingResult
{
	public ICollection<float> Vector { get; set; } = new List<float>();
	public TokenUsageInfo EmbeddingTokensSpent { get; set; } = new();
	public EmbeddingResult(ICollection<float> vector, int inputTokenCount, int totalTokenCount)
	{
		Vector = vector;
		EmbeddingTokensSpent = new TokenUsageInfo(inputTokenCount, totalTokenCount);
	}
	public EmbeddingResult()
	{
	}
	public int GetTotalTokenCount() => EmbeddingTokensSpent.TotalTokenCount;
	public int GetInputTokenCount() => EmbeddingTokensSpent.InputTokenCount;
}
