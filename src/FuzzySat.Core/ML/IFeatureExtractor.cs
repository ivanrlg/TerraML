namespace FuzzySat.Core.ML;

/// <summary>
/// Extracts a feature vector from pixel band values for ML classification.
/// </summary>
public interface IFeatureExtractor
{
    /// <summary>Gets the names of all features produced by this extractor.</summary>
    IReadOnlyList<string> FeatureNames { get; }

    /// <summary>
    /// Extracts a feature vector from pixel band values.
    /// </summary>
    /// <param name="bandValues">Pixel values per band.</param>
    /// <returns>Feature vector as a float array.</returns>
    float[] ExtractFeatures(IDictionary<string, double> bandValues);
}
