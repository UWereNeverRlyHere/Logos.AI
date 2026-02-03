using Logos.AI.Engine.Configuration;
using OpenAI.Chat;
namespace Logos.AI.Engine.Reasoning;

public record LlmRequestDto
{
	public LlmOptions LlmOptions { get; init; }
	public ChatResponseFormat ResponseFormat { get; init; }
	public object UserMessageContent { get; init; }

}

