using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace Logos.AI.Abstractions.Knowledge;
/// <summary>
/// Запис для проміжного результату після, розбиття документу на чанки
/// </summary>
public record SimpleDocumentChunk
{
	/// <summary>
	/// Унікальний ідентифікатор документа
	/// </summary>
	public Guid DocumentId { get; init; } = Guid.NewGuid();

	/// <summary>
	/// Ім'я файлу (наприклад, 'report.pdf').
	/// </summary>
	[Description("Ім'я файлу")]
	public string FileName { get; init; } = string.Empty;
	[Description("Можливий опис документа")]
	[MaxLength(500)]
	public string DocumentDescription { get; init; } = string.Empty;
	/// <summary>
	/// Назва документа (заголовок).
	/// </summary>
	[Description("Назва документу (заголовок)")]
	public string DocumentTitle { get; private set; } = string.Empty;

	[Description("Дата індексації")]
	public DateTime IndexedAt { get; init; } = DateTime.UtcNow;

	[Description("Фрагменти документа (чанки)")]
	public List<SimpleChunk> Chunks { get; init; } = new();

	public SimpleDocumentChunk()
	{
	}
	public SimpleDocumentChunk(IngestionUploadData data)
	{
		FileName = data.FileName;
		DocumentTitle = data.Title;
		DocumentDescription = data.Description;
	}
	public SimpleDocumentChunk(string fileName)
	{
		FileName = fileName;
		DocumentTitle = fileName;
	}
	public void SetTitleIfNotEmpty(string title)
	{
		if (!string.IsNullOrWhiteSpace(DocumentTitle)) return;
		if (!string.IsNullOrWhiteSpace(title)) DocumentTitle = title;
	}
	public void AddChunks(List<SimpleChunk> chunks)
	{
		Chunks.AddRange(chunks);
	}
};

public record SimpleChunk
{
	/// <summary>
	/// Номер сторінки, на якій знаходиться цей фрагмент тексту.
	/// </summary>
	[Description("Номер сторінки")]
	public int PageNumber { get; init; }

	/// <summary>
	/// Текстовий вміст фрагмента.
	/// </summary>
	[Description("Зміст фрагмента")]
	public string Content { get; init; } = string.Empty;
	public SimpleChunk(int pageNumber, string content)
	{
		PageNumber = pageNumber;
		Content = content;
	}
}