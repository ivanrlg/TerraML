using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace FuzzySat.Core.Raster;

/// <summary>
/// Reads multispectral raster imagery using GDAL.
/// Supports GeoTIFF and other GDAL-compatible formats.
/// </summary>
public sealed class GdalRasterReader : IRasterReader
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
    public MultispectralImage Read(string filePath, IReadOnlyList<string>? bandNames = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        EnsureInitialized();

        using var dataset = OpenDataset(filePath);
        var bandCount = dataset.RasterCount;
        var rows = dataset.RasterYSize;
        var cols = dataset.RasterXSize;

        var bands = new List<Band>(bandCount);
        for (var i = 1; i <= bandCount; i++)
        {
            var name = bandNames is not null && i - 1 < bandNames.Count
                ? bandNames[i - 1]
                : $"Band{i}";

            using var gdalBand = dataset.GetRasterBand(i);
            var data = new double[rows, cols];
            var buffer = new double[rows * cols];

            gdalBand.ReadRaster(0, 0, cols, rows, buffer, cols, rows, 0, 0);

            for (var row = 0; row < rows; row++)
                for (var col = 0; col < cols; col++)
                    data[row, col] = buffer[row * cols + col];

            bands.Add(new Band(name, data));
        }

        return new MultispectralImage(bands);
    }

    /// <inheritdoc />
    public RasterInfo ReadInfo(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        EnsureInitialized();

        using var dataset = OpenDataset(filePath);
        using var firstBand = dataset.GetRasterBand(1);
        var dataType = Gdal.GetDataTypeName(firstBand.DataType);

        return new RasterInfo(
            filePath: filePath,
            rows: dataset.RasterYSize,
            columns: dataset.RasterXSize,
            bandCount: dataset.RasterCount,
            dataType: dataType,
            driverName: dataset.GetDriver().ShortName,
            projection: dataset.GetProjection());
    }

    private static Dataset OpenDataset(string filePath)
    {
        try
        {
            var dataset = Gdal.Open(filePath, Access.GA_ReadOnly);
            if (dataset is not null) return dataset;
        }
        catch (Exception ex)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Raster file not found: '{filePath}'.", filePath, ex);

            throw new IOException($"Failed to open raster file: '{filePath}'.", ex);
        }

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Raster file not found: '{filePath}'.", filePath);

        throw new IOException($"Failed to open raster file: '{filePath}'.");
    }
}
