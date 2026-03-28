namespace FuzzySat.Core.Training;

/// <summary>
/// Extracts mean and standard deviation per class per band from labeled training data.
/// Implements the training algorithm from the thesis: center = mean, spread = stddev.
/// </summary>
public static class TrainingDataExtractor
{
    /// <summary>
    /// Computes spectral statistics (mean + stddev) for each land cover class.
    /// </summary>
    /// <param name="samples">Labeled pixel samples with consistent band names.</param>
    /// <returns>Dictionary of class name to spectral statistics.</returns>
    public static IReadOnlyDictionary<string, SpectralStatistics> ExtractStatistics(
        IEnumerable<LabeledPixelSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var sampleList = samples.ToList();

        if (sampleList.Count == 0)
            throw new ArgumentException("At least one sample is required.", nameof(samples));

        // Validate all samples are well-formed
        for (var i = 0; i < sampleList.Count; i++)
        {
            if (sampleList[i] is null)
                throw new ArgumentException($"Sample at index {i} is null.", nameof(samples));
            if (string.IsNullOrWhiteSpace(sampleList[i].ClassName))
                throw new ArgumentException($"Sample at index {i} has a null or whitespace ClassName.", nameof(samples));
            if (sampleList[i].BandValues is null || sampleList[i].BandValues.Count == 0)
                throw new ArgumentException($"Sample at index {i} has no band values.", nameof(samples));
        }

        // Verify all samples have the same band names
        var bandNames = sampleList[0].BandValues.Keys.OrderBy(k => k).ToList();
        for (var i = 1; i < sampleList.Count; i++)
        {
            var currentBands = sampleList[i].BandValues.Keys.OrderBy(k => k).ToList();
            if (!bandNames.SequenceEqual(currentBands))
                throw new ArgumentException(
                    $"Sample {i} has different bands than sample 0. Expected [{string.Join(", ", bandNames)}], got [{string.Join(", ", currentBands)}].",
                    nameof(samples));
        }

        var grouped = sampleList.GroupBy(s => s.ClassName);
        var result = new Dictionary<string, SpectralStatistics>();

        foreach (var group in grouped)
        {
            var className = group.Key;
            var classSamples = group.ToList();
            var meanPerBand = new Dictionary<string, double>();
            var stdDevPerBand = new Dictionary<string, double>();

            foreach (var band in bandNames)
            {
                var values = classSamples.Select(s => s.BandValues[band]).ToList();
                var mean = values.Average();
                var variance = values.Select(v => (v - mean) * (v - mean)).Average();
                var stdDev = Math.Sqrt(variance);

                // Guard: stddev = 0 means all samples identical — use small epsilon
                if (stdDev == 0)
                    stdDev = 1e-10;

                meanPerBand[band] = mean;
                stdDevPerBand[band] = stdDev;
            }

            result[className] = new SpectralStatistics(className, meanPerBand, stdDevPerBand);
        }

        return result.AsReadOnly();
    }
}
