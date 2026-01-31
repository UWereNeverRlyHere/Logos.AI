using System.Security.Cryptography;
using System.Text;
using Google.Protobuf.Collections;
using Logos.AI.Abstractions.Features.Knowledge;
using Qdrant.Client.Grpc;
namespace Logos.AI.Engine.Knowledge.Qdrant;
public static class QdrantMapper
{
	public static  Guid GenerateGuidFromSeed(string input)
	{
		using var md5 = MD5.Create();
		return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
	}
	public static KnowledgeDictionary ToKnowledgeDictionary(this MapField<string, Value> payload)
	{
		var dict = new KnowledgeDictionary();
		foreach (var item in payload)
		{
			dict.Put(item.Key, ToCsharpObject(item.Value)); 
		}
		return dict;
	}
	// Метод для конвертації об'єктів C# у формат Qdrant (для запису)
	public static Value ToQdrantValue(this object? v)
	{
		if (v == null) return new Value { NullValue = NullValue.NullValue };

		return v switch
		{
			int i => new Value { IntegerValue = i },
			long l => new Value { IntegerValue = l },
			float f => new Value { DoubleValue = f },
			double d => new Value { DoubleValue = d },
			string s => new Value { StringValue = s },
			bool b => new Value { BoolValue = b },
			Guid g => new Value { StringValue = g.ToString() },
			DateTime dt => new Value { StringValue = dt.ToString("O") },
			_ => new Value { StringValue = v.ToString() ?? string.Empty }
		};
	}
	
	/// <summary>
	/// Конвертує gRPC Value (Qdrant) у базовий C# object.
	/// </summary>
	private static object ToCsharpObject(Value value)
	{
		return value.KindCase switch
		{
			Value.KindOneofCase.StringValue => value.StringValue,
			Value.KindOneofCase.IntegerValue => value.IntegerValue,
			Value.KindOneofCase.DoubleValue => value.DoubleValue,
			Value.KindOneofCase.BoolValue => value.BoolValue,
			// Для складних об'єктів (структур/списків), якщо ви їх будете використовувати
			Value.KindOneofCase.StructValue => value.StructValue, 
			Value.KindOneofCase.ListValue => value.ListValue,
			Value.KindOneofCase.NullValue => null!,
			_ => value.ToString() // Fallback
		};
	}
}