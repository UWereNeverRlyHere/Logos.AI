using Logos.AI.Abstractions.Knowledge;
using Logos.AI.Abstractions.Knowledge.Retrieval;
using Logos.AI.Abstractions.RAG;
namespace Logos.AI.Abstractions.Diagnostics;

public record PatientAnalysisAugmentation
{
	public string ProtocolSearchTag { get; set; }
	public ICollection<KnowledgeChunk> FoundProtocols { get; set; } = new List<KnowledgeChunk>();
	public static ICollection<PatientAnalysisAugmentation> CreateFromAugmentationResult(RetrievalAugmentationResult data)
	{
		return data.RetrievalResults.Select(retrieval => new PatientAnalysisAugmentation()
			{
				ProtocolSearchTag = retrieval.Query,
				FoundProtocols = retrieval.FoundChunks
					.DistinctBy(c => new { c.DocumentId, c.PageNumber }) // Дедуплікація ТІЛЬКИ в межах одного запиту
					.ToList()
			})
			.Where(x => x.FoundProtocols.Count > 0)
			.ToList();
	}
};
