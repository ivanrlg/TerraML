using FuzzySat.Core.FuzzyLogic.Classification;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FuzzySat.Core.ML;

/// <summary>
/// Hybrid classifier that uses ML.NET with fuzzy membership degrees as features.
/// Supports Random Forest (FastForest/OVA) and SDCA MaximumEntropy trainers.
/// Thread-safe: uses lock around PredictionEngine (not thread-safe by design).
/// </summary>
public sealed class HybridClassifier : IClassifier
{
    private readonly object _lock = new();
    private readonly PredictionEngine<PixelFeatureData, PixelPrediction> _predictionEngine;
    private readonly FuzzyFeatureExtractor _featureExtractor;

    private HybridClassifier(
        MLContext mlContext,
        ITransformer model,
        FuzzyFeatureExtractor featureExtractor,
        SchemaDefinition inputSchema)
    {
        _featureExtractor = featureExtractor;
        _predictionEngine = mlContext.Model.CreatePredictionEngine<PixelFeatureData, PixelPrediction>(
            model, inputSchemaDefinition: inputSchema);
    }

    /// <inheritdoc />
    public string ClassifyPixel(IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);

        var features = _featureExtractor.ExtractFeatures(bandValues);
        var input = new PixelFeatureData { Features = features };

        lock (_lock)
        {
            return _predictionEngine.Predict(input).PredictedLabel;
        }
    }

    /// <summary>
    /// Trains a hybrid classifier using a Random Forest (FastForest/OVA) trainer.
    /// </summary>
    /// <param name="trainingSamples">Labeled training data.</param>
    /// <param name="featureExtractor">Fuzzy feature extractor.</param>
    /// <param name="numberOfTrees">Number of trees in the forest. Must be at least 1.</param>
    public static HybridClassifier TrainRandomForest(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        FuzzyFeatureExtractor featureExtractor,
        int numberOfTrees = 100)
    {
        if (numberOfTrees < 1)
            throw new ArgumentOutOfRangeException(nameof(numberOfTrees), "Must be at least 1.");

        return Train(trainingSamples, featureExtractor, (mlContext, _) =>
            mlContext.MulticlassClassification.Trainers.OneVersusAll(
                mlContext.BinaryClassification.Trainers.FastForest(
                    numberOfTrees: numberOfTrees)));
    }

    /// <summary>
    /// Trains a hybrid classifier using SDCA MaximumEntropy trainer.
    /// </summary>
    /// <param name="trainingSamples">Labeled training data.</param>
    /// <param name="featureExtractor">Fuzzy feature extractor.</param>
    /// <param name="maximumNumberOfIterations">Max iterations. Must be at least 1.</param>
    public static HybridClassifier TrainSdca(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        FuzzyFeatureExtractor featureExtractor,
        int maximumNumberOfIterations = 100)
    {
        if (maximumNumberOfIterations < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumNumberOfIterations), "Must be at least 1.");

        return Train(trainingSamples, featureExtractor, (mlContext, _) =>
            mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                maximumNumberOfIterations: maximumNumberOfIterations));
    }

    private static HybridClassifier Train(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        FuzzyFeatureExtractor featureExtractor,
        Func<MLContext, int, IEstimator<ITransformer>> trainerFactory)
    {
        ArgumentNullException.ThrowIfNull(trainingSamples);
        ArgumentNullException.ThrowIfNull(featureExtractor);

        if (trainingSamples.Count == 0)
            throw new ArgumentException("At least one training sample is required.", nameof(trainingSamples));

        // Validate samples
        for (var i = 0; i < trainingSamples.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(trainingSamples[i].Label))
                throw new ArgumentException($"Sample at index {i} has a null or whitespace label.", nameof(trainingSamples));
            if (trainingSamples[i].BandValues is null)
                throw new ArgumentException($"Sample at index {i} has null band values.", nameof(trainingSamples));
        }

        var mlContext = new MLContext(seed: 42);
        var featureCount = featureExtractor.FeatureNames.Count;

        var dataList = new List<PixelFeatureData>();
        foreach (var (label, bandValues) in trainingSamples)
        {
            dataList.Add(new PixelFeatureData
            {
                Label = label,
                Features = featureExtractor.ExtractFeatures(bandValues)
            });
        }

        var schemaDef = SchemaDefinition.Create(typeof(PixelFeatureData));
        schemaDef["Features"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureCount);
        var dataView = mlContext.Data.LoadFromEnumerable(dataList, schemaDef);

        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(trainerFactory(mlContext, featureCount))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(dataView);

        return new HybridClassifier(mlContext, model, featureExtractor, schemaDef);
    }
}
