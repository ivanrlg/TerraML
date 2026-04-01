using FuzzySat.Core.FuzzyLogic.Classification;
using FuzzySat.Core.FuzzyLogic.Rules;

namespace FuzzySat.Core.ML;

/// <summary>
/// High-level orchestrator for benchmarking classification methods.
/// Bridges method names to classifier factories and delegates to <see cref="ModelComparisonEngine"/>.
/// </summary>
public static class ClassifierBenchmark
{
    /// <summary>
    /// Runs a cross-validated benchmark comparing the specified classification methods.
    /// </summary>
    /// <param name="samples">Labeled training samples.</param>
    /// <param name="ruleSet">Fuzzy rule set for hybrid methods. Null if only pure-ML methods are selected.</param>
    /// <param name="bandNames">Band names matching the sample dictionaries.</param>
    /// <param name="methodNames">Method names to compare (e.g., "Random Forest", "ML: LightGBM").</param>
    /// <param name="numberOfFolds">Number of cross-validation folds.</param>
    /// <param name="progress">Optional progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ranked comparison result sorted by Kappa descending.</returns>
    public static ModelComparisonResult RunBenchmark(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> samples,
        FuzzyRuleSet? ruleSet,
        IReadOnlyList<string> bandNames,
        IReadOnlyList<string> methodNames,
        int numberOfFolds = 5,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(bandNames);
        ArgumentNullException.ThrowIfNull(methodNames);

        if (samples.Count == 0)
            throw new ArgumentException("At least one training sample is required.", nameof(samples));

        if (methodNames.Count == 0)
            throw new ArgumentException("At least one method name is required.", nameof(methodNames));

        progress?.Report("Building features...");

        var hasHybrid = methodNames.Any(m => !m.StartsWith("ML: ", StringComparison.Ordinal));
        var hasPureML = methodNames.Any(m => m.StartsWith("ML: ", StringComparison.Ordinal));

        IFeatureExtractor? fuzzyExtractor = null;
        if (hasHybrid)
        {
            if (ruleSet is null)
                throw new ArgumentException(
                    "A FuzzyRuleSet is required when hybrid methods are selected.", nameof(ruleSet));
            fuzzyExtractor = new FuzzyFeatureExtractor(ruleSet, bandNames.ToList());
        }

        IFeatureExtractor? rawExtractor = null;
        if (hasPureML)
            rawExtractor = new RawFeatureExtractor(bandNames.ToList());

        var factories = new List<(string Name,
            Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier> Factory)>();

        foreach (var method in methodNames)
        {
            var extractor = method.StartsWith("ML: ", StringComparison.Ordinal)
                ? rawExtractor ?? throw new InvalidOperationException("Raw extractor not initialized.")
                : fuzzyExtractor ?? throw new InvalidOperationException("Fuzzy extractor not initialized.");

            factories.Add(method switch
            {
                "Random Forest" => (method, fold => HybridClassifier.TrainRandomForest(fold, extractor)),
                "SDCA" => (method, fold => HybridClassifier.TrainSdca(fold, extractor)),
                "LightGBM" => (method, fold => LightGbmClassifier.Train(fold, extractor)),
                "SVM" => (method, fold => SvmClassifier.Train(fold, extractor)),
                "Logistic Regression" => (method, fold => LogisticRegressionClassifier.Train(fold, extractor)),
                "ML: Random Forest" => (method, fold => HybridClassifier.TrainRandomForest(fold, extractor)),
                "ML: SDCA" => (method, fold => HybridClassifier.TrainSdca(fold, extractor)),
                "ML: LightGBM" => (method, fold => LightGbmClassifier.Train(fold, extractor)),
                "ML: SVM" => (method, fold => SvmClassifier.Train(fold, extractor)),
                "ML: Logistic Regression" => (method, fold => LogisticRegressionClassifier.Train(fold, extractor)),
                _ => throw new ArgumentException($"Unknown classification method: '{method}'.", nameof(methodNames))
            });
        }

        return ModelComparisonEngine.Compare(samples, factories, numberOfFolds,
            progress: progress, cancellationToken: cancellationToken);
    }
}
