using System.ComponentModel;
namespace Logos.AI.Abstractions.Validation;

public enum UncertaintyType
{
    /// <summary>
    /// Невизначеність відсутня. Модель впевнена.
    /// </summary>
    None,

    /// <summary>
    /// Точкова (фокальна) невпевненість. 
    /// Проблема в 1-2 конкретних термінах (наприклад, прізвище або назва ліків).
    /// </summary>
    Focal,

    /// <summary>
    /// Дифузна (розмита) невпевненість. 
    /// Множинні провали. Модель "плаває" у темі або втратила контекст.
    /// </summary>
    Diffuse
}
public record UncertaintyResult
{
    private readonly UncertaintyType _type;
    /// <summary>
    /// Тип невпевненості: 'None', 'Focal' (точкова) чи 'Diffuse' (розмита)
    /// </summary>
    [Description("Тип невпевненості: 'None', 'Focal' (точкова) чи 'Diffuse' (розмита).")]

    public UncertaintyType Type
    {
        get => _type;
        init
        {
            _type = value;
            Reason = _type switch
            {
                UncertaintyType.None => "High confidence across all tokens.",
                UncertaintyType.Diffuse => "Multiple low-confidence tokens detected. Model likely lost context or is hallucinating broadly.",
                UncertaintyType.Focal => "Model is confident globally but stumbled on specific terms (rare names or typos).",
                _ => "High confidence across all tokens."
            };
        } 
    }
    [Description("Пояснення до типу невпевненості.")]
    public string Reason { get; private set; } = string.Empty;
    /// <summary>
    /// Список найслабших токенів
    /// </summary>
    [Description("Список найслабших токенів.")]
    public List<string> TopWeakestLinks { get; init; } = new();

    public UncertaintyResult(UncertaintyType type, List<string> topWeakestLinks)
    {
        Type = type;
        TopWeakestLinks = topWeakestLinks;
    }
    public UncertaintyResult() : this(UncertaintyType.None, new List<string>()) { }
}
