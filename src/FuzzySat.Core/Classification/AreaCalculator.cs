namespace FuzzySat.Core.Classification;

/// <summary>
/// Computes per-class area statistics from a classification result
/// using the pixel size derived from GDAL GeoTransform metadata.
/// </summary>
public static class AreaCalculator
{
    /// <summary>
    /// Statistics for a single land cover class including pixel count and area.
    /// </summary>
    public sealed record ClassAreaStats(
        string ClassName,
        int PixelCount,
        double AreaM2,
        double AreaHectares,
        double Percentage);

    /// <summary>
    /// Calculates area statistics per class.
    /// If geoTransform is null or invalid, areas are set to 0 (pixel counts still reported).
    /// </summary>
    /// <param name="result">The classification result.</param>
    /// <param name="geoTransform">GDAL GeoTransform (6 coefficients). Index 1 = pixel width, index 5 = pixel height (negative).</param>
    /// <returns>Per-class statistics sorted by area descending.</returns>
    public static IReadOnlyList<ClassAreaStats> Calculate(
        ClassificationResult result,
        double[]? geoTransform = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        // Count pixels per class
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var r = 0; r < result.Rows; r++)
        {
            for (var c = 0; c < result.Columns; c++)
            {
                var cls = result.GetClass(r, c);
                counts[cls] = counts.GetValueOrDefault(cls) + 1;
            }
        }

        // Compute pixel area in m² from GeoTransform
        var pixelAreaM2 = 0.0;
        if (geoTransform is { Length: >= 6 })
        {
            var pixelWidth = Math.Abs(geoTransform[1]);
            var pixelHeight = Math.Abs(geoTransform[5]);
            if (pixelWidth > 0 && pixelHeight > 0)
                pixelAreaM2 = pixelWidth * pixelHeight;
        }

        var totalPixels = result.Rows * result.Columns;

        var stats = counts
            .Select(kv =>
            {
                var pct = totalPixels > 0 ? Math.Round((double)kv.Value / totalPixels * 100.0, 2) : 0.0;
                var areaM2 = kv.Value * pixelAreaM2;
                var areaHa = areaM2 / 10_000.0;
                return new ClassAreaStats(kv.Key, kv.Value, areaM2, areaHa, pct);
            })
            .OrderByDescending(s => s.PixelCount)
            .ToList();

        return stats.AsReadOnly();
    }
}
