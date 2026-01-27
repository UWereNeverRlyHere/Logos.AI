using Logos.AI.Engine.Configuration;
using Logos.AI.Engine.Data;
using Logos.AI.Engine.RAG;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Logos.AI.Engine;

public static class LogosEngineExtensions
{
	public static void AddLogosEngine(this IHostApplicationBuilder builder)
	{
		var connectionString = builder.Configuration.GetConnectionString("LogosDatabase");
		builder.Services.AddDbContext<LogosDbContext>(options => options.UseSqlite(connectionString));
		
		builder.Services.AddHttpClient<OpenAiEmbeddingService>();
		builder.Services.AddSingleton<QdrantService>();
		builder.Services.AddSingleton<RagQueryService>();
		builder.Services.AddScoped<SqlChunkLoaderService>();
		builder.Services.AddScoped<PdfService>();

		builder.ConfigureOptions();
	}

	private static void ConfigureOptions(this IHostApplicationBuilder builder)
	{
		builder.Services.Configure<RagOptions>(builder.Configuration.GetSection(RagOptions.SectionName));
		builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
	}
}
