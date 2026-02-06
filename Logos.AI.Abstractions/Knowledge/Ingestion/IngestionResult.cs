using System.ComponentModel;
using System.Diagnostics;
using Logos.AI.Abstractions.Common;
using Logos.AI.Abstractions.Knowledge.Entities;
namespace Logos.AI.Abstractions.Knowledge.Ingestion;
/// <summary>
/// Результат масового наповнення бази знань з N документів
/// </summary>
public record BulkIngestionResult
{
	public double TotalProcessingTimeSeconds { get; init; } 
	public int FullTotalTokenCount { get; init; }
	public int FullInputTokenCount { get; init; }
	public int ChunksCount => IngestionResults.Select(x=>x.ChunksCount).Sum();
	public int TotalDocuments => IngestionResults.Count;
	public int SuccessfulDocuments => IngestionResults.Count(x=>x.IsSuccess);
	public int FailedDocuments => IngestionResults.Count(x=>!x.IsSuccess);
	public ICollection<IngestionResult> IngestionResults { get; init; } = new List<IngestionResult>();
	
	public BulkIngestionResult(double totalProcessingTimeSeconds, ICollection<IngestionResult> ingestionResults)
	{
		TotalProcessingTimeSeconds = totalProcessingTimeSeconds;
		IngestionResults = ingestionResults;
		FullInputTokenCount = ingestionResults.Select(x=>x.FullInputTokenCount).Sum();
		FullTotalTokenCount = ingestionResults.Select(x=>x.FullTotalTokenCount).Sum();
	}
	public ICollection<IngestionResult> GetSuccess() => IngestionResults.Where(x=>x.IsSuccess).ToList();
	public ICollection<IngestionResult> GetFail() => IngestionResults.Where(x=>!x.IsSuccess).ToList();
}
/// <summary>
/// Результат наповнення бази знань з одного документу 
/// </summary>
public record IngestionResult
{
	[Description("Загальний час виконання всієї операції (в секундах)")]
	public required double TotalProcessingTimeSeconds { get; init;}
	public bool IsSuccess { get; init; } = false;
	public bool IsAlreadyExists { get; init; } = false;
	public string? FileName { get; set; }
	public string DocumentTitle { get; init; } = string.Empty;
	[Description("Опис документа")]
	public string? DocumentDescription { get; set; }
	public int TotalCharacters { get; init; }
	public int TotalWords { get; init; }
	public int ChunksCount { get; init; }
	public string Message { get; init; } = string.Empty;
	public int FullTotalTokenCount { get; init; }
	public int FullInputTokenCount { get; init; }
	
	public ICollection<IngestionTokenUsageDetails> TokenUsageDetails { get; init; } = new List<IngestionTokenUsageDetails>();
	public static IngestionResult CreateFail(Stopwatch stopwatch, string message)
	{
		return CreateFail(stopwatch.Elapsed.TotalSeconds, message);
	}
	public static IngestionResult CreateFail(double totalProcessingTimeSeconds, string message)
	{
		return new IngestionResult
		{
			Message = message,
			TotalProcessingTimeSeconds = totalProcessingTimeSeconds
		};
	}
	public static IngestionResult CreateSuccess(double totalProcessingTimeSeconds, DocumentChunkingResult chunkingResultResult, ICollection<IngestionTokenUsageDetails> tokenUsageDetails)
	{
		tokenUsageDetails = tokenUsageDetails.OrderBy(x=>x.ContentLength).ToList();
		return new IngestionResult
		{
			TotalProcessingTimeSeconds = totalProcessingTimeSeconds,
			IsSuccess = true,
			Message = "Success",
			FileName = chunkingResultResult.FileName,
			DocumentTitle = chunkingResultResult.DocumentTitle,
			DocumentDescription = chunkingResultResult.DocumentDescription,
			ChunksCount = chunkingResultResult.Chunks.Count,
			TotalWords = chunkingResultResult.TotalWords,
			TotalCharacters = chunkingResultResult.TotalCharacters,
			TokenUsageDetails = tokenUsageDetails,
			FullInputTokenCount = tokenUsageDetails.Select(x=>x.TokenUsageInfo.InputTokenCount).Sum(),
			FullTotalTokenCount = tokenUsageDetails.Select(x=>x.TokenUsageInfo.TotalTokenCount).Sum()
		};
	}
	
	public static IngestionResult CreateSuccess(Stopwatch stopwatch, DocumentChunkingResult chunkingResultResult, ICollection<IngestionTokenUsageDetails> tokenUsageDetails)
	{
		return CreateSuccess(stopwatch.Elapsed.TotalSeconds, chunkingResultResult, tokenUsageDetails);
	}
	public static IngestionResult CreateExists(Stopwatch stopwatch, Document doc)
	{
		return new IngestionResult
		{
			TotalProcessingTimeSeconds = stopwatch.Elapsed.TotalSeconds,
			IsSuccess = true, 
			IsAlreadyExists = true,
			Message = "Document already exists (skipped)",
			FileName = doc.FileName,
			DocumentDescription = doc.DocumentDescription,
			DocumentTitle = doc.DocumentTitle,
			ChunksCount = doc.Chunks.Count,
			TotalCharacters = doc.TotalCharacters,
			TotalWords = doc.TotalWords,
		};
	}
};

public record IngestionTokenUsageDetails
{
	public string Content { get; init; } = string.Empty;
	public long ContentLength => Content.Length;
	public TokenUsageInfo TokenUsageInfo { get; init; }
}