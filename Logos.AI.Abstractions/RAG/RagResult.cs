using System.ComponentModel;
using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.Reasoning;
namespace Logos.AI.Abstractions.RAG;

public record RagResult
{
	[Description("Загальний час виконання всієї операції (в секундах)")]
	public required double TotalProcessingTimeSeconds { get; init; }
	
	[Description("Сумарна кількість токенів, витрачених на модель для аналізу медичних даних")]
	public required TokenUsageInfo ReasoningTokensSpent { get; init; } = new(0, 0);
	
	[Description("Результат LLM аналізу")]
	public required MedicalAnalyzingLLmResponse MedicalAnalyzingLLmResponse { get; init; }
}
