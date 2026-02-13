using Logos.AI.Abstractions.Validation;

namespace Logos.AI.Engine.Validation;

/// <summary>
/// Статичний калькулятор для розрахунку штрафних коефіцієнтів.
/// Відповідає за бізнес-логіку: за що і наскільки штрафувати.
/// </summary>
public static class ConfidencePenaltyCalculator
{
    // Поріг для слабкого токена (20%)
    private const double WeakTokenThreshold = 0.20;
    private const double HighPerplexityThreshold = 5.0;
    private const double HighEntropyThreshold = 1.2;

    // --- Штрафні коефіцієнти ---
    private const double PenaltyPerplexity = 0.6;
    private const double PenaltyEntropy = 0.75;
    private const double PenaltyWeakToken = 0.8;
    private const double PenaltyDiffuseUncertainty = 0.5;

    public static (double Penalty, List<string> Details) Calculate(
        List<(string Token, double LogProb)> meaningfulTokenData,
        LogProbMetrics metrics,
        UncertaintyResult uncertainty)
    {
        double penalty = 1.0;
        var details = new List<string>();

        // 1. Perplexity
        if (metrics.Perplexity > HighPerplexityThreshold)
        {
            penalty *= PenaltyPerplexity;
            details.Add($"Risk Penalty: High Perplexity (> {HighPerplexityThreshold:F1}) applied (x{PenaltyPerplexity}).");
        }

        // 2. Entropy
        if (metrics.Entropy > HighEntropyThreshold)
        {
            penalty *= PenaltyEntropy;
            details.Add($"Risk Penalty: High Entropy (> {HighEntropyThreshold:F1}) applied (x{PenaltyEntropy}).");
        }

        // 3. Weak Tokens (Нова комбінована логіка)
        // Перевіряємо, чи є сенс взагалі запускати глибокий аналіз
        if (metrics.WeakestTokenProbability < WeakTokenThreshold)
        {
            var weakPenaltyInfo = CalculateWeakTokenPenalty(meaningfulTokenData, metrics.WeakestTokenProbability);
            if (weakPenaltyInfo.HasPenalty)
            {
                penalty *= PenaltyWeakToken;
                details.Add(weakPenaltyInfo.Reason);
            }
        }

        // 4. Diffuse Uncertainty
        if (uncertainty.Type == UncertaintyType.Diffuse)
        {
            penalty *= PenaltyDiffuseUncertainty;
            details.Add($"Risk Penalty: Diffuse uncertainty detected (x{PenaltyDiffuseUncertainty}).");
        }

        return (penalty, details);
    }

    private static (bool HasPenalty, string Reason) CalculateWeakTokenPenalty(List<(string Token, double LogProb)> tokens, double minProb)
    {
        int totalWeak = 0;
        int consecutiveWeak = 0;
        bool clusterFound = false;

        // Проходимо один раз по списку
        foreach (var t in tokens)
        {
            // Використовуємо Math.Exp, бо LogProb - це натуральний логарифм
            if (Math.Exp(t.LogProb) < WeakTokenThreshold)
            {
                totalWeak++;
                consecutiveWeak++;
                
                if (consecutiveWeak >= 5)
                {
                    clusterFound = true;
                }
            }
            else
            {
                consecutiveWeak = 0;
            }
        }

        // Правило А: Забагато слабких токенів загалом
        if (totalWeak > 7)
        {
            return (true, $"Risk Penalty: Too many weak tokens detected ({totalWeak} > 7) (x{PenaltyWeakToken}). Min Conf: {minProb:P0}");
        }

        // Правило Б: Кластер слабких токенів підряд
        if (clusterFound)
        {
            return (true, $"Risk Penalty: Cluster of weak tokens detected (5+ in a row) (x{PenaltyWeakToken}). Min Conf: {minProb:P0}");
        }

        return (false, string.Empty);
    }
}