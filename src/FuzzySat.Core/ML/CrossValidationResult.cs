using FuzzySat.Core.Validation;

namespace FuzzySat.Core.ML;

/// <summary>
/// Aggregated result of stratified k-fold cross-validation.
/// Contains per-fold metrics and summary statistics.
/// </summary>
public sealed class CrossValidationResult
{
    /// <summary>Gets the per-fold accuracy metrics.</summary>
    public IReadOnlyList<AccuracyMetrics> FoldMetrics { get; }

    /// <summary>Gets the number of folds used.</summary>
    public int NumberOfFolds => FoldMetrics.Count;

    /// <summary>Gets the mean overall accuracy across all folds.</summary>
    public double MeanOverallAccuracy { get; }

    /// <summary>Gets the standard deviation of overall accuracy across folds.</summary>
    public double StdOverallAccuracy { get; }

    /// <summary>Gets the mean Kappa coefficient across all folds.</summary>
    public double MeanKappa { get; }

    /// <summary>Gets the standard deviation of Kappa across folds.</summary>
    public double StdKappa { get; }

    /// <summary>
    /// Creates a cross-validation result from per-fold metrics.
    /// </summary>
    public CrossValidationResult(IReadOnlyList<AccuracyMetrics> foldMetrics)
    {
        ArgumentNullException.ThrowIfNull(foldMetrics);
        if (foldMetrics.Count == 0)
            throw new ArgumentException("At least one fold result is required.", nameof(foldMetrics));

        FoldMetrics = foldMetrics;

        var oaValues = foldMetrics.Select(m => m.OverallAccuracy).ToArray();
        var kappaValues = foldMetrics.Select(m => m.KappaCoefficient).ToArray();

        MeanOverallAccuracy = oaValues.Average();
        MeanKappa = kappaValues.Average();
        StdOverallAccuracy = ComputeStdDev(oaValues);
        StdKappa = ComputeStdDev(kappaValues);
    }

    private static double ComputeStdDev(double[] values)
    {
        if (values.Length <= 1)
            return 0.0;

        var mean = values.Average();
        var sumSqDiff = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSqDiff / (values.Length - 1));
    }
}
