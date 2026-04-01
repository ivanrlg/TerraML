using FuzzySat.Core.Raster;
using FuzzySat.Core.Visualization;

namespace FuzzySat.Web.Services;

/// <summary>
/// Service wrapping GDAL raster operations for the Web layer.
/// Validates file paths and restricts to known raster extensions.
/// Registered as singleton since GdalRasterReader handles thread-safe initialization.
/// Delegates rendering to Core's <see cref="RgbCompositeRenderer"/> and
/// <see cref="BandStatisticsCalculator"/>.
/// </summary>
public sealed class RasterService
{
    private readonly GdalRasterReader _reader = new();

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".tif", ".tiff", ".img", ".hdf", ".nc", ".jp2", ".vrt"
    };

    /// <summary>
    /// Reads raster metadata without loading pixel data.
    /// Validates that the file exists and has a supported raster extension.
    /// </summary>
    public RasterInfo GetInfo(string filePath)
    {
        ValidateRasterPath(filePath);
        return _reader.ReadInfo(filePath);
    }

    /// <summary>
    /// Reads a multispectral image with all bands.
    /// Validates that the file exists and has a supported raster extension.
    /// </summary>
    public MultispectralImage ReadImage(string filePath, IReadOnlyList<string>? bandNames = null)
    {
        ValidateRasterPath(filePath);
        return _reader.Read(filePath, bandNames);
    }

    /// <summary>
    /// Computes basic statistics for a single band.
    /// </summary>
    public BandStatistics ComputeBandStatistics(Band band) =>
        BandStatisticsCalculator.Compute(band);

    /// <summary>
    /// Renders a grayscale PNG preview of a single band, normalized to 0-255.
    /// Returns the PNG as a byte array suitable for base64 embedding.
    /// </summary>
    public byte[] RenderBandPreview(Band band, int maxWidth = 800, int maxHeight = 600)
    {
        ArgumentNullException.ThrowIfNull(band);
        var (min, max) = BandStatisticsCalculator.ComputeMinMax(band);
        return RgbCompositeRenderer.RenderGrayscale(band, min, max, maxWidth, maxHeight);
    }

    /// <summary>
    /// Renders a grayscale PNG preview reusing pre-computed min/max from BandStatistics.
    /// Avoids redundant full-band scan.
    /// </summary>
    public byte[] RenderBandPreview(Band band, BandStatistics stats, int maxWidth = 800, int maxHeight = 600) =>
        RgbCompositeRenderer.RenderGrayscale(band, stats, maxWidth, maxHeight);

    /// <summary>
    /// Renders an RGB composite PNG from three bands (one per channel).
    /// Each channel is independently normalized to 0-255 using its own min/max statistics.
    /// </summary>
    public byte[] RenderRgbComposite(
        Band redBand, Band greenBand, Band blueBand,
        BandStatistics redStats, BandStatistics greenStats, BandStatistics blueStats,
        int maxWidth = 800, int maxHeight = 600) =>
        RgbCompositeRenderer.RenderRgb(redBand, greenBand, blueBand,
            redStats, greenStats, blueStats, maxWidth, maxHeight);

    private static void ValidateRasterPath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

        var fullPath = Path.GetFullPath(filePath);

        // Reject UNC paths to prevent network file access
        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException("UNC/network paths are not allowed.", nameof(filePath));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Raster file not found.", fullPath);

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException(
                $"Unsupported raster format '{ext}'. Supported: {string.Join(", ", AllowedExtensions)}",
                nameof(filePath));
    }
}
