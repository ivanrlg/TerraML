namespace FuzzySat.Core.Raster;

/// <summary>
/// A single spectral band from a multispectral image.
/// Stores pixel values as a 2D array [row, col].
/// </summary>
public sealed class Band
{
    /// <summary>Gets the band name (e.g., "VNIR1", "SWIR1").</summary>
    public string Name { get; }

    /// <summary>Gets the number of rows (height) in pixels.</summary>
    public int Rows { get; }

    /// <summary>Gets the number of columns (width) in pixels.</summary>
    public int Columns { get; }

    private readonly double[,] _data;

    /// <summary>
    /// Creates a new band from a 2D array of pixel values.
    /// </summary>
    /// <param name="name">Band name.</param>
    /// <param name="data">Pixel values [row, col]. A defensive copy is made.</param>
    public Band(string name, double[,] data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(data);

        if (data.GetLength(0) == 0 || data.GetLength(1) == 0)
            throw new ArgumentException("Band data must have at least one pixel.", nameof(data));

        Name = name;
        Rows = data.GetLength(0);
        Columns = data.GetLength(1);
        _data = (double[,])data.Clone();
    }

    /// <summary>Gets the pixel value at [row, col].</summary>
    public double this[int row, int col] => _data[row, col];
}
