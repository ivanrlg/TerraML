using FuzzySat.Core.Classification;

namespace FuzzySat.Core.Raster;

/// <summary>
/// Writes classification results to a raster file.
/// </summary>
public interface IRasterWriter
{
    /// <summary>
    /// Writes a classification result as a raster file (e.g., GeoTIFF).
    /// Each pixel value corresponds to the land cover class code.
    /// </summary>
    /// <param name="filePath">Output file path.</param>
    /// <param name="result">The classification result to write.</param>
    void Write(string filePath, ClassificationResult result);
}
