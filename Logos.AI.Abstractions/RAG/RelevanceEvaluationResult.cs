using System.ComponentModel;
using System.Text.Json.Serialization;
using Logos.AI.Abstractions.Validation;
namespace Logos.AI.Abstractions.RAG;
[Description("Результат оцінки релевантності чанків документа. Включає загальну категорію релевантності, числову оцінку, список ідентифікаторів корисних чанків та пояснення рішення моделі.")]
public record RelevanceEvaluationResult
{
	[Description("Загальна оцінка релевантності документа: High, Medium, Low, Irrelevant")]
	public required string RelevanceLevel { get; init; }
	
	[Description("Середня числова оцінка корисності (0.0 - 1.0)")]
	public required double Score { get; init; }
	
	[Description("Список ідентифікаторів (Guid) лише тих фрагментів, які містять корисну інформацію.")]
	public required List<Guid> RelevantChunkIds { get; init; } = new();
	
	[Description("Коротке пояснення: чому обрано ці фрагменти або чому відхилено документ.")]
	public required string Reasoning { get; init; }
	
	[JsonIgnore] 
	[Description("Швидка перевірка: чи є хоч щось корисне.")]
	public bool IsRelevant => RelevantChunkIds.Count > 0 && Score >= 0.5;
	[JsonIgnore]
	public ConfidenceValidationResult ConfidenceValidationResult { get; set; } = new();
}