using FuzzySat.Core.Raster;
using SkiaSharp;

namespace FuzzySat.Web.Services;

/// <summary>
/// Service wrapping GDAL raster operations for the Web layer.
/// Validates file paths and restricts to known raster extensions.
/// Registered as singleton since GdalRasterReader handles thread-safe initialization.
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
    public BandStatistics ComputeBandStatistics(Band band)
    {
        ArgumentNullException.ThrowIfNull(band);

        var rows = band.Rows;
        var cols = band.Columns;
        var count = (long)rows * cols;

        var min = double.MaxValue;
        var max = double.MinValue;
        var sum = 0.0;

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var val = band[r, c];
                if (val < min) min = val;
                if (val > max) max = val;
                sum += val;
            }
        }

        var mean = sum / count;

        // Second pass for stddev
        var sumSqDiff = 0.0;
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var diff = band[r, c] - mean;
                sumSqDiff += diff * diff;
            }
        }
        var stdDev = Math.Sqrt(sumSqDiff / count);

        // Histogram (256 bins, long to avoid overflow on large rasters)
        var histogram = new long[256];
        var range = max - min;
        if (range > 0)
        {
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var bin = (int)((band[r, c] - min) / range * 255);
                    if (bin > 255) bin = 255;
                    if (bin < 0) bin = 0;
                    histogram[bin]++;
                }
            }
        }
        else
        {
            histogram[128] = count;
        }

        return new BandStatistics(min, max, mean, stdDev, histogram);
    }

    /// <summary>
    /// Renders a grayscale PNG preview of a single band, normalized to 0-255.
    /// Returns the PNG as a byte array suitable for base64 embedding.
    /// </summary>
    public byte[] RenderBandPreview(Band band, int maxWidth = 800, int maxHeight = 600)
    {
        ArgumentNullException.ThrowIfNull(band);

        // Compute min/max (needed when no BandStatistics available)
        var rows = band.Rows;
        var cols = band.Columns;
        var min = double.MaxValue;
        var max = double.MinValue;
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
            {
                var v = band[r, c];
                if (v < min) min = v;
                if (v > max) max = v;
            }

        return RenderBandPreviewInternal(band, min, max, maxWidth, maxHeight);
    }

    /// <summary>
    /// Validates that a raster file path points to an existing file
    /// with a supported raster extension.
    /// </summary>
    /// <summary>
    /// Renders a grayscale PNG preview reusing pre-computed min/max from BandStatistics.
    /// Avoids redundant full-band scan.
    /// </summary>
    public byte[] RenderBandPreview(Band band, BandStatistics stats, int maxWidth = 800, int maxHeight = 600)
    {
        ArgumentNullException.ThrowIfNull(band);
        ArgumentNullException.ThrowIfNull(stats);
        return RenderBandPreviewInternal(band, stats.Min, stats.Max, maxWidth, maxHeight);
    }

    private byte[] RenderBandPreviewInternal(Band band, double min, double max, int maxWidth, int maxHeight)
    {
        var rows = band.Rows;
        var cols = band.Columns;

        var scale = Math.Min(1.0, Math.Min((double)maxWidth / cols, (double)maxHeight / rows));
        var outW = Math.Max(1, (int)(cols * scale));
        var outH = Math.Max(1, (int)(rows * scale));
        var range = max - min;

        using var bitmap = new SKBitmap(outW, outH, SKColorType.Gray8, SKAlphaType.Opaque);
        var pixels = bitmap.GetPixelSpan();

        for (var y = 0; y < outH; y++)
        {
            var srcRow = Math.Min((int)(y / scale), rows - 1);
            for (var x = 0; x < outW; x++)
            {
                var srcCol = Math.Min((int)(x / scale), cols - 1);
                byte gray = range > 0
                    ? (byte)Math.Clamp((band[srcRow, srcCol] - min) / range * 255, 0, 255)
                    : (byte)128;
                pixels[y * outW + x] = gray;
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

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
