namespace FuzzySat.Core.Classification;

/// <summary>
/// A land cover class definition with a numeric code and display name.
/// </summary>
public sealed record LandCoverClass
{
    /// <summary>Gets the class name (e.g., "Urban", "Water", "Forest").</summary>
    public required string Name { get; init; }

    /// <summary>Gets the numeric code used in classification rasters (e.g., 1, 2, 3).</summary>
    public required int Code { get; init; }

    /// <summary>Gets the display color as a hex string (e.g., "#FF0000"). Optional.</summary>
    public string? Color { get; init; }
}
