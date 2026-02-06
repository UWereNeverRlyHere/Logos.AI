using System.ComponentModel;
using System.Text.Json.Serialization;
using Logos.AI.Abstractions.Common;
namespace Logos.AI.Abstractions.RAG;
[Description("Результат виконання операції векторизації тексту. Включає вектори, використану кількість токенів та інформацію про використання токенів.")]
public record EmbeddingResult
{
	[JsonIgnore]
	[Description("Вектори тексту, представлені у вигляді числових значень. Кожне значення вектора відповідає одному слову або словосполученню.")]
	public ICollection<float> Vector { get; set; } = new List<float>();
	
	[Description("Загальна кількість векторів у результаті векторизації.")]
	public long TotalVectors => Vector.Count;
	
	[Description("Інформація про використання токенів під час векторизації тексту.")]
	public TokenUsageInfo EmbeddingTokensSpent { get; set; } = new();
	public EmbeddingResult(ICollection<float> vector, int inputTokenCount, int totalTokenCount)
	{
		Vector = vector;
		EmbeddingTokensSpent = new TokenUsageInfo(inputTokenCount, totalTokenCount);
	}
	public EmbeddingResult()
	{
	}
	public int GetTotalTokenCount() => EmbeddingTokensSpent.TotalTokenCount;
	public int GetInputTokenCount() => EmbeddingTokensSpent.InputTokenCount;
}
