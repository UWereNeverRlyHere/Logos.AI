using System.Text.Json.Serialization;
namespace Logos.AI.Abstractions.Domain.Diagnostics;

/// <summary>
/// DDD Value Object: Окремий показник лабораторного аналізу.
/// Відображає рядок з твого JSON.
/// </summary>
public record Indicator
{
	[JsonPropertyName("Name")]
	public required string Name { get; init; } // "Глюкоза"

	[JsonPropertyName("Value")]
	public required string Value { get; init; } // "0.000000" або "негативні"

	[JsonPropertyName("MeasurementName")]
	public string? Unit { get; init; } // "ммоль/л"

	[JsonPropertyName("ValueAccuracy")]
	public string? Accuracy { get; init; } // "=", "<"

	[JsonPropertyName("RefText")]
	public string? ReferenceRange { get; init; } // "Від 0 до 4"
}