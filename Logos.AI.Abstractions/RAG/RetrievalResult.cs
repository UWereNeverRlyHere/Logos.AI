using System.ComponentModel;
using Logos.AI.Abstractions.Knowledge.Retrieval;
namespace Logos.AI.Abstractions.RAG;

[Description("Результат пошуку для ОДНОГО конкретного запиту. Включає час виконання, текст запиту, знайдені чанки та оцінки релевантності. Використовується для аналізу результатів пошуку та вибору найбільш релевантних чанків.")]
public record RetrievalResult

{
	[Description("Час виконання цього конкретного пошуку (в секундах)")]
	public double DurationSeconds { get; init; }
	[Description("Текст запиту, за яким виконувався пошук")]
	public string Query { get; init; } = string.Empty;
	
	[Description("Середній Score для цього конкретного запиту")]
	public float AverageScore => FoundChunks.Any() ? FoundChunks.Average(c => c.Score) : 0f;
	
	[Description("Загальна кількість знайдених чанків для цього конкретного запиту")]
	public int TotalChunksFound => FoundChunks.Count;
	
	[Description("Знайдені чанки та їх оцінки релевантності для цього конкретного запиту")]
	public ICollection<KnowledgeChunk> FoundChunks { get; init; } = new List<KnowledgeChunk>();
	[Description("Чанки, знайдені саме для цього запиту (сирі дані)")]
	public EmbeddingResult Embedding { get; init; }
	
	[Description("Оцінки релевантності знайдених чанків для цього конкретного запиту")]
	public ICollection<RelevanceEvaluationResult> RelevanceEvaluations { get; init; } = new List<RelevanceEvaluationResult>();

	public RetrievalResult(string query,EmbeddingResult embedding, ICollection<KnowledgeChunk> chunks, double durationSeconds)
	{
		Query = query;
		Embedding = embedding;
		FoundChunks = chunks;
		DurationSeconds = durationSeconds;
	}
}
