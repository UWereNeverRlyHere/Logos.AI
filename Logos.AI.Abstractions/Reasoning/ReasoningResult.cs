using Logos.AI.Abstractions.Common;
namespace Logos.AI.Abstractions.Reasoning;
/// <summary>
/// Контракт, який гарантує, що результат містить дані для оцінки впевненості.
/// </summary>
public interface IReasoningResult
{
	// Сирі дані для математики
	IReadOnlyList<LogProbToken> LogProbs { get; }
	TokenUsageInfo TokenUsage { get; }
	// Розрахункова властивість (зручно мати одразу)
	double AverageConfidence { get; }
}

public record ReasoningResult<T> : IReasoningResult
{
	// "чистий" бізнес-результат (наприклад, MedicalContextReasoningResult)
	public required T Data { get; init; }
	// Метадані AI
	public required IReadOnlyList<LogProbToken> LogProbs { get; init; } = [];
	public required TokenUsageInfo TokenUsage { get; init; }
	// Реалізація інтерфейсу для валідатора
	public double AverageConfidence => LogProbs.Count > 0 ? Math.Exp(LogProbs.Average(p => p.LogProb)) : 0.0;
	// Додатково: Сирий текст (іноді треба глянути, що там прийшло до парсингу JSON)
	public string? RawContent { get; init; }
}

