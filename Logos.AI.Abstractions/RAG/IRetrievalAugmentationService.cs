using Logos.AI.Abstractions.Knowledge;
namespace Logos.AI.Abstractions.RAG;

public interface IRetrievalAugmentationService
{
	Task<RetrievalAugmentationResult> RetrieveContextAsync(ICollection<string> queries, CancellationToken ct = default);
}
