using FuzzySat.Core.FuzzyLogic.Classification;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FuzzySat.Core.ML;

/// <summary>
/// Hybrid classifier that uses ML.NET with fuzzy membership degrees as features.
/// Supports multiple ML trainers (Random Forest, Neural Network, etc.).
/// </summary>
public sealed class HybridClassifier : IClassifier
{
    private readonly MLContext _mlContext;
    private readonly ITransformer _model;
    private readonly FuzzyFeatureExtractor _featureExtractor;
    private readonly PredictionEngine<PixelFeatureData, PixelPrediction> _predictionEngine;

    private HybridClassifier(
        MLContext mlContext,
        ITransformer model,
        FuzzyFeatureExtractor featureExtractor,
        SchemaDefinition inputSchema)
    {
        _mlContext = mlContext;
        _model = model;
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
        var prediction = _predictionEngine.Predict(input);
        return prediction.PredictedLabel;
    }

    /// <summary>
    /// Trains a hybrid classifier using a Random Forest (FastForest) trainer.
    /// </summary>
    public static HybridClassifier TrainRandomForest(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        FuzzyFeatureExtractor featureExtractor,
        int numberOfTrees = 100)
    {
        return Train(trainingSamples, featureExtractor, (mlContext, featureCount) =>
            mlContext.MulticlassClassification.Trainers.OneVersusAll(
                mlContext.BinaryClassification.Trainers.FastForest(
                    numberOfTrees: numberOfTrees)));
    }

    /// <summary>
    /// Trains a hybrid classifier using a Stochastic Dual Coordinate Ascent (SDCA) trainer.
    /// Lightweight alternative to neural networks for multiclass classification.
    /// </summary>
    public static HybridClassifier TrainSdca(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        FuzzyFeatureExtractor featureExtractor,
        int maximumNumberOfIterations = 100)
    {
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

        var mlContext = new MLContext(seed: 42);
        var featureCount = featureExtractor.FeatureNames.Count;

        // Build training data
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

        // Build pipeline
        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(trainerFactory(mlContext, featureCount))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(dataView);

        return new HybridClassifier(mlContext, model, featureExtractor, schemaDef);
    }
}
