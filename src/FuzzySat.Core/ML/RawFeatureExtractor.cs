namespace FuzzySat.Core.ML;

/// <summary>
/// Feature extractor that passes band values through unchanged, without applying
/// any membership functions. Useful for meta-learners (e.g., stacking) where
/// features are already computed (one-hot encoded predictions).
/// </summary>
public sealed class RawFeatureExtractor : IFeatureExtractor
{
    /// <inheritdoc />
    public IReadOnlyList<string> FeatureNames { get; }

    /// <summary>
    /// Creates a raw feature extractor with the given feature names.
    /// </summary>
    /// <param name="featureNames">Ordered list of feature names (must match band-value keys).</param>
    public RawFeatureExtractor(IReadOnlyList<string> featureNames)
    {
        ArgumentNullException.ThrowIfNull(featureNames);
        FeatureNames = featureNames.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public float[] ExtractFeatures(IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);

        var features = new float[FeatureNames.Count];
        for (var i = 0; i < FeatureNames.Count; i++)
            features[i] = (float)bandValues[FeatureNames[i]];

        return features;
    }
}
