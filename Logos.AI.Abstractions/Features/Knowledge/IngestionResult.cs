namespace Logos.AI.Abstractions.Features.Knowledge;

public record IngestionResult(
	bool   IsSuccess, 
	string FileName, 
	int    ChunksCount, 
	string Message
);