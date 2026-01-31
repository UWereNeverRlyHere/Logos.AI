using System.ComponentModel;
namespace Logos.AI.Abstractions.Features.Knowledge;

public record SimpleDocumentChunk
{
	/// <summary>
	/// Ім'я файлу (наприклад, 'report.pdf').
	/// </summary>
	[Description("Ім'я файлу")]
	public string FileName { get; init; } = string.Empty;
	/// <summary>
	/// Назва документа (заголовок).
	/// </summary>
	[Description("Назва документу (заголовок)")]
	public string DocumentTitle { get; private set; } = string.Empty;
	[Description("Фрагменти документа (чанки)")]
	public List<SimpleChunk> Chunks { get; init; } = new();

	public SimpleDocumentChunk()
	{
	}
	public SimpleDocumentChunk(string fileName)
	{
		FileName = fileName;
		DocumentTitle = fileName;
	}
	public void SetTitleIfNotEmpty(string title)
	{
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