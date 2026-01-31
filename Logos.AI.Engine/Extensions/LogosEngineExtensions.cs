using Logos.AI.Abstractions.Features.Knowledge.Contracts;
using Logos.AI.Abstractions.Features.RAG;
using Logos.AI.Engine.Configuration;
using Logos.AI.Engine.Data;
using Logos.AI.Engine.Knowledge;
using Logos.AI.Engine.Knowledge.Qdrant;
using Logos.AI.Engine.RAG;
using Logos.AI.Engine.Reasoning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Qdrant.Client;
namespace Logos.AI.Engine.Extensions;

public static class LogosEngineExtensions
{
	public static void AddLogosEngine(this IHostApplicationBuilder builder)
	{
		var connectionString = builder.Configuration.GetConnectionString("LogosDatabase");
		builder.Services.AddDbContext<LogosDbContext>(options => options.UseSqlite(connectionString));

		builder.Services.AddHttpClient<OpenAIEmbeddingService>();
		builder.Services.AddSingleton<QdrantService>();
		builder.Services.AddSingleton<RagQueryService>();

		builder.Services.AddScoped<SqlChunkService>();
		builder.Services.AddScoped<PdfChunkService>();

		builder.Services.AddScoped<MedicalContextReasoningService>();
		builder.Services.AddScoped<ClinicalReasoningService>();
		builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
		builder.Services.AddScoped<IAugmentationService, AugmentationService>();

		builder.ConfigureOptions();

		builder.Services.AddSingleton<ChatClient>(sp =>
		{
			var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
			return new ChatClient(options.Model, options.ApiKey);
		});
		builder.Services.AddSingleton<QdrantClient>(sp =>
		{
			var options = sp.GetRequiredService<IOptions<RagOptions>>().Value;
			return new QdrantClient(options.Qdrant.Host, options.Qdrant.Port);
		});
		
	}

	private static void ConfigureOptions(this IHostApplicationBuilder builder)
	{
		builder.Services.Configure<RagOptions>(builder.Configuration.GetSection(RagOptions.SectionName));
		builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
	}
}
