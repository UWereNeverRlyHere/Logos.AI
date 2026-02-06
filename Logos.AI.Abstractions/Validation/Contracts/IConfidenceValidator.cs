using Logos.AI.Abstractions.Reasoning;
namespace Logos.AI.Abstractions.Validation.Contracts;

/// <summary>
/// Інтерфейс для валідації впевненості результатів міркування моделі.
/// </summary>
public interface IConfidenceValidator
{
	/// <summary>
	/// Виконує асинхронну валідацію впевненості на основі отриманого результату міркування.
	/// </summary>
	/// <param name="reasoningResult">Результат міркування моделі.</param>
	/// <returns>Результат валідації впевненості.</returns>
	Task<ConfidenceValidationResult> ValidateAsync(IReasoningResult reasoningResult);
}
