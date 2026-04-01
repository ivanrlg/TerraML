using FuzzySat.Core.Classification;
using FuzzySat.Core.ML;
using FuzzySat.Core.Raster;
using FuzzySat.Core.Training;

namespace FuzzySat.Web.Services;

/// <summary>
/// Bridges Core's HybridClassifier (ML.NET Random Forest / SDCA) to async Web
/// operations with progress reporting. Uses fuzzy membership degrees as enriched
/// features for ML training, then classifies pixel-by-pixel.
/// Registered as singleton since it holds no mutable state.
/// </summary>
public sealed class HybridClassificationService
{
    /// <summary>
    /// Classifies a multispectral image using an ML.NET hybrid classifier
    /// trained on fuzzy-enriched features from the given training samples.
    /// Must be called from a background thread (via Task.Run) to avoid blocking Blazor UI.
    /// </summary>
    public ClassificationResult Classify(
        MultispectralImage image,
        TrainingSession session,
        IReadOnlyList<LabeledPixelSample> trainingSamples,
        ClassificationOptions options,
        IProgress<ClassificationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(trainingSamples);
        ArgumentNullException.ThrowIfNull(options);

        if (trainingSamples.Count == 0)
            throw new ArgumentException("At least one training sample is required.", nameof(trainingSamples));

        // Stage 1: Build fuzzy rule set (needed for feature extraction)
        progress?.Report(new ClassificationProgress("Building membership functions", 0, image.Rows, 0));
        var ruleSet = ClassificationService.BuildRuleSet(session, options.MembershipFunctionType);

        // Stage 2: Create feature extractor and train ML model
        progress?.Report(new ClassificationProgress("Training ML model", 0, image.Rows, 5));
        var featureExtractor = new FuzzyFeatureExtractor(ruleSet, session.BandNames.ToList());

        var mlSamples = trainingSamples
            .Select(s => (s.ClassName, (IDictionary<string, double>)new Dictionary<string, double>(s.BandValues)))
            .ToList();

        cancellationToken.ThrowIfCancellationRequested();

        var classifier = TrainClassifier(options.ClassificationMethod, mlSamples, featureExtractor);

        progress?.Report(new ClassificationProgress("ML model trained", 0, image.Rows, 15));

        // Stage 3: Classify pixel-by-pixel using the hybrid classifier
        var classMap = new string[image.Rows, image.Columns];
        var confidenceMap = new double[image.Rows, image.Columns];

        for (var row = 0; row < image.Rows; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var col = 0; col < image.Columns; col++)
            {
                var pixel = image.GetPixelVector(row, col);
                var bandValues = (IDictionary<string, double>)pixel.BandValues;

                classMap[row, col] = classifier.ClassifyPixel(bandValues);
                // HybridClassifier doesn't provide confidence, use 1.0 for predicted class
                confidenceMap[row, col] = 1.0;
            }

            if (row % 10 == 0 || row == image.Rows - 1)
            {
                var pct = 15.0 + (row + 1.0) / image.Rows * 80.0; // 15-95%
                progress?.Report(new ClassificationProgress(
                    "Classifying pixels", row + 1, image.Rows, pct));
            }
        }

        // Stage 4: Build result
        progress?.Report(new ClassificationProgress("Building result", image.Rows, image.Rows, 98));

        var classes = session.ClassNames
            .Select((name, i) => new LandCoverClass { Name = name, Code = i + 1 })
            .ToList();

        var result = new ClassificationResult(classMap, confidenceMap, classes);
        progress?.Report(new ClassificationProgress("Complete", image.Rows, image.Rows, 100));

        return result;
    }

    /// <summary>
    /// Trains the appropriate ML classifier based on the method name.
    /// </summary>
    private static FuzzySat.Core.FuzzyLogic.Classification.IClassifier TrainClassifier(
        string method,
        List<(string, IDictionary<string, double>)> samples,
        FuzzyFeatureExtractor extractor) => method switch
    {
        "Random Forest" => HybridClassifier.TrainRandomForest(samples, extractor),
        "SDCA" => HybridClassifier.TrainSdca(samples, extractor),
        "LightGBM" => LightGbmClassifier.Train(samples, extractor),
        "SVM" => SvmClassifier.Train(samples, extractor),
        "Logistic Regression" => LogisticRegressionClassifier.Train(samples, extractor),
        "MLP Neural Network" => NeuralNetClassifier.Train(samples, extractor),
        "Ensemble (Voting)" => TrainVotingEnsemble(samples, extractor),
        "Ensemble (Stacking)" => StackingClassifier.Train(samples, extractor,
            GetDefaultBaseFactories(extractor), numberOfFolds: 3),
        _ => throw new ArgumentException($"Unknown hybrid method: '{method}'.")
    };

    private static EnsembleClassifier TrainVotingEnsemble(
        List<(string, IDictionary<string, double>)> samples,
        FuzzyFeatureExtractor extractor)
    {
        var classifiers = new FuzzySat.Core.FuzzyLogic.Classification.IClassifier[]
        {
            HybridClassifier.TrainRandomForest(samples, extractor),
            HybridClassifier.TrainSdca(samples, extractor),
            LightGbmClassifier.Train(samples, extractor)
        };
        return EnsembleClassifier.MajorityVote(classifiers);
    }

    private static List<Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>,
        FuzzySat.Core.FuzzyLogic.Classification.IClassifier>> GetDefaultBaseFactories(
        FuzzyFeatureExtractor extractor) =>
    [
        fold => HybridClassifier.TrainRandomForest(fold, extractor, numberOfTrees: 50),
        fold => HybridClassifier.TrainSdca(fold, extractor),
        fold => LightGbmClassifier.Train(fold, extractor)
    ];
}
