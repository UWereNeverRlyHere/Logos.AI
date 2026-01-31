namespace Logos.AI.Abstractions.Features.Knowledge.Contracts;

public interface IKnowledgeService
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