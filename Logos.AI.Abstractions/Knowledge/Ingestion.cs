using System.ComponentModel;
using System.Diagnostics;
using Logos.AI.Abstractions.Common;
namespace Logos.AI.Abstractions.Knowledge;

public record IngestionUploadData
{
	private string _fileName = string.Empty;
	private string _title = string.Empty;
	private string _description = string.Empty;
	public string FileName { get => _fileName; set => _fileName = value; }
	public string FilePath { get; set; } = string.Empty;
	public string Title { get => _title; set => _title = value; }
	public string Description { get => _description; init => _description = value; }
	public byte[] FileData { get; init; } = [];
	public IngestionUploadData(byte[] fileData, string fileName)
	{
		try
		{
			FileData = fileData;
			_fileName = fileName;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}
	public IngestionUploadData(string path) : this(File.ReadAllBytes(path), Path.GetFileName(path))
	{
		FilePath = path;
	}
	public IngestionUploadData(string base64Content, string fileName) : this(Convert.FromBase64String(base64Content), fileName)
	{
	}
	public IngestionUploadData SetDescription(string description)
	{
		_description = description;
		return this;
	}
	public IngestionUploadData SetTitle(string title)
	{
		_title = title;
		return this;
	}
	public IngestionUploadData SetFileName(string fileName)
	{
		_fileName = fileName;
		return this;
	}
}

public record BulkIngestionResult
{
	public double TotalProcessingTimeSeconds { get; init; } 
	public int FullTotalTokenCount { get; init; }
	public int FullInputTokenCount { get; init; }
	public int ChunksCount => Ingestions.Select(x=>x.ChunksCount).Sum();
	public int IngestionCount => Ingestions.Count;
	public int IngestionSuccessCount => Ingestions.Count(x=>x.IsSuccess);
	public int IngestionFailCount => Ingestions.Count(x=>!x.IsSuccess);
	public ICollection<IngestionResult> Ingestions { get; init; } = new List<IngestionResult>();
	
	public BulkIngestionResult(double totalProcessingTimeSeconds, ICollection<IngestionResult> ingestions)
	{
		TotalProcessingTimeSeconds = totalProcessingTimeSeconds;
		Ingestions = ingestions;
		FullInputTokenCount = ingestions.Select(x=>x.FullInputTokenCount).Sum();
		FullTotalTokenCount = ingestions.Select(x=>x.FullTotalTokenCount).Sum();
	}
	
	public ICollection<IngestionResult> GetSuccessIngestions() => Ingestions.Where(x=>x.IsSuccess).ToList();
	public ICollection<IngestionResult> GetFailIngestions() => Ingestions.Where(x=>!x.IsSuccess).ToList();
}
public record IngestionResult
{
	[Description("Загальний час виконання всієї операції (в секундах)")]
	public required double TotalProcessingTimeSeconds { get; init;}
	public bool IsSuccess { get; init; } = false;
	public string FileName { get; init; } = string.Empty;
	public string DocumentTitle { get; init; } = string.Empty;
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
	public static IngestionResult CreateSuccess(double totalProcessingTimeSeconds, SimpleDocumentChunk chunkResult, ICollection<IngestionTokenUsageDetails> tokenUsageDetails)
	{
		tokenUsageDetails = tokenUsageDetails.OrderBy(x=>x.ContentLength).Distinct().ToList();
		return new IngestionResult
		{
			TotalProcessingTimeSeconds = totalProcessingTimeSeconds,
			IsSuccess = true,
			Message = "Success",
			FileName = chunkResult.FileName,
			DocumentTitle = chunkResult.DocumentTitle,
			ChunksCount = chunkResult.Chunks.Count,
			TokenUsageDetails = tokenUsageDetails,
			FullInputTokenCount = tokenUsageDetails.Select(x=>x.TokenUsageInfo.InputTokenCount).Sum(),
			FullTotalTokenCount = tokenUsageDetails.Select(x=>x.TokenUsageInfo.TotalTokenCount).Sum()
		};
	}
	
	public static IngestionResult CreateSuccess(Stopwatch stopwatch, SimpleDocumentChunk chunkResult, ICollection<IngestionTokenUsageDetails> tokenUsageDetails)
	{
		return CreateSuccess(stopwatch.Elapsed.TotalSeconds, chunkResult, tokenUsageDetails);
	}
};

public record IngestionTokenUsageDetails
{
	public string Content { get; init; } = string.Empty;
	public long ContentLength => Content.Length;
	public TokenUsageInfo TokenUsageInfo { get; init; }
}