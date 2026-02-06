using System.ComponentModel;
namespace Logos.AI.Abstractions.Knowledge.Retrieval;

/// <summary>
/// Представляє фрагмент знань (чанк), що зберігається у базі даних.
/// Містить текст, метадані джерела та оцінку релевантності.
/// </summary>
public record KnowledgeChunk
{
	public Guid Id { get; init; } = Guid.NewGuid();

	[Description("ID документу, до якого належить цей фрагмент")]
	public Guid DocumentId { get; init; }

	[Description("Назва документу (заголовок)")]
	public string DocumentTitle { get; init; } = string.Empty;
	[Description("Можливий опис документу")]
	public string DocumentDescription { get; init; } = string.Empty;

	[Description("Ім'я файлу")]
	public string FileName { get; init; } = string.Empty;

	[Description("Номер сторінки, на якій знаходиться фрагмент тексту")]
	public int PageNumber { get; init; }

	[Description("Зміст фрагмента")]
	public string Content { get; init; } = string.Empty;

	[Description("Оцінка релевантності (схожості) знайденого фрагмента (від 0 до 1)")]
	public float Score { get; init; }
}
