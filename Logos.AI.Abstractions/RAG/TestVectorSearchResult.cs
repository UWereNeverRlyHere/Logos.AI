using Logos.AI.Abstractions.Knowledge;
namespace Logos.AI.Abstractions.RAG;

public record TestVectorSearchResult
{
	public List<string> ExtractedContext { get; init; } = new List<string>();
	public List<KnowledgeChunk> Results { get; init; } = new List<KnowledgeChunk>();
	
	public void SortByScore()
	{
		Results.Sort((a, b) => b.Score.CompareTo(a.Score));
	}
	public TestVectorSearchResult(List<string> extractedContex)
	{
		this.ExtractedContext = extractedContex;
	}

	public void AddResults(List<KnowledgeChunk> results)
	{
		Results.AddRange(results);
	}
}
