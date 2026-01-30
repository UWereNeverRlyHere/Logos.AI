using Logos.AI.Abstractions.Features.Validation;
using Logos.AI.Engine.RAG;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.Validation;
/// <summary>
/// Оцінка впевненості (LogProbs)
/// </summary>
public record ConfidenceResult(double Score, string Status, string Details);

public class ConfidenceValidator : IConfidenceValidator
{
    private readonly OpenAiEmbeddingService _embeddingService;
    private readonly ILogger<ConfidenceValidator> _logger;

    public ConfidenceValidator(OpenAiEmbeddingService embeddingService, ILogger<ConfidenceValidator> logger)
    {
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<ConfidenceResult> ValidateConsistencyAsync(List<string> hypotheses, CancellationToken ct = default)
    {
        if (hypotheses.Count < 2) 
            return new ConfidenceResult(1.0, "Н/Д", "Недостатньо даних для порівняння");

        // 1. Отримуємо ембеддінги для всіх варіантів відповідей
        var embeddings = new List<float[]>();
        foreach (var text in hypotheses)
        {
            var vector = await _embeddingService.GetEmbeddingAsync(text, ct);
            embeddings.Add(vector.ToArray());
        }

        // 2. Рахуємо середню косинусну подібність між усіма парами (Self-Consistency Score)
        double totalSimilarity = 0;
        int pairs = 0;

        for (int i = 0; i < embeddings.Count; i++)
        {
            for (int j = i + 1; j < embeddings.Count; j++)
            {
                totalSimilarity += CosineSimilarity(embeddings[i], embeddings[j]);
                pairs++;
            }
        }

        double confidenceScore = totalSimilarity / pairs;

        // 3. Інтерпретація результату
        string status = confidenceScore switch
        {
            > 0.92 => "Висока",
            > 0.85 => "Середня",
            _ => "Низька (Суперечливі результати)"
        };

        return new ConfidenceResult(
            confidenceScore, 
            status, 
            $"Середня схожість відповідей: {confidenceScore:F4}. Кількість ітерацій: {hypotheses.Count}"
        );
    }

    private double CosineSimilarity(float[] V1, float[] V2)
    {
        double dot = 0.0d, mag1 = 0.0d, mag2 = 0.0d;
        for (int n = 0; n < V1.Length; n++)
        {
            dot += V1[n] * V2[n];
            mag1 += Math.Pow(V1[n], 2);
            mag2 += Math.Pow(V2[n], 2);
        }
        return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
    }
}