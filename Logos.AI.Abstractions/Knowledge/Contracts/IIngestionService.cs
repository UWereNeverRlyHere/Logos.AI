namespace Logos.AI.Abstractions.Knowledge.Contracts;

public interface IIngestionService
{
	/// <summary>
	/// Завантаження одного документу
	/// </summary>
	Task<IngestionResult> IngestFileAsync(IngestionUploadData uploadData, CancellationToken ct = default);
	/// <summary>
	/// Завантаження колекції документів
	/// </summary>
	Task<ICollection<IngestionResult>> IngestFilesAsync(ICollection<IngestionUploadData> uploadData, CancellationToken ct = default);
}