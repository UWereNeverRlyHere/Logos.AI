using System.Text;
using System.Text.Json;
using Logos.AI.Abstractions.Features.Knowledge;
using Logos.AI.Abstractions.Features.PatientAnalysis;
using Logos.AI.Abstractions.Features.RAG;
using Logos.AI.Engine.Knowledge;
using Logos.AI.Engine.RAG;
using Logos.AI.Engine.Reasoning;
using Microsoft.AspNetCore.Mvc;

namespace Logos.AI.API.Controllers;

[Route("rag")]
public class RagController(
	SqlChunkService    sqlChunkService,
	QdrantService            qdrantService,
	RagQueryService          queryService,
	MedicalContextReasoningService  medicalContextReasoningService,
	ClinicalReasoningService clinicalReasoningService,
	IKnowledgeService knowledgeService,
	IConfiguration           config) : Controller
{
	// GET: rag/index
	[HttpGet("index")]
	public async Task<IActionResult> Index()
	{
		// Завантажуємо список документів з SQL (це наше джерело правди для списків)
		var docs = await sqlChunkService.GetAllDocumentsAsync();
		ViewBag.AllDocuments = docs;

		// Повертаємо пустий список результатів, щоб View не ламалася
		return View("Index", new List<KnowledgeChunk>());
	}
	[HttpPost("TestVectorSearch")]
	public async Task<IActionResult> TestVectorSearch([FromBody] AnalyzePatientRequest reqData)
	{
		var processedContext = await medicalContextReasoningService.ProcessAsync(reqData);
		var result = new TestVectorSearchResult(processedContext.Queries);
		result.ExtractedContext.Add("Ниркова недостатність");
		result.ExtractedContext.Add("Гіперкреатинінемія");
		result.ExtractedContext.Add("Гіперурикемія");
		result.ExtractedContext.Add("Протокол лікування хронічної хвороби нирок");
		result.ExtractedContext.Add("Протокол лікування підвищеного рівня сечової кислоти");
		try
		{
			await qdrantService.EnsureCollectionAsync();
			var searchTasks = result.ExtractedContext
				.Select(context => queryService.SearchAsync(context))
				.ToList();

			var results = await Task.WhenAll(searchTasks);
			foreach (var r in results)
			{
				result.AddResults(r);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
		result.SortByScore();
		return Ok(result);
	}

	[HttpPost("TestClinicalReasoning")]
	public async Task<IActionResult> TestClinicalReasoning([FromBody] AnalyzePatientRequest reqData)
	{
		var processedContext = await medicalContextReasoningService.ProcessAsync(reqData);
		var result = new TestVectorSearchResult(processedContext.Queries);
		try
		{
			await qdrantService.EnsureCollectionAsync();
			var searchTasks = result.ExtractedContext
				.Select(context => queryService.SearchAsync(context))
				.ToList();
			var results = await Task.WhenAll(searchTasks);
			foreach (var r in results)
			{
				result.AddResults(r);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
		result.SortByScore();
		var answer = await clinicalReasoningService.AnalyzeAsync(JsonSerializer.Serialize(reqData), result.Results);

		return Ok(answer);
	}
	// POST: rag/search
	[HttpPost("search")]
	public async Task<IActionResult> Search(string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return RedirectToAction("Index");
		}

		try
		{
			// Переконуємось, що колекція існує перед пошуком
			await qdrantService.EnsureCollectionAsync();

			// 1. Виконуємо пошук через RagQueryService
			var results = await queryService.SearchAsync(query);

			// 2. Форматуємо результати для відображення (так як немає LLM генерації)
			if (results.Count > 0)
			{
				var sb = new StringBuilder();
				sb.AppendLine($"Found {results.Count} relevant fragments:\n");

				foreach (var item in results)
				{
					sb.AppendLine($"--- Page {item.PageNumber} (Score: {item.Score:F2}) ---");
					sb.AppendLine(item.Content);
					sb.AppendLine(); // Пустий рядок між фрагментами
				}

				ViewBag.Answer = sb.ToString();
				ViewBag.SourceDocuments = string.Join(", ", results.Select(r => r.FileName).Distinct());
			}
			else
			{
				ViewBag.Answer = "No relevant information found in the knowledge base.";
			}

			// 3. Оновлюємо список документів для сайдбару
			var docs = await sqlChunkService.GetAllDocumentsAsync();
			ViewBag.AllDocuments = docs;

			return View("Index", results);
		}
		catch (Exception ex)
		{
			ViewBag.Message = $"Error during search: {ex.Message}";
			var docs = await sqlChunkService.GetAllDocumentsAsync();
			ViewBag.AllDocuments = docs;
			return View("Index", new List<KnowledgeChunk>());
		}
	}

	// Зберігаємо старий метод Query для сумісності, якщо він десь викликається
	[HttpPost("query")]
	public async Task<IActionResult> Query(string question)
	{
		return await Search(question);
	}

	// POST: rag/upload
	[HttpPost("upload")]
	public async Task<IActionResult> Upload(IFormFile file)
	{
		if (file == null || file.Length == 0)
		{
			ViewBag.Message = "Please select a PDF file.";
			return await Index();
		}

		try
		{
			// 1. Зберігаємо файл фізично
			var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
			if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

			// Генеруємо унікальне ім'я файлу
			var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
			var filePath = Path.Combine(uploadsFolder, uniqueFileName);

			await using (var fs = new FileStream(filePath, FileMode.Create))
			{
				await file.CopyToAsync(fs);
			}

			var res =await knowledgeService.IngestFileAsync(new IngestionUploadData(filePath));

			if (res.IsSuccess) return Ok(new { message = $"Uploaded {file.FileName}", chunks = res.ChunksCount });
			return BadRequest(res.Message);
		}
		catch (Exception ex)
		{
			// Логування помилки
			ViewBag.Message = $"Error: {ex.Message}";
		}

		// Оновлюємо список документів і повертаємо View
		var finalDocs = await sqlChunkService.GetAllDocumentsAsync();
		ViewBag.AllDocuments = finalDocs;

		return View("Index", new List<KnowledgeChunk>());
	}
}
