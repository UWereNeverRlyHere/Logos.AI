namespace Logos.AI.Abstractions.Diagnostics;

/// <summary>
/// DDD Value Object: Окремий показник лабораторного аналізу.
/// Відображає рядок з твого JSON.
/// </summary>
public record Indicator
{
	public required string Name { get; init; } // "Глюкоза"

	public required string Value { get; init; } // "0.000000" або "негативні"

	public string? Unit { get; init; } // "ммоль/л"

	public string? Accuracy { get; init; } // "=", "<"

	public string? ReferenceRange { get; init; } // "Від 0 до 4"
}