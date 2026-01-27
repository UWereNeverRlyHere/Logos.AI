using Logos.AI.Abstractions.Features.Knowledge;
using Logos.AI.Engine.RAG;
using Microsoft.AspNetCore.Mvc;

namespace Logos.AI.API.Controllers;

[Route("rag")]
public class RagController(
    SqlChunkLoaderService sqlChunkLoaderService,
    QdrantService qdrantService,
    RagQueryService queryService,
    OpenAIEmbeddingService embeddingService,
    IConfiguration config) : Controller
{
    // GET: rag/index
    [HttpGet("index")]
    public async Task<IActionResult> Index()
    {
        // Завантажуємо список документів з SQL (це наше джерело правди для списків)
        var docs = await sqlChunkLoaderService.GetAllDocumentsAsync();
        ViewBag.AllDocuments = docs;

        // Повертаємо пустий список результатів, щоб View не ламалася
        return View("Index", new List<KnowledgeChunk>());
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
            // Він сам зробить векторизацію і запит в Qdrant
            var results = await queryService.SearchAsync(query);

            // 2. Оновлюємо список документів для сайдбару (щоб не зникав)
            var docs = await sqlChunkLoaderService.GetAllDocumentsAsync();
            ViewBag.AllDocuments = docs;

            // 3. Повертаємо результати
            return View("Index", results);
        }
        catch (Exception ex)
        {
            ViewBag.Message = $"Error during search: {ex.Message}";
            var docs = await sqlChunkLoaderService.GetAllDocumentsAsync();
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

            // 2. Парсимо PDF (зберігаючи номери сторінок!)
            // Це CPU-bound операція
            var rawPages = PdfService.ExtractTextWithPages(filePath);

            if (rawPages.Count == 0)
            {
                ViewBag.Message = "Could not extract text from PDF (it might be an image scan).";
                return await Index();
            }

            // 3. Нарізаємо на чанки
            int chunkSize = config.GetValue<int>("Rag:ChunkSizeWords", 300);
            int overlap = config.GetValue<int>("Rag:ChunkOverlapWords", 50);

            var chunks = PdfService.ChunkTextWithPages(rawPages, chunkSize, overlap);

            // 4. Зберігаємо в SQL (Архів + Метадані)
            // Повертає ID документа, який ми використаємо для зв'язку в Qdrant
            var documentId = await sqlChunkLoaderService.SaveDocumentAsync(file.FileName, filePath, chunks);

            // 5. Векторизація та збереження в Qdrant
            
            // ВАЖЛИВО: Переконуємось, що колекція існує
            await qdrantService.EnsureCollectionAsync();

            // Це IO-bound операція (OpenAI API + Qdrant API)
            for (int i = 0; i < chunks.Count; i++)
            {
                var (page, text) = chunks[i];

                // А. Отримуємо вектор
                var embList = await embeddingService.GetEmbeddingAsync(text);
                var vector = embList.ToArray();

                // Б. Формуємо ID точки (щоб був стабільним)
                var pointId = $"{documentId}-{i}";

                // В. Формуємо Payload (дані, які повернуться при пошуку)
                var payload = new Dictionary<string, object>
                {
                    ["documentId"] = documentId.ToString(),
                    ["fileName"] = file.FileName,
                    ["pageNumber"] = page,     // Важливо: номер сторінки
                    ["chunkIndex"] = i,
                    ["fullText"] = text,       // Важливо: повний текст для LLM
                    ["preview"] = text.Length > 200 ? text.Substring(0, 200) + "..." : text
                };

                // Г. Відправляємо в Qdrant
                await qdrantService.UpsertChunkAsync(pointId, vector, payload);
            }

            ViewBag.Message = $"Success! Uploaded '{file.FileName}', saved to SQL, and indexed {chunks.Count} chunks in Qdrant.";
        }
        catch (Exception ex)
        {
            // Логування помилки
            ViewBag.Message = $"Error: {ex.Message}";
        }

        // Оновлюємо список документів і повертаємо View
        var finalDocs = await sqlChunkLoaderService.GetAllDocumentsAsync();
        ViewBag.AllDocuments = finalDocs;

        return View("Index", new List<KnowledgeChunk>());
    }
}
