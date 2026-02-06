using Logos.AI.Abstractions.Knowledge.Retrieval;
namespace Logos.AI.Abstractions.Knowledge.Contracts;

public interface IVectorStorageService
{
	/// <summary>
	/// Полностью удаляет и пересоздает коллекцию.
	/// ВНИМАНИЕ: Все данные будут потеряны.
	/// </summary>
	Task RecreateCollectionAsync(CancellationToken ct = default);
	/// <summary>
	/// Перевіряє існування колекції у векторній базі даних та створює її, якщо вона відсутня.
	/// </summary>
	Task EnsureCollectionAsync(CancellationToken ct = default);

	/// <summary>
	/// Завантажує список точок (чанків) у векторну базу даних пакетним запитом.
	/// </summary>
	Task UpsertChunksAsync(IEnumerable<VectorPointDto> points, CancellationToken ct = default);

	/// <summary>
	/// Виконує семантичний пошук схожих фрагментів знань за вектором.
	/// </summary>
	Task<List<KnowledgeChunk>> SearchAsync(float[] vector, CancellationToken ct = default);
}
