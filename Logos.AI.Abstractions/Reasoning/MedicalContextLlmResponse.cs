using System.ComponentModel;
namespace Logos.AI.Abstractions.Reasoning;
public record MedicalContextLlmResponse
{
	[Description("Чи є вхідні дані медичною інформацією")]
	public required bool IsMedical { get; init; } = false;
	
	[Description("Чи вимагає цей випадок глибокого клінічного аналізу (Reasoning)? true — якщо є будь-які відхилення, скарги, хронічні хвороби або складні поєднання показників. false — якщо це АБСОЛЮТНО здорова людина без скарг і всі показники в ідеальній нормі.")]
	public required bool RequiresComplexAnalysis { get; init; } 
	// ------------------
	[Description("Пояснення, чому дані не є медичними, або коротка тематика документа")]
	public required string Reason { get; init; } = "empty";
	[Description("Список пошукових запитів для клінічних протоколів")]
	public required List<string> Queries { get; init; } = new();
	[Description("Internal reasoning: step-by-step analysis of deviations and logic before forming queries.")]
	public string ThinkingScratchpad { get; set; }
}