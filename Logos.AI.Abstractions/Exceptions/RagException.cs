using System.Net;
using Logos.AI.Abstractions.Reasoning;
using Logos.AI.Abstractions.Validation;
namespace Logos.AI.Abstractions.Exceptions;

public class RagException : LogosException
{
	public RagException(HttpStatusCode httpStatusCode, string message, Exception innerException) : base(httpStatusCode, message, innerException)
	{
	}
	public RagException(string message, HttpStatusCode httpStatusCode) : base(message, httpStatusCode)
	{
	}
	public RagException(string message) : base(message)
	{
	}
	public RagException(string message, Exception exception) : base(message, exception)
	{
	}
	public RagException(string message, object data) : base(message, data)
	{
	}
	public RagException(string message, object data, Exception exception) : base(message, data, exception)
	{
	}

	public static void ThrowForNotMedical(IReasoningResult reasoningResult) =>
		throw new RagException("Medical context is not valid: Data is not medical.", reasoningResult);
	public static void ThrowForConfidanceValidationFailed(ConfidenceValidationResult validationRes) =>
		throw new RagException("Medical context confidence validation failed.", validationRes);
}
