using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.RAG;
namespace Logos.AI.Abstractions.Reasoning.Contracts;

/// <summary>
/// Базовий інтерфейс для всіх сервісів логічного виводу (Reasoning).
/// </summary>
/// <typeparam name="TRequest">Тип вхідного запиту для аналізу.</typeparam>
/// <typeparam name="TResponse">Тип бізнес-даних, що повертаються у результаті аналізу.</typeparam>
public interface IReasoningService<in TRequest, TResponse>
{
	/// <summary>
	/// Виконує аналіз вхідного запиту за допомогою мовної моделі.
	/// </summary>
	/// <param name="request">Об'єкт запиту з вхідними даними.</param>
	/// <param name="ct">Токен скасування операції.</param>
	/// <returns>Результат аналізу, загорнутий у <see cref="ReasoningResult{TResponse}"/>, що містить бізнес-дані та метадані моделі (наприклад, ймовірності токенів).</returns>
	Task<ReasoningResult<TResponse>> AnalyzeAsync(TRequest request, CancellationToken ct = default);
}

/// <summary>
/// Інтерфейс сервісу для аналізу та формування медичного контексту.
/// </summary>
public interface IMedicalContextReasoningService : IReasoningService<PatientAnalyzeLLMRequest, MedicalContextLlmResponse>
{
	/// <summary>
	/// Аналізує текстовий запит для визначення медичного контексту.
	/// </summary>
	/// <param name="request">Текстовий опис або запит.</param>
	/// <param name="ct">Токен скасування операції.</param>
	/// <returns>Результат аналізу медичного контексту.</returns>
	Task<ReasoningResult<MedicalContextLlmResponse>> AnalyzeAsync(string                    request,         CancellationToken ct = default);

	/// <summary>
	/// Оцінює релевантність отриманих даних із бази знань (RAG) відносно запиту.
	/// </summary>
	/// <param name="retrievalResult">Результати пошуку в базі знань.</param>
	/// <param name="ct">Токен скасування операції.</param>
	/// <returns>Результат оцінки релевантності.</returns>
	Task<ReasoningResult<RelevanceEvaluationResult>> EvaluateRelevanceAsync(RetrievalResult retrievalResult, CancellationToken ct = default);
}

/// <summary>
/// Інтерфейс сервісу для глибокого клінічного аналізу та обґрунтування висновків на основі даних пацієнта.
/// </summary>
public interface IMedicalAnalyzingReasoningService : IReasoningService<AugmentedPatientAnalyze, MedicalAnalyzingLLmResponse>
{
}
