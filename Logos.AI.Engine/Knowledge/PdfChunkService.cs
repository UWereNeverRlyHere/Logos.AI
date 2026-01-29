using System.Text;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
namespace Logos.AI.Engine.Knowledge;

public class PdfChunkService
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

	public bool TryChunkDocument(string filePath, out List<(int Page, string Text)> chunks, out string error)
	{
		error = string.Empty;
		chunks = new List<(int Page, string Text)>();
		try
		{
			var rawPages = ExtractTextWithPages(filePath);
			if (rawPages.Count == 0)
			{
				error = "Could not extract text from PDF (it might be an image scan).";
				return false;
			}
			chunks = ChunkTextWithPages(rawPages);
			return true;
		}
		catch (Exception e)
		{
			error = $"Failed to parse PDF: {e.Message}";
			return false;
		}
	}

	private List<(int Page, string Text)> ExtractTextWithPages(string filePath)
	{
		var result = new List<(int Page, string Text)>();
		try
		{
			using var document = PdfDocument.Open(filePath);
			foreach (var page in document.GetPages())
			{
				var text = ContentOrderTextExtractor.GetText(page);
				if (!string.IsNullOrWhiteSpace(text))
				{
					result.Add((page.Number, text));
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
	private List<(int Page, string Chunk)> ChunkTextWithPages(List<(int Page, string Text)> pages)
	{
		var result = new List<(int Page, string Chunk)>();

		foreach (var (pageNumber, pageText) in pages)
		{
			// 1. Разбиваем страницу на предложения (грубо)
			// Мы используем простой подход: сплитим по точке, но восстанавливаем точку в конце.
			var rawSentences = SplitIntoSentences(pageText);

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
					result.Add((pageNumber, currentChunk.ToString().Trim()));

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
				result.Add((pageNumber, currentChunk.ToString().Trim()));
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
