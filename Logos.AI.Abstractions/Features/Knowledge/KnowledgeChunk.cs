namespace Logos.AI.Abstractions.Features.Knowledge;

public record KnowledgeChunk
{
	public Guid DocumentId { get; init; }
	public string FileName { get; init; } = string.Empty;
	public int PageNumber { get; init; }
	public string Content { get; init; } = string.Empty; 
	public float Score { get; init; } 
}