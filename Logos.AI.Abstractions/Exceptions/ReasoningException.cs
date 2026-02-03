using System.Net;
namespace Logos.AI.Abstractions.Exceptions;

public class ReasoningException : LogosException
{
	public ReasoningException(HttpStatusCode httpStatusCode, string message, Exception innerException) : base(httpStatusCode, message, innerException)
	{
	}
	public ReasoningException(string message, HttpStatusCode httpStatusCode) : base(message, httpStatusCode)
	{
	}
	public ReasoningException(string message) : base(message)
	{
	}
	
	public ReasoningException(string message, object data) : base(message)
	{
		Data = data;
	}
	
	public ReasoningException(string message, object data, Exception exception) : base(message, exception)
	{
		Data = data;
	}
}
