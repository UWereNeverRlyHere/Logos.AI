namespace Logos.AI.Abstractions.Knowledge.VectorStorage;

public record QdrantUpsertData
{
	public string PointId  { get; init; } = string.Empty;
	public required float[] Vector  { get; init; } 
	public Dictionary<string, object> Payload { get; init; } = new();
}
