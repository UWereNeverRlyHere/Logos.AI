using Google.Protobuf.Collections;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Logos.AI.Abstractions.Features.Knowledge; 

namespace Logos.AI.Engine.RAG;

public class QdrantService
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantService> _logger;
    private readonly string _collectionName = "logos_knowledge_base";
    private readonly int _vectorSize = 1536; // text-embedding-3-small

    public QdrantService(IConfiguration config, ILogger<QdrantService> logger)
    {
        _logger = logger;
        var host = config["Qdrant:Host"] ?? "localhost";
        var port = int.Parse(config["Qdrant:Port"] ?? "6334");
        _client = new QdrantClient(host, port);
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
            qdrantPayload.Add(kvp.Key, ConvertToValue(kvp.Value));
        }

        var point = new PointStruct
        {
            Id = GenerateGuidFromSeed(pointId),
            Vectors = vector,
            Payload = { qdrantPayload }
        };

        await _client.UpsertAsync(_collectionName, new[] { point }, cancellationToken: ct);
    }

    // === НОВИЙ МЕТОД ПОШУКУ ===
    public async Task<List<KnowledgeChunk>> SearchAsync(float[] vector, int limit = 5, CancellationToken ct = default)
    {
        var results = await _client.SearchAsync(
            collectionName: _collectionName,
            vector: vector,
            limit: (ulong)limit,
            payloadSelector: true, // <--- ВАЖЛИВО: Кажемо Qdrant повернути дані (текст, сторінку)
            cancellationToken: ct
        );

        var chunks = new List<KnowledgeChunk>();
        foreach (var point in results)
        {
            var p = point.Payload;
            chunks.Add(new KnowledgeChunk
            {
                DocumentId = TryGetGuid(p, "documentId"),
                FileName = TryGetString(p, "fileName"),
                PageNumber = TryGetInt(p, "pageNumber"),
                Content = TryGetString(p, "fullText"), // Текст для виводу
                Score = point.Score
            });
        }
        return chunks;
    }

    // --- Helpers ---
    private Value ConvertToValue(object v) => v switch
    {
        int i => i, long l => l, float f => (double)f, double d => d, string s => s, bool b => b, _ => v.ToString()
    };
    private string TryGetString(MapField<string, Value> p, string k) => p.ContainsKey(k) ? p[k].StringValue : "";
    private int TryGetInt(MapField<string, Value> p, string k) => p.ContainsKey(k) ? (int)p[k].IntegerValue : 0;
    private Guid TryGetGuid(MapField<string, Value> p, string k) => p.ContainsKey(k) && Guid.TryParse(p[k].StringValue, out var g) ? g : Guid.Empty;
    private Guid GenerateGuidFromSeed(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        return new Guid(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));
    }
    
    // Заглушка для старого коду, якщо десь викликається
    public async Task<List<string>> GetAllUploadedDocumentsAsync() => new();
}