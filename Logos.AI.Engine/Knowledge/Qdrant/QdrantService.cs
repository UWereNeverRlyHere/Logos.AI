using Google.Protobuf.Collections;
using Logos.AI.Abstractions.Features.Knowledge;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
namespace Logos.AI.Engine.Knowledge.Qdrant;
public class QdrantService
{
    private readonly ILogger<QdrantService> _logger;
    private readonly QdrantClient _client;
    private readonly string _collectionName;
    private readonly int _vectorSize;
    private readonly RagOptions _options; 
    public QdrantService(QdrantClient client, IOptions<RagOptions> options, ILogger<QdrantService> logger)
    {
        _options = options.Value;
        _collectionName = _options.Qdrant.CollectionName;
        _vectorSize = _options.Qdrant.VectorSize;
        _logger = logger;
        _client = client;
    }
    public async Task EnsureCollectionAsync(CancellationToken ct = default)
    {
        var collections = await _client.ListCollectionsAsync(ct);
        if (!collections.Contains(_collectionName))
        {
            await _client.CreateCollectionAsync(_collectionName,
                new VectorParams { Size = (ulong)_vectorSize, Distance = Distance.Cosine },
                cancellationToken: ct);
        }
    }
    public async Task UpsertChunkAsync(string pointId, float[] vector, Dictionary<string, object> payload, CancellationToken ct = default)
    {
        var qdrantPayload = new MapField<string, Value>();
        foreach (var kvp in payload)
        {
            qdrantPayload.Add(kvp.Key, kvp.Value.ToQdrantValue());
        }
  
        var point = new PointStruct
        {
            Id = QdrantMapper.GenerateGuidFromSeed(pointId),
            Vectors = vector,
            Payload = { qdrantPayload }
        };

        await _client.UpsertAsync(_collectionName, [point], cancellationToken: ct);
    }

    public async Task<List<KnowledgeChunk>> SearchAsync(float[] vector, CancellationToken ct = default)
    {
        /*var filter = new Filter
        {
            Must = { // "Must" означає "AND"
                new Condition {
                    Field = new FieldCondition {
                        Key = "fileName", // Фільтруємо за назвою файлу
                        Match = new Match { Keyword = "3191.pdf" } // Тільки цей файл
                    }
                }
            }
        };*/
        var searchParams = new SearchParams()
        {
            Exact = true,
            //HnswEf = 256,
        };
        var results = await _client.SearchAsync(
            collectionName: _collectionName,
            vector: vector,
            limit: _options.Qdrant.TopK,
            payloadSelector: true,
            scoreThreshold: _options.MinScore,
            searchParams: searchParams,
            cancellationToken: ct
            
            //,filter: filter
        );

        var chunks = new List<KnowledgeChunk>();
        foreach (var point in results)
        {
            var p = point.Payload;
            chunks.Add(p.ToKnowledgeDictionary().ToChunk(point.Score));
        }
        return chunks;
    }
}