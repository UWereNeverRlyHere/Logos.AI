using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Logos.AI.Engine.RAG;

public class PdfService
{
    private readonly int _chunkSizeWords;
    private readonly int _chunkOverlapWords;
    public PdfService(IOptions<RagOptions> options)
    {
        var ragOptions = options.Value;
        _chunkSizeWords = ragOptions.ChunkSizeWords;
        _chunkOverlapWords = ragOptions.ChunkOverlapWords;
    }
    /// <summary>
    /// Витягує текст посторінково.
    /// Повертає список кортежів: (Номер сторінки, Текст).
    /// </summary>
    public List<(int Page, string Text)> ExtractTextWithPages(string filePath)
    {
        var result = new List<(int Page, string Text)>();

        try
        {
            using var document = PdfDocument.Open(filePath);
            
            foreach (var page in document.GetPages())
            {
                // Використовуємо ContentOrderTextExtractor для кращого склеювання слів
                var text = ContentOrderTextExtractor.GetText(page);
                
                if (!string.IsNullOrWhiteSpace(text))
                {
                    result.Add((page.Number, text));
                }
            }
        }
        catch (Exception ex)
        {
            // Логування краще робити вище, тут прокидаємо
            throw new Exception($"Failed to parse PDF: {ex.Message}", ex);
        }

        return result;
    }

    /// <summary>
    /// Розбиває текст на чанки, зберігаючи номер сторінки.
    /// </summary>
    public List<(int Page, string Chunk)> ChunkTextWithPages(List<(int Page, string Text)> pages)
    {
        var result = new List<(int Page, string Chunk)>();

        foreach (var (pageAuth, pageText) in pages)
        {
            var words = pageText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Якщо сторінка порожня або дуже мала, додаємо як є
            if (words.Length == 0) continue;
            if (words.Length <= _chunkSizeWords)
            {
                result.Add((pageAuth, pageText));
                continue;
            }

            // Sliding Window алгоритм в межах однієї сторінки
            for (int i = 0; i < words.Length; i += (_chunkSizeWords - _chunkOverlapWords))
            {
                var chunkWords = words.Skip(i).Take(_chunkSizeWords);
                var chunkText = string.Join(" ", chunkWords);
                
                result.Add((pageAuth, chunkText));

                // Щоб не вийти за межі циклу зайвий раз
                if (i + _chunkSizeWords >= words.Length) break;
            }
        }

        return result;
    }
}