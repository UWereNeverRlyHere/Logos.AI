using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Reasoning;
namespace Logos.AI.Abstractions.PatientAnalysis;

public record AugmentedPatientAnalyze
{
	public required PatientAnalyzeRagRequest PatientAnalyzeData { get; init; } 
	public required MedicalContextLlmResponse ProposedMedicalContext { get; init; }
	public required ICollection<RetrievalResult> RetrievalResults { get; init; }
}
