using Microsoft.ML;
using Microsoft.ML.Data;
#pragma warning disable CS1591 // Missing XML comment — ML.NET data model properties

namespace FuzzySat.Core.ML;

/// <summary>
/// K-Means clustering for automated training area suggestion.
/// Groups pixels by spectral similarity to identify potential land cover classes.
/// </summary>
public sealed class KMeansClusterer
{
    private readonly MLContext _mlContext;
    private readonly ITransformer _model;
    private readonly PredictionEngine<ClusterInput, ClusterPrediction> _predictionEngine;

    /// <summary>Gets the number of clusters.</summary>
    public int NumberOfClusters { get; }

    private KMeansClusterer(MLContext mlContext, ITransformer model, int numberOfClusters, SchemaDefinition inputSchema)
    {
        _mlContext = mlContext;
        _model = model;
        _predictionEngine = mlContext.Model.CreatePredictionEngine<ClusterInput, ClusterPrediction>(
            model, inputSchemaDefinition: inputSchema);
        NumberOfClusters = numberOfClusters;
    }

    /// <summary>
    /// Trains a K-Means clusterer from pixel feature vectors.
    /// </summary>
    /// <param name="pixelFeatures">Feature vectors (e.g., raw band values or fuzzy features).</param>
    /// <param name="numberOfClusters">Number of clusters (potential classes).</param>
    public static KMeansClusterer Train(
        IReadOnlyList<float[]> pixelFeatures,
        int numberOfClusters = 7)
    {
        ArgumentNullException.ThrowIfNull(pixelFeatures);
        if (pixelFeatures.Count == 0)
            throw new ArgumentException("At least one sample is required.", nameof(pixelFeatures));
        if (numberOfClusters < 2)
            throw new ArgumentOutOfRangeException(nameof(numberOfClusters), "At least 2 clusters required.");

        var mlContext = new MLContext(seed: 42);

        var featureSize = pixelFeatures[0].Length;
        var data = pixelFeatures.Select(f => new ClusterInput { Features = f }).ToList();
        var schemaDef = SchemaDefinition.Create(typeof(ClusterInput));
        schemaDef["Features"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, featureSize);
        var dataView = mlContext.Data.LoadFromEnumerable(data, schemaDef);

        var pipeline = mlContext.Clustering.Trainers.KMeans(
            featureColumnName: "Features",
            numberOfClusters: numberOfClusters);

        var model = pipeline.Fit(dataView);
        return new KMeansClusterer(mlContext, model, numberOfClusters, schemaDef);
    }

    /// <summary>
    /// Predicts the cluster assignment for a pixel.
    /// </summary>
    public int Predict(float[] features)
    {
        var prediction = _predictionEngine.Predict(new ClusterInput { Features = features });
        return (int)prediction.PredictedClusterId;
    }

    /// <summary>ML.NET input for clustering.</summary>
    public sealed class ClusterInput
    {
        [VectorType]
        public float[] Features { get; set; } = [];
    }

    /// <summary>ML.NET prediction output for clustering.</summary>
    public sealed class ClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint PredictedClusterId { get; set; }

        [ColumnName("Score")]
        public float[] Distances { get; set; } = [];
    }
}
