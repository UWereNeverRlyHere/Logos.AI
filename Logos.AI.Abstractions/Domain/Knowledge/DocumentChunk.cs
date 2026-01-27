namespace Logos.AI.Abstractions.Domain.Knowledge;
public class DocumentChunk
{
	public Guid Id { get; set; } = Guid.NewGuid();
	// Зв'язок з батьком
	public Guid DocumentId { get; set; }
	public virtual Document Document { get; set; } = null!;

	public int PageNumber { get; set; }
	public string Content { get; set; } = string.Empty;
    
	// Вектор можна не зберігати тут, якщо він в Qdrant, 
	// але іноді корисно мати хеш тексту для перевірки дублікатів.
	public int TokenCount { get; set; }
}