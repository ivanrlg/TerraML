namespace FuzzySat.Core.Raster;

/// <summary>
/// Reads multispectral raster imagery from a file source.
/// </summary>
public interface IRasterReader
{
    /// <summary>
    /// Reads a multispectral image from the specified file path.
    /// </summary>
    /// <param name="filePath">Path to the raster file (e.g., GeoTIFF).</param>
    /// <param name="bandNames">Band names to assign, in order. If null, uses "Band1", "Band2", etc.</param>
    /// <returns>A multispectral image with the specified bands.</returns>
    MultispectralImage Read(string filePath, IReadOnlyList<string>? bandNames = null);

    /// <summary>
    /// Reads metadata about a raster file without loading pixel data.
    /// </summary>
    /// <param name="filePath">Path to the raster file.</param>
    /// <returns>Raster metadata information.</returns>
    RasterInfo ReadInfo(string filePath);
}
