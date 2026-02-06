using System.ComponentModel;
using Logos.AI.Abstractions.Knowledge.Ingestion;
namespace Logos.AI.Abstractions.Knowledge.Entities;
[Description("Представлення документа")]
public sealed class Document
{
	[Description("Ідентифікатор документа. Вираховується з хешу контетна самого документа")]
	public required Guid Id { get; init; }
	[Description("Назва файлу, яку передавали при створенні")]
	public string? FileName { get; set; }
	[Description("Назва документа")]
	public required string DocumentTitle { get; init; } = string.Empty;
	[Description("Опис документа")]
	public string? DocumentDescription { get; set; }
	[Description("Розмір документа в байтах")]
	public required long FileSizeBytes { get; init; }
	[Description("Загальна кількість символів в документі")]
	public required int TotalCharacters { get; init; } 
	[Description("Загальна кількість слів в документі")]
	public required int TotalWords { get; init; } 
	[Description("Дата створення документа")]
	public required DateTime UploadedAt { get; init; } = DateTime.UtcNow;
	[Description("Чи документ оброблений")]
	public required bool IsProcessed { get; set; } 
	public DocumentContent? Content { get; private set; }
	
	// Навігаційна властивість EF Core
	public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
	public void SetContent(byte[] data, string? extension)
	{
		Content = new DocumentContent(Id, data, extension);
	}
	public static Document CreateFromSimpleDocumentChunk(IngestionUploadDto uploadDto, DocumentChunkingResult documentChunkingResult)
	{
		return new Document
		{
			Id = documentChunkingResult.DocumentId,
			FileName = documentChunkingResult.FileName,
			DocumentTitle = documentChunkingResult.DocumentTitle,
			DocumentDescription = documentChunkingResult.DocumentDescription,
			FileSizeBytes = uploadDto.FileData.Length,
			TotalCharacters = documentChunkingResult.TotalCharacters,
			TotalWords = documentChunkingResult.TotalWords,
			UploadedAt = documentChunkingResult.IndexedAt,
			IsProcessed = true,
			Content = new DocumentContent(documentChunkingResult.DocumentId, uploadDto.FileData, uploadDto.FileExtension)
		};
	}
}