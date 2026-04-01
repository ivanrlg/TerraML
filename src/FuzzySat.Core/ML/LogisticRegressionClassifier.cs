using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace FuzzySat.Core.ML;

/// <summary>
/// ML.NET classifier using L-BFGS Maximum Entropy (Logistic Regression) for multiclass.
/// Solid baseline — fast training, produces calibrated probabilities.
/// No additional NuGet required — included in Microsoft.ML 5.0.0.
/// </summary>
public sealed class LogisticRegressionClassifier : MlClassifierBase
{
    private LogisticRegressionClassifier(
        MLContext mlContext,
        ITransformer model,
        IFeatureExtractor featureExtractor,
        SchemaDefinition inputSchema)
        : base(mlContext, model, featureExtractor, inputSchema)
    {
    }

    /// <summary>
    /// Trains a Logistic Regression multiclass classifier (LbfgsMaximumEntropy)
    /// on fuzzy-enriched features.
    /// </summary>
    /// <param name="trainingSamples">Labeled training data.</param>
    /// <param name="featureExtractor">Fuzzy feature extractor.</param>
    /// <param name="historySize">L-BFGS history size (higher = more memory, better convergence). Must be at least 1.</param>
    public static LogisticRegressionClassifier Train(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        IFeatureExtractor featureExtractor,
        int historySize = 50)
    {
        if (historySize < 1)
            throw new ArgumentOutOfRangeException(nameof(historySize), "Must be at least 1.");

        return TrainBase(trainingSamples, featureExtractor, (mlContext, _) =>
            mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(
                new LbfgsMaximumEntropyMulticlassTrainer.Options
                {
                    HistorySize = historySize
                }),
            (ctx, model, ext, schema) => new LogisticRegressionClassifier(ctx, model, ext, schema));
    }
}
