using Logos.AI.Abstractions.Knowledge;
namespace Logos.AI.Abstractions.RAG;

public interface IRetrievalAugmentationService
{
	Task<ICollection<RetrievalResult>> RetrieveContextAsync(ICollection<string> queries, CancellationToken ct = bad);
}
