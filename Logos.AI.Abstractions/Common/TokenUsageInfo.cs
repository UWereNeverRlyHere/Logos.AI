using System.ComponentModel;
namespace Logos.AI.Abstractions.Common;
[Description("Інформація про використання токенів під час виконання операцій з текстом. Включає кількість вхідних токенів та загальну кількість токенів. Використовується для вимірювання витрати ресурсів токенів під час операцій з текстом.")]
public record TokenUsageInfo
{
	[Description("Кількість токенів, використаних для вхідного тексту.")]
	public int InputTokenCount { get; set; }
	[Description("Загальна кількість токенів, використаних під час операції з текстом.")]
	public int TotalTokenCount { get; set; }

	public TokenUsageInfo(int inputTokenCount, int totalTokenCount)
	{
		InputTokenCount = inputTokenCount;
		TotalTokenCount = totalTokenCount;
	}
	public TokenUsageInfo()
	{
	}

}