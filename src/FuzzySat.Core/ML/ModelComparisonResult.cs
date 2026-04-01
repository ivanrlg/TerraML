namespace FuzzySat.Core.ML;

/// <summary>
/// Result of comparing a single classifier via cross-validation.
/// </summary>
public sealed record ClassifierResult(
    string Name,
    double MeanOA,
    double StdOA,
    double MeanKappa,
    double StdKappa,
    long TrainingTimeMs);

/// <summary>
/// Aggregated result of comparing multiple classifiers.
/// Results are sorted by MeanKappa descending (best first).
/// </summary>
public sealed class ModelComparisonResult
{
    /// <summary>Gets the per-classifier results sorted by MeanKappa descending.</summary>
    public IReadOnlyList<ClassifierResult> Results { get; }

    /// <summary>Gets the best model (highest MeanKappa).</summary>
    public ClassifierResult BestModel => Results[0];

    /// <summary>
    /// Creates a comparison result from per-classifier results.
    /// </summary>
    public ModelComparisonResult(IReadOnlyList<ClassifierResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results.Count == 0)
            throw new ArgumentException("At least one classifier result is required.", nameof(results));

        Results = results.OrderByDescending(r => r.MeanKappa).ToList();
    }
}
