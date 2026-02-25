using Logos.AI.Abstractions.Diagnostics;
namespace Logos.AI.Abstractions.PatientAnalysis;

public record PatientAnalyzeRagRequest
{
	public Guid SessionId { get; init; }
	public PatientMetaData Patient { get; init; }
	public ICollection<string> UserComments { get; init; } = new List<string>();
	public ICollection<DefaultAnalysis> Analyses { get; init; } = new List<DefaultAnalysis>();
}

public record PatientAnalyzeLLMRequest
{
	public Guid SessionId { get; init; }
	public PatientMetaData Patient { get; init; }
	public ICollection<string> UserComments { get; init; } = new List<string>();
	public ICollection<Analysis> Analyses { get; init; } = new List<Analysis>();

	public PatientAnalyzeLLMRequest(PatientAnalyzeRagRequest request)
	{
		SessionId = request.SessionId;
		Patient = request.Patient;
		UserComments = request.UserComments;
		Analyses = request.Analyses.Select(da => new Analysis
		{
			Date = da.Date,
			Name = da.Name,
			Description = da.Description,
			Indicators = da.Indicators
				.Select(indicator => new NumericIndicator(indicator))
				.ToList()
		}).ToList();
		
	}
}