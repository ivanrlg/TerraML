using FuzzySat.Core.FuzzyLogic.Classification;
using FuzzySat.Core.ML;
using FuzzySat.Core.Training;

namespace FuzzySat.Web.Services;

/// <summary>
/// Bridges Core's ModelComparisonEngine to async Web operations with progress reporting.
/// </summary>
public sealed class ModelComparisonService
{
    /// <summary>
    /// Runs model comparison with the specified methods and fold count.
    /// Must be called from a background thread.
    /// </summary>
    public ModelComparisonResult Compare(
        TrainingSession session,
        IReadOnlyList<LabeledPixelSample> trainingSamples,
        string membershipFunctionType,
        IReadOnlyList<string> selectedMethods,
        int numberOfFolds,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(trainingSamples);
        ArgumentNullException.ThrowIfNull(selectedMethods);

        if (trainingSamples.Count == 0)
            throw new ArgumentException("At least one training sample is required.", nameof(trainingSamples));

        progress?.Report("Building features...");

        // Build both extractors — hybrid methods use fuzzy features, pure ML uses raw bands
        var hasFuzzy = selectedMethods.Any(m => !m.StartsWith("ML: ", StringComparison.Ordinal));
        var hasPureML = selectedMethods.Any(m => m.StartsWith("ML: ", StringComparison.Ordinal));

        IFeatureExtractor? fuzzyExtractor = null;
        if (hasFuzzy)
        {
            var ruleSet = ClassificationService.BuildRuleSet(session, membershipFunctionType);
            fuzzyExtractor = new FuzzyFeatureExtractor(ruleSet, session.BandNames.ToList());
        }

        IFeatureExtractor? rawExtractor = null;
        if (hasPureML)
            rawExtractor = new RawFeatureExtractor(session.BandNames.ToList());

        var mlSamples = trainingSamples
            .Select(s => (s.ClassName, (IDictionary<string, double>)new Dictionary<string, double>(s.BandValues)))
            .ToList();

        var factories = new List<(string Name,
            Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier> Factory)>();

        foreach (var method in selectedMethods)
        {
            factories.Add(method switch
            {
                // Hybrid methods (fuzzy-enriched features)
                "Random Forest" => (method, fold => HybridClassifier.TrainRandomForest(fold, fuzzyExtractor ?? throw new InvalidOperationException("Fuzzy extractor not initialized for hybrid method."))),
                "SDCA" => (method, fold => HybridClassifier.TrainSdca(fold, fuzzyExtractor ?? throw new InvalidOperationException("Fuzzy extractor not initialized for hybrid method."))),
                "LightGBM" => (method, fold => LightGbmClassifier.Train(fold, fuzzyExtractor ?? throw new InvalidOperationException("Fuzzy extractor not initialized for hybrid method."))),
                "SVM" => (method, fold => SvmClassifier.Train(fold, fuzzyExtractor ?? throw new InvalidOperationException("Fuzzy extractor not initialized for hybrid method."))),
                "Logistic Regression" => (method, fold => LogisticRegressionClassifier.Train(fold, fuzzyExtractor ?? throw new InvalidOperationException("Fuzzy extractor not initialized for hybrid method."))),
                // Pure ML methods (raw band values only)
                "ML: Random Forest" => (method, fold => HybridClassifier.TrainRandomForest(fold, rawExtractor ?? throw new InvalidOperationException("Raw extractor not initialized for pure ML method."))),
                "ML: SDCA" => (method, fold => HybridClassifier.TrainSdca(fold, rawExtractor ?? throw new InvalidOperationException("Raw extractor not initialized for pure ML method."))),
                "ML: LightGBM" => (method, fold => LightGbmClassifier.Train(fold, rawExtractor ?? throw new InvalidOperationException("Raw extractor not initialized for pure ML method."))),
                "ML: SVM" => (method, fold => SvmClassifier.Train(fold, rawExtractor ?? throw new InvalidOperationException("Raw extractor not initialized for pure ML method."))),
                "ML: Logistic Regression" => (method, fold => LogisticRegressionClassifier.Train(fold, rawExtractor ?? throw new InvalidOperationException("Raw extractor not initialized for pure ML method."))),
                _ => throw new ArgumentException($"Unknown method: '{method}'.")
            });
        }

        return ModelComparisonEngine.Compare(mlSamples, factories, numberOfFolds,
            progress: progress, cancellationToken: cancellationToken);
    }
}
