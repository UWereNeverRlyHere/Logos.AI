using System.ComponentModel;
using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.Knowledge.Retrieval;
using Logos.AI.Abstractions.Reasoning;
using Logos.AI.Abstractions.Validation;
namespace Logos.AI.Abstractions.RAG;

public record RetrievalAugmentationResult
{
	[Description("Загальний час виконання всієї операції (в секундах)")]
	public required double TotalProcessingTimeSeconds { get; init; }

	[Description("Сумарна кількість токенів, витрачених на ембеддінг всіх запитів")]
	public TokenUsageInfo EmbeddingTokensSpent { get; init; }

	[Description("Сумарна кількість токенів, витрачених на модель для виділення контексту")]
	public required TokenUsageInfo ReasoningTokensSpent { get; init; }

	[Description("Сумарна кількість токенів, витрачених на пошук даних та аугментацію")]
	public required TokenUsageInfo AugmentationTokensSpent { get; init; }

	[Description("Середній Score релевантності по всім унікальним чанкам")]
	public float GlobalAverageScore { get; init; }
	[Description("Результат виділення медичного контексту")]
	public required MedicalContextLlmResponse MedicalContextLlmResponse { get; init; }

	[Description("Результат перевірки впевненості моделі, після виділення медичного контексту")]
	public required ConfidenceValidationResult MedicalContextConfidence { get; init; } = new();

	[Description("Загальна кількість результатів пошуку (запитів) у цьому аугментуванні")]
	public int TotalRetrievalResults => RetrievalResults.Count;

	[Description("Загальна кількість знайдених чанків у всіх результатах пошуку")]
	public int TotalChunksFound => RetrievalResults.Sum(r => r.TotalChunksFound);

	[Description("Детальна історія: який запит -> який вектор -> які чанки знайшли")]
	public required ICollection<ExtendedRetrievalResult> RetrievalResults { get; init; } = new List<ExtendedRetrievalResult>();

	[Description("Повертає всі знайдені чанки у результаті пошуку, включаючи дублікати. Результат може бути використаний для аналізу всіх знайдених чанків.")]
	public IEnumerable<KnowledgeChunk> GetChunks() => RetrievalResults.SelectMany(r => r.FoundChunks);

	[Description("Повертає унікальні знайдені чанки, відсортовані за релевантністю. Унікальність визначається за DocumentId та PageNumber. Результат може бути використаний для аналізу найбільш релевантних чанків.")]
	public ICollection<KnowledgeChunk> GetUniqueChunks()
	{
		return GetChunks()
			.DistinctBy(c => new
			{
				c.DocumentId,
				c.PageNumber
			}) // Унікальність за документом і сторінкою
			.OrderByDescending(c => c.Score) // Спочатку найбільш релевантні
			.ToList();
	}
	private RetrievalAugmentationResult()
	{
		var eTotalInput = RetrievalResults.Sum(s => s.Embedding.GetInputTokenCount());
		var eTotalTotal = RetrievalResults.Sum(s => s.Embedding.GetTotalTokenCount());
		EmbeddingTokensSpent = new TokenUsageInfo(eTotalInput, eTotalTotal);
		AugmentationTokensSpent = new TokenUsageInfo(EmbeddingTokensSpent.InputTokenCount + ReasoningTokensSpent.InputTokenCount, EmbeddingTokensSpent.TotalTokenCount + ReasoningTokensSpent.TotalTokenCount);
		GlobalAverageScore = RetrievalResults.Any() ? RetrievalResults.SelectMany(c => c.FoundChunks).Average(c => c.Score) : 0f;
	}

	[Description("DTO для ініціалізації через Create")]
	public record CreateRequest
	{
		public required double TotalProcessingTimeSeconds { get; init; }
		public required MedicalContextLlmResponse MedicalContextLlmResponse { get; init; }
		public required TokenUsageInfo ReasoningTokensSpent { get; init; }
		public required ICollection<ExtendedRetrievalResult> RetrievalResults { get; init; }
		public required ConfidenceValidationResult MedicalContextConfidence { get; init; }
	}
	/// <summary>
	/// Фабрика для створення RetrievalAugmentationResult з автоматичним розрахунком похідних полів
	/// </summary>
	public static RetrievalAugmentationResult Create(CreateRequest request)
	{
		var eTotalInput = request.RetrievalResults.Sum(s => s.Embedding.GetInputTokenCount());
		var eTotalTotal = request.RetrievalResults.Sum(s => s.Embedding.GetTotalTokenCount());
		var embeddingTokensSpent = new TokenUsageInfo(eTotalInput, eTotalTotal);
		var augmentationTokensSpent = new TokenUsageInfo(embeddingTokensSpent.InputTokenCount + request.ReasoningTokensSpent.InputTokenCount, embeddingTokensSpent.TotalTokenCount + request.ReasoningTokensSpent.TotalTokenCount);
		var globalAverageScore = request.RetrievalResults.Any() ? request.RetrievalResults.SelectMany(c => c.FoundChunks).Average(c => c.Score) : 0f;
		
		var result = new RetrievalAugmentationResult
		{
			TotalProcessingTimeSeconds = request.TotalProcessingTimeSeconds,
			MedicalContextLlmResponse = request.MedicalContextLlmResponse,
			ReasoningTokensSpent = request.ReasoningTokensSpent,
			RetrievalResults = request.RetrievalResults,
			MedicalContextConfidence = request.MedicalContextConfidence,
			EmbeddingTokensSpent = embeddingTokensSpent,
			AugmentationTokensSpent = augmentationTokensSpent,
			GlobalAverageScore = globalAverageScore
		};
		return result;
	}
}
