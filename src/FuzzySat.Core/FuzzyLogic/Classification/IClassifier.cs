namespace FuzzySat.Core.FuzzyLogic.Classification;

/// <summary>
/// Classifies a pixel into a land cover class based on its spectral band values.
/// </summary>
public interface IClassifier
{
    /// <summary>
    /// Classifies a single pixel.
    /// </summary>
    /// <param name="bandValues">Pixel values per band name (e.g., reflectance values).</param>
    /// <returns>The predicted land cover class name.</returns>
    string ClassifyPixel(IDictionary<string, double> bandValues);
}
