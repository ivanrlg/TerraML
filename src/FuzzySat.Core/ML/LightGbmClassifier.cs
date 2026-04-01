using Microsoft.ML;
using Microsoft.ML.Data;

namespace FuzzySat.Core.ML;

/// <summary>
/// ML.NET classifier using LightGBM (gradient boosting) with fuzzy membership features.
/// Generally the strongest classifier for tabular data.
/// </summary>
public sealed class LightGbmClassifier : MlClassifierBase
{
    private LightGbmClassifier(
        MLContext mlContext,
        ITransformer model,
        FuzzyFeatureExtractor featureExtractor,
        SchemaDefinition inputSchema)
        : base(mlContext, model, featureExtractor, inputSchema)
    {
    }

    /// <summary>
    /// Trains a LightGBM multiclass classifier on fuzzy-enriched features.
    /// </summary>
    /// <param name="trainingSamples">Labeled training data.</param>
    /// <param name="featureExtractor">Fuzzy feature extractor.</param>
    /// <param name="numberOfLeaves">Max leaves per tree. Must be at least 2.</param>
    /// <param name="numberOfIterations">Number of boosting iterations. Must be at least 1.</param>
    /// <param name="learningRate">Learning rate (shrinkage). Must be positive.</param>
    public static LightGbmClassifier Train(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        FuzzyFeatureExtractor featureExtractor,
        int numberOfLeaves = 31,
        int numberOfIterations = 100,
        double learningRate = 0.1)
    {
        if (numberOfLeaves < 2)
            throw new ArgumentOutOfRangeException(nameof(numberOfLeaves), "Must be at least 2.");
        if (numberOfIterations < 1)
            throw new ArgumentOutOfRangeException(nameof(numberOfIterations), "Must be at least 1.");
        if (learningRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(learningRate), "Must be positive.");

        return TrainBase(trainingSamples, featureExtractor, (mlContext, _) =>
            mlContext.MulticlassClassification.Trainers.LightGbm(
                numberOfLeaves: numberOfLeaves,
                numberOfIterations: numberOfIterations,
                learningRate: learningRate),
            (ctx, model, ext, schema) => new LightGbmClassifier(ctx, model, ext, schema));
    }
}
