using Logos.AI.Abstractions.RAG;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
namespace Logos.AI.Engine.RAG;

// ReSharper disable once InconsistentNaming
public class OpenAIEmbeddingService(EmbeddingClient client, IOptions<OpenAiOptions> options, ILogger<OpenAIEmbeddingService> logger)
{
    private readonly EmbeddingOptions _options = options.Value.Embedding;

    /*public async Task<ICollection<float>> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        // VERSION - Community SDK 
        var options = new EmbeddingGenerationOptions
        {
            Dimensions = _options.Dimensions
        };
        OpenAIEmbedding embedding = await _client.GenerateEmbeddingAsync(text,options, ct);
        //Usage?
        var vector = embedding.ToFloats().ToArray();
        return vector;
    }*/
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
}