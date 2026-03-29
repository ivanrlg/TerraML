namespace FuzzySat.Core.Training;

/// <summary>
/// DTO for serializing/deserializing training sessions to/from JSON.
/// Shared between CLI commands and other consumers.
/// </summary>
public sealed class TrainingSessionDto
{
    /// <summary>Session identifier.</summary>
    public string Id { get; set; } = "";
    /// <summary>Session creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Ordered class names.</summary>
    public List<string> ClassNames { get; set; } = [];
    /// <summary>Ordered band names.</summary>
    public List<string> BandNames { get; set; } = [];
    /// <summary>Spectral statistics per class.</summary>
    public Dictionary<string, SpectralStatisticsDto> Statistics { get; set; } = [];

    /// <summary>
    /// Creates a DTO from a TrainingSession.
    /// </summary>
    public static TrainingSessionDto FromSession(TrainingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new TrainingSessionDto
        {
            Id = session.Id,
            CreatedAt = session.CreatedAt,
            ClassNames = session.ClassNames.ToList(),
            BandNames = session.BandNames.ToList(),
            Statistics = session.Statistics.ToDictionary(
                kvp => kvp.Key,
                kvp => new SpectralStatisticsDto
                {
                    ClassName = kvp.Value.ClassName,
                    MeanPerBand = kvp.Value.MeanPerBand.ToDictionary(b => b.Key, b => b.Value),
                    StdDevPerBand = kvp.Value.StdDevPerBand.ToDictionary(b => b.Key, b => b.Value)
                })
        };
    }

    /// <summary>
    /// Reconstructs a TrainingSession from this DTO.
    /// </summary>
    public TrainingSession ToSession()
    {
        var statistics = new Dictionary<string, SpectralStatistics>();
        foreach (var (className, statsDto) in Statistics)
        {
            statistics[className] = new SpectralStatistics(
                className,
                statsDto.MeanPerBand,
                statsDto.StdDevPerBand);
        }

        return TrainingSession.CreateFromStatistics(
            statistics, ClassNames, BandNames, Id, CreatedAt);
    }
}

/// <summary>
/// DTO for spectral statistics serialization.
/// </summary>
public sealed class SpectralStatisticsDto
{
    /// <summary>Land cover class name.</summary>
    public string ClassName { get; set; } = "";
    /// <summary>Mean reflectance per band.</summary>
    public Dictionary<string, double> MeanPerBand { get; set; } = [];
    /// <summary>Standard deviation per band.</summary>
    public Dictionary<string, double> StdDevPerBand { get; set; } = [];
}
