using Logos.AI.Abstractions.Diagnostics;
namespace Logos.AI.Abstractions.PatientAnalysis;

public record PatientAnalyzeRagRequest
{
	public Guid SessionId { get; init; }
	public PatientMetaData Patient { get; init; }
	public ICollection<string> UserComments { get; init; } = new List<string>();
	public ICollection<Analysis> Analyses { get; init; } = new List<Analysis>();
	public ICollection<PatientAnalysisAugmentation> Augmentations { get; init; } = new List<PatientAnalysisAugmentation>();
}