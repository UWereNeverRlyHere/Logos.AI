using Logos.AI.Abstractions.Knowledge.Ingestion;
namespace Logos.AI.Abstractions.Knowledge.Contracts;

public interface IDocumentChunkService
{
	/// <summary>
	/// Намагається розбити документ на фрагменти (чанки).
	/// </summary>
	/// <param name="uploadDto">Дані завантаженого файлу.</param>
	/// <param name="documentChunkingResult">Результат розбиття (вихідний параметр).</param>
	/// <param name="error">Повідомлення про помилку, якщо розбиття не вдалося (вихідний параметр).</param>
	/// <returns>True, якщо розбиття успішне; інакше false.</returns>
	bool TryChunkDocument(IngestionUploadDto uploadDto, out DocumentChunkingResult documentChunkingResult, out string error);
}
