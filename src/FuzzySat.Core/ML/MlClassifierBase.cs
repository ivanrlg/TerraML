using FuzzySat.Core.FuzzyLogic.Classification;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FuzzySat.Core.ML;

/// <summary>
/// Abstract base class for ML.NET classifiers that use fuzzy membership degrees as features.
/// Encapsulates the shared pipeline: MLContext creation, schema definition, data loading,
/// MapValueToKey/MapKeyToValue, PredictionEngine with thread-safe lock.
/// Subclasses only provide the <see cref="IEstimator{TTransformer}"/> trainer.
/// </summary>
public abstract class MlClassifierBase : IClassifier
{
    private readonly object _lock = new();
    private readonly PredictionEngine<PixelFeatureData, PixelPrediction> _predictionEngine;
    private readonly FuzzyFeatureExtractor _featureExtractor;

    /// <summary>
    /// Initializes a new instance from a trained ML.NET model.
    /// </summary>
    protected MlClassifierBase(
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
    /// Shared training pipeline. Validates samples, extracts features, builds ML.NET pipeline,
    /// fits the model, and invokes the constructor factory to create the concrete classifier.
    /// </summary>
    /// <typeparam name="T">The concrete classifier type.</typeparam>
    /// <param name="trainingSamples">Labeled training data (class name + band values).</param>
    /// <param name="featureExtractor">Fuzzy feature extractor.</param>
    /// <param name="trainerFactory">
    /// Factory that receives an <see cref="MLContext"/> and the feature count,
    /// and returns the trainer estimator to append to the pipeline.
    /// </param>
    /// <param name="constructor">
    /// Factory that receives the MLContext, trained model, feature extractor, and schema,
    /// and returns the concrete classifier instance.
    /// </param>
    protected static T TrainBase<T>(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        FuzzyFeatureExtractor featureExtractor,
        Func<MLContext, int, IEstimator<ITransformer>> trainerFactory,
        Func<MLContext, ITransformer, FuzzyFeatureExtractor, SchemaDefinition, T> constructor)
        where T : MlClassifierBase
    {
        ArgumentNullException.ThrowIfNull(trainingSamples);
        ArgumentNullException.ThrowIfNull(featureExtractor);

        if (trainingSamples.Count == 0)
            throw new ArgumentException("At least one training sample is required.", nameof(trainingSamples));

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

        return constructor(mlContext, model, featureExtractor, schemaDef);
    }
}
