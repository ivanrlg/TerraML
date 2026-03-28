namespace FuzzySat.Core.Training;

/// <summary>
/// Training statistics for one land cover class: mean and standard deviation per band.
/// These parameters are used to construct Gaussian membership functions.
/// </summary>
public sealed class SpectralStatistics
{
    /// <summary>Gets the land cover class name.</summary>
    public string ClassName { get; }

    /// <summary>Gets the mean reflectance per band.</summary>
    public IReadOnlyDictionary<string, double> MeanPerBand { get; }

    /// <summary>Gets the standard deviation per band.</summary>
    public IReadOnlyDictionary<string, double> StdDevPerBand { get; }

    /// <summary>
    /// Creates spectral statistics for a land cover class.
    /// </summary>
    public SpectralStatistics(
        string className,
        IDictionary<string, double> meanPerBand,
        IDictionary<string, double> stdDevPerBand)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(className, nameof(className));
        ArgumentNullException.ThrowIfNull(meanPerBand);
        ArgumentNullException.ThrowIfNull(stdDevPerBand);

        if (meanPerBand.Count == 0)
            throw new ArgumentException("At least one band is required.", nameof(meanPerBand));

        if (!meanPerBand.Keys.OrderBy(k => k).SequenceEqual(stdDevPerBand.Keys.OrderBy(k => k)))
            throw new ArgumentException("Mean and stddev must have the same band keys.", nameof(stdDevPerBand));

        foreach (var (band, stdDev) in stdDevPerBand)
        {
            if (!double.IsFinite(stdDev) || stdDev <= 0)
                throw new ArgumentOutOfRangeException(nameof(stdDevPerBand),
                    $"Standard deviation for band '{band}' must be a finite positive number, got {stdDev}.");
        }

        foreach (var (band, mean) in meanPerBand)
        {
            if (!double.IsFinite(mean))
                throw new ArgumentOutOfRangeException(nameof(meanPerBand),
                    $"Mean for band '{band}' must be finite, got {mean}.");
        }

        ClassName = className;
        MeanPerBand = new Dictionary<string, double>(meanPerBand).AsReadOnly();
        StdDevPerBand = new Dictionary<string, double>(stdDevPerBand).AsReadOnly();
    }
}
