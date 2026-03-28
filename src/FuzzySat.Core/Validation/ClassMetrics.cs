namespace FuzzySat.Core.Validation;

/// <summary>
/// Accuracy metrics for a single land cover class.
/// </summary>
public sealed record ClassMetrics(
    string ClassName,
    double ProducersAccuracy,
    double UsersAccuracy,
    int ActualCount,
    int PredictedCount);
