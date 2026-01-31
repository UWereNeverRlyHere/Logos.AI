using Logos.AI.Abstractions.Diagnostics;
namespace Logos.AI.Abstractions.PatientAnalysis;

public record AnalyzePatientRequest
{
	public Guid SessionId { get; init; }
	public PatientMetaData Patient { get; init; }
	public ICollection<string> UserComments { get; init; } = new List<string>();
	public ICollection<Analysis> Analyses { get; init; } = new List<Analysis>();
}
public record AnalyzePatientResponse
{
	public Guid SessionId { get; init; }
	public string RecommendationMarkdown { get; init; } 
	// Метадані якості (Quality Attributes)
	public bool IsBiologicallyPlausible { get; init; }
	public double AiConfidenceScore { get; init; }
	public double ContextDiversityScore { get; init; }
	public List<string> Sources { get; init; }
}
