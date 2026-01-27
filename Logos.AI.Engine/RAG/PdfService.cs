using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Logos.AI.Engine.RAG;

public static class PdfService
{
    /// <summary>
    /// Витягує текст посторінково.
    /// Повертає список кортежів: (Номер сторінки, Текст).
    /// </summary>
    public static List<(int Page, string Text)> ExtractTextWithPages(string filePath)
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
    public static List<(int Page, string Chunk)> ChunkTextWithPages(
        List<(int Page, string Text)> pages, 
        int maxWords = 300, 
        int overlap = 50)
    {
        var result = new List<(int Page, string Chunk)>();

        foreach (var (pageAuth, pageText) in pages)
        {
            var words = pageText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Якщо сторінка порожня або дуже мала, додаємо як є
            if (words.Length == 0) continue;
            if (words.Length <= maxWords)
            {
                result.Add((pageAuth, pageText));
                continue;
            }

            // Sliding Window алгоритм в межах однієї сторінки
            for (int i = 0; i < words.Length; i += (maxWords - overlap))
            {
                var chunkWords = words.Skip(i).Take(maxWords);
                var chunkText = string.Join(" ", chunkWords);
                
                result.Add((pageAuth, chunkText));

                // Щоб не вийти за межі циклу зайвий раз
                if (i + maxWords >= words.Length) break;
            }
        }

        return result;
    }
}