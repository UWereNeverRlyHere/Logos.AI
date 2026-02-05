using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Logos.AI.Abstractions.Knowledge.VectorStorage;
namespace Logos.AI.Abstractions.Knowledge;

/// <summary>
/// Запис для проміжного результату після, розбиття документу на чанки
/// </summary>
public record SimpleDocumentChunk
{
	[Description("Ідентифікатор документа. Вираховується з хешу файлу")]
	public Guid DocumentId { get; private init; }
	[Description("Ім'я файлу")]
	public string FileName { get; init; } = string.Empty;
	[Description("Можливий опис документа")]
	[MaxLength(500)]
	public string DocumentDescription { get; init; } = string.Empty;
	[Description("Назва документу (заголовок)")]
	public string DocumentTitle { get; private set; } = string.Empty;
	[Description("Дата індексації")]
	public DateTime IndexedAt { get; init; } = DateTime.UtcNow;
	[Description("Загальна кількість символів в документі")]
	public int TotalCharacters { get; set; } 
	[Description("Загальна кількість слів в документі")]
	public int TotalWords { get; set; } 
	[Description("Фрагменти документа (чанки)")]
	public List<SimpleChunk> Chunks { get; init; } = new();
	public SimpleDocumentChunk(IngestionUploadData data)
	{
		DocumentId = data.DocumentId;
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
