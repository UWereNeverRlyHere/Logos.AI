using System.ComponentModel;
using System.Text.Json.Serialization;
namespace Logos.AI.Abstractions.Reasoning;

public record MedicalContextReasoningResult
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
