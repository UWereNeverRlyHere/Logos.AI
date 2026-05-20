using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.Diagnostics;
namespace Logos.AI.Abstractions.PatientAnalysis;

public record PatientAnalyzeRagRequest
{
	public Guid SessionId { get; init; }
	public PatientMetaData Patient { get; init; }
	public ICollection<string> AdditionalInformation { get; init; } = new List<string>();
	public ICollection<DefaultAnalysis> Analyses { get; init; } = new List<DefaultAnalysis>();

	/// <summary>
	/// Мова, якою має бути сформована відповідь LLM ("uk" / "en"). За замовчуванням — українська.
	/// </summary>
	public string Language { get; init; } = SupportedLanguages.Default;
}

public record PatientAnalyzeLLMRequest
{
	public Guid SessionId { get; init; }
	public PatientMetaData Patient { get; init; }
	public ICollection<string> AdditionalInformation { get; init; } = new List<string>();
	public ICollection<Analysis> Analyses { get; init; } = new List<Analysis>();

	/// <summary>
	/// Мова, якою має бути сформована відповідь LLM ("uk" / "en").
	/// </summary>
	public string Language { get; init; } = SupportedLanguages.Default;

	public PatientAnalyzeLLMRequest(PatientAnalyzeRagRequest request)
	{
		SessionId = request.SessionId;
		Patient = request.Patient;
		AdditionalInformation = request.AdditionalInformation;
		Language = SupportedLanguages.Normalize(request.Language);
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