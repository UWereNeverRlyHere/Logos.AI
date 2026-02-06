using System.Net;
namespace Logos.AI.Abstractions.Exceptions;

public abstract class LogosException : Exception
{
	public HttpStatusCode HttpStatusCode { get; init; } = HttpStatusCode.InternalServerError;
	public object? Data { get; init; } = null;
	public string? Code { get; init; } = null;
	public LogosException(HttpStatusCode httpStatusCode, string message, Exception innerException) : base(message, innerException)
	{
		HttpStatusCode = httpStatusCode;
	}
	public LogosException(string message, HttpStatusCode httpStatusCode) : base(message) => HttpStatusCode = httpStatusCode;
	public LogosException(string message) : base(message)
	{
	}
	public LogosException(string message, Exception exception) : base(message, exception)
	{
	}
	public LogosException(string message, object data) : base(message)
	{
		Data = data;
	}
	
	public LogosException(string message, object data, Exception exception) : base(message, exception)
	{
		Data = data;
	}
}
public class NotFoundException(string   name, object key) : LogosException($"Entity '{name}' ({key}) was not found.", HttpStatusCode.NotFound);
public class ValidationException(string message) : LogosException(message, HttpStatusCode.BadRequest);
