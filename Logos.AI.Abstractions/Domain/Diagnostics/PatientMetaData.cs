namespace Logos.AI.Abstractions.Domain.Diagnostics;

public record PatientMetaData
{
	public Guid Guid { get; init; }
	public string Gender { get; init; }
	public string Age { get; init; }
	public string? Race { get; init; }
	public string? Ethnicity { get; init; }
	public ICollection<string> Diagnosis { get; init; } = new HashSet<string>();
	public ICollection<string> ChronicDiseases { get; init; } = new HashSet<string>();
	public ICollection<string> AdditionalInformation { get; init; } = new List<string>();
}
