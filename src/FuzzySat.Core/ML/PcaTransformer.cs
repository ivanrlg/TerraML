using Microsoft.ML;
using Microsoft.ML.Data;

namespace FuzzySat.Core.ML;

/// <summary>
/// Reduces dimensionality of feature vectors using Principal Component Analysis (PCA)
/// via ML.NET. Useful for Sentinel-2 (13 bands → 111 features) to avoid curse of dimensionality.
/// </summary>
public sealed class PcaTransformer
{
    private readonly object _lock = new();
    private readonly PredictionEngine<PcaInput, PcaOutput> _predictionEngine;
    private readonly int _rank;
    private readonly int _originalDimension;

    /// <summary>Gets the number of principal components (output dimensions).</summary>
    public int Rank => _rank;

    /// <summary>Gets the original feature dimension before PCA.</summary>
    public int OriginalDimension => _originalDimension;

    private PcaTransformer(MLContext mlContext, ITransformer model, SchemaDefinition inputSchema, int rank, int originalDimension)
    {
        _rank = rank;
        _originalDimension = originalDimension;
        _predictionEngine = mlContext.Model.CreatePredictionEngine<PcaInput, PcaOutput>(
            model, inputSchemaDefinition: inputSchema);
    }

    /// <summary>
    /// Fits a PCA model to the given feature vectors.
    /// </summary>
    /// <param name="featureVectors">Training feature vectors (all must have the same length and be non-null).</param>
    /// <param name="rank">Number of principal components to keep. Must be at least 1.</param>
    /// <param name="seed">Random seed for reproducibility.</param>
    public static PcaTransformer Fit(IReadOnlyList<float[]> featureVectors, int rank, int? seed = 42)
    {
        ArgumentNullException.ThrowIfNull(featureVectors);
        if (featureVectors.Count == 0)
            throw new ArgumentException("At least one feature vector is required.", nameof(featureVectors));
        if (rank < 1)
            throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be at least 1.");

        var firstVector = featureVectors[0]
            ?? throw new ArgumentException("Feature vector at index 0 is null.", nameof(featureVectors));
        var dimension = firstVector.Length;
        if (dimension < 1)
            throw new ArgumentException("Feature vectors must have at least one element.", nameof(featureVectors));
        if (rank > dimension)
            throw new ArgumentOutOfRangeException(nameof(rank),
                $"Rank ({rank}) cannot exceed feature dimension ({dimension}).");

        // Validate all vectors are non-null and have the same length
        for (var i = 1; i < featureVectors.Count; i++)
        {
            var vector = featureVectors[i]
                ?? throw new ArgumentException($"Feature vector at index {i} is null.", nameof(featureVectors));
            if (vector.Length != dimension)
                throw new ArgumentException(
                    $"Feature vector at index {i} has length {vector.Length}, expected {dimension}.",
                    nameof(featureVectors));
        }

        var mlContext = new MLContext(seed: seed);

        var data = featureVectors.Select(f => new PcaInput { Features = f }).ToList();

        var schemaDef = SchemaDefinition.Create(typeof(PcaInput));
        schemaDef["Features"].ColumnType = new VectorDataViewType(NumberDataViewType.Single, dimension);
        var dataView = mlContext.Data.LoadFromEnumerable(data, schemaDef);

        var pipeline = mlContext.Transforms.ProjectToPrincipalComponents(
            outputColumnName: "PcaFeatures",
            inputColumnName: "Features",
            rank: rank);

        var model = pipeline.Fit(dataView);

        return new PcaTransformer(mlContext, model, schemaDef, rank, dimension);
    }

    /// <summary>
    /// Transforms a single feature vector using the fitted PCA model.
    /// Uses a cached PredictionEngine for performance (thread-safe via lock).
    /// </summary>
    /// <param name="features">Input feature vector (must match original dimension).</param>
    /// <returns>Reduced feature vector with <see cref="Rank"/> dimensions.</returns>
    public float[] Transform(float[] features)
    {
        ArgumentNullException.ThrowIfNull(features);
        if (features.Length != _originalDimension)
            throw new ArgumentException(
                $"Expected {_originalDimension} features, got {features.Length}.", nameof(features));

        var input = new PcaInput { Features = features };

        lock (_lock)
        {
            return _predictionEngine.Predict(input).PcaFeatures;
        }
    }

    /// <summary>
    /// Transforms multiple feature vectors in batch.
    /// </summary>
    public IReadOnlyList<float[]> TransformBatch(IReadOnlyList<float[]> featureVectors)
    {
        ArgumentNullException.ThrowIfNull(featureVectors);
        if (featureVectors.Count == 0) return [];

        var results = new List<float[]>(featureVectors.Count);
        for (var i = 0; i < featureVectors.Count; i++)
        {
            var f = featureVectors[i]
                ?? throw new ArgumentException($"Feature vector at index {i} is null.", nameof(featureVectors));
            if (f.Length != _originalDimension)
                throw new ArgumentException(
                    $"Feature vector at index {i}: expected {_originalDimension} features, got {f.Length}.",
                    nameof(featureVectors));

            results.Add(Transform(f));
        }

        return results;
    }

    private sealed class PcaInput
    {
        [VectorType]
        public float[] Features { get; set; } = [];
    }

    private sealed class PcaOutput
    {
        [VectorType]
        public float[] PcaFeatures { get; set; } = [];
    }
}
