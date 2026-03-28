namespace FuzzySat.Core.Validation;

/// <summary>
/// Aggregated accuracy metrics extracted from a confusion matrix.
/// Provides a summary view of classification performance.
/// </summary>
public sealed class AccuracyMetrics
{
    /// <summary>Gets the overall accuracy (proportion correct).</summary>
    public double OverallAccuracy { get; }

    /// <summary>Gets Cohen's Kappa coefficient.</summary>
    public double KappaCoefficient { get; }

    /// <summary>Gets the total number of test samples.</summary>
    public int TotalSamples { get; }

    /// <summary>Gets the number of correctly classified samples.</summary>
    public int CorrectCount { get; }

    /// <summary>Gets per-class metrics.</summary>
    public IReadOnlyList<ClassMetrics> PerClassMetrics { get; }

    /// <summary>
    /// Computes accuracy metrics from a confusion matrix.
    /// </summary>
    public AccuracyMetrics(ConfusionMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        OverallAccuracy = matrix.OverallAccuracy;
        KappaCoefficient = matrix.KappaCoefficient;
        TotalSamples = matrix.TotalSamples;
        CorrectCount = matrix.CorrectCount;

        PerClassMetrics = matrix.ClassNames
            .Select(c => new ClassMetrics(
                c,
                matrix.ProducersAccuracy(c),
                matrix.UsersAccuracy(c),
                matrix.RowTotal(c),
                matrix.ColumnTotal(c)))
            .ToList()
            .AsReadOnly();
    }
}
