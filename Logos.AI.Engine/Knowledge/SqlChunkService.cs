using Logos.AI.Abstractions.Knowledge;
using Logos.AI.Abstractions.Knowledge.Contracts;
using Logos.AI.Engine.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.Knowledge;

/// <summary>
/// Сервіс для збереження "сирих" текстів та метаданих у SQLite.
/// Це наш "холодний" архів, з якого можна відновити Qdrant.
/// </summary>
public class SqlChunkService(LogosDbContext dbContext, ILogger<SqlChunkService> logger) : IStorageService
{
	public async Task<Document?> GetDocumentByIdAsync(Guid id, CancellationToken ct = default)
	{
		return await dbContext.Documents
			.Include(d => d.Chunks)
			.AsNoTracking()
			.FirstOrDefaultAsync(d => d.Id == id, ct);
	}

	/// <summary>
	/// Зберігає документ та його фрагменти в базу.
	/// </summary>
	public async Task<Guid> SaveDocumentAsync(
		string                        fileName,
		string                        filePath,
		SimpleDocumentChunk simpleDocumentChunk,
		CancellationToken             ct = default)
	{
		// 1. Перевірка дублікатів
		var existing = await dbContext.Documents
			.FirstOrDefaultAsync(d => d.Id == simpleDocumentChunk.DocumentId, ct);
		if (existing != null)
		{
			logger.LogWarning("Document with hash {Id} already exists. Skipping SQL save.", existing.Id);
			return existing.Id;
		}

		// 2. Створення документа
		var document = new Document
		{
			Id = simpleDocumentChunk.DocumentId,
			FileName = simpleDocumentChunk.FileName,
			DocumentTitle = simpleDocumentChunk.DocumentTitle,
			DocumentDescription = simpleDocumentChunk.DocumentDescription,
			FilePath = filePath,
			UploadedAt = simpleDocumentChunk.IndexedAt,
			IsProcessed = true,
			FileSizeBytes = File.Exists(filePath) ? new FileInfo(filePath).Length : 0
		};

		// 3. Мапінг чанків з правильними сторінками
		var chunksEntities = simpleDocumentChunk.Chunks.Select(c => new DocumentChunk
		{
			Id = Guid.NewGuid(),
			DocumentId = document.Id,
			PageNumber = c.PageNumber, 
			Content = c.Content,
			TokenCount = c.Content.Length / 4
		}).ToList();

		document.Chunks = chunksEntities;

		// 4. Збереження
		dbContext.Documents.Add(document);
		await dbContext.SaveChangesAsync(ct);

		logger.LogInformation("Saved document {FileName} with {Count} chunks (Pages preserved)", fileName, chunksEntities.Count);
		return document.Id;
	}

	/// <summary>
	/// Отримати всі чанки для переіндексації (Re-indexing flow).
	/// </summary>
	public async Task<List<DocumentChunk>> LoadAllChunksAsync(CancellationToken ct = default)
	{
		// AsNoTracking() пришвидшує читання, бо нам не треба відстежувати зміни
		return await dbContext.Chunks
			.AsNoTracking()
			.Include(c => c.Document) // Підтягуємо назву файлу, якщо треба
			.ToListAsync(ct);
	}

	/// <summary>
	/// Отримати список завантажених файлів (для Адмінки).
	/// </summary>
	public async Task<List<Document>> GetAllDocumentsAsync(CancellationToken ct = default)
	{
		return await dbContext.Documents
			.AsNoTracking()
			.OrderByDescending(d => d.UploadedAt)
			.ToListAsync(ct);
	}
}
