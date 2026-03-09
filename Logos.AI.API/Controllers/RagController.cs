using System.Text;
using System.Text.Json;
using Logos.AI.Abstractions.Knowledge.Contracts;
using Logos.AI.Abstractions.Knowledge.Ingestion;
using Logos.AI.Abstractions.Knowledge.Retrieval;
using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.RAG;
using Logos.AI.Engine.Extensions;
using Logos.AI.Engine.RAG;
using Microsoft.AspNetCore.Mvc;
namespace Logos.AI.API.Controllers;
[ApiController]
[Route("rag")]
public class RagController(
	IStorageService      storageService,
	RagOrchestrator       orchestrator,
	IRetrievalAugmentationService retrievalAugmentationService,
	IIngestionService             ingestionService) : Controller
{
	// GET all documents (API)
	[HttpGet("documents")]
	public async Task<IActionResult> GetDocuments()
	{
		var docs = await storageService.GetAllDocumentsAsync();
		return Ok(docs.Select(d => new { d.Id, d.DocumentTitle, d.FileName, d.UploadedAt }));
	}
	
	// GET: rag/index
	[HttpGet("index")]
	public async Task<IActionResult> Index()
	{
		// Завантажуємо список документів з SQL (це наше джерело правди для списків)
		var docs = await storageService.GetAllDocumentsAsync();
		ViewBag.AllDocuments = docs;

		// Повертаємо пустий список результатів, щоб View не ламалася
		return View("Index", new List<KnowledgeChunk>());
	}
	
	// GET: rag/testExamples
	[HttpGet("testExamples")]
	public IActionResult TestExamples()
	{
		return View();
	}

	// POST: rag/analyzeTestPatient/{patientId}
	[HttpPost("analyzeTestPatient/{patientId}")]
	public async Task<IActionResult> AnalyzeTestPatient(string patientId)
	{
		try
		{
			// Формуємо шлях до папки PatientAnalyzeData
			var filePath = Path.Combine(Directory.GetCurrentDirectory(), "PatientAnalyzeData", $"{patientId}.json");
			
			if (!System.IO.File.Exists(filePath))
			{
				return NotFound(new { error = $"File not found: {filePath}" });
			}

			// Читаємо JSON файл
			var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
			var options = LogosJsonExtensions.IndentedOptions;
			// Десеріалізуємо у запит
			var reqData = JsonSerializer.Deserialize<PatientAnalyzeRagRequest>(jsonContent, options);

			if (reqData == null) 
				return BadRequest(new { error = "Invalid JSON format." });

			// Запускаємо оркестратор
			var processedContext = await orchestrator.GenerateResponseAsync(reqData);
			
			return Ok(processedContext);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message });
		}
	}
	
	
	[HttpPost("testAugmentation")]
	public async Task<IActionResult> TestAugmentation([FromBody] PatientAnalyzeRagRequest reqData)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);
		var processedContext = await retrievalAugmentationService.AugmentAsync(reqData);
		return Ok(processedContext);
	}
	
	[HttpPost("testGeneration")]
	public async Task<IActionResult> TestGeneration([FromBody] PatientAnalyzeRagRequest reqData)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);
		var processedContext = await orchestrator.GenerateResponseAsync(reqData);
		return Ok(processedContext);
	}
	[HttpPost("testUpload")]
	public async Task<IActionResult> TestUpload(List<IFormFile>? files, IFormFile? formFile, [FromForm] string? path)
	{
		var uploadDataList = new List<IngestionUploadDto>();
		var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
		if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

		// 1. Обработка локального пути (файл или папка), если передан
		if (!string.IsNullOrWhiteSpace(path))
		{
			if (Directory.Exists(path))
			{
				var dirFiles = Directory.GetFiles(path, "*.pdf", SearchOption.AllDirectories);
				foreach (var f in dirFiles)
				{
					uploadDataList.Add(new IngestionUploadDto(f));
				}
			}
			else if (System.IO.File.Exists(path) && Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
			{
				uploadDataList.Add(new IngestionUploadDto(path));
			}
		}

		// 2. Обработка загруженных файлов из формы
		var allFiles = new List<IFormFile>();
		if (files != null) allFiles.AddRange(files);
		if (formFile != null) allFiles.Add(formFile);

		foreach (var file in allFiles)
		{
			if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
				continue;

			var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
			var filePath = Path.Combine(uploadsFolder, uniqueFileName);

			await using (var fs = new FileStream(filePath, FileMode.OpenOrCreate))
			{
				await file.CopyToAsync(fs);
			}
			uploadDataList.Add(new IngestionUploadDto(filePath));
		}

		if (uploadDataList.Count == 0)
		{
			return BadRequest(new { error = "No PDF files found to ingest. Provide 'files' in form-data or a valid local 'path'." });
		}
		
		var results = await ingestionService.IngestFilesAsync(uploadDataList);
		return Ok(results);
	}
	/*[HttpPost("TestClinicalReasoning")]
	public async Task<IActionResult> TestClinicalReasoning([FromBody] PatientAnalyzeLlmRequest reqData)
	{
		var processedContext = await medicalContextReasoningService.AnalyzeAsync(reqData);
		var result = new TestVectorSearchResult(processedContext.Queries);
		try
		{
			await qdrantService.EnsureCollectionAsync();
			var searchTasks = result.ExtractedContext
				.Select(context => retrievalAugmentationService.SearchAsync(context))
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
		var answer = await medicalAnalyzingReasoningService.AnalyzeAsync(JsonSerializer.Serialize(reqData), result.Results);

		return Ok(answer);
	}*/
	// POST: rag/search
	[HttpPost("search")]
	public async Task<IActionResult> SearchContext([FromForm] string question)
	{
		if (string.IsNullOrWhiteSpace(question))
			return BadRequest(new { error = "Введіть пошуковий запит." });

		try
		{
			// Розбиваємо запит по комах, якщо користувач ввів декілька фраз
			// Наприклад: "Гіпертонія, цукровий діабет" -> ["Гіпертонія", "цукровий діабет"]
			var queries = question.Split(',')
				.Select(q => q.Trim())
				.Where(q => !string.IsNullOrWhiteSpace(q))
				.ToList();

			// Викликаємо метод пошуку нашого сервісу
			var results = await retrievalAugmentationService.RetrieveContextAsync(queries);
			
			// Повертаємо масив RetrievalResult у JSON
			return Ok(results);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = $"Search Error: {ex.Message}" });
		}
	}
	// Зберігаємо старий метод Query для сумісності, якщо він десь викликається
	[HttpPost("query")]
	public async Task<IActionResult> Query(string question)
	{
		return await SearchContext(question);
	}

	// POST: rag/upload
	[HttpPost("upload")]
	[DisableRequestSizeLimit] // Знімає обмеження на розмір папки (28.6 МБ)
	[RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)] // Дозволяє великі multipart дані
	public async Task<IActionResult> Upload(List<IFormFile> files)
	{
		if (files == null || files.Count == 0)
			return BadRequest(new { error = "Please select at least one PDF file." });

		// Фільтруємо тільки PDF
		var pdfFiles = files.Where(f => Path.GetExtension(f.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)).ToList();
		if (pdfFiles.Count == 0)
			return BadRequest(new { error = "No PDF files found in the selection." });

		try
		{
			var uploadDataList = new List<IngestionUploadDto>();
			foreach (var file in pdfFiles)
			{
				// Читаємо файл у пам'ять
				using var ms = new MemoryStream();
				await file.CopyToAsync(ms);
				var fileBytes = ms.ToArray();
				
				uploadDataList.Add(new IngestionUploadDto(fileBytes, file.FileName));
			}

			// Запускаємо масове паралельне завантаження (Bulk Ingestion)
			var results = await ingestionService.IngestFilesAsync(uploadDataList);
			
			// Повертаємо весь об'єкт BulkIngestionResult у форматі JSON
			return Ok(results);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = $"Server Error: {ex.Message}" });
		}
	}
}
