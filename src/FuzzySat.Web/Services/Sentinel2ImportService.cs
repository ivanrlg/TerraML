using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MaxRev.Gdal.Core;
using Microsoft.Extensions.Logging;
using OSGeo.GDAL;

namespace FuzzySat.Web.Services;

/// <summary>
/// Imports Sentinel-2 band files from a folder of individual TIFFs/JP2s
/// and stacks them into a multi-band VRT file for use in FuzzySat.
/// Registered as singleton (stateless, thread-safe via GDAL init lock).
/// </summary>
public sealed class Sentinel2ImportService
{
    private static bool _initialized;
    private static readonly object InitLock = new();
    private readonly ILogger<Sentinel2ImportService> _logger;

    /// <summary>Maximum number of files to scan in a folder (DoS guard).</summary>
    private const int MaxBandFiles = 50;

    /// <summary>Supported single-band raster extensions.</summary>
    private static readonly HashSet<string> BandExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".tif", ".tiff", ".jp2"
    };

    /// <summary>
    /// Regex to extract Sentinel-2 band name from filename.
    /// Matches patterns like: B02.tif, _B04_10m.tif, T19PCK_20251222_B8A_20m.jp2,
    /// 2026-03-22_Sentinel-2_L2A_B01_(Raw).tiff (Copernicus Browser format)
    /// </summary>
    private static readonly Regex BandNameRegex = new(
        @"_?(B(?:0[1-9]|1[0-2]|8A))(?:_(\d+)m)?(?:_\([^)]*\))?\.(?:tif|tiff|jp2)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Sentinel-2 MSI native resolution lookup (meters).
    /// Used as fallback when resolution cannot be parsed from filename.
    /// </summary>
    private static readonly Dictionary<string, int> NativeResolution = new(StringComparer.OrdinalIgnoreCase)
    {
        ["B01"] = 60, ["B02"] = 10, ["B03"] = 10, ["B04"] = 10,
        ["B05"] = 20, ["B06"] = 20, ["B07"] = 20, ["B08"] = 10,
        ["B8A"] = 20, ["B09"] = 60, ["B10"] = 60, ["B11"] = 20, ["B12"] = 20
    };

    /// <summary>
    /// Sentinel-2 band sort order for consistent stacking.
    /// </summary>
    private static readonly Dictionary<string, int> BandSortOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["B01"] = 1, ["B02"] = 2, ["B03"] = 3, ["B04"] = 4,
        ["B05"] = 5, ["B06"] = 6, ["B07"] = 7, ["B08"] = 8,
        ["B8A"] = 9, ["B09"] = 10, ["B10"] = 11, ["B11"] = 12, ["B12"] = 13
    };

    public enum InputFormat { BandFolder, SafePackage, ZipArchive, Unknown }

    public record Sentinel2BandInfo(
        string BandName,
        string FilePath,
        int ResolutionMeters,
        int Width,
        int Height,
        string DataType,
        string Projection,
        double[] GeoTransform);

    public record ImportOptions(
        IReadOnlyList<Sentinel2BandInfo> SelectedBands,
        string OutputPath);

    public record ImportProgress(
        int CurrentBand,
        int TotalBands,
        string CurrentBandName,
        string Status);

    public Sentinel2ImportService(ILogger<Sentinel2ImportService> logger)
    {
        _logger = logger;
    }

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
    /// Validates that a path is not UNC/network and resolves it to a full path.
    /// </summary>
    private static string ValidateAndResolvePath(string path, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
        var fullPath = Path.GetFullPath(path);

        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException("UNC/network paths are not allowed.", paramName);

        return fullPath;
    }

    /// <summary>
    /// Detects the input format from a path.
    /// </summary>
    public InputFormat DetectFormat(string path)
    {
        var fullPath = ValidateAndResolvePath(path, nameof(path));

        if (Directory.Exists(fullPath))
        {
            // Check for .SAFE package (contains MTD_MSIL2A.xml or name ends with .SAFE)
            if (fullPath.EndsWith(".SAFE", StringComparison.OrdinalIgnoreCase) ||
                File.Exists(Path.Combine(fullPath, "MTD_MSIL2A.xml")))
                return InputFormat.SafePackage;

            // Check for folder with band files
            var bandFiles = Directory.GetFiles(fullPath)
                .Where(f => BandExtensions.Contains(Path.GetExtension(f)))
                .ToList();
            if (bandFiles.Count > 0)
                return InputFormat.BandFolder;
        }

        if (File.Exists(fullPath) &&
            Path.GetExtension(fullPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return InputFormat.ZipArchive;

        return InputFormat.Unknown;
    }

    /// <summary>
    /// Discovers Sentinel-2 bands in a folder of individual raster files.
    /// Opens each file with GDAL to read resolution, dimensions, and projection.
    /// </summary>
    public List<Sentinel2BandInfo> DiscoverBands(string path)
    {
        var fullPath = ValidateAndResolvePath(path, nameof(path));
        EnsureInitialized();

        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"Directory not found: '{fullPath}'.");

        var files = Directory.GetFiles(fullPath)
            .Where(f => BandExtensions.Contains(Path.GetExtension(f)))
            .Take(MaxBandFiles)
            .ToList();

        var bands = new List<Sentinel2BandInfo>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var match = BandNameRegex.Match(fileName);
            if (!match.Success)
                continue; // Skip non-band files (e.g., TCI, AOT, SCL)

            var bandName = match.Groups[1].Value.ToUpperInvariant();

            // Try resolution from filename, fallback to lookup table
            int resolution;
            if (match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out var parsedRes))
                resolution = parsedRes;
            else if (NativeResolution.TryGetValue(bandName, out var nativeRes))
                resolution = nativeRes;
            else
                resolution = 0; // Unknown

            // Read metadata via GDAL
            try
            {
                using var dataset = Gdal.Open(file, Access.GA_ReadOnly);
                if (dataset is null) continue;

                var gt = new double[6];
                dataset.GetGeoTransform(gt);

                // If resolution still unknown, compute from GeoTransform pixel size
                if (resolution == 0 && gt[1] != 0)
                    resolution = (int)Math.Round(Math.Abs(gt[1]));

                using var firstBand = dataset.GetRasterBand(1);

                bands.Add(new Sentinel2BandInfo(
                    BandName: bandName,
                    FilePath: file,
                    ResolutionMeters: resolution,
                    Width: dataset.RasterXSize,
                    Height: dataset.RasterYSize,
                    DataType: Gdal.GetDataTypeName(firstBand.DataType),
                    Projection: dataset.GetProjection() ?? "",
                    GeoTransform: gt));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                _logger.LogWarning(ex, "Failed to open '{File}' with GDAL, skipping", file);
            }
        }

        // Sort bands in Sentinel-2 spectral order
        bands.Sort((a, b) =>
        {
            var orderA = BandSortOrder.GetValueOrDefault(a.BandName, 99);
            var orderB = BandSortOrder.GetValueOrDefault(b.BandName, 99);
            return orderA.CompareTo(orderB);
        });

        return bands;
    }

    /// <summary>
    /// Builds a multi-band VRT file from selected single-band files.
    /// All bands must have the same dimensions (same resolution group).
    /// The VRT is a lightweight XML file that references the original band files.
    /// </summary>
    public Task<string> BuildVrtAsync(
        ImportOptions options,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.SelectedBands.Count == 0)
            throw new ArgumentException("At least one band must be selected.", nameof(options));

        var outputPath = ValidateAndResolvePath(options.OutputPath, nameof(options.OutputPath));

        // Ensure output has .vrt extension
        if (!outputPath.EndsWith(".vrt", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Output path must have .vrt extension.", nameof(options.OutputPath));

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bands = options.SelectedBands;
            var reference = bands[0];

            // Validate all bands have same dimensions
            for (var i = 1; i < bands.Count; i++)
            {
                if (bands[i].Width != reference.Width || bands[i].Height != reference.Height)
                    throw new InvalidOperationException(
                        $"Band {bands[i].BandName} ({bands[i].Width}x{bands[i].Height}) has different dimensions " +
                        $"than {reference.BandName} ({reference.Width}x{reference.Height}). " +
                        "Select bands at the same resolution or use the same resolution group.");
            }

            progress?.Report(new ImportProgress(0, bands.Count, "", "Building VRT"));

            // Build VRT XML
            var vrt = new XElement("VRTDataset",
                new XAttribute("rasterXSize", reference.Width),
                new XAttribute("rasterYSize", reference.Height));

            // Add SRS if available
            if (!string.IsNullOrWhiteSpace(reference.Projection))
                vrt.Add(new XElement("SRS", reference.Projection));

            // Add GeoTransform (InvariantCulture to ensure dot decimal separator)
            if (reference.GeoTransform is { Length: 6 })
            {
                vrt.Add(new XElement("GeoTransform",
                    string.Join(", ", reference.GeoTransform.Select(
                        v => v.ToString("G17", CultureInfo.InvariantCulture)))));
            }

            for (var i = 0; i < bands.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var band = bands[i];
                progress?.Report(new ImportProgress(i + 1, bands.Count, band.BandName,
                    $"Adding {band.BandName}"));

                var vrtBand = new XElement("VRTRasterBand",
                    new XAttribute("dataType", band.DataType),
                    new XAttribute("band", i + 1),
                    new XElement("Description", band.BandName),
                    new XElement("SimpleSource",
                        new XElement("SourceFilename",
                            new XAttribute("relativeToVRT", "0"),
                            band.FilePath),
                        new XElement("SourceBand", 1),
                        new XElement("SrcRect",
                            new XAttribute("xOff", 0),
                            new XAttribute("yOff", 0),
                            new XAttribute("xSize", band.Width),
                            new XAttribute("ySize", band.Height)),
                        new XElement("DstRect",
                            new XAttribute("xOff", 0),
                            new XAttribute("yOff", 0),
                            new XAttribute("xSize", reference.Width),
                            new XAttribute("ySize", reference.Height))));

                vrt.Add(vrtBand);
            }

            // Write VRT file
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            File.WriteAllText(outputPath, vrt.ToString(), Encoding.UTF8);

            progress?.Report(new ImportProgress(bands.Count, bands.Count, "",
                $"VRT created with {bands.Count} bands"));

            return outputPath;
        }, cancellationToken);
    }

    /// <summary>
    /// Returns the available resolution groups from a list of discovered bands.
    /// </summary>
    public static IReadOnlyList<int> GetAvailableResolutions(IReadOnlyList<Sentinel2BandInfo> bands)
    {
        return bands
            .Select(b => b.ResolutionMeters)
            .Where(r => r > 0)
            .Distinct()
            .OrderBy(r => r)
            .ToList();
    }
}
