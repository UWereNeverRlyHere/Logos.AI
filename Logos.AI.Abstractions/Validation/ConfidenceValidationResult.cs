using System.ComponentModel;

namespace Logos.AI.Abstractions.Validation;

/// <summary>
/// Рівень впевненості моделі у власній відповіді.
/// </summary>
public enum ConfidenceLevel
{
	/// <summary>
	/// Майже детерміновано (впевнено).
	/// </summary>
	[Description("Майже детерміновано (впевнено)")]
	Certain = 4,

	/// <summary>
	/// Висока впевненість. Модель "не сумнівалася" у жодному токені.
	/// </summary>
	[Description("Висока впевненість. Модель не сумнівалася у жодному токені.")]
	High = 3,

	/// <summary>
	/// Середня впевненість. Були деякі вагання, але загалом контекст зрозумілий.
	/// </summary>
	[Description("Середня впевненість. Були деякі вагання, але загалом контекст зрозумілий.")]
	Medium = 2,

	/// <summary>
	/// Низька впевненість. Є ризик галюцинації або дані неоднозначні.
	/// </summary>
	[Description("Низька впевненість. Є ризик галюцинації або дані неоднозначні.")]
	Low = 1,

	/// <summary>
	/// Критично низька. Модель "ворожила". Відповідь небезпечна.
	/// </summary>
	[Description("Критично низька. Модель ворожила. Відповідь небезпечна.")]
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
	[Description("Загальний бал впевненості (зазвичай від 0 до 1).")]
	public double Score { get; init; }

	/// <summary>
	/// Чи вважається результат валідації успішним (прохід по порогу).
	/// </summary>
	[Description("Чи вважається результат валідації успішним (прохід по порогу).")]
	public bool IsValid { get; init; }

	/// <summary>
	/// Категоріальний рівень впевненості.
	/// </summary>
	[Description("Категоріальний рівень впевненості.")]
	public ConfidenceLevel ConfidenceLevel { get; init; }

	/// <summary>
	/// Детальні метрики, що використані для розрахунку.
	/// </summary>
	[Description("Детальні метрики, що використані для розрахунку.")]
	public ValidationMetrics Metrics { get; init; } = new();

	/// <summary>
	/// Додаткові деталі або пояснення до результату.
	/// </summary>
	[Description("Додаткові деталі або пояснення до результату.")]
	public List<string> Details { get; init; } = new();
	/// <summary>
	/// Тип невпевненості: 'None', 'Focal' (точкова) чи 'Diffuse' (розмита)
	/// </summary>
	[Description("Тип невпевненості: 'None', 'Focal' (точкова) чи 'Diffuse' (розмита).")]
	public string UncertaintyType { get; init; } = "None"; 
	[Description("Пояснення до типу невпевненості.")]
	public string UncertaintyReason { get; init; } = "None"; 
	/// <summary>
	/// Список найслабших токенів
	/// </summary>
	[Description("Список найслабших токенів.")]
	public List<string> TopWeakestLinks { get; init; } = new();
}

/// <summary>
/// Набір метрик для оцінки впевненості.
/// </summary>
public record ValidationMetrics
{
	/// <summary>
	/// Впевненість на рівні окремих токенів.
	/// </summary>
	[Description("Впевненість на рівні окремих токенів.")]
	public MetricItem TokenConfidence { get; init; } = new();

	/// <summary>
	/// Показник перплексії (Perplexity).
	/// </summary>
	[Description("Показник перплексії (Perplexity).")]
	public MetricItem Perplexity { get; init; } = new();

	/// <summary>
	/// Оцінка за методом Ву (Wu Score).
	/// </summary>
	[Description("Оцінка за методом Ву (Wu Score).")]
	public MetricItem WuScore { get; init; } = new();
	/// <summary>
	/// Інформаційна ентропія (міра невизначеності вибору токенів).
	/// </summary>
	[Description("Інформаційна ентропія (міра невизначеності вибору токенів).")]
	public MetricItem Entropy { get; init; } = new();
}

/// <summary>
/// Окремий елемент метрики.
/// </summary>
public record MetricItem
{
	/// <summary>
	/// Числове значення метрики.
	/// </summary>
	[Description("Числове значення метрики.")]
	public double Value { get; init; }

	/// <summary>
	/// Текстовий опис або форматоване значення.
	/// </summary>
	[Description("Текстовий опис або форматоване значення.")]
	public string Description { get; init; } = string.Empty;

	/// <summary>
	/// Рейтинг (рівень впевненості) для конкретної метрики.
	/// </summary>
	[Description("Рейтинг (рівень впевненості) для конкретної метрики.")]
	public ConfidenceLevel Rating { get; init; }
}