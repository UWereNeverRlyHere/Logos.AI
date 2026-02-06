using Logos.AI.Abstractions.Knowledge.Entities;
using Logos.AI.Abstractions.Knowledge.Ingestion;
namespace Logos.AI.Abstractions.Knowledge.Contracts;

/// <summary>
/// Интерфейс для хранилища документов и их чанков.
/// Абстрагирует конкретную реализацию базы данных (SQLite, Postgres, Mongo и т.д.)
/// </summary>
public interface IStorageService
{
	/// <summary>
	/// Получить документ по ID
	/// </summary>
	Task<Document?> GetDocumentByIdAsync(Guid id, CancellationToken ct = default);

	/// <summary>
	/// Сохранить документ и его чанки
	/// </summary>
	Task<Guid> SaveDocumentAsync(IngestionUploadDto uploadDto, DocumentChunkingResult documentChunkingResult, CancellationToken ct = default);

	/// <summary>
	/// Загрузить все чанки (например для переиндексации)
	/// </summary>
	Task<List<DocumentChunk>> LoadAllChunksAsync(CancellationToken ct = default);

	/// <summary>
	/// Получить список всех документов
	/// </summary>
	Task<List<Document>> GetAllDocumentsAsync(CancellationToken ct = default);
	
	Task SaveChangesAsync(CancellationToken ct = default);
}
