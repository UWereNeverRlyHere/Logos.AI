using Microsoft.Extensions.DependencyInjection;
using Logos.AI.Engine.Extensions;

namespace Logos.AI.API.Configuration;

/// <summary>
/// Клас для централізованого налаштування серіалізації JSON в проекті.
/// </summary>
public static class JsonSerializationExtensions
{
    /// <summary>
    /// Конфігурує налаштування JSON для контролерів.
    /// </summary>
    public static IMvcBuilder AddLogosJsonOptions(this IMvcBuilder builder)
    {
        return builder.AddJsonOptions(options =>
        {
            LogosJsonExtensions.ConfigureLogosOptions(options.JsonSerializerOptions);
        });
    }
}
