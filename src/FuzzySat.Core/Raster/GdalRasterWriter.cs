using FuzzySat.Core.Classification;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace FuzzySat.Core.Raster;

/// <summary>
/// Writes classification results as GeoTIFF rasters using GDAL.
/// Each pixel value is the numeric code of the predicted land cover class.
/// </summary>
public sealed class GdalRasterWriter : IRasterWriter
{
    private static bool _initialized;
    private static readonly object InitLock = new();

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (InitLock)
        {
            if (_initialized) return;
            GdalBase.ConfigureAll();
            _initialized = true;
        }
    }

    /// <inheritdoc />
    public void Write(string filePath, ClassificationResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        ArgumentNullException.ThrowIfNull(result);
        EnsureInitialized();

        // Build class name → code mapping
        var classCodeMap = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var lc in result.Classes)
            classCodeMap[lc.Name] = lc.Code;

        var rows = result.Rows;
        var cols = result.Columns;

        using var driver = Gdal.GetDriverByName("GTiff")
            ?? throw new InvalidOperationException("GTiff driver is not available. Ensure GDAL is properly initialized.");
        using var dataset = driver.Create(filePath, cols, rows, 1, DataType.GDT_Int32, null)
            ?? throw new IOException($"Failed to create output raster: '{filePath}'.");

        var buffer = new int[rows * cols];
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                var className = result.GetClass(row, col);
                if (!classCodeMap.TryGetValue(className, out var code))
                    throw new InvalidOperationException(
                        $"Unknown land cover class '{className}' at row {row}, column {col}.");
                buffer[row * cols + col] = code;
            }
        }

        using var band = dataset.GetRasterBand(1);
        band.WriteRaster(0, 0, cols, rows, buffer, cols, rows, 0, 0);
        dataset.FlushCache();
    }
}
