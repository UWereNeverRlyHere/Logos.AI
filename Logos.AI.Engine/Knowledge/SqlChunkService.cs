using Logos.AI.Abstractions.Domain.Knowledge;
using Logos.AI.Engine.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.Knowledge;

/// <summary>
/// Сервіс для збереження "сирих" текстів та метаданих у SQLite.
/// Це наш "холодний" архів, з якого можна відновити Qdrant.
/// </summary>
public class SqlChunkService(LogosDbContext dbContext, ILogger<SqlChunkService> logger)
{
	/// <summary>
	/// Зберігає документ та його фрагменти в базу.
	/// </summary>
	public async Task<Guid> SaveDocumentAsync(
		string                        fileName,
		string                        filePath,
		List<(int Page, string Text)> chunksWithPages,
		CancellationToken             ct = default)
	{
		// 1. Перевірка дублікатів
		var existing = await dbContext.Documents
			.FirstOrDefaultAsync(d => d.FileName == fileName, ct);

		if (existing != null)
		{
			logger.LogWarning("Document {FileName} already exists. Skipping SQL save.", fileName);
			return existing.Id;
		}

		// 2. Створення документа
		var document = new Document
		{
			Id = Guid.NewGuid(),
			FileName = fileName,
			FilePath = filePath,
			UploadedAt = DateTime.UtcNow,
			IsProcessed = true,
			FileSizeBytes = File.Exists(filePath) ? new FileInfo(filePath).Length : 0
		};

		// 3. Мапінг чанків з правильними сторінками
		var chunksEntities = chunksWithPages.Select(c => new DocumentChunk
		{
			Id = Guid.NewGuid(),
			DocumentId = document.Id,
			PageNumber = c.Page, 
			Content = c.Text,
			TokenCount = c.Text.Length / 4
		}).ToList();

		document.Chunks = chunksEntities;

		// 4. Збереження
		dbContext.Documents.Add(document);
		await dbContext.SaveChangesAsync(ct);

		logger.LogInformation("Saved document {FileName} with {Count} chunks (Pages preserved).", fileName, chunksEntities.Count);
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
