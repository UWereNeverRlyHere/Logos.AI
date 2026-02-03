namespace Logos.AI.Abstractions.Validation;

/// <summary>
/// Рівень впевненості моделі у власній відповіді.
/// </summary>
public enum ConfidenceLevel
{
	/// <summary>
	/// Висока впевненість. Модель "не сумнівалася" у жодному токені.
	/// </summary>
	High = 3,

	/// <summary>
	/// Середня впевненість. Були деякі вагання, але загалом контекст зрозумілий.
	/// </summary>
	Medium = 2,

	/// <summary>
	/// Низька впевненість. Є ризик галюцинації або дані неоднозначні.
	/// </summary>
	Low = 1,

	/// <summary>
	/// Критично низька. Модель "ворожила". Відповідь небезпечна.
	/// </summary>
	Uncertain = 0
}

public record ConfidenceValidationResult
{
	private readonly double _score;
	/// <summary>
	/// Чи пройшла відповідь мінімальний поріг якості.
	/// </summary>
	/// <summary>
	/// Числове значення впевненості (0.0 - 1.0).
	/// При встановленні автоматично визначає Level та IsValid.
	/// </summary>
	public double Score
	{
		get => _score;
		init
		{
			_score = value;
            
			// Визначення рівня
			Level = value switch
			{
				>= 0.85 => ConfidenceLevel.High,
				>= 0.60 => ConfidenceLevel.Medium,
				>= 0.40 => ConfidenceLevel.Low,
				_ => ConfidenceLevel.Uncertain
			};

			// Поріг валідності (можна налаштовувати, але 0.5 - це "монетка")
			// Для медицини краще 0.6, але залишимо 0.5 як мінімум
			IsValid = value >= 0.5; 
		}
	}
	public bool IsValid { get; init; }
	/// <summary>
	/// Категорія впевненості (High, Low...).
	/// </summary>
	public ConfidenceLevel Level { get; init; }

	/// <summary>
	/// Детальний список причин, чому оцінка саме така.
	/// </summary>
	public List<string> Details { get; init; } = new();
	
}