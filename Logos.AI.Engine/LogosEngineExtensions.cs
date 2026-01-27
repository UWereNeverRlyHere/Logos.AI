using Logos.AI.Engine.Data;
using Logos.AI.Engine.RAG;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RAG_Search.Services;

namespace Logos.AI.Engine;

public static class LogosEngineExtensions
{
	public static void AddLogosEngine(this IHostApplicationBuilder builder)
	{
		var connectionString = builder.Configuration.GetConnectionString("LogosDatabase");
		builder.Services.AddDbContext<LogosDbContext>(options => options.UseSqlite(connectionString));
		
		
		builder.Services.AddHttpClient<OpenAIEmbeddingService>();
		builder.Services.AddSingleton<QdrantService>();
		builder.Services.AddSingleton<RagQueryService>();
		builder.Services.AddSingleton<SqlChunkLoaderService>();
	}
}
