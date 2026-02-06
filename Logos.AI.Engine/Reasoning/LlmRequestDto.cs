using Logos.AI.Engine.Configuration;
using Logos.AI.Engine.Extensions;
using OpenAI.Chat;
namespace Logos.AI.Engine.Reasoning;

public record LlmRequestDto
{
	public required LlmOptions LlmOptions { get; init; }
	public required ChatResponseFormat ResponseFormat { get; init; }
    
	// Это свойство для чтения результата
	public string UserMessageJsonContent { get; private set; } = string.Empty;

	// А это "виртуальное" свойство только для инициализации
	public required object Content
	{
		init
		{
			// Если передали уже готовую строку JSON
			if (value is string strContent)
			{
				UserMessageJsonContent = strContent;
			}
			else
			{
				// Если объект - сериализуем (тут твой экстеншн)
				UserMessageJsonContent = value.SerializeToJson(false);
			}
		}
	}
}