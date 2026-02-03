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
        var response = new ErrorResponse();
        var statusCode = HttpStatusCode.InternalServerError;
        var message = exception.Message;
        switch (exception)
        {
            case LogosException logosException:
                statusCode =logosException.HttpStatusCode;
                break;
            
            case InvalidOperationException:
                statusCode = HttpStatusCode.BadRequest;
                break;

            default:
                logger.LogError(exception, "Unhandled exception occurred");
                message = $"An internal server error occurred: {exception.Message}"; 
                break;
        }

        context.Response.StatusCode = (int)statusCode;
        //TODO if isRelease don't show stack trace and message (Security Risk)

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            RespectNullableAnnotations = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };213
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

public record ErrorResponse
{
    public string Code { get; set; }
    public string Message { get; set; }
    public string Details { get; set; }
    public object Data { get; set; }
}