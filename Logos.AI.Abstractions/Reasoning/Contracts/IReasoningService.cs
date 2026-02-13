using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.RAG;
namespace Logos.AI.Abstractions.Reasoning.Contracts;

// Базовий інтерфейс для всіх Reasoning сервісів
public interface IReasoningService<in TRequest, TResponse>
{
	// TResponse - це твій MedicalContextReasoningResult (бізнес-дані)
	// А повертаємо ми ReasoningResult<TResponse> (обгортку з LogProbs)
	Task<ReasoningResult<TResponse>> AnalyzeAsync(TRequest request, CancellationToken ct = default);
}

// Конкретний інтерфейс для Medical Context
public interface IMedicalContextReasoningService : IReasoningService<PatientAnalyzeRagRequest, MedicalContextLlmResponse>
{
	Task<ReasoningResult<MedicalContextLlmResponse>> AnalyzeAsync(string                    request,         CancellationToken ct = default);
	Task<ReasoningResult<RelevanceEvaluationResult>> EvaluateRelevanceAsync(ExtendedRetrievalResult extendedRetrievalResult, CancellationToken ct = default);
}

// Конкретний інтерфейс для Clinical Reasoning
public interface IMedicalAnalyzingReasoningService : IReasoningService<PatientAnalyzeRagRequest, MedicalAnalyzingLLmResponse>
{
}
