namespace FuzzySat.Core.Configuration;

/// <summary>
/// Configuration for a single spectral band.
/// </summary>
public sealed record BandConfiguration
{
    /// <summary>Gets the band name (e.g., "VNIR1").</summary>
    public required string Name { get; init; }

    /// <summary>Gets the band index in the source raster (0-based).</summary>
    public required int SourceIndex { get; init; }

    /// <summary>Gets an optional description (e.g., "Visible Near Infrared 1").</summary>
    public string? Description { get; init; }
}
