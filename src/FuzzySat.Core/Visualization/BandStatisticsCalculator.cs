using FuzzySat.Core.Raster;

namespace FuzzySat.Core.Visualization;

/// <summary>
/// Computes statistics (min, max, mean, stddev, histogram) from a spectral band.
/// </summary>
public static class BandStatisticsCalculator
{
    /// <summary>
    /// Computes only min and max values for a band (single pass).
    /// Use when full statistics are not needed (e.g., grayscale rendering).
    /// </summary>
    public static (double Min, double Max) ComputeMinMax(Band band)
    {
        ArgumentNullException.ThrowIfNull(band);

        var rows = band.Rows;
        var cols = band.Columns;
        var min = double.MaxValue;
        var max = double.MinValue;

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var val = band[r, c];
                if (val < min)
                    min = val;
                if (val > max)
                    max = val;
            }
        }

        return (min, max);
    }

    /// <summary>
    /// Computes basic statistics for a single band.
    /// </summary>
    public static BandStatistics Compute(Band band)
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
                if (val < min)
                    min = val;
                if (val > max)
                    max = val;
                sum += val;
            }
        }

        var mean = sum / count;

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

        var histogram = new long[256];
        var range = max - min;
        if (range > 0)
        {
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var bin = (int)((band[r, c] - min) / range * 255);
                    if (bin > 255)
                        bin = 255;
                    if (bin < 0)
                        bin = 0;
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
