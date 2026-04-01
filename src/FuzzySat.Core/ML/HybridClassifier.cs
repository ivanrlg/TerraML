using FuzzySat.Core.FuzzyLogic.Classification;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FuzzySat.Core.ML;

/// <summary>
/// Hybrid classifier that uses ML.NET with an <see cref="IFeatureExtractor"/> for features.
/// Supports Random Forest (FastForest/OVA) and SDCA MaximumEntropy trainers.
/// Extends <see cref="MlClassifierBase"/> for shared ML.NET pipeline.
/// Thread-safe: inherits lock-based prediction from base class.
/// </summary>
public sealed class HybridClassifier : MlClassifierBase
{
    private HybridClassifier(
        MLContext mlContext,
        ITransformer model,
        IFeatureExtractor featureExtractor,
        SchemaDefinition inputSchema)
        : base(mlContext, model, featureExtractor, inputSchema)
    {
    }

    /// <summary>
    /// Trains a hybrid classifier using a Random Forest (FastForest/OVA) trainer.
    /// </summary>
    /// <param name="trainingSamples">Labeled training data.</param>
    /// <param name="featureExtractor">Feature extractor for transforming band values into ML features.</param>
    /// <param name="numberOfTrees">Number of trees in the forest. Must be at least 1.</param>
    public static HybridClassifier TrainRandomForest(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        IFeatureExtractor featureExtractor,
        int numberOfTrees = 100)
    {
        if (numberOfTrees < 1)
            throw new ArgumentOutOfRangeException(nameof(numberOfTrees), "Must be at least 1.");

        return TrainBase(trainingSamples, featureExtractor, (mlContext, _) =>
            mlContext.MulticlassClassification.Trainers.OneVersusAll(
                mlContext.BinaryClassification.Trainers.FastForest(
                    numberOfTrees: numberOfTrees)),
            (ctx, model, ext, schema) => new HybridClassifier(ctx, model, ext, schema));
    }

    /// <summary>
    /// Trains a hybrid classifier using SDCA MaximumEntropy trainer.
    /// </summary>
    /// <param name="trainingSamples">Labeled training data.</param>
    /// <param name="featureExtractor">Feature extractor for transforming band values into ML features.</param>
    /// <param name="maximumNumberOfIterations">Max iterations. Must be at least 1.</param>
    public static HybridClassifier TrainSdca(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        IFeatureExtractor featureExtractor,
        int maximumNumberOfIterations = 100)
    {
        if (maximumNumberOfIterations < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumNumberOfIterations), "Must be at least 1.");

        return TrainBase(trainingSamples, featureExtractor, (mlContext, _) =>
            mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                maximumNumberOfIterations: maximumNumberOfIterations),
            (ctx, model, ext, schema) => new HybridClassifier(ctx, model, ext, schema));
    }
}
