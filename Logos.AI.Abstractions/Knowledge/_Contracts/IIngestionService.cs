using Logos.AI.Abstractions.Knowledge.VectorStorage;
namespace Logos.AI.Abstractions.Knowledge._Contracts;

public interface IIngestionService
{
	/// <summary>
	/// Завантаження одного документу
	/// </summary>
	Task<IngestionResult> IngestFileAsync(IngestionUploadData uploadData, CancellationToken ct = default);
	/// <summary>
	/// Завантаження колекції документів
	/// </summary>
	Task<BulkIngestionResult> IngestFilesAsync(ICollection<IngestionUploadData> uploadData, CancellationToken ct = default);
}