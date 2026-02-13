namespace Logos.AI.Abstractions.Knowledge.Retrieval;
internal static class KnowledgePayloadFields
{
	public const string DocumentId = "documentId";
	public const string DocumentTitle = "documentTitle";
	public const string DocumentDescription = "documentDescription";
	public const string PageNumber = "pageNumber";
	public const string FullText = "fullText";
	public const string IndexedAt = "indexedAt";
}
public class KnowledgeDictionary
{
	private Dictionary<string, object> Payload { get; } = new()
	{
		[KnowledgePayloadFields.DocumentId] = "",
		[KnowledgePayloadFields.DocumentTitle] = "",
		//[KnowledgePayloadFields.DocumentDescription] = "", // Можливо додам генерацію опису з ШІ
		[KnowledgePayloadFields.PageNumber] = "",
		[KnowledgePayloadFields.FullText] = "",
		//["indexedAt"] = DateTime.UtcNow.ToString("O")
	};
	public static KnowledgeDictionary Create()
	{
		return new KnowledgeDictionary();
	}
	public void Put(string key, object value)
	{
		Payload[key] = value;
	}
	public KnowledgeDictionary SetDocumentId(Guid docId)
	{
		Payload[KnowledgePayloadFields.DocumentId] = docId.ToString();
		return this;
	}
	public KnowledgeDictionary SetDocumentId(string docId)
	{
		Payload[KnowledgePayloadFields.DocumentId] = docId;
		return this;
	}
	public KnowledgeDictionary SetDocumentTitle(string documentTitle)
	{
		Payload[KnowledgePayloadFields.DocumentTitle] = documentTitle;
		return this;
	}
	public KnowledgeDictionary SetDocumentDescription(string documentDescription)
	{
		// Перевірка на null, щоб не записувати сміття в базу, якщо опису немає
		if (!string.IsNullOrEmpty(documentDescription))
		{
			Payload[KnowledgePayloadFields.DocumentDescription] = documentDescription;
		}
		return this;
	}
	public KnowledgeDictionary SetPageNumber(int pageNumber)
	{
		Payload[KnowledgePayloadFields.PageNumber] = pageNumber;
		return this;
	}
	public KnowledgeDictionary SetFullText(string fullText)
	{
		Payload[KnowledgePayloadFields.FullText] = fullText;
		return this;
	}
	public KnowledgeDictionary SetIndexedAt(DateTime indexedAt)
	{
		Payload[KnowledgePayloadFields.IndexedAt] = indexedAt.ToString("O");
		return this;
	}

	public KnowledgeDictionary SetIndexedAtNow()
	{
		Payload[KnowledgePayloadFields.IndexedAt] = DateTime.UtcNow.ToString("O");
		return this;
	}

	public Dictionary<string, object> GetPayload()
	{
		return Payload;
	}

	public KnowledgeChunk ToChunk(float score)
	{
		return new KnowledgeChunk
		{
			// Безпечний парсинг Guid
			DocumentId = Guid.TryParse(GetVal(KnowledgePayloadFields.DocumentId), out var g) ? g : Guid.Empty,
			DocumentTitle = GetVal(KnowledgePayloadFields.DocumentTitle),
			DocumentDescription = GetVal(KnowledgePayloadFields.DocumentDescription),
			// Безпечний парсинг Int
			PageNumber = int.TryParse(GetVal(KnowledgePayloadFields.PageNumber), out var p) ? p : 0,
			Content = GetVal(KnowledgePayloadFields.FullText),
			Score = score
		};
	}
	private string GetVal(string key)
	{
		return Payload.TryGetValue(key, out var value)
			? value.ToString() ?? string.Empty
			: string.Empty;
	}
}
