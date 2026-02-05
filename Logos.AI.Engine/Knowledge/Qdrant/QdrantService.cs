using Google.Protobuf.Collections;
using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.Knowledge;
using Logos.AI.Abstractions.Knowledge._Contracts;
using Logos.AI.Abstractions.Knowledge.VectorStorage;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
namespace Logos.AI.Engine.Knowledge.Qdrant;
public class QdrantService : IVectorStorageService
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
        _logger.LogInformation("Ensuring Qdrant collection '{CollectionName}' exists...", _collectionName);
        var collections = await _client.ListCollectionsAsync(ct);
        if (!collections.Contains(_collectionName))
        {
            await _client.CreateCollectionAsync(_collectionName, new VectorParams { Size = (ulong)_vectorSize, Distance = Distance.Cosine }, cancellationToken: ct);
            _logger.LogInformation("Qdrant collection '{CollectionName}' created", _collectionName);
        }
        else
        {
            _logger.LogInformation("Qdrant collection '{CollectionName}' already exists", _collectionName);
        }
    }
    public async Task UpsertChunksAsync(IEnumerable<QdrantUpsertData> points, CancellationToken ct = default)
    {
        var pointStructs = points.Select(p =>
        {
            var qdrantPayload = new MapField<string, Value>();
            foreach (var kvp in p.Payload)
            {
                qdrantPayload.Add(kvp.Key, kvp.Value.ToQdrantValue());
            }

            return new PointStruct
            {
                // Используем GuidUtils для генерации стабильного Guid из строкового ID чанка
                Id = GuidUtils.GenerateGuidFromSeed(p.PointId),
                Vectors = p.Vector,
                Payload = { qdrantPayload }
            };
        }).ToList();

        if (pointStructs.Count > 0)
        {
            await _client.UpsertAsync(_collectionName, pointStructs, cancellationToken: ct);
        }
    }

    public async Task<List<KnowledgeChunk>> SearchAsync(float[] vector, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching Qdrant with threshold {OptionsMinScore}...", _options.MinScore);

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
        _logger.LogInformation("Found {Count} chunks above threshold", results.Count);

        return chunks;
    }
}