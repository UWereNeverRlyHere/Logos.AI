using Logos.AI.Abstractions.Domain.Diagnostics;
namespace Logos.AI.Abstractions.Features.Validation;

public record BiologicalValidationResult
{
	/*public BiologicalValidationResult ValidateIndicator(Indicator indicator)
	{
		// 1. Спроба парсингу в число
		if (double.TryParse(indicator.Value, out double numericValue))
		{
			// Якщо це число - використовуємо К-С тест або перевірку діапазону
			return ValidateNumeric(numericValue, indicator.ReferenceRange);
		}

		// 2. Якщо це текст (якісний аналіз)
		return ValidateCategorical(indicator.Value, indicator.ReferenceRange);
	}

	private BiologicalValidationResult ValidateCategorical(string value, string? reference)
	{
		// Нормалізація тексту для порівняння
		var valNorm = value.ToLower().Trim();
		var refNorm = reference?.ToLower().Trim() ?? "";

		// Логіка: якщо в нормі має бути "не виявлено", а ми маємо "позитивний" - це аномалія
		bool isAbnormal = (refNorm.Contains("негативн") || refNorm.Contains("не виявлено")) 
			&& (valNorm.Contains("позитивн") || valNorm.Contains("виявлено"));

		return new BiologicalValidationResult(
			!isAbnormal, 
			isAbnormal ? 0.0 : 1.0, 
			isAbnormal ? "Виявлено патологічний маркер" : "Якісний показник у нормі"
		);
	}*/
}
