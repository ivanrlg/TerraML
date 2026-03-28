using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;

namespace FuzzySat.Core.Training;

/// <summary>
/// Encapsulates training results: spectral statistics per class that can be used
/// to build a FuzzyRuleSet for classification. Bridges training and inference.
/// </summary>
public sealed class TrainingSession
{
    /// <summary>Gets the unique session identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the session creation timestamp.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Gets the ordered list of class names (defines rule evaluation order).</summary>
    public IReadOnlyList<string> ClassNames { get; }

    /// <summary>Gets the ordered list of band names (consistent across all classes).</summary>
    public IReadOnlyList<string> BandNames { get; }

    /// <summary>Gets the spectral statistics per class.</summary>
    public IReadOnlyDictionary<string, SpectralStatistics> Statistics { get; }

    private TrainingSession(
        IReadOnlyDictionary<string, SpectralStatistics> statistics,
        IReadOnlyList<string> classNames,
        IReadOnlyList<string> bandNames,
        string? id = null,
        DateTime? createdAt = null)
    {
        Statistics = statistics;
        ClassNames = classNames;
        BandNames = bandNames;
        Id = id ?? Guid.NewGuid().ToString();
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a training session from labeled pixel samples.
    /// Extracts mean + stddev per class per band.
    /// </summary>
    public static TrainingSession CreateFromSamples(IEnumerable<LabeledPixelSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var statistics = TrainingDataExtractor.ExtractStatistics(samples);
        var classNames = statistics.Keys.ToList().AsReadOnly();
        var bandNames = statistics.Values.First().MeanPerBand.Keys.ToList().AsReadOnly();

        return new TrainingSession(statistics, classNames, bandNames);
    }

    /// <summary>
    /// Creates a training session from pre-computed statistics (e.g., loaded from JSON).
    /// </summary>
    public static TrainingSession CreateFromStatistics(
        IDictionary<string, SpectralStatistics> statistics,
        IEnumerable<string> classNames,
        IEnumerable<string> bandNames,
        string? id = null,
        DateTime? createdAt = null)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentNullException.ThrowIfNull(classNames);
        ArgumentNullException.ThrowIfNull(bandNames);

        if (statistics.Count == 0)
            throw new ArgumentException("At least one class is required.", nameof(statistics));

        // Defensive copy of statistics dictionary
        var statsCopy = new Dictionary<string, SpectralStatistics>(statistics);

        var classNameList = classNames.ToList();
        var bandNameList = bandNames.ToList();

        if (classNameList.Count == 0)
            throw new ArgumentException("At least one class name is required.", nameof(classNames));
        if (bandNameList.Count == 0)
            throw new ArgumentException("At least one band name is required.", nameof(bandNames));

        // Validate classNames match statistics keys
        var statsKeySet = new HashSet<string>(statsCopy.Keys);
        if (!statsKeySet.SetEquals(classNameList))
            throw new ArgumentException("classNames must match statistics keys exactly.", nameof(classNames));

        // Validate bandNames match each class's band keys
        var bandNameSet = new HashSet<string>(bandNameList);
        foreach (var (classKey, classStats) in statsCopy)
        {
            if (!bandNameSet.SetEquals(classStats.MeanPerBand.Keys))
                throw new ArgumentException(
                    $"Band names do not match statistics bands for class '{classKey}'.", nameof(bandNames));
        }

        return new TrainingSession(
            statsCopy.AsReadOnly(),
            classNameList.AsReadOnly(),
            bandNameList.AsReadOnly(),
            id,
            createdAt);
    }

    /// <summary>
    /// Builds a FuzzyRuleSet from this session's statistics.
    /// Each class becomes a FuzzyRule with Gaussian MFs: center = mean, spread = stddev.
    /// </summary>
    public FuzzyRuleSet BuildRuleSet()
    {
        var rules = new List<FuzzyRule>();

        foreach (var className in ClassNames)
        {
            var stats = Statistics[className];
            var bandMFs = new Dictionary<string, IMembershipFunction>();

            foreach (var bandName in BandNames)
            {
                bandMFs[bandName] = new GaussianMembershipFunction(
                    name: $"{className}_{bandName}",
                    center: stats.MeanPerBand[bandName],
                    spread: stats.StdDevPerBand[bandName]);
            }

            rules.Add(new FuzzyRule(className, bandMFs));
        }

        return new FuzzyRuleSet(rules);
    }
}
