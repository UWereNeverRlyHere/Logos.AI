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
    private readonly int _vectorSize = 1536; // Для text-embedding-3-small

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
            _logger.LogInformation("Created collection {Name}", _collectionName);
        }
    }

    public async Task UpsertChunkAsync(string pointId, float[] vector, Dictionary<string, object> payload, CancellationToken ct = default)
    {
        // Перетворюємо payload у формат Qdrant
        var qdrantPayload = new MapField<string, Value>();
        foreach (var kvp in payload)
        {
            qdrantPayload.Add(kvp.Key, ConvertToQdrantValue(kvp.Value));
        }

        // PointStruct вимагає Guid або UInt64. 
        // Якщо pointId - це рядок типу "Guid-Index", нам треба або хешувати його в Guid, 
        // або (простіше) використовувати Guid безпосередньо, якщо це можливо.
        // Для спрощення тут припускаємо, що ми передаємо Guid, або генеруємо його.
        // АЛЕ: Qdrant .NET клієнт підтримує і Guid PointId.
        
        // Хай pointId буде Guid. (Ми в контролері це поправимо або захешуємо рядок)
        var pointGuid = GenerateGuidFromSeed(pointId); 

        var point = new PointStruct
        {
            Id = pointGuid,
            Vectors = vector,
            Payload = { qdrantPayload }
        };

        await _client.UpsertAsync(_collectionName, new[] { point }, cancellationToken: ct);
    }
    
    // --- ПОШУК (ВИПРАВЛЕНО) ---
    public async Task<List<KnowledgeChunk>> SearchAsync(float[] vector, int limit = 5, CancellationToken ct = default)
    {
        var results = await _client.SearchAsync(
            collectionName: _collectionName,
            vector: vector,
            limit: (ulong)limit,
            payloadSelector: true, // <--- Твоя правка: забираємо весь Payload
            cancellationToken: ct
        );

        var chunks = new List<KnowledgeChunk>();

        foreach (var point in results)
        {
            // Безпечне витягування даних (Null checks + Defaults)
            var p = point.Payload;
            
            var chunk = new KnowledgeChunk
            {
                DocumentId = TryGetGuid(p, "documentId"),
                FileName = TryGetString(p, "fileName"),
                PageNumber = TryGetInt(p, "pageNumber"),
                Content = TryGetString(p, "fullText"), // Ось текст для LLM
                Score = point.Score
            };
            
            chunks.Add(chunk);
        }

        return chunks;
    }

    // --- Helpers ---

    private Value ConvertToQdrantValue(object value)
    {
        return value switch
        {
            int i => i, // Implicit conversion to Value
            long l => l,
            float f => (double)f, // Qdrant uses double
            double d => d,
            string s => s,
            bool b => b,
            _ => value.ToString()
        };
    }
    
    // Допоміжні методи для безпечного парсингу Payload
    private string TryGetString(MapField<string, Value> payload, string key) 
        => payload.ContainsKey(key) ? payload[key].StringValue : string.Empty;

    private int TryGetInt(MapField<string, Value> payload, string key) 
        => payload.ContainsKey(key) ? (int)payload[key].IntegerValue : 0;

    private Guid TryGetGuid(MapField<string, Value> payload, string key)
        => payload.ContainsKey(key) && Guid.TryParse(payload[key].StringValue, out var g) ? g : Guid.Empty;

    // Стійкий генератор GUID з рядка (щоб ID був однаковим для того самого чанка)
    private Guid GenerateGuidFromSeed(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
    
    // Тимчасово для адмінки (хоча це краще брати з SQL)
    public async Task<List<string>> GetAllUploadedDocumentsAsync()
    {
         return new List<string>(); 
    }
}