/*
using Logos.AI.Abstractions.Features.Validation;
using MathNet.Numerics.Distributions;
using Microsoft.Extensions.Logging;
namespace Logos.AI.Engine.Validation;
/// <summary>
/// К-С тест (MathNet)
/// Валідатор біологічної релевантності на основі критерію Колмогорова-Смірнова.
/// Використовується для оцінки відповідності даних пацієнта очікуваним статистичним розподілам.
/// </summary>
public class BiologicalValidator : IBiologicalValidator
{
    //Варто використати там, де є аномалії.
    //Ідея: Використовуйте К-С тест для виявлення суперечливих даних.
    //Наприклад, якщо в одного пацієнта за один день є два результати одного аналізу з різних лабораторій, і вони сильно розбігаються.
    //Або для порівняння динаміки: чи вкладається поточний стрибок показника в статистичну криву одужання.

    private readonly ILogger<BiologicalValidator> _logger;

    public BiologicalValidator(ILogger<BiologicalValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Виконує К-С тест для порівняння вибірки пацієнта з теоретичним нормальним розподілом.
    /// </summary>
    /// <param name="sampleData">Набір отриманих значень (наприклад, динаміка показника).</param>
    /// <param name="mean">Еталонне середнє значення (Mean) для групи.</param>
    /// <param name="stdDev">Еталонне стандартне відхилення (StdDev) для групи.</param>
    /// <returns>Результат валідації з p-value.</returns>
    public BiologicalValidationResult ValidateDistribution(double[] sampleData, double mean, double stdDev)
    {
        if (sampleData.Length < 3)
        {
            _logger.LogWarning("Insufficient data for K-S test.");
            return new BiologicalValidationResult(false, 0, "Замало даних для статистичного аналізу");
        }

        // 1. Створюємо теоретичний розподіл (нормальний) на основі медичних норм
        var dist = new Normal(mean, stdDev);

        // 2. Розраховуємо статистику Колмогорова-Смірнова (D-statistic)
        // D = max|Fn(x) - F(x)|
        double dStatistic = KolmogorovSmirnovTest(sampleData, dist);

        // 3. Розраховуємо p-value (спрощено для великих вибірок або через апроксимацію)
        // В рамках дипломної роботи ми можемо використовувати граничні значення D для alpha=0.05
        double pValue = CalculatePValue(dStatistic, sampleData.Length);

        bool isValid = pValue > 0.05; // Якщо p < 0.05, відкидаємо гіпотезу про ідентичність розподілів

        return new BiologicalValidationResult(
            isValid, 
            pValue, 
            isValid ? "Дані відповідають біологічній нормі" : "Виявлено статистичне відхилення від норми"
        );
    }

    private double KolmogorovSmirnovTest(double[] sample, Normal referenceDist)
    {
        var sortedSample = sample.OrderBy(x => x).ToArray();
        int n = sortedSample.Length;
        double maxDiff = 0;

        for (int i = 0; i < n; i++)
        {
            double empiricalCdf = (double)(i + 1) / n;
            double theoreticalCdf = referenceDist.CumulativeDistribution(sortedSample[i]);
            
            double diff = Math.Abs(empiricalCdf - theoreticalCdf);
            if (diff > maxDiff) maxDiff = diff;
        }

        return maxDiff;
    }

    private double CalculatePValue(double d, int n)
    {
        // Наближена формула Колмогорова для p-value
        double sqrtN = Math.Sqrt(n);
        double z = (sqrtN + 0.12 + 0.11 / sqrtN) * d;
        
        // Використовуємо функцію виживання (Survival function) для розподілу Колмогорова
        if (z < 0) return 1.0;
        if (z < 0.4) return 1.0; // p-value дуже велике

        // Апроксимація серією
        double sum = 0;
        for (int k = 1; k <= 10; k++)
        {
            sum += Math.Pow(-1, k - 1) * Math.Exp(-2 * k * k * z * z);
        }
        
        double p = 2 * sum;
        return Math.Clamp(p, 0.0, 1.0);
    }
}
*/

