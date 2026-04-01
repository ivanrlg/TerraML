using FuzzySat.Core.FuzzyLogic.Classification;
using FuzzySat.Core.Validation;

namespace FuzzySat.Core.ML;

/// <summary>
/// Stratified k-fold cross-validator for <see cref="IClassifier"/> implementations.
/// Splits data preserving class proportions, trains on k-1 folds, evaluates on held-out fold.
/// </summary>
public static class CrossValidator
{
    /// <summary>
    /// Runs stratified k-fold cross-validation using the provided classifier factory.
    /// </summary>
    /// <param name="samples">All labeled training samples.</param>
    /// <param name="classifierFactory">
    /// Factory that trains a classifier from a subset of samples.
    /// Receives the training fold samples and returns a trained <see cref="IClassifier"/>.
    /// </param>
    /// <param name="numberOfFolds">Number of folds (must be at least 2).</param>
    /// <param name="seed">Random seed for reproducible shuffling.</param>
    /// <returns>Aggregated cross-validation result with per-fold and summary metrics.</returns>
    public static CrossValidationResult Evaluate(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> samples,
        Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier> classifierFactory,
        int numberOfFolds = 5,
        int seed = 42)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(classifierFactory);

        if (numberOfFolds < 2)
            throw new ArgumentOutOfRangeException(nameof(numberOfFolds), "Must be at least 2.");

        var distinctClasses = samples.Select(s => s.Label).Distinct().Count();
        if (samples.Count < numberOfFolds)
            throw new ArgumentException(
                $"Need at least {numberOfFolds} samples for {numberOfFolds}-fold CV, got {samples.Count}.",
                nameof(samples));
        if (distinctClasses < 2)
            throw new ArgumentException(
                "At least 2 distinct classes are required for cross-validation.", nameof(samples));

        var folds = CreateStratifiedFolds(samples, numberOfFolds, seed);
        var foldMetrics = new List<AccuracyMetrics>();

        for (var i = 0; i < numberOfFolds; i++)
        {
            var trainSamples = new List<(string Label, IDictionary<string, double> BandValues)>();
            for (var j = 0; j < numberOfFolds; j++)
            {
                if (j != i)
                    trainSamples.AddRange(folds[j]);
            }

            var testSamples = folds[i];

            var classifier = classifierFactory(trainSamples);
            try
            {
                var actual = new List<string>();
                var predicted = new List<string>();
                foreach (var (label, bandValues) in testSamples)
                {
                    actual.Add(label);
                    predicted.Add(classifier.ClassifyPixel(bandValues));
                }

                var matrix = new ConfusionMatrix(actual, predicted);
                foldMetrics.Add(new AccuracyMetrics(matrix));
            }
            finally
            {
                (classifier as IDisposable)?.Dispose();
            }
        }

        return new CrossValidationResult(foldMetrics);
    }

    /// <summary>
    /// Creates stratified folds: shuffles within each class, distributes round-robin across folds.
    /// </summary>
    private static List<List<(string Label, IDictionary<string, double> BandValues)>> CreateStratifiedFolds(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> samples,
        int numberOfFolds,
        int seed)
    {
        var rng = new Random(seed);

        // Group by class and shuffle each group
        var byClass = samples
            .GroupBy(s => s.Label)
            .ToDictionary(g => g.Key, g => g.OrderBy(_ => rng.Next()).ToList());

        var folds = new List<List<(string Label, IDictionary<string, double> BandValues)>>();
        for (var i = 0; i < numberOfFolds; i++)
            folds.Add([]);

        // Round-robin distribute each class across folds
        foreach (var classSamples in byClass.Values)
        {
            for (var i = 0; i < classSamples.Count; i++)
                folds[i % numberOfFolds].Add(classSamples[i]);
        }

        return folds;
    }
}
