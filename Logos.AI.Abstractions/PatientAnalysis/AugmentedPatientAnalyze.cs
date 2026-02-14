using System.ComponentModel;
using System.Text.Json.Serialization;
using Logos.AI.Abstractions.RAG;
using Logos.AI.Abstractions.Reasoning;

namespace Logos.AI.Abstractions.PatientAnalysis;

public record AugmentedPatientAnalyze
{
    [Description("Дані для аналізу пацієнта")]
    public required PatientAnalyzeRagRequest PatientAnalyzeData { get; init; } 
    
    [Description("Попередній (можливий) діагноз пацієнта")]
    public required PreliminaryHypothesisDto PreliminaryDiagnosticHypothesis { get; init; }
    
    [Description("Результати пошуку релевантного контексту для пацієнта")]
    public required ICollection<ContextRetrievalDto> RetrievalResults { get; init; }
}

public record ContextRetrievalDto
{
    [Description("Запит для пошуку контексту")]
    public required string SearchQuery { get; init; }
    
    [Description("Знайдені фрагменти контексту")]
    public required ICollection<ContextChunkDto> FoundChunks { get; init; }

    /// <summary>
    /// Фабричний метод для перетворення "важких" результатів пошуку у легкі DTO для LLM.
    /// </summary>
    public static ICollection<ContextRetrievalDto> CreateFromRetrievalResults(ICollection<RetrievalResult> retrievalResults)
    {
        return retrievalResults.Select(result => new ContextRetrievalDto
            {
                SearchQuery = result.Query,
                // Мапимо кожен знайдений чанк у легкий DTO
                FoundChunks = result.FoundChunks.Select(chunk => new ContextChunkDto
                {
                    Title = chunk.DocumentTitle, 
                    PageNumber = chunk.PageNumber,
                    Content = chunk.Content,
                    Score = chunk.Score
                }).ToList()
            })
            .ToList();
    }
}

public record ContextChunkDto
{
    [Description("Назва документу (заголовок)")]
    public required string Title { get; init; }       
    
    [Description("Номер сторінки, на якій знаходиться фрагмент тексту")]
    public int PageNumber { get; init; }
    
    [Description("Зміст фрагмента")]
    public required string Content { get; init; }     
    
    [Description("Оцінка релевантності (схожості) знайденого фрагмента (від 0 до 1)")]
    public required float Score { get; init; }
}

/// <summary>
/// Очищена версія гіпотези для подачі в LLM (без технічних прапорців типу RequiresComplexAnalysis).
/// </summary>
public record PreliminaryHypothesisDto
{

    [Description("Попередній висновок/причина")]
    public required string Reason { get; init; }

    [Description("Пошукові запити, що використовувалися")]
    public required List<string> Queries { get; init; }

    [Description("Ланцюжок думок (Thinking Process)")]
    public string? ThinkingScratchpad { get; init; }
    [JsonIgnore] 
    public bool RequiresComplexAnalysis { get; init; }

    public static PreliminaryHypothesisDto FromResponse(MedicalContextLlmResponse fullResponse)
    {
        return new PreliminaryHypothesisDto
        {
            Reason = fullResponse.Reason,
            Queries = fullResponse.Queries,
            ThinkingScratchpad = fullResponse.ThinkingScratchpad,
            RequiresComplexAnalysis = fullResponse.RequiresComplexAnalysis
        };
    }
}