namespace FuzzySat.Core.Raster;

/// <summary>
/// Metadata for a single raster band: index, data type, optional description, and color interpretation.
/// </summary>
/// <param name="Index">1-based band index.</param>
/// <param name="DataType">Pixel data type name (e.g., "UInt16", "Float64").</param>
/// <param name="Description">Band description from metadata, if available.</param>
/// <param name="ColorInterpretation">GDAL color interpretation (e.g., "Red", "Green", "Gray").</param>
public sealed record BandInfo(int Index, string DataType, string? Description, string? ColorInterpretation);
