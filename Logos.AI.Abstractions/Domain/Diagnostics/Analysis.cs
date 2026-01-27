namespace Logos.AI.Abstractions.Domain.Diagnostics;

public record Analysis
{	
	public string Name { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public DateTime Date { get; init; } = DateTime.Now;
	public ICollection<Indicator> Indicators { get; init; } = new List<Indicator>();
}
