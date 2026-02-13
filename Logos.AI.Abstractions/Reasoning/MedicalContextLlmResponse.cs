using System.ComponentModel;
using System.Text.Json.Serialization;
namespace Logos.AI.Abstractions.Reasoning;
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
	public required List<string> Queries { get; init; } = new();
	[JsonPropertyName("_thinking_scratchpad")]
	[Description("Internal reasoning: step-by-step analysis of deviations and logic before forming queries.")]
	public string ThinkingScratchpad { get; set; }
}