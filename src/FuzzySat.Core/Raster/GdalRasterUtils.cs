using System.Globalization;
using System.Xml.Linq;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace FuzzySat.Core.Raster;

/// <summary>
/// GDAL utility operations: VRT band subsetting and format conversion.
/// </summary>
public static class GdalRasterUtils
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

    /// <summary>
    /// Creates a VRT file referencing a subset of bands from a multiband source raster.
    /// Output must reside in the same directory as the source file.
    /// </summary>
    public static string CreateBandSubsetVrt(string sourcePath, IReadOnlyList<int> bandIndices, string outputVrtPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath, nameof(sourcePath));
        ArgumentNullException.ThrowIfNull(bandIndices);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputVrtPath, nameof(outputVrtPath));

        if (bandIndices.Count == 0)
            throw new ArgumentException("At least one band index is required.", nameof(bandIndices));

        ValidateOutputPath(sourcePath, outputVrtPath);
        EnsureInitialized();

        using var dataset = Gdal.Open(sourcePath, Access.GA_ReadOnly)
            ?? throw new IOException($"Failed to open raster: '{sourcePath}'.");

        var rows = dataset.RasterYSize;
        var cols = dataset.RasterXSize;

        var vrt = new XElement("VRTDataset",
            new XAttribute("rasterXSize", cols),
            new XAttribute("rasterYSize", rows));

        var projection = dataset.GetProjection();
        if (!string.IsNullOrWhiteSpace(projection))
            vrt.Add(new XElement("SRS", projection));

        var gt = new double[6];
        dataset.GetGeoTransform(gt);
        if (gt[0] != 0 || gt[1] != 0 || gt[3] != 0 || gt[5] != 0)
        {
            vrt.Add(new XElement("GeoTransform",
                string.Join(", ", gt.Select(v => v.ToString("G17", CultureInfo.InvariantCulture)))));
        }

        for (var i = 0; i < bandIndices.Count; i++)
        {
            var srcBandIdx = bandIndices[i];
            if (srcBandIdx < 1 || srcBandIdx > dataset.RasterCount)
                throw new ArgumentOutOfRangeException(nameof(bandIndices),
                    $"Band index {srcBandIdx} is out of range (1-{dataset.RasterCount}).");

            using var gdalBand = dataset.GetRasterBand(srcBandIdx);
            var dataType = Gdal.GetDataTypeName(gdalBand.DataType);

            var vrtBand = new XElement("VRTRasterBand",
                new XAttribute("dataType", dataType),
                new XAttribute("band", i + 1),
                new XElement("SimpleSource",
                    new XElement("SourceFilename",
                        new XAttribute("relativeToVRT", "0"),
                        Path.GetFullPath(sourcePath)),
                    new XElement("SourceBand", srcBandIdx),
                    new XElement("SrcRect",
                        new XAttribute("xOff", 0),
                        new XAttribute("yOff", 0),
                        new XAttribute("xSize", cols),
                        new XAttribute("ySize", rows)),
                    new XElement("DstRect",
                        new XAttribute("xOff", 0),
                        new XAttribute("yOff", 0),
                        new XAttribute("xSize", cols),
                        new XAttribute("ySize", rows))));

            vrt.Add(vrtBand);
        }

        var dir = Path.GetDirectoryName(outputVrtPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(outputVrtPath, vrt.ToString(), System.Text.Encoding.UTF8);
        return outputVrtPath;
    }

    /// <summary>
    /// Translates a raster to a different format using GDAL.
    /// Output must reside in the same directory as the source and must not overwrite the source.
    /// </summary>
    public static string TranslateFormat(string sourcePath, string outputPath, string? outputFormat = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath, nameof(sourcePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));

        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Output path cannot be the same as source path.", nameof(outputPath));

        ValidateOutputPath(sourcePath, outputPath);
        EnsureInitialized();

        var format = outputFormat ?? InferFormat(Path.GetExtension(outputPath));

        using var srcDataset = Gdal.Open(sourcePath, Access.GA_ReadOnly)
            ?? throw new IOException($"Failed to open source raster: '{sourcePath}'.");

        var driver = Gdal.GetDriverByName(format)
            ?? throw new ArgumentException($"Unknown GDAL driver: '{format}'.", nameof(outputFormat));

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var outDataset = driver.CreateCopy(outputPath, srcDataset, 0, null, null, null);
        if (outDataset is null)
            throw new IOException($"Failed to create output raster: '{outputPath}'.");

        return outputPath;
    }

    /// <summary>
    /// Validates that the output path resides in the same directory as the source
    /// and does not contain path traversal sequences.
    /// </summary>
    private static void ValidateOutputPath(string sourcePath, string outputPath)
    {
        var sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? "";
        var outputFull = Path.GetFullPath(outputPath);

        if (!outputFull.StartsWith(sourceDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                "Output path must reside in the same directory as the source file.", nameof(outputPath));
    }

    private static string InferFormat(string extension) => extension.ToLowerInvariant() switch
    {
        ".tif" or ".tiff" => "GTiff",
        ".jp2" => "JP2OpenJPEG",
        ".img" => "HFA",
        ".nc" => "netCDF",
        ".hdf" => "HDF4",
        _ => "GTiff"
    };
}
