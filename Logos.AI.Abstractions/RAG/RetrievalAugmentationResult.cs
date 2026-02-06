using System.ComponentModel;
using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.Knowledge.Retrieval;
using Logos.AI.Abstractions.Validation;
namespace Logos.AI.Abstractions.RAG;

public record RetrievalAugmentationResult
{
	[Description("Загальний час виконання всієї операції (в секундах)")]
	public double TotalProcessingTimeSeconds { get; private set;}
	[Description("Сумарна кількість токенів, витрачених на ембеддінг всіх запитів")]
	public TokenUsageInfo FullEmbeddingTokensSpent{ get; private set;}
	
	[Description("Середній Score релевантності по всім унікальним чанкам")]
	public float GlobalAverageScore { get; private set;}
	[Description("Результат перевірки впевненості моделі, після виділення медичного контексту")]
	public ConfidenceValidationResult MedicalContextConfidence { get; init; } = new();
	[Description("Загальна кількість результатів пошуку (запитів) у цьому аугментуванні")]
	public int TotalRetrievalResults => RetrievalResults.Count;
	[Description("Загальна кількість знайдених чанків у всіх результатах пошуку")]
	public int TotalChunksFound => RetrievalResults.Sum(r => r.TotalChunksFound);
	
	[Description("Детальна історія: який запит -> який вектор -> які чанки знайшли")]
	public ICollection<RetrievalResult> RetrievalResults { get; init; } = new List<RetrievalResult>();
	/// <summary>
	/// Отримати всі знайдені чанки "як є" (можуть бути дублікати, якщо різні запити знайшли одне й те саме).
	/// </summary>
	public IEnumerable<KnowledgeChunk> GetChunks() => RetrievalResults.SelectMany(r => r.FoundChunks);
	
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

	
	public RetrievalAugmentationResult(double totalProcessingTimeSeconds, ConfidenceValidationResult confidence, ICollection<RetrievalResult> retrievalResults )
	{
		TotalProcessingTimeSeconds = totalProcessingTimeSeconds;
		RetrievalResults = retrievalResults;
		MedicalContextConfidence = confidence;
		var totalInput = RetrievalResults.Sum(s => s.Embedding.GetInputTokenCount());
		var totalTotal = RetrievalResults.Sum(s => s.Embedding.GetTotalTokenCount());
		FullEmbeddingTokensSpent =  new TokenUsageInfo(totalInput, totalTotal);
		var unique = GetUniqueChunks();
		GlobalAverageScore = unique.Any() ? unique.Average(c => c.Score) : 0f;
	}
}

