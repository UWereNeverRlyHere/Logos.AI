using System.ComponentModel;
namespace Logos.AI.Abstractions.Knowledge.Entities;
public sealed class DocumentContent
{
	[Description("Ідентифікатор документа - це і Primary Key, і Foreign Key (Shared Primary Key)")]
	public Guid DocumentId { get; init; } = Guid.NewGuid();
	[Description("Дані файлу, у масиві байтів")]
	public byte[] Data { get; init; } = [];
	[Description("Розширення файлу")]
	public string? FileExtension { get; private set; } // pdf, docx
	public Document Document { get; private set; } = null!;
	// Конструктор для EF
	private DocumentContent() { }

	public DocumentContent(Guid documentId, byte[] data, string? extension)
	{
		DocumentId = documentId;
		Data = data;
		FileExtension = extension;
	}
}