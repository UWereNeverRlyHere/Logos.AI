using Logos.AI.Engine.RAG;
using Microsoft.AspNetCore.Mvc;
using RAG_Search.Services;
namespace Logos.AI.API.Controllers
{
	[Route("rag")]
	public class RagController : Controller
	{
		private readonly OpenAIEmbeddingService _embedding;
		private readonly QdrantService _qdrant;
		private readonly RagQueryService _ragQuery;
		private readonly IConfiguration _config;
		private readonly SqlChunkLoaderService _sqlChunkLoaderService;


		public RagController(
			OpenAIEmbeddingService embedding,
			QdrantService          qdrant,
			RagQueryService        ragQuery,
			IConfiguration         config,
			SqlChunkLoaderService  sqlChunkLoaderService)

		{
			_embedding = embedding;
			_qdrant = qdrant;
			_ragQuery = ragQuery;
			_config = config;
			_sqlChunkLoaderService = sqlChunkLoaderService;
		}

		[HttpGet("index")]
		public async Task<IActionResult> IndexAsync()
		{
			var allDocs = await _qdrant.GetAllUploadedDocumentsAsync();

			ViewBag.AllDocuments = allDocs;

			return View();
		}

		[HttpPost("upload")]
		public async Task<IActionResult> Upload(IFormFile file)
		{
			if (file == null || file.Length == 0) return View("Index");

			try
			{
				// 1. Збереження файлу
				var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
				if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

				var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
				var filePath = Path.Combine(uploadsFolder, uniqueFileName);

				using (var fs = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(fs);
				}

				// 2. Посторінковий парсинг (зберігаємо структуру)
				var rawPages = PdfService.ExtractTextWithPages(filePath);

				// 3. Нарізка на чанки (з прив'язкою до сторінок)
				int chunkSize = _config.GetValue<int>("Rag:ChunkSizeWords", defaultValue:300);
				int overlap = _config.GetValue<int>("Rag:ChunkOverlapWords", defaultValue:50);

				// List<(int Page, string Chunk)>
				var chunkedData = PdfService.ChunkTextWithPages(rawPages, chunkSize, overlap);

				// 4. Збереження в SQL (Тепер у нас є реальні PageNumbers!)
				var documentId = await _sqlChunkLoaderService.SaveDocumentAsync(file.FileName, filePath, chunkedData);

				// 5. Векторизація для Qdrant
				for (int i = 0; i < chunkedData.Count; i++)
				{
					var (page, text) = chunkedData[i];

					var embList = await _embedding.GetEmbeddingAsync(text);
					var embArray = embList.ToArray();

					var pointId = $"{documentId}-{i}"; // Unique ID

					var payload = new Dictionary<string, object>
					{
						["documentId"] = documentId.ToString(),
						["chunkIndex"] = i,
						["pageNumber"] = page, 
						["fileName"] = file.FileName,
						["preview"] = text.Length > 300 ? text.Substring(0, 300) + "..." : text,
						["fullText"] = text
					};

					await _qdrant.UpsertChunkAsync(pointId, embArray, payload);
				}

				ViewBag.Message = $"Uploaded '{file.FileName}'. Processed {chunkedData.Count} chunks from {rawPages.Count} pages.";
			}
			catch (Exception ex)
			{
				ViewBag.Message = $"Error: {ex.Message}";
			}

			ViewBag.AllDocuments = await _sqlChunkLoaderService.GetAllDocumentsAsync();
			return View("Index");
		}


		[HttpPost("LoadDBData")]
		public async Task<IActionResult> LoadDBData()
		{
			// Step 1: Extract text and chunk
			var text = _sqlChunkLoaderService.LoadChunksFromSql();
			int chunkSize = int.Parse(_config["Rag:ChunkSizeWords"] ?? "300");
			int overlap = int.Parse(_config["Rag:ChunkOverlapWords"] ?? "50");
			var chunks = PdfService.ChunkText(text, chunkSize, overlap);

			var docId = Guid.NewGuid();

			for (int i = 0; i < chunks.Count; i++)
			{
				var chunk = chunks[i];

				// Step 2: Get embedding asynchronously
				var embList = await _embedding.GetEmbeddingAsync(chunk);
				var embArray = embList.ToArray(); // convert List<float> -> float[]

				// Step 3: Prepare payload
				var pointId = $"{docId}-{i}";

				var payload = new Dictionary<string, object>
				{
					["documentId"] = docId.ToString(),
					["chunkIndex"] = i,
					["preview"] = chunk.Length > 200 ? chunk.Substring(0, 200) : chunk,

					["fileName"] = "SQL Database"
				};

				// Step 4: Upsert chunk to Qdrant
				await _qdrant.UpsertChunkAsync(pointId, embArray, payload);
			}


			ViewBag.Message = $"Uploaded SQL Database and indexed {chunks.Count} chunks.";
			//return RedirectToAction("Index");
			return View("Index");
		}


		[HttpPost("query")]
		public async Task<IActionResult> Query(string question)
		{
			if (string.IsNullOrWhiteSpace(question))
			{
				ViewBag.Message = "Please enter a question.";
				return View("Index");
			}

			var allDocs = await _qdrant.GetAllUploadedDocumentsAsync();
			ViewBag.AllDocuments = allDocs;

			var (answer, files) = await _ragQuery.AnswerAsync(question);
			ViewBag.SourceDocuments = files[0].ToString();
			;
			ViewBag.Question = question;
			ViewBag.Answer = answer;

			//return RedirectToAction("Index");
			return View("Index");
		}
	}
}
