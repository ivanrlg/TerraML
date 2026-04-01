using FuzzySat.Core.Raster;
using SkiaSharp;

namespace FuzzySat.Core.Visualization;

/// <summary>
/// Renders RGB composite and grayscale band previews as PNG byte arrays using SkiaSharp.
/// </summary>
public static class RgbCompositeRenderer
{
    /// <summary>
    /// Renders a grayscale PNG preview of a single band, normalized to 0-255.
    /// </summary>
    public static byte[] RenderGrayscale(Band band, double min, double max, int maxWidth = 800, int maxHeight = 600)
    {
        ArgumentNullException.ThrowIfNull(band);

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

    /// <summary>
    /// Renders a grayscale PNG preview reusing pre-computed statistics.
    /// </summary>
    public static byte[] RenderGrayscale(Band band, BandStatistics stats, int maxWidth = 800, int maxHeight = 600)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return RenderGrayscale(band, stats.Min, stats.Max, maxWidth, maxHeight);
    }

    /// <summary>
    /// Renders an RGB composite PNG from three bands (one per channel).
    /// Each channel is independently normalized to 0-255 using its own min/max statistics.
    /// </summary>
    public static byte[] RenderRgb(
        Band redBand, Band greenBand, Band blueBand,
        BandStatistics redStats, BandStatistics greenStats, BandStatistics blueStats,
        int maxWidth = 800, int maxHeight = 600)
    {
        ArgumentNullException.ThrowIfNull(redBand);
        ArgumentNullException.ThrowIfNull(greenBand);
        ArgumentNullException.ThrowIfNull(blueBand);
        ArgumentNullException.ThrowIfNull(redStats);
        ArgumentNullException.ThrowIfNull(greenStats);
        ArgumentNullException.ThrowIfNull(blueStats);

        if (greenBand.Rows != redBand.Rows || greenBand.Columns != redBand.Columns ||
            blueBand.Rows != redBand.Rows || blueBand.Columns != redBand.Columns)
            throw new ArgumentException(
                "All RGB bands must have identical dimensions for composite rendering.");

        var rows = redBand.Rows;
        var cols = redBand.Columns;

        var scale = Math.Min(1.0, Math.Min((double)maxWidth / cols, (double)maxHeight / rows));
        var outW = Math.Max(1, (int)(cols * scale));
        var outH = Math.Max(1, (int)(rows * scale));

        var rRange = redStats.Max - redStats.Min;
        var gRange = greenStats.Max - greenStats.Min;
        var bRange = blueStats.Max - blueStats.Min;

        using var bitmap = new SKBitmap(outW, outH, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var pixels = bitmap.GetPixelSpan();

        for (var y = 0; y < outH; y++)
        {
            var srcRow = Math.Min((int)(y / scale), rows - 1);
            for (var x = 0; x < outW; x++)
            {
                var srcCol = Math.Min((int)(x / scale), cols - 1);

                var r = rRange > 0
                    ? (byte)Math.Clamp((redBand[srcRow, srcCol] - redStats.Min) / rRange * 255, 0, 255)
                    : (byte)128;
                var g = gRange > 0
                    ? (byte)Math.Clamp((greenBand[srcRow, srcCol] - greenStats.Min) / gRange * 255, 0, 255)
                    : (byte)128;
                var b = bRange > 0
                    ? (byte)Math.Clamp((blueBand[srcRow, srcCol] - blueStats.Min) / bRange * 255, 0, 255)
                    : (byte)128;

                var offset = (y * outW + x) * 4;
                pixels[offset] = r;
                pixels[offset + 1] = g;
                pixels[offset + 2] = b;
                pixels[offset + 3] = 255;
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }
}
