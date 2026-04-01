using Microsoft.ML;
using Microsoft.ML.Data;

namespace FuzzySat.Core.ML;

/// <summary>
/// ML.NET classifier using Linear SVM with One-vs-All strategy for multiclass.
/// Classic method for spectral classification with good performance in high-dimensional spaces.
/// No additional NuGet required — included in Microsoft.ML 5.0.0.
/// </summary>
public sealed class SvmClassifier : MlClassifierBase
{
    private SvmClassifier(
        MLContext mlContext,
        ITransformer model,
        IFeatureExtractor featureExtractor,
        SchemaDefinition inputSchema)
        : base(mlContext, model, featureExtractor, inputSchema)
    {
    }

    /// <summary>
    /// Trains an SVM multiclass classifier (LinearSvm/OVA) on fuzzy-enriched features.
    /// </summary>
    /// <param name="trainingSamples">Labeled training data.</param>
    /// <param name="featureExtractor">Fuzzy feature extractor.</param>
    /// <param name="numberOfIterations">Number of training iterations. Must be at least 1.</param>
    public static SvmClassifier Train(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        IFeatureExtractor featureExtractor,
        int numberOfIterations = 100)
    {
        if (numberOfIterations < 1)
            throw new ArgumentOutOfRangeException(nameof(numberOfIterations), "Must be at least 1.");

        return TrainBase(trainingSamples, featureExtractor, (mlContext, _) =>
            mlContext.MulticlassClassification.Trainers.OneVersusAll(
                mlContext.BinaryClassification.Trainers.LinearSvm(
                    numberOfIterations: numberOfIterations)),
            (ctx, model, ext, schema) => new SvmClassifier(ctx, model, ext, schema));
    }
}
