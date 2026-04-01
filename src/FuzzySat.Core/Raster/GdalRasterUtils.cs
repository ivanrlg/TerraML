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
    /// </summary>
    /// <param name="sourcePath">Path to the source multiband raster.</param>
    /// <param name="bandIndices">1-based band indices to include.</param>
    /// <param name="outputVrtPath">Output VRT file path.</param>
    /// <returns>The output VRT file path.</returns>
    public static string CreateBandSubsetVrt(string sourcePath, IReadOnlyList<int> bandIndices, string outputVrtPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath, nameof(sourcePath));
        ArgumentNullException.ThrowIfNull(bandIndices);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputVrtPath, nameof(outputVrtPath));
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
    /// The output format is inferred from the file extension unless explicitly provided.
    /// </summary>
    /// <param name="sourcePath">Source raster path.</param>
    /// <param name="outputPath">Output raster path.</param>
    /// <param name="outputFormat">GDAL driver short name (e.g., "GTiff"). Null to infer from extension.</param>
    /// <returns>The output file path.</returns>
    public static string TranslateFormat(string sourcePath, string outputPath, string? outputFormat = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath, nameof(sourcePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
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
