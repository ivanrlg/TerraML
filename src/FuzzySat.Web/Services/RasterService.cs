using FuzzySat.Core.Raster;

namespace FuzzySat.Web.Services;

/// <summary>
/// Service wrapping GDAL raster operations for the Web layer.
/// Registered as singleton since GdalRasterReader handles thread-safe initialization.
/// </summary>
public sealed class RasterService
{
    private readonly GdalRasterReader _reader = new();

    /// <summary>
    /// Reads raster metadata without loading pixel data.
    /// </summary>
    public RasterInfo GetInfo(string filePath) => _reader.ReadInfo(filePath);

    /// <summary>
    /// Reads a multispectral image with all bands.
    /// </summary>
    public MultispectralImage ReadImage(string filePath, IReadOnlyList<string>? bandNames = null)
        => _reader.Read(filePath, bandNames);

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
}
