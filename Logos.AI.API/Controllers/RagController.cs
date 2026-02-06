using System.Text;
using Logos.AI.Abstractions.Knowledge.Contracts;
using Logos.AI.Abstractions.Knowledge.Ingestion;
using Logos.AI.Abstractions.Knowledge.Retrieval;
using Logos.AI.Abstractions.PatientAnalysis;
using Logos.AI.Abstractions.RAG;
using Microsoft.AspNetCore.Mvc;
namespace Logos.AI.API.Controllers;
[ApiController]
[Route("rag")]
public class RagController(
	IStorageService      storageService,
	IVectorStorageService qdrantService,
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
	[HttpPost("testAugmentation")]
	public async Task<IActionResult> TestVectorSearch([FromBody] PatientAnalyzeLlmRequest reqData)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);
		var processedContext = await retrievalAugmentationService.AugmentAsync(reqData);
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
	public async Task<IActionResult> Search(string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return RedirectToAction("Index");
		}
		try
		{
			// 1. Виконуємо пошук через RagQueryService
			var searchResults = await retrievalAugmentationService.RetrieveContextAsync(new[] { query });
			var results = searchResults.SelectMany(r => r.FoundChunks).ToList();
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
			var docs = await storageService.GetAllDocumentsAsync();
			ViewBag.AllDocuments = docs;
			return View("Index", results);
		}
		catch (Exception ex)
		{
			ViewBag.Message = $"Error during search: {ex.Message}";
			var docs = await storageService.GetAllDocumentsAsync();
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
	public async Task<IActionResult> Upload(List<IFormFile> files, string uploadMode)
	{
		if (files == null || files.Count == 0)
		{
			ViewBag.Message = "Please select at least one PDF file.";
			return await Index();
		}

		// Фильтруем только PDF
		var pdfFiles = files.Where(f => Path.GetExtension(f.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)).ToList();
		if (pdfFiles.Count == 0)
		{
			ViewBag.Message = "No PDF files found in the selection.";
			return await Index();
		}

		try
		{
			// 1. Зберігаємо файли фізично
			var uploadDataList = new List<IngestionUploadDto>();
			foreach (var file in pdfFiles)
			{
				// Генеруємо унікальне ім'я файлу
				using var ms = new MemoryStream();
				await file.CopyToAsync(ms);
				var fileBytes = ms.ToArray();
				// Используем основной конструктор
				uploadDataList.Add(new IngestionUploadDto(fileBytes, file.FileName));
			}

			if (uploadMode == "folder")
			{
				var results = await ingestionService.IngestFilesAsync(uploadDataList);
				var success = results.GetSuccess();
				if (success.Any())
				{
					return Ok(new
					{
						message = $"Uploaded {results.TotalDocuments} files from folder",
						chunks = results.ChunksCount
					});
				}
				
				return BadRequest(string.Join("; ", results.GetFail().Select(r => r.Message)));
			}
			// Випадок одного файлу (або якщо вибрано режим file, беремо перший PDF)
			var res = await ingestionService.IngestFileAsync(uploadDataList[0]);

			if (res.IsSuccess)
				return Ok(new
				{
					message = $"Uploaded {pdfFiles[0].FileName}",
					chunks = res.ChunksCount
				});
			return BadRequest(res.Message);
		}
		catch (Exception ex)
		{
			// Логування помилки
			ViewBag.Message = $"Error: {ex.Message}";
		}

		// Оновлюємо список документів і повертаємо View
		var finalDocs = await storageService.GetAllDocumentsAsync();
		ViewBag.AllDocuments = finalDocs;

		return View("Index", new List<KnowledgeChunk>());
	}
}
