using Logos.AI.Abstractions.PatientAnalysis;

namespace Logos.AI.Abstractions.RAG;

public interface IRetrievalAugmentationService
{
    /// <summary>
    /// Виконує базовий (технічний) пошук у векторній базі даних за заданим списком текстових запитів.
    /// Не виконує попереднього аналізу пацієнта чи додаткової валідації результатів.
    /// </summary>
    /// <param name="queries">Колекція пошукових фраз.</param>
    /// <param name="ct">Токен скасування.</param>
    /// <returns>Результат пошуку з чанками та метаданими.</returns>
    Task<ICollection<ExtendedRetrievalResult>> RetrieveContextAsync(ICollection<string> queries, CancellationToken ct = default);

    /// <summary>
    /// Виконує "розумну" аугментацію:
    /// 1. Аналізує дані пацієнта (Medical Context Analysis) для формування пошукових запитів.
    /// 2. Валідує впевненість моделі щодо контексту.
    /// 3. Виконує пошук релевантної інформації.
    /// </summary>
    /// <param name="request">Структурований запит з даними пацієнта.</param>
    /// <param name="ct">Токен скасування.</param>
    /// <returns>Результат пошуку, адаптований під контекст пацієнта.</returns>
    Task<RetrievalAugmentationResult> AugmentAsync(PatientAnalyzeRagRequest request, CancellationToken ct = default);

    /// <summary>
    /// Перевантаження методу <see cref="AugmentAsync(PatientAnalyzeRagRequest, CancellationToken)"/> для роботи з JSON-рядком.
    /// </summary>
    /// <param name="jsonRequest">JSON-рядок, що представляє PatientAnalyzeLlmRequest.</param>
    /// <param name="ct">Токен скасування.</param>
    /// <returns>Результат пошуку.</returns>
    ///Task<RetrievalAugmentationResult> AugmentAsync(string jsonRequest, CancellationToken ct = default);

    /// <summary>
    /// Виконує найбільш повний та точний цикл RAG:
    /// 1. <see cref="AugmentAsync(PatientAnalyzeRagRequest, CancellationToken)"/> (Аналіз + Пошук).
    /// 2. Групування знайдених чанків по документах.
    /// 3. **AI-валідація релевантності** кожного документа відносно запиту пацієнта (Relevance Check).
    /// 4. Фільтрація нерелевантного "шуму".
    /// </summary>
    /// <param name="request">Структурований запит з даними пацієнта.</param>
    /// <param name="ct">Токен скасування.</param>
    /// <returns>Результат пошуку, що містить тільки перевірені та релевантні дані, а також оцінки AI.</returns>
    ///Task<RetrievalAugmentationResult> AugmentValidatedAsync(PatientAnalyzeRagRequest request, CancellationToken ct = default);
}