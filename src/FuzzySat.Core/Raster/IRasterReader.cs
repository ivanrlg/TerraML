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
    /// Reads only the specified bands by 1-based index from a raster file.
    /// More memory-efficient than Read() when only a subset of bands is needed.
    /// </summary>
    /// <param name="filePath">Path to the raster file.</param>
    /// <param name="bandIndices">1-based band indices to read.</param>
    /// <returns>A list of bands in the order requested.</returns>
    IReadOnlyList<Band> ReadBands(string filePath, IReadOnlyList<int> bandIndices);

    /// <summary>
    /// Reads metadata about a raster file without loading pixel data.
    /// </summary>
    /// <param name="filePath">Path to the raster file.</param>
    /// <returns>Raster metadata information.</returns>
    RasterInfo ReadInfo(string filePath);
}
