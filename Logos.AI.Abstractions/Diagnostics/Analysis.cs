namespace Logos.AI.Abstractions.Diagnostics;

public record DefaultAnalysis
{	
	public string Name { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public DateTime Date { get; init; } = DateTime.Now;
	public ICollection<Indicator> Indicators { get; init; } = new List<Indicator>();
}

public record Analysis
{	
	public string Name { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public DateTime Date { get; init; } = DateTime.Now;
	public ICollection<NumericIndicator> Indicators { get; init; } = new List<NumericIndicator>();
}