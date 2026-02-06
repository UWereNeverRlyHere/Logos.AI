using System.ComponentModel;
namespace Logos.AI.Abstractions.Knowledge.Retrieval;
[Description("Представлення точок векторного простору та Payload, для збереження у векторній базі")]
public record VectorPointDto
{
	public string PointId  { get; init; } = string.Empty;
	public required float[] Vector  { get; init; } 
	public Dictionary<string, object> Payload { get; init; } = new();
}
