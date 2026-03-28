namespace FuzzySat.Core.Training;

/// <summary>
/// A labeled training pixel: a class name and its spectral band values.
/// </summary>
public sealed record LabeledPixelSample
{
    /// <summary>
    /// Gets the land cover class name (e.g., "Forest").
    /// </summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// Gets the spectral band values (e.g., {"VNIR1": 75.5, "SWIR1": 85.0}).
    /// </summary>
    public required IReadOnlyDictionary<string, double> BandValues { get; init; }
}
