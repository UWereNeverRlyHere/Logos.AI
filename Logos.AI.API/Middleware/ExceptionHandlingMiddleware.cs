using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Logos.AI.Abstractions.Exceptions;
namespace Logos.AI.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{ 
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var statusCode = HttpStatusCode.InternalServerError;
        var message = exception.Message;
        string code = null;
        object? data = null;
        switch (exception)
        {
            case LogosException logosException:
                statusCode =logosException.HttpStatusCode;
                data = logosException.Data;
                code = logosException.Code;
                break;
            default:
                logger.LogError(exception, "Unhandled exception occurred");
                message = $"An internal server error occurred: {exception.Message}"; 
                break;
        }

        context.Response.StatusCode = (int)statusCode;
        //TODO if isRelease don't show stack trace and message (Security Risk)

        var jsonOptions = new JsonSerializerOptions();
        Logos.AI.Engine.Extensions.LogosJsonExtensions.ConfigureLogosOptions(jsonOptions);

        var response = new ErrorResponse
        {
            Code = code??statusCode.ToString(),
            Message = message,
            Details = exception.StackTrace,
            Data = data
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

public record ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public object? Data { get; set; }
    
}