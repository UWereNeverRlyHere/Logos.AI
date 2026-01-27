namespace Logos.AI.Engine.Configuration;

public record RagOptions
{
	public const string SectionName = "Rag"; 
	public int ChunkSizeWords { get; init; } = 300; 
	public int ChunkOverlapWords { get; init; } = 50;
	public float MinScore { get; init; } = 0.5f;
	public ulong TopK { get; init; } = 5;
	public QdrantOptions Qdrant { get; init; } = new();
}

public record QdrantOptions
{
	public string Host { get; init; } = "localhost";
	public string CollectionName{ get; init; } = "logos_knowledge_base";
	public int VectorSize{ get; init; } = 1536;
	public int Port { get; init; } = 6334;
	public string ApiKey { get; init; } = string.Empty;
}
