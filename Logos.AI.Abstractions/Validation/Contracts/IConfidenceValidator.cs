using Logos.AI.Abstractions.Reasoning;
namespace Logos.AI.Abstractions.Validation.Contracts;

public interface IConfidenceValidator
{
	Task<ConfidenceValidationResult> ValidateAsync(IReasoningResult reasoningResult);
}
