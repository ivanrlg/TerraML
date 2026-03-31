namespace FuzzySat.Core.Persistence;

/// <summary>
/// Metadata DTO for a persisted classification result.
/// The actual pixel data (class map + confidence map) is stored separately as binary.
/// </summary>
public sealed class ClassificationResultDto
{
    /// <summary>Number of rows in the classification map.</summary>
    public int Rows { get; set; }

    /// <summary>Number of columns in the classification map.</summary>
    public int Columns { get; set; }

    /// <summary>Ordered class names used in the classification.</summary>
    public List<string> ClassNames { get; set; } = [];

    /// <summary>Class codes corresponding to each class name.</summary>
    public List<int> ClassCodes { get; set; } = [];

    /// <summary>Display colors corresponding to each class name.</summary>
    public List<string?> ClassColors { get; set; } = [];
}
