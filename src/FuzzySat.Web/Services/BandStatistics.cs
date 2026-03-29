namespace FuzzySat.Web.Services;

/// <summary>
/// Statistics computed from a single spectral band.
/// </summary>
public sealed record BandStatistics(
    double Min,
    double Max,
    double Mean,
    double StdDev,
    int[] Histogram);
