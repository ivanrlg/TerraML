namespace FuzzySat.Core.Raster;

/// <summary>
/// A multispectral image composed of multiple spectral bands.
/// All bands must have the same dimensions.
/// </summary>
public sealed class MultispectralImage
{
    private readonly Dictionary<string, Band> _bands;

    /// <summary>Gets the ordered list of band names.</summary>
    public IReadOnlyList<string> BandNames { get; }

    /// <summary>Gets the number of rows (height) in pixels.</summary>
    public int Rows { get; }

    /// <summary>Gets the number of columns (width) in pixels.</summary>
    public int Columns { get; }

    /// <summary>
    /// Creates a multispectral image from a collection of bands.
    /// All bands must have the same dimensions.
    /// </summary>
    public MultispectralImage(IEnumerable<Band> bands)
    {
        ArgumentNullException.ThrowIfNull(bands);

        var bandList = bands.ToList();

        if (bandList.Count == 0)
            throw new ArgumentException("At least one band is required.", nameof(bands));

        Rows = bandList[0].Rows;
        Columns = bandList[0].Columns;

        _bands = new Dictionary<string, Band>(StringComparer.Ordinal);
        foreach (var band in bandList)
        {
            if (band.Rows != Rows || band.Columns != Columns)
                throw new ArgumentException(
                    $"Band '{band.Name}' has dimensions ({band.Rows}x{band.Columns}) but expected ({Rows}x{Columns}).",
                    nameof(bands));

            if (!_bands.TryAdd(band.Name, band))
                throw new ArgumentException($"Duplicate band name: '{band.Name}'.", nameof(bands));
        }

        BandNames = bandList.Select(b => b.Name).ToList().AsReadOnly();
    }

    /// <summary>Gets a band by name.</summary>
    public Band GetBand(string name) => _bands[name];

    /// <summary>
    /// Extracts a pixel vector at the given position across all bands.
    /// </summary>
    public PixelVector GetPixelVector(int row, int col)
    {
        var values = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (name, band) in _bands)
            values[name] = band[row, col];
        return new PixelVector(row, col, values);
    }
}
