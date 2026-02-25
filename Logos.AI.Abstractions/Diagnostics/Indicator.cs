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
public record NumericIndicator
{
	public string Name { get; init; }

	public double Value { get; init; }

	public string? Unit { get; init; }

	public string? Accuracy { get; init; }

	public bool IsOutOfRange { get; init; }
	public double DeviationPercentage { get; init; }
	public string DeviationType { get; init; }
	
	public NumericIndicator(Indicator indicator)
	{
		Name = indicator.Name;
		var normalizedValue = indicator.Value.Trim().Replace(',', '.');
		Value = double.Parse(normalizedValue, System.Globalization.NumberStyles.Any, 
			System.Globalization.CultureInfo.InvariantCulture);
		Unit = indicator.Unit;
		Accuracy = indicator.Accuracy;

		// Парсинг референсного диапазона
		if (!string.IsNullOrWhiteSpace(indicator.ReferenceRange) && 
			TryParseRange(indicator.ReferenceRange.Trim().Replace(",", "."), out var min, out var max))
		{
			// Проверка выхода за пределы нормы
			IsOutOfRange = Value < min || Value > max;

			if (!IsOutOfRange)
			{
				// Значение в норме
				DeviationType = "None";
				DeviationPercentage = 0;
			}
			else if (Value < min)
			{
				// Ниже нормы
				DeviationType = "Lower";
				DeviationPercentage = Math.Round(Math.Abs((Value - min) / min * 100), 2);
			}
			else
			{
				// Выше нормы
				DeviationType = "Upper";
				DeviationPercentage = Math.Round(Math.Abs((Value - max) / max * 100), 2);
			}
		}
		else
		{
			// Если диапазон не распознан
			IsOutOfRange = false;
			DeviationType = "None";
			DeviationPercentage = 0;
		}
	}

	private static bool TryParseRange(string range, out double min, out double max)
	{
		min = max = 0;

		var parts = range.Split(new[] { '-', '–', '—' }, StringSplitOptions.RemoveEmptyEntries);
		
		if (parts.Length == 2 &&
			double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Any, 
				System.Globalization.CultureInfo.InvariantCulture, out min) &&
			double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any, 
				System.Globalization.CultureInfo.InvariantCulture, out max))
		{
			return true;
		}

		return false;
	}
}