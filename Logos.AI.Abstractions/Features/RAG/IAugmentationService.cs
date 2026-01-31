using Logos.AI.Abstractions.Features.Knowledge;
namespace Logos.AI.Abstractions.Features.RAG;

public interface IAugmentationService
{
	Task<ICollection<KnowledgeChunk>> RetrieveContextAsync(IEnumerable<string> queries, CancellationToken ct = default);
}
