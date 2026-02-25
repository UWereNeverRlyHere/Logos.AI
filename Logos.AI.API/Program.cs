using Logos.AI.Abstractions.Knowledge.Contracts;
using Logos.AI.API.Configuration;
using Logos.AI.API.Middleware;
using Logos.AI.Engine.Extensions;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
	.AddLogosJsonOptions();
  //  .AddNewtonsoftJson(); // Подключаем Newtonsoft.Json

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddLogging(logging => logging.AddConsole());
builder.AddLogosEngine();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
	app.MapOpenApi();
}
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<Logos.AI.Engine.Data.LogosDbContext>();
	await dbContext.Database.MigrateAsync();
	await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
	var qdrantService = scope.ServiceProvider.GetRequiredService<IVectorStorageService>();
	//await qdrantService.RecreateCollectionAsync();
	await qdrantService.EnsureCollectionAsync();
}
await app.RunAsync();
