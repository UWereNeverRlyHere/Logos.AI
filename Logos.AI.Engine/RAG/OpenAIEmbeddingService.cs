using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
namespace Logos.AI.Engine.RAG;

public class OpenAiEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingClient _client;
    private readonly EmbeddingOptions _options;

    public OpenAiEmbeddingService(IOptions<OpenAiOptions> options, HttpClient httpClient)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentNullException("OpenAI API key is missing in configuration.");

        _options = options.Value.Embedding;
        _client = new EmbeddingClient(_options.Model, apiKey);
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<float>> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        // VERSION - Community SDK 
        var options = new EmbeddingGenerationOptions
        {
            Dimensions = _options.Dimensions 
        };
        OpenAIEmbedding embedding = await _client.GenerateEmbeddingAsync(text,options, ct);
        float[] vector = embedding.ToFloats().ToArray();
        return vector.ToList();
    }
}