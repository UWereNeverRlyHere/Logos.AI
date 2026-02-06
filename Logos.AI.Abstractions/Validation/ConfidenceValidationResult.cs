namespace Logos.AI.Abstractions.Validation;

/// <summary>
/// Рівень впевненості моделі у власній відповіді.
/// </summary>
public enum ConfidenceLevel
{
	Certain = 4, //  Almost deterministic
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

/// <summary>
/// Результат валідації впевненості моделі.
/// </summary>
public record ConfidenceValidationResult
{
	/// <summary>
	/// Загальний бал впевненості (зазвичай від 0 до 1).
	/// </summary>
	public double Score { get; init; }

	/// <summary>
	/// Чи вважається результат валідації успішним (прохід по порогу).
	/// </summary>
	public bool IsValid { get; init; }

	/// <summary>
	/// Категоріальний рівень впевненості.
	/// </summary>
	public ConfidenceLevel ConfidenceLevel { get; init; }

	/// <summary>
	/// Детальні метрики, що використані для розрахунку.
	/// </summary>
	public ValidationMetrics Metrics { get; init; } = new();

	/// <summary>
	/// Додаткові деталі або пояснення до результату.
	/// </summary>
	public List<string> Details { get; init; } = new();
}

/// <summary>
/// Набір метрик для оцінки впевненості.
/// </summary>
public record ValidationMetrics
{
	/// <summary>
	/// Впевненість на рівні окремих токенів.
	/// </summary>
	public MetricItem TokenConfidence { get; init; } = new();

	/// <summary>
	/// Показник перплексії (Perplexity).
	/// </summary>
	public MetricItem Perplexity { get; init; } = new();

	/// <summary>
	/// Оцінка за методом Ву (Wu Score).
	/// </summary>
	public MetricItem WuScore { get; init; } = new();
}

/// <summary>
/// Окремий елемент метрики.
/// </summary>
public record MetricItem
{
	/// <summary>
	/// Числове значення метрики.
	/// </summary>
	public double Value { get; init; }

	/// <summary>
	/// Текстовий опис або форматоване значення.
	/// </summary>
	public string Description { get; init; } = string.Empty;

	/// <summary>
	/// Рейтинг (рівень впевненості) для конкретної метрики.
	/// </summary>
	public ConfidenceLevel Rating { get; init; }
}