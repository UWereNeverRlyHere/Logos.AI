using System.Text;
using Logos.AI.Abstractions.Knowledge;
using Logos.AI.Abstractions.Knowledge.Contracts;
using Logos.AI.Abstractions.Knowledge.Ingestion;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
namespace Logos.AI.Engine.Knowledge;

public class PdfChunkService : IDocumentChunkService
{
	private readonly int _chunkSizeWords;
	private readonly int _chunkOverlapWords;

	// Список разделителей в порядке приоритета:
	// 1. Двойной перенос строки (конец абзаца)
	// 2. Точка, знак вопроса, восклицательный знак (конец предложения)
	// 3. Обычный перенос строки
	// 4. Пробел (последнее средство)
	private readonly char[] _sentenceEndings = ['.', '!', '?'];
	private readonly string[] _paragraphSplitters = ["\r\n\r\n", "\n\n", "\r\r"];

	public PdfChunkService(IOptions<RagOptions> options)
	{
		var ragOptions = options.Value;
		_chunkSizeWords = ragOptions.ChunkSizeWords;
		_chunkOverlapWords = ragOptions.ChunkOverlapWords;
	}

// Змінюємо сигнатуру методу: додаємо out string docTitle
	public bool TryChunkDocument(IngestionUploadDto uploadDto, out DocumentChunkingResult documentChunkingResult, out string error)
	{
		error = string.Empty; 
		documentChunkingResult = new DocumentChunkingResult(uploadDto);
		try
		{
			var rawPages = ExtractTextWithPages(uploadDto.FileData);
			if (rawPages.Count == 0)
			{
				error = "Could not extract text from PDF.";
				return false;
			}
			foreach (var page in rawPages)
			{
				documentChunkingResult.TotalCharacters += page.Content.Length;
				documentChunkingResult.TotalWords += CountWords(page.Content);
			}
		
			documentChunkingResult.SetTitleIfNotEmpty(ExtractTitleFromFirstPage(rawPages[0].Content));
			documentChunkingResult.AddChunks(ChunkTextWithPages(rawPages));
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

// Додаємо приватний метод-евристику
	private string ExtractTitleFromFirstPage(string text)
	{
		// Розбиваємо на рядки
		var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
			.Select(l => l.Trim())
			.Where(l => !string.IsNullOrWhiteSpace(l))
			.ToList();

		// Евристика 1: Шукаємо рядок, що починається на "Настанова" (найточніше для МОЗ)
		var guidelineLine = lines.FirstOrDefault(l => l.StartsWith("Настанова", StringComparison.OrdinalIgnoreCase));
		if (guidelineLine != null) return guidelineLine;
		
		// Евристика 2: Якщо немає "Настанова", беремо перші 1-2 рядки, які не є технічними
		// Ігноруємо рядки типу "--- PAGE 1 ---" або URL
		var cleanLines = lines.Where(l =>
				!l.StartsWith("--- PAGE") &&
				!l.StartsWith("http") &&
				l.Length > 5 // Ігноруємо надто короткі "сміттєві" рядки
		).Take(2).ToList();

		if (cleanLines.Count > 0) return string.Join(". ", cleanLines); // Об'єднуємо заголовок і підзаголовок
		return string.Empty;
	}

	private List<TextFragment> ExtractTextWithPages(byte[] file)
	{
		var result = new List<TextFragment>();
		try
		{
			using var document = PdfDocument.Open(file);
			foreach (var page in document.GetPages())
			{
				var text = ContentOrderTextExtractor.GetText(page);
				if (!string.IsNullOrWhiteSpace(text))
				{
					result.Add(new TextFragment(page.Number, text));
				}
			}
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to parse PDF: {ex.Message}", ex);
		}
		return result;
	}

	/// <summary>
	/// Умная нарезка текста (Recursive Chunking) с сохранением страниц.
	/// Старается не разрывать предложения.
	/// </summary>
	private List<TextFragment> ChunkTextWithPages(List<TextFragment> pages)
	{
		var result = new List<TextFragment>();

		foreach (var page in pages)
		{
			// 1. Разбиваем страницу на предложения (грубо)
			// Мы используем простой подход: сплитим по точке, но восстанавливаем точку в конце.
			var rawSentences = SplitIntoSentences(page.Content);

			var currentChunk = new StringBuilder();
			var currentWordCount = 0;

			// Буфер для реализации overlap (храним последние предложения)
			var overlapBuffer = new Queue<string>();
			var overlapWordCount = 0;

			foreach (var sentence in rawSentences)
			{
				var sentenceWordCount = CountWords(sentence);

				// Если добавление предложения превысит лимит чанка
				if (currentWordCount + sentenceWordCount > _chunkSizeWords && currentWordCount > 0)
				{
					// 1. Сохраняем текущий чанк
					result.Add(new TextFragment(page.PageNumber, currentChunk.ToString().Trim()));

					// 2. Начинаем новый чанк
					currentChunk.Clear();
					currentWordCount = 0;

					// 3. Добавляем Overlap (предыдущие предложения) в начало нового чанка
					foreach (var overlapSentence in overlapBuffer)
					{
						currentChunk.Append(overlapSentence);
						currentChunk.Append(" ");
						currentWordCount += CountWords(overlapSentence);
					}
				}

				// Добавляем текущее предложение в чанк
				currentChunk.Append(sentence);
				currentChunk.Append(" "); // Пробел между предложениями
				currentWordCount += sentenceWordCount;

				// Обновляем буфер Overlap
				overlapBuffer.Enqueue(sentence);
				overlapWordCount += sentenceWordCount;

				// Если буфер переполнен (больше чем размер overlap), удаляем старое
				while (overlapWordCount > _chunkOverlapWords && overlapBuffer.Count > 0)
				{
					var removed = overlapBuffer.Dequeue();
					overlapWordCount -= CountWords(removed);
				}
			}

			// Добавляем остаток (последний чанк на странице)
			if (currentChunk.Length > 0)
			{
				result.Add(new TextFragment(page.PageNumber, currentChunk.ToString().Trim()));
			}
		}

		return result;
	}

	// Вспомогательный метод для разбиения на предложения
	private List<string> SplitIntoSentences(string text)
	{
		var finalSentences = new List<string>();

		// 1. КРОК 1: Спочатку ріжемо текст на Абзаци (використовуючи ту саму змінну!)
		// Це рятує заголовки, списки та текст без крапок.
		var paragraphs = text.Split(_paragraphSplitters, StringSplitOptions.RemoveEmptyEntries);

		foreach (var paragraph in paragraphs)
		{
			// 2. КРОК 2: Всередині кожного абзацу шукаємо речення
			var buffer = new StringBuilder();

			for (int i = 0; i < paragraph.Length; i++)
			{
				char c = paragraph[i];
				buffer.Append(c);

				// Якщо це кінець речення (.!?) і далі пробіл або кінець рядка
				if (_sentenceEndings.Contains(c) && (i + 1 >= paragraph.Length || char.IsWhiteSpace(paragraph[i + 1])))
				{
					finalSentences.Add(buffer.ToString().Trim());
					buffer.Clear();
				}
			}

			// 3. ВАЖЛИВО: Якщо в буфері щось залишилось (наприклад, заголовок без крапки)
			// Ми додаємо це як окремий сегмент.
			if (buffer.Length > 0)
			{
				var remaining = buffer.ToString().Trim();
				if (!string.IsNullOrWhiteSpace(remaining))
				{
					finalSentences.Add(remaining);
				}
			}
		}

		return finalSentences;
	}

	private int CountWords(string text)
	{
		if (string.IsNullOrWhiteSpace(text)) return 0;
		return text.Split(new[]
		{
			' ', '\r', '\n'
		}, StringSplitOptions.RemoveEmptyEntries).Length;
	}
}
