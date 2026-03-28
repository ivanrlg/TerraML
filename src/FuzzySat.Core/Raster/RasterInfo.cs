namespace FuzzySat.Core.Raster;

/// <summary>
/// Metadata about a raster file: dimensions, band count, projection, and pixel type.
/// </summary>
public sealed class RasterInfo
{
    /// <summary>Gets the file path of the raster.</summary>
    public string FilePath { get; }

    /// <summary>Gets the number of rows (height) in pixels.</summary>
    public int Rows { get; }

    /// <summary>Gets the number of columns (width) in pixels.</summary>
    public int Columns { get; }

    /// <summary>Gets the number of spectral bands.</summary>
    public int BandCount { get; }

    /// <summary>Gets the pixel data type name (e.g., "Float64", "UInt16").</summary>
    public string DataType { get; }

    /// <summary>Gets the coordinate reference system (e.g., "EPSG:4326"). Null if unknown.</summary>
    public string? Projection { get; }

    /// <summary>Gets the driver/format name (e.g., "GTiff").</summary>
    public string DriverName { get; }

    /// <summary>
    /// Creates raster metadata.
    /// </summary>
    public RasterInfo(
        string filePath,
        int rows,
        int columns,
        int bandCount,
        string dataType,
        string driverName,
        string? projection = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(dataType, nameof(dataType));
        ArgumentException.ThrowIfNullOrWhiteSpace(driverName, nameof(driverName));

        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
        if (bandCount <= 0) throw new ArgumentOutOfRangeException(nameof(bandCount));

        FilePath = filePath;
        Rows = rows;
        Columns = columns;
        BandCount = bandCount;
        DataType = dataType;
        DriverName = driverName;
        Projection = projection;
    }
}
