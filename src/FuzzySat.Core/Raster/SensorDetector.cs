namespace FuzzySat.Core.Raster;

/// <summary>
/// Detects the likely satellite sensor from raster band count.
/// </summary>
public static class SensorDetector
{
    /// <summary>
    /// Suggests a sensor name based on the number of bands in a raster.
    /// Returns null if the band count doesn't match a known sensor.
    /// </summary>
    public static string? DetectFromBandCount(int bandCount) => bandCount switch
    {
        13 => "Sentinel-2",
        12 => "Sentinel-2 (no B10)",
        7 => "Landsat 8/9",
        8 => "Landsat 8/9",
        6 => "Landsat 5/7",
        4 => "NAIP",
        _ => null
    };

    /// <summary>
    /// Returns default band names for a known sensor, or null if unknown.
    /// </summary>
    public static IReadOnlyList<string>? GetBandNames(string sensor) => sensor switch
    {
        "Sentinel-2" => ["B01", "B02", "B03", "B04", "B05", "B06", "B07", "B08", "B8A", "B09", "B10", "B11", "B12"],
        "Sentinel-2 (no B10)" => ["B01", "B02", "B03", "B04", "B05", "B06", "B07", "B08", "B8A", "B09", "B11", "B12"],
        "Landsat 8/9" when sensor == "Landsat 8/9" => ["Coastal", "Blue", "Green", "Red", "NIR", "SWIR1", "SWIR2"],
        "Landsat 5/7" => ["Blue", "Green", "Red", "NIR", "SWIR1", "SWIR2"],
        "NAIP" => ["Red", "Green", "Blue", "NIR"],
        _ => null
    };
}
