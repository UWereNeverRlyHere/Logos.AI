namespace Logos.AI.API.Models;

public class DocumentChunk
{
    public string Id { get; set; } // e.g. $"{docId}-{chunkIndex}"
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Text { get; set; }
}