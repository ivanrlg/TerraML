namespace FuzzySat.Web.Services;

/// <summary>
/// Lightweight summary of a persisted project for the History page.
/// </summary>
public sealed record ProjectSummary
{
    /// <summary>Filename-safe persistence key (used for load/delete operations).</summary>
    public required string Key { get; init; }

    /// <summary>User-facing display name from ClassifierConfiguration.ProjectName.</summary>
    public required string Name { get; init; }

    public int BandCount { get; init; }
    public int ClassCount { get; init; }
    public string? ClassificationMethod { get; init; }
    public double? OverallAccuracy { get; init; }
    public double? KappaCoefficient { get; init; }
    public DateTime LastModified { get; init; }
    public ProjectStatus Status { get; init; }
}

/// <summary>
/// Represents the furthest stage a project has reached.
/// </summary>
public enum ProjectStatus
{
    Configured,
    Trained,
    Classified,
    Validated
}
