using System.ComponentModel;

namespace Logos.AI.Abstractions.Features.Knowledge;

/// <summary>
/// Представляє фрагмент знань (чанк), знайдений у базі даних.
/// Містить текст, метадані джерела та оцінку релевантності.
/// </summary>
public record KnowledgeChunk
{
	/// <summary>
	/// Унікальний ідентифікатор документа, до якого належить цей фрагмент.
	/// </summary>
	[Description("ID документу")]
	public Guid DocumentId { get; init; }
	/// <summary>
	/// Назва документа (заголовок).
	/// </summary>
	[Description("Назва документу (заголовок)")]
	public string DocumentTitle { get; init; } = string.Empty;

	/// <summary>
	/// Ім'я файлу (наприклад, 'report.pdf').
	/// </summary>
	[Description("Ім'я файлу")]
	public string FileName { get; init; } = string.Empty;

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

	/// <summary>
	/// Оцінка релевантності (схожості) знайденого фрагмента (від 0 до 1).
	/// </summary>
	[Description("Оцінка релевантності")]
	public float Score { get; init; } 
}
