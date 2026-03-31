using FuzzySat.Core.Classification;

namespace FuzzySat.Core.Configuration;

/// <summary>
/// Complete configuration for a fuzzy classification project.
/// Serializable to/from JSON for persistence.
/// </summary>
public sealed class ClassifierConfiguration
{
    /// <summary>Gets or sets the project name.</summary>
    public required string ProjectName { get; init; }

    /// <summary>Gets or sets the band configurations.</summary>
    public required IReadOnlyList<BandConfiguration> Bands { get; init; }

    /// <summary>Gets or sets the land cover class definitions.</summary>
    public required IReadOnlyList<LandCoverClass> Classes { get; init; }

    /// <summary>Gets or sets the path to the training data file. Optional.</summary>
    public string? TrainingDataPath { get; init; }

    /// <summary>Gets or sets the path to the input raster. Optional.</summary>
    public string? InputRasterPath { get; init; }

    /// <summary>Gets or sets the path for the output classification raster. Optional.</summary>
    public string? OutputRasterPath { get; init; }

    /// <summary>Gets or sets the Sentinel-2 import folder path. Optional.</summary>
    public string? ImportFolderPath { get; init; }

    /// <summary>Gets or sets the input mode ("DirectPath" or "ImportSentinel2"). Optional.</summary>
    public string? InputMode { get; init; }
}
