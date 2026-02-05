using Logos.AI.Abstractions.Knowledge;

namespace Logos.AI.Abstractions.Knowledge.Contracts;

public interface IDocumentChunkService
{
	/// <summary>
	/// Намагається розбити документ на фрагменти (чанки).
	/// </summary>
	/// <param name="uploadData">Дані завантаженого файлу.</param>
	/// <param name="simpleDocumentChunk">Результат розбиття (вихідний параметр).</param>
	/// <param name="error">Повідомлення про помилку, якщо розбиття не вдалося (вихідний параметр).</param>
	/// <returns>True, якщо розбиття успішне; інакше false.</returns>
	bool TryChunkDocument(IngestionUploadData uploadData, out SimpleDocumentChunk simpleDocumentChunk, out string error);
}
