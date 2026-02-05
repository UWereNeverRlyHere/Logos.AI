using System.ComponentModel;
namespace Logos.AI.Abstractions.Knowledge.Entities;
[Description("Представлення чанку з документа")]
public sealed class DocumentChunk
{
	[Description("Ідентифікатор Чанку")]
	public Guid Id { get; set; } = Guid.NewGuid();
	[Description("Ідентифікатор документа (ключ)")]
	public Guid DocumentId { get; set; }
	public Document Document { get; set; } = null!;
	[Description("Номер сторінки, з якого взятий чанк")]
	public int PageNumber { get; set; }
	[Description("Текст чанку")]
	public string Content { get; set; } = string.Empty;
	// Вектор можна не зберігати тут, якщо він в Qdrant, 
	// але іноді корисно мати хеш тексту для перевірки дублікатів.
	[Description("Кількість токенів у чанку")]
	public int TokenCount { get; set; }
}