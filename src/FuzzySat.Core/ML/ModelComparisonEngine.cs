using System.Diagnostics;
using FuzzySat.Core.FuzzyLogic.Classification;

namespace FuzzySat.Core.ML;

/// <summary>
/// Runs cross-validation for multiple classifiers and produces a ranked comparison.
/// </summary>
public static class ModelComparisonEngine
{
    /// <summary>
    /// Compares classifiers by running k-fold cross-validation for each.
    /// </summary>
    /// <param name="samples">All labeled training samples.</param>
    /// <param name="classifierFactories">
    /// Named factories: each produces a trained classifier from a sample subset.
    /// </param>
    /// <param name="numberOfFolds">Number of CV folds.</param>
    /// <param name="seed">Random seed for reproducibility.</param>
    /// <param name="progress">Optional progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison result with per-classifier metrics sorted by Kappa.</returns>
    public static ModelComparisonResult Compare(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> samples,
        IReadOnlyList<(string Name, Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier> Factory)> classifierFactories,
        int numberOfFolds = 5,
        int seed = 42,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(classifierFactories);

        if (classifierFactories.Count == 0)
            throw new ArgumentException("At least one classifier factory is required.", nameof(classifierFactories));

        var results = new List<ClassifierResult>();

        for (var i = 0; i < classifierFactories.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (name, factory) = classifierFactories[i];
            progress?.Report($"Evaluating {name} ({i + 1}/{classifierFactories.Count})...");

            var sw = Stopwatch.StartNew();
            var cvResult = CrossValidator.Evaluate(samples, factory, numberOfFolds, seed);
            sw.Stop();

            results.Add(new ClassifierResult(
                name,
                cvResult.MeanOverallAccuracy,
                cvResult.StdOverallAccuracy,
                cvResult.MeanKappa,
                cvResult.StdKappa,
                sw.ElapsedMilliseconds));

            progress?.Report($"{name}: OA={cvResult.MeanOverallAccuracy:P1} Kappa={cvResult.MeanKappa:F3} ({sw.ElapsedMilliseconds}ms)");
        }

        return new ModelComparisonResult(results);
    }
}
