namespace Logos.AI.Abstractions.Diagnostics;

public record PatientMetaData
{
	public Guid Guid { get; init; }
	public string Gender { get; init; }
	public DateTime DateOfBirth { get; init; }
	public string? Race { get; init; }
	public string? Ethnicity { get; init; }
	public ICollection<string> Diagnosis { get; init; } = new HashSet<string>();
	public ICollection<string> ChronicDiseases { get; init; } = new HashSet<string>();
	public ICollection<string> AdditionalInformation { get; init; } = new List<string>();
	public string Age 
	{
		get
		{
			var age = DateTime.Now.Year - DateOfBirth.Year;
			if (DateTime.Now.DayOfYear < DateOfBirth.DayOfYear) age--;
			return $"{age}р";
		}
	}
}
