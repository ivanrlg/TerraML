using MaxRev.Gdal.Core;
using OSGeo.GDAL;

// ═══════════════════════════════════════════════════════════════════
// Sentinel-2 SAFE → Multiband GeoTIFF Preprocessor
// ═══════════════════════════════════════════════════════════════════
// Reads 10 useful bands, resamples 20m→10m, optionally crops a subset,
// outputs a single compressed GeoTIFF ready for FuzzySat classification.
// ═══════════════════════════════════════════════════════════════════

GdalBase.ConfigureAll();

// ── Configuration ──────────────────────────────────────────────────
var safeDir = args.Length > 0 ? args[0] :
    @"C:\Users\ivanr\Downloads\S2A_MSIL1C_20260327T151941_N0512_R125_T19PCK_20260327T215543.SAFE\S2A_MSIL1C_20260327T151941_N0512_R125_T19PCK_20260327T215543.SAFE";

var outputPath = args.Length > 1 ? args[1] :
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Documents", "FuzzySat", "sentinel2-merida.tif");

// Subset size (in 10m pixels). Set to 0 to use full tile.
var subsetSize = args.Length > 2 ? int.Parse(args[2]) : 2000;

// Bands to include (skip B01=aerosol, B09=water vapor, B10=cirrus)
var bandDefs = new (string Id, string Name, int NativeRes)[]
{
    ("B02", "Blue",       10),
    ("B03", "Green",      10),
    ("B04", "Red",        10),
    ("B05", "RedEdge1",   20),
    ("B06", "RedEdge2",   20),
    ("B07", "RedEdge3",   20),
    ("B08", "NIR",        10),
    ("B8A", "NIR_Narrow", 20),
    ("B11", "SWIR1",      20),
    ("B12", "SWIR2",      20),
};

// ── Find IMG_DATA folder ───────────────────────────────────────────
var granuleDir = Path.Combine(safeDir, "GRANULE");
if (!Directory.Exists(granuleDir))
{
    Console.Error.WriteLine($"ERROR: GRANULE directory not found in {safeDir}");
    return 1;
}

var tileDir = Directory.GetDirectories(granuleDir).FirstOrDefault();
if (tileDir is null)
{
    Console.Error.WriteLine("ERROR: No tile directory found in GRANULE/");
    return 1;
}

var imgDataDir = Path.Combine(tileDir, "IMG_DATA");
if (!Directory.Exists(imgDataDir))
{
    Console.Error.WriteLine($"ERROR: IMG_DATA not found in {tileDir}");
    return 1;
}

Console.WriteLine($"Sentinel-2 SAFE: {Path.GetFileName(safeDir)}");
Console.WriteLine($"Tile: {Path.GetFileName(tileDir)}");
Console.WriteLine($"Output: {outputPath}");
Console.WriteLine($"Bands: {bandDefs.Length}");
Console.WriteLine($"Subset: {(subsetSize > 0 ? $"{subsetSize}x{subsetSize} px" : "full tile")}");
Console.WriteLine();

// ── Read reference band (B02, 10m) for dimensions and geotransform ──
var refFile = Directory.GetFiles(imgDataDir, "*_B02.jp2").FirstOrDefault();
if (refFile is null)
{
    Console.Error.WriteLine("ERROR: B02.jp2 not found");
    return 1;
}

using var refDs = Gdal.Open(refFile, Access.GA_ReadOnly);
var fullWidth = refDs.RasterXSize;   // 10980
var fullHeight = refDs.RasterYSize;  // 10980
var geoTransform = new double[6];
refDs.GetGeoTransform(geoTransform);
var projection = refDs.GetProjection();

Console.WriteLine($"Reference (B02): {fullWidth}x{fullHeight}, pixel={geoTransform[1]}m");

// ── Compute subset window ──────────────────────────────────────────
int xOff, yOff, outWidth, outHeight;
var outGeoTransform = (double[])geoTransform.Clone();

if (subsetSize > 0 && subsetSize < fullWidth)
{
    // Center the subset, biased slightly toward a less-cloudy area
    // (offset 40% from top-left to avoid edge/corner clouds)
    xOff = Math.Min((int)(fullWidth * 0.4), fullWidth - subsetSize);
    yOff = Math.Min((int)(fullHeight * 0.4), fullHeight - subsetSize);
    outWidth = subsetSize;
    outHeight = subsetSize;

    // Update geotransform origin
    outGeoTransform[0] = geoTransform[0] + xOff * geoTransform[1]; // X origin
    outGeoTransform[3] = geoTransform[3] + yOff * geoTransform[5]; // Y origin
}
else
{
    xOff = 0;
    yOff = 0;
    outWidth = fullWidth;
    outHeight = fullHeight;
}

Console.WriteLine($"Output dimensions: {outWidth}x{outHeight} (offset: {xOff},{yOff})");
Console.WriteLine();

// ── Create output GeoTIFF ──────────────────────────────────────────
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

var driver = Gdal.GetDriverByName("GTiff");
var createOptions = new[] { "COMPRESS=DEFLATE", "TILED=YES", "BIGTIFF=IF_SAFER" };

using var outDs = driver.Create(outputPath, outWidth, outHeight, bandDefs.Length,
    DataType.GDT_UInt16, createOptions);
outDs.SetGeoTransform(outGeoTransform);
outDs.SetProjection(projection);

// ── Process each band ──────────────────────────────────────────────
for (var i = 0; i < bandDefs.Length; i++)
{
    var (bandId, bandName, nativeRes) = bandDefs[i];
    var bandFile = Directory.GetFiles(imgDataDir, $"*_{bandId}.jp2").FirstOrDefault();

    if (bandFile is null)
    {
        Console.Error.WriteLine($"  WARNING: {bandId}.jp2 not found, skipping");
        continue;
    }

    Console.Write($"  [{i + 1}/{bandDefs.Length}] {bandId} ({bandName}, {nativeRes}m) ... ");

    using var srcDs = Gdal.Open(bandFile, Access.GA_ReadOnly);
    var srcWidth = srcDs.RasterXSize;
    var srcHeight = srcDs.RasterYSize;
    var srcBand = srcDs.GetRasterBand(1);

    // Compute source window for this band's native resolution
    var scaleFactor = nativeRes / 10.0; // 1.0 for 10m, 2.0 for 20m
    var srcXOff = (int)(xOff / scaleFactor);
    var srcYOff = (int)(yOff / scaleFactor);
    var srcReadW = (int)(outWidth / scaleFactor);
    var srcReadH = (int)(outHeight / scaleFactor);

    // Clamp to source dimensions
    srcReadW = Math.Min(srcReadW, srcWidth - srcXOff);
    srcReadH = Math.Min(srcReadH, srcHeight - srcYOff);

    // Read source data at native resolution
    var srcData = new short[srcReadW * srcReadH];
    srcBand.ReadRaster(srcXOff, srcYOff, srcReadW, srcReadH,
        srcData, srcReadW, srcReadH, 0, 0);

    // If 20m band, resample to 10m using GDAL's RasterIO oversampling
    // (ReadRaster with different buf size does bilinear-like resampling)
    var outData = new short[outWidth * outHeight];
    if (nativeRes == 10)
    {
        // Direct copy (same resolution)
        Array.Copy(srcData, outData, Math.Min(srcData.Length, outData.Length));
    }
    else
    {
        // Resample: read again with output buffer size = 10m target
        srcBand.ReadRaster(srcXOff, srcYOff, srcReadW, srcReadH,
            outData, outWidth, outHeight, 0, 0);
    }

    // Write to output
    var outBand = outDs.GetRasterBand(i + 1);
    outBand.WriteRaster(0, 0, outWidth, outHeight, outData, outWidth, outHeight, 0, 0);
    outBand.SetDescription(bandName);
    outBand.FlushCache();

    Console.WriteLine($"OK ({srcWidth}x{srcHeight} → {outWidth}x{outHeight})");
}

outDs.FlushCache();

// ── Report ─────────────────────────────────────────────────────────
var fileInfo = new FileInfo(outputPath);
Console.WriteLine();
Console.WriteLine($"Done! Output: {outputPath}");
Console.WriteLine($"Size: {fileInfo.Length / 1024.0 / 1024.0:F1} MB");
Console.WriteLine($"Bands: {bandDefs.Length} ({string.Join(", ", bandDefs.Select(b => b.Name))})");
Console.WriteLine($"Resolution: 10m");
Console.WriteLine($"Dimensions: {outWidth}x{outHeight}");
Console.WriteLine();
Console.WriteLine("Use this path in FuzzySat ProjectSetup as Input Raster.");

return 0;
