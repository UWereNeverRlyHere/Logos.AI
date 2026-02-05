using Logos.AI.Abstractions.Knowledge.VectorStorage;
namespace Logos.AI.Abstractions.Knowledge._Contracts;

public interface IVectorStorageService
{
	/// <summary>
	/// Перевіряє існування колекції у векторній базі даних та створює її, якщо вона відсутня.
	/// </summary>
	Task EnsureCollectionAsync(CancellationToken ct = default);

	/// <summary>
	/// Завантажує список точок (чанків) у векторну базу даних пакетним запитом.
	/// </summary>
	Task UpsertChunksAsync(IEnumerable<QdrantUpsertData> points, CancellationToken ct = default);

	/// <summary>
	/// Виконує семантичний пошук схожих фрагментів знань за вектором.
	/// </summary>
	Task<List<KnowledgeChunk>> SearchAsync(float[] vector, CancellationToken ct = default);
}
