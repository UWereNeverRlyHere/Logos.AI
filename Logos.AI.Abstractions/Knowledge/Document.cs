namespace Logos.AI.Abstractions.Knowledge;

public sealed class Document
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string FileName { get; set; } = string.Empty;
	public string DocumentTitle { get; set; } = string.Empty;
	public string DocumentDescription { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty; // Фізичний шлях на диску
	public byte[] ? RawFile { get; set; } // можливо, потім буду файли таки в базі тримати
	public long FileSizeBytes { get; set; }
	public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
	public bool IsProcessed { get; set; } // Чи розпарсили ми його успішно
	
	// Навігаційна властивість EF Core
	public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}