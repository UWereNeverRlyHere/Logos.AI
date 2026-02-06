using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace Logos.AI.Abstractions.Knowledge.Ingestion;

[Description("Запис для проміжного результату після, розбиття документу на чанки")]
public record DocumentChunkingResult
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
	public List<TextFragment> Chunks { get; init; } = new();
	public DocumentChunkingResult(IngestionUploadDto dto)
	{
		DocumentId = dto.DocumentId;
		FileName = dto.FileName;
		DocumentTitle = dto.Title;
		DocumentDescription = dto.Description;
	}
	public DocumentChunkingResult(string fileName)
	{
		FileName = fileName;
		DocumentTitle = fileName;
	}
	public void SetTitleIfNotEmpty(string title)
	{
		if (!string.IsNullOrWhiteSpace(DocumentTitle)) return;
		if (!string.IsNullOrWhiteSpace(title)) DocumentTitle = title;
	}
	public void AddChunks(List<TextFragment> chunks)
	{
		Chunks.AddRange(chunks);
	}
};
public record TextFragment
{
	[Description("Номер сторінки, на якій знаходиться фрагмент тексту")]
	public int PageNumber { get; init; }
	[Description("Текстовий вміст фрагмента")]
	public string Content { get; init; } = string.Empty;
	public TextFragment(int pageNumber, string content)
	{
		PageNumber = pageNumber;
		Content = content;
	}
}
