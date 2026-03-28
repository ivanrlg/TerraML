using Microsoft.ML;
using Microsoft.ML.Data;

namespace FuzzySat.Core.ML;

/// <summary>
/// K-Means clustering for automated training area suggestion.
/// Groups pixels by spectral similarity to identify potential land cover classes.
/// Thread-safe: uses lock around PredictionEngine.
/// </summary>
public sealed class KMeansClusterer
{
    private readonly object _lock = new();
    private readonly PredictionEngine<ClusterInput, ClusterPrediction> _predictionEngine;
    private readonly int _featureSize;

    /// <summary>Gets the number of clusters.</summary>
    public int NumberOfClusters { get; }

    private KMeansClusterer(MLContext mlContext, ITransformer model, int numberOfClusters, SchemaDefinition inputSchema, int featureSize)
    {
        _predictionEngine = mlContext.Model.CreatePredictionEngine<ClusterInput, ClusterPrediction>(
            model, inputSchemaDefinition: inputSchema);
        NumberOfClusters = numberOfClusters;
        _featureSize = featureSize;
    }

    /// <summary>
    /// Trains a K-Means clusterer from pixel feature vectors.
    /// </summary>
    /// <param name="pixelFeatures">Feature vectors. All must be non-null and same length.</param>
    /// <param name="numberOfClusters">Number of clusters (potential classes). Must be at least 2.</param>
    public static KMeansClusterer Train(
        IReadOnlyList<float[]> pixelFeatures,
        int numberOfClusters = 7)
    {
        ArgumentNullException.ThrowIfNull(pixelFeatures);
        if (pixelFeatures.Count == 0)
            throw new ArgumentException("At least one sample is required.", nameof(pixelFeatures));
        if (numberOfClusters < 2)
            throw new ArgumentOutOfRangeException(nameof(numberOfClusters), "At least 2 clusters required.");

        // Validate all samples
        var featureSize = pixelFeatures[0]?.Length
            ?? throw new ArgumentException("Sample at index 0 is null.", nameof(pixelFeatures));
        if (featureSize == 0)
            throw new ArgumentException("Feature vectors must have at least one element.", nameof(pixelFeatures));

        for (var i = 1; i < pixelFeatures.Count; i++)
        {
            if (pixelFeatures[i] is null)
                throw new ArgumentException($"Sample at index {i} is null.", nameof(pixelFeatures));
            if (pixelFeatures[i].Length != featureSize)
                throw new ArgumentException(
                    $"Sample at index {i} has length {pixelFeatures[i].Length}, expected {featureSize}.",
                    nameof(pixelFeatures));
        }

        var mlContext = new MLContext(seed: 42);

        var data = pixelFeatures.Select(f => new ClusterInput { Features = f }).ToList();
        var schemaDef = SchemaDefinition.Create(typeof(ClusterInput));
        schemaDef["Features"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureSize);
        var dataView = mlContext.Data.LoadFromEnumerable(data, schemaDef);

        var pipeline = mlContext.Clustering.Trainers.KMeans(
            featureColumnName: "Features",
            numberOfClusters: numberOfClusters);

        var model = pipeline.Fit(dataView);
        return new KMeansClusterer(mlContext, model, numberOfClusters, schemaDef, featureSize);
    }

    /// <summary>
    /// Predicts the cluster assignment for a pixel.
    /// </summary>
    /// <param name="features">Feature vector. Must match the training feature size.</param>
    public int Predict(float[] features)
    {
        ArgumentNullException.ThrowIfNull(features);
        if (features.Length != _featureSize)
            throw new ArgumentException(
                $"Feature vector has length {features.Length}, expected {_featureSize}.", nameof(features));

        lock (_lock)
        {
            var prediction = _predictionEngine.Predict(new ClusterInput { Features = features });
            return (int)prediction.PredictedClusterId;
        }
    }

    /// <summary>ML.NET input for clustering.</summary>
    public sealed class ClusterInput
    {
        /// <summary>Gets or sets the feature vector.</summary>
        public float[] Features { get; set; } = [];
    }

    /// <summary>ML.NET prediction output for clustering.</summary>
    public sealed class ClusterPrediction
    {
        /// <summary>Gets or sets the predicted cluster ID.</summary>
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId { get; set; }

        /// <summary>Gets or sets the distances to cluster centroids.</summary>
        [ColumnName("Score")]
        public float[] Distances { get; set; } = [];
    }
}
