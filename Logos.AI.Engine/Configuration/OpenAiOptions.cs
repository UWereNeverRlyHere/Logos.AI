namespace Logos.AI.Engine.Configuration;

public record OpenAiOptions
{
	public const string SectionName = "OpenAI";
	public string Model { get; init; } = "gpt-4o";
	public string ApiKey { get; init; } = string.Empty;
	public LlmOptions MedicalAnalyzing { get; init; } = new();
	public LlmOptions MedicalContext { get; init; } = new();
	public LlmOptions MedicalRelevance { get; init; } = new();
	public EmbeddingOptions Embedding { get; init; } = new();
}

public record LlmOptions
{
	public string Model { get; init; } = "gpt-4o";
	public string PromptFile { get; init; } = "";
	public int MaxTokens { get; init; } = 1024;
	public bool IncludeLogProbabilities { get; init; } = true;
	public int TopLogProbabilityCount { get; init; } = 5;
	public float Temperature { get; init; } = 0.2f;
	public float TopP { get; init; } = 0.95f;
	public float TopK { get; init; } = 30f;
	public int N { get; init; } = 1;
}
public record EmbeddingOptions
{ 
	public string Model { get; init; } = "text-embedding-3-small";
	public int Dimensions { get; init; } = 1536;
}