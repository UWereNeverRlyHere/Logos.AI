using System.Collections.Concurrent;
using Logos.AI.Engine.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
namespace Logos.AI.Engine.Reasoning;

public interface IChatClientFactory
{ 
	ChatClient GetClient(string model);
}

public class ChatClientFactory(IOptions<OpenAiOptions> options) : IChatClientFactory
{
	private readonly OpenAiOptions _options = options.Value;
	private readonly ConcurrentDictionary<string, ChatClient> _clients = new();

	public ChatClient GetClient(string model)
	{
		var modelName = string.IsNullOrWhiteSpace(model) ? _options.Model : model;
		return _clients.GetOrAdd(modelName, m => new ChatClient(m, _options.ApiKey));
	}
}
