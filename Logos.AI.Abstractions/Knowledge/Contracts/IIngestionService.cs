using Logos.AI.Abstractions.Knowledge.Ingestion;
namespace Logos.AI.Abstractions.Knowledge.Contracts;

public interface IIngestionService
{
	/// <summary>
	/// Завантаження одного документу
	/// </summary>
	Task<IngestionResult> IngestFileAsync(IngestionUploadDto uploadDto, CancellationToken ct = default);
	/// <summary>
	/// Завантаження колекції документів
	/// </summary>
	Task<BulkIngestionResult> IngestFilesAsync(ICollection<IngestionUploadDto> uploadData, CancellationToken ct = default);
}