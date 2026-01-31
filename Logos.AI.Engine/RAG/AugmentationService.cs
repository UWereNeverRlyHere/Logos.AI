using Logos.AI.Abstractions.Features.Knowledge;
using Logos.AI.Engine.Knowledge;
using Logos.AI.Engine.Knowledge.Qdrant;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.RAG;
public interface IAugmentationService
{
    // Приймає список запитів (наприклад: "протокол лікування діабету", "глікемія норми")
    Task<List<KnowledgeChunk>> RetrieveContextAsync(IEnumerable<string> queries, CancellationToken ct = default);
}
//Retrieval Augmented — дополнение запроса пользователя найденной релевантной информацией.
public class AugmentationService : IAugmentationService
{
    private readonly OpenAIEmbeddingService _embeddingService;
    private readonly QdrantService _qdrantService;
    private readonly ILogger<AugmentationService> _logger;

    public AugmentationService(
        OpenAIEmbeddingService embeddingService, 
        QdrantService qdrantService,
        ILogger<AugmentationService> logger)
    {
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _logger = logger;
    }

    public async Task<List<KnowledgeChunk>> RetrieveContextAsync(IEnumerable<string> queries, CancellationToken ct = default)
    {
        var allChunks = new List<KnowledgeChunk>();

        // 1. Паралельний пошук для кожного запиту
        // Тут теж можна використати Parallel.ForEachAsync, якщо запитів багато, 
        // але зазвичай їх 3-5, тому Task.WhenAll підійде ідеально.
        var tasks = queries.Select(async query => 
        {
            try 
            {
                // А. Векторизація запиту
                var vectorEnum = await _embeddingService.GetEmbeddingAsync(query, ct);
                var vector = vectorEnum.ToArray();

                // Б. Пошук у Qdrant (повертає топ-3 або топ-5 для цього запиту)
                return await _qdrantService.SearchAsync(vector, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка пошуку для запиту '{Query}'", query);
                return new List<KnowledgeChunk>();
            }
        });

        var results = await Task.WhenAll(tasks);

        // 2. Об'єднання результатів
        foreach (var res in results)
        {
            allChunks.AddRange(res);
        }

        // 3. Дедуплікація (важливо!)
        // Якщо різні запити знайшли один і той самий шматок тексту, нам не треба його дублювати.
        // Використовуємо DistinctBy по DocumentId + PageNumber (або просто по змісту)
        var uniqueChunks = allChunks
            .DistinctBy(c => new { c.DocumentId, c.PageNumber }) // Або c.Content.GetHashCode()
            .OrderByDescending(c => c.Score) // Найбільш релевантні зверху
            .Take(10) // Обмежуємо загальний розмір контексту (наприклад, 10 шматків)
            .ToList();

        return uniqueChunks;
    }
}