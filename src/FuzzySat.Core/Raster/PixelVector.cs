namespace FuzzySat.Core.Raster;

/// <summary>
/// A single pixel's spectral values across all bands.
/// Implements IDictionary-compatible band value access for classification.
/// </summary>
public sealed class PixelVector
{
    /// <summary>Gets the band values indexed by band name.</summary>
    public IReadOnlyDictionary<string, double> BandValues { get; }

    /// <summary>Gets the pixel row position in the image.</summary>
    public int Row { get; }

    /// <summary>Gets the pixel column position in the image.</summary>
    public int Column { get; }

    /// <summary>
    /// Creates a pixel vector from band name-value pairs.
    /// </summary>
    public PixelVector(int row, int column, IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);

        if (bandValues.Count == 0)
            throw new ArgumentException("At least one band value is required.", nameof(bandValues));

        Row = row;
        Column = column;
        BandValues = new Dictionary<string, double>(bandValues).AsReadOnly();
    }
}
