using Logos.AI.Abstractions.Reasoning;
using Logos.AI.Abstractions.Validation;
using Logos.AI.Abstractions.Validation.Contracts;
using Microsoft.Extensions.Logging;

namespace Logos.AI.Engine.Validation;

public class ConfidenceValidator(ILogger<ConfidenceValidator> logger) : IConfidenceValidator
{
    // Вага "слабкої ланки". 
    // Якщо встановити 1.0 - оцінка буде дорівнювати найменш впевненому токену.
    // Якщо 0.0 - оцінка буде просто середнім арифметичним.
    // 0.4 - збалансований підхід.
    private const double WeakestLinkWeight = 0.4; 

    public Task<ConfidenceValidationResult> ValidateAsync(IReasoningResult reasoningResult)
    {
        var details = new List<string>();

        // 1. Якщо LogProbs немає (модель не повернула або це старий формат)
        if (reasoningResult.LogProbs == null || reasoningResult.LogProbs.Count == 0)
        {
            details.Add("No LogProbs provided by LLM. Assuming Medium confidence fallback.");
            return Task.FromResult(new ConfidenceValidationResult
            {
                Score = 0.7, // Fallback
                Details = details
            });
        }

        // 2. Математика
        // 2.1 Середня впевненість (Linear Probability)
        // LogProb - це логарифм (наприклад -0.1). Linear = exp(-0.1) ≈ 0.9
        var linearProbs = reasoningResult.LogProbs
            .Where(p => p.LinearProbability.HasValue)
            .Select(p => p.LinearProbability!.Value)
            .ToList();

        if (linearProbs.Count == 0) // Якщо раптом щось пішло не так з конвертацією
        {
             return Task.FromResult(new ConfidenceValidationResult { Score = 0.5 });
        }

        double avgConfidence = linearProbs.Average();
        details.Add($"Average Token Confidence: {avgConfidence:P1}");

        // 2.2 Min Probability (Найслабша ланка)
        // Ми беремо не 1 найгірший токен (це може бути просто кома), а 5-й перцентиль найгірших, 
        // або просто мінімум, якщо текст короткий.
        double minConfidence = linearProbs.Min();
        
        // Знаходимо саме слово, в якому модель сумнівалася (для логів)
        var weakestToken = reasoningResult.LogProbs.MinBy(p => p.LinearProbability)?.Token;
        details.Add($"Weakest Link (Min Confidence): {minConfidence:P1} (Token: '{weakestToken}')");

        // 2.3 Перплексія (Perplexity) - міра "здивування"
        // PPL = exp( - sum(log_probs) / N )
        // Чим менше, тим краще. > 20 - погано. < 5 - супер.
        double sumLogProbs = reasoningResult.LogProbs.Sum(p => p.LogProb);
        double perplexity = Math.Exp(-sumLogProbs / reasoningResult.LogProbs.Count);
        
        // Нормалізуємо перплексію у Score (0..1), де 1 - ідеально.
        // Це емпірична формула: Score = 1 / (1 + 0.1 * (PPL - 1))
        // Якщо PPL=1 (ідеал), Score=1. Якщо PPL=11, Score=0.5. Якщо PPL=100, Score=0.09.
        double perplexityScore = 1.0 / (1.0 + 0.1 * Math.Max(0, perplexity - 1));
        details.Add($"Perplexity: {perplexity:F2} (Score contribution: {perplexityScore:P1})");


        // 3. Фінальна формула Score (Weighted Ensemble)
        // Ми комбінуємо Average (загальне розуміння) та Min (безпека).
        // Score = (Avg * 0.6) + (Min * 0.4)
        // Це "карає" модель, якщо хоча б частина відповіді є галюцинацією.
        
        double finalScore = (avgConfidence * (1 - WeakestLinkWeight)) + (minConfidence * WeakestLinkWeight);
        
        // Додатковий штраф за високу перплексію (якщо модель пише "брєд")
        if (perplexity > 10.0)
        {
            finalScore *= 0.8; // Штраф 20%
            details.Add("Penalty applied due to high perplexity (unstable text generation).");
        }

        return Task.FromResult(new ConfidenceValidationResult
        {
            Score = finalScore,
            Details = details
        });
    }
}