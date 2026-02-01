using System.ComponentModel;
using System.Text.Json.Serialization;
using Logos.AI.Abstractions.Diagnostics;
namespace Logos.AI.Abstractions.PatientAnalysis;

public record PatientAnalyzeLlmRequest
{
	public Guid SessionId { get; init; }
	public PatientMetaData Patient { get; init; }
	public ICollection<string> UserComments { get; init; } = new List<string>();
	public ICollection<Analysis> Analyses { get; init; } = new List<Analysis>();
	public ICollection<PatientAnalysisAugmentation> Augmentations { get; init; } = new List<PatientAnalysisAugmentation>();
}
public record MedicalContextLlmResponse
{
	[JsonPropertyName("isMedical")]
	[Description("Чи є вхідні дані медичною інформацією")]
	public required bool IsMedical { get; init; } = false;
	[JsonPropertyName("reason")]
	[Description("Пояснення, чому дані не є медичними, або коротка тематика документа")]
	public required string Reason { get; init; } = "empty";
	[JsonPropertyName("queries")]
	[Description("Список пошукових запитів для клінічних протоколів")]
	public required List<string> Queries { get; init; } = new List<string>();
}

/// <summary>
/// Кореневий об'єкт відповіді від AI про стан пацієнта.
/// </summary>
public record MedicalAnalyzingLLmResponse
{
    [Description("Загальний висновок та статус пацієнта")]
    public required ClinicalSummary Summary { get; init; }

    [Description("Список виявлених відхилень або значущих фактів")]
    public required List<string> KeyFindings { get; init; } = [];

    [Description("Клінічні гіпотези або попередні діагнози")]
    public required List<ClinicalHypothesis> Hypotheses { get; init; } = [];

    [Description("План дій: діагностика, лікування, консультації")]
    public required ActionPlan Plan { get; init; }

    [Description("Список використаних джерел (протоколів)")]
    public required List<ClinicalReference> References { get; init; } = [];

    [Description("Повний текст звіту у форматі Markdown для відображення користувачу")]
    public string? FormattedReport { get; init; }
}

public record ClinicalSummary
{
    [Description("Загальний статус здоров'я: Healthy, Warning, Critical")]
    public required string Status { get; init; } // Можна зробити Enum, але для AI краще string

    [Description("Коротке резюме для лікаря/пацієнта (1-2 речення)")]
    public required string ShortConclusion { get; init; }
}

public record ClinicalHypothesis
{
    [Description("Назва можливого стану або діагнозу")]
    public required string Condition { get; init; }

    [Description("Впевненість у гіпотезі: Low, Medium, High")]
    public string Confidence { get; init; } = "Medium";

    [Description("Пояснення, чому зроблено такий висновок")]
    public required string Rationale { get; init; }
}

public record ActionPlan
{
    [Description("Рекомендовані додаткові обстеження")]
    public required List<RecommendationItem> Diagnostics { get; init; } = [];

    [Description("Рекомендовані консультації спеціалістів")]
    public required List<RecommendationItem> Consultations { get; init; } = [];

    [Description("Рекомендації щодо способу життя та лікування")]
    public required List<RecommendationItem> LifestyleAndTherapy { get; init; } = [];
}

public record RecommendationItem
{
    [Description("Суть рекомендації (назва аналізу, дії або поради)")]
    public required string Action { get; init; }

    [Description("Пріоритет виконання: Routine, Urgent")]
    public string Priority { get; init; } = "Routine";

    [Description("Назва протоколу або джерела, на якому базується рекомендація")]
    public string? ProtocolReference { get; init; }
    [Description("Тип джерела рекомендації. Важливо: 'Protocol' - якщо знайдено в наданих документах, 'GeneralPractice' - якщо це загальновідома медична практика, але протоколу в контексті немає.")]
    public required string SourceType { get; init; } // "Protocol" | "GeneralPractice"
    
    [Description("Обґрунтування рекомендації (нащо це робити)")]
    public string? Reasoning { get; init; }
}

public record ClinicalReference
{
    [Description("Ідентифікатор або номер протоколу")]
    public required string SourceId { get; init; }

    [Description("Повна назва джерела")]
    public required string Title { get; init; }
}