using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Reasoning;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
namespace Logos.AI.Engine.RAG;

// ReSharper disable once InconsistentNaming
public class OpenAIEmbeddingService(EmbeddingClient client, IOptions<OpenAiOptions> options, ILogger<OpenAIEmbeddingService> logger)
{
    private readonly EmbeddingOptions _options = options.Value.Embedding;
    public async Task<EmbeddingResult> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var opt = new EmbeddingGenerationOptions
        {
            Dimensions = _options.Dimensions
        };
        var result = await client.GenerateEmbeddingsAsync([text], opt, ct);
        var usage = result.Value.Usage;
        logger.LogInformation(
            "Embedding usage: Input tokens: {Input}, Total tokens: {Total}",
            usage.InputTokenCount,
            usage.TotalTokenCount
        );

        // Отримуємо перший (і єдиний у нашому випадку) вектор
        var vector = result.Value[0].ToFloats().ToArray();
        return new EmbeddingResult(vector, usage.InputTokenCount, usage.TotalTokenCount);
    }
    public async Task<List<EmbeddingResult>> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var textList = texts.ToList();
        if (textList.Count == 0) return new List<EmbeddingResult>();

        var opt = new EmbeddingGenerationOptions { Dimensions = _options.Dimensions };
    
        // Передаем весь список текстов сразу
        var result = await client.GenerateEmbeddingsAsync(textList, opt, ct);
        var usage = result.Value.Usage;

        return result.Value.Select(e => new EmbeddingResult(
            e.ToFloats().ToArray(), 
            usage.InputTokenCount / textList.Count, 
            usage.TotalTokenCount / textList.Count
        )).ToList();
    }
}