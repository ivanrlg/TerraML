namespace FuzzySat.Core.Raster;

/// <summary>
/// Calculates spectral indices as derived bands from multispectral imagery.
/// Indices use the normalized difference formula: (A - B) / (A + B).
/// </summary>
public static class SpectralIndexCalculator
{
    /// <summary>
    /// Calculates a normalized difference index: (bandA - bandB) / (bandA + bandB).
    /// Returns 0 when both bands are 0 at a pixel.
    /// </summary>
    public static Band NormalizedDifference(Band bandA, Band bandB, string outputName)
    {
        ArgumentNullException.ThrowIfNull(bandA);
        ArgumentNullException.ThrowIfNull(bandB);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputName, nameof(outputName));

        if (bandA.Rows != bandB.Rows || bandA.Columns != bandB.Columns)
            throw new ArgumentException("Bands must have the same dimensions.");

        var data = new double[bandA.Rows, bandA.Columns];
        for (var row = 0; row < bandA.Rows; row++)
        {
            for (var col = 0; col < bandA.Columns; col++)
            {
                var a = bandA[row, col];
                var b = bandB[row, col];
                var sum = a + b;
                data[row, col] = sum == 0.0 ? 0.0 : (a - b) / sum;
            }
        }

        return new Band(outputName, data);
    }

    /// <summary>
    /// NDVI = (NIR - Red) / (NIR + Red). Vegetation index.
    /// </summary>
    public static Band Ndvi(Band nir, Band red) => NormalizedDifference(nir, red, "NDVI");

    /// <summary>
    /// NDWI = (Green - NIR) / (Green + NIR). Water index.
    /// </summary>
    public static Band Ndwi(Band green, Band nir) => NormalizedDifference(green, nir, "NDWI");

    /// <summary>
    /// NDBI = (SWIR - NIR) / (SWIR + NIR). Built-up index.
    /// </summary>
    public static Band Ndbi(Band swir, Band nir) => NormalizedDifference(swir, nir, "NDBI");
}
