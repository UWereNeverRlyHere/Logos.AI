using Logos.AI.Abstractions.PatientAnalysis;
namespace Logos.AI.Abstractions.Reasoning;

// Базовий інтерфейс для всіх Reasoning сервісів
public interface IReasoningService<in TRequest, TResponse>
{
	// TResponse - це твій MedicalContextReasoningResult (бізнес-дані)
	// А повертаємо ми ReasoningResult<TResponse> (обгортку з LogProbs)
	Task<ReasoningResult<TResponse>> AnalyzeAsync(TRequest request, CancellationToken ct = default);
}

// Конкретний інтерфейс для Medical Context
public interface IMedicalContextReasoningService
	: IReasoningService<PatientAnalyzeLlmRequest, MedicalContextLlmResponse>
{
}

// Конкретний інтерфейс для Clinical Reasoning
public interface IMedicalAnalyzingReasoningService : IReasoningService<PatientAnalyzeLlmRequest, MedicalAnalyzingLLmResponse> 
{
}