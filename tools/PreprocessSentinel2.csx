#!/usr/bin/env dotnet-script
// Sentinel-2 SAFE → Single multiband GeoTIFF preprocessor
// Uses GDAL via MaxRev.Gdal.Core (same as FuzzySat)
//
// Usage: dotnet script tools/PreprocessSentinel2.csx
//
// What it does:
// 1. Reads 10 useful Sentinel-2 bands (skips B01/B09/B10 = atmospheric)
// 2. Resamples 20m bands to 10m using bilinear interpolation
// 3. Crops to a 2000x2000 pixel subset (configurable) to keep file manageable
// 4. Stacks into a single compressed GeoTIFF with band names

// Since dotnet-script may not be available, this is also a reference for
// the standalone console app below.

Console.WriteLine("This is a reference script. Use the console app instead:");
Console.WriteLine("  dotnet run --project tools/S2Preprocess");
