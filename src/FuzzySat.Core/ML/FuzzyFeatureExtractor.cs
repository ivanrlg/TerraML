using FuzzySat.Core.FuzzyLogic.Rules;

namespace FuzzySat.Core.ML;

/// <summary>
/// Extracts ML features from pixel values by computing fuzzy membership degrees
/// for each class-band combination. Produces a feature vector that combines
/// raw spectral values with fuzzy membership degrees.
/// </summary>
public sealed class FuzzyFeatureExtractor : IFeatureExtractor
{
    private readonly FuzzyRuleSet _ruleSet;
    private readonly IReadOnlyList<string> _bandNames;

    /// <summary>Gets the names of all features produced by this extractor.</summary>
    public IReadOnlyList<string> FeatureNames { get; }

    /// <summary>
    /// Creates a feature extractor from a fuzzy rule set.
    /// </summary>
    public FuzzyFeatureExtractor(FuzzyRuleSet ruleSet, IReadOnlyList<string> bandNames)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        ArgumentNullException.ThrowIfNull(bandNames);

        _ruleSet = ruleSet;
        _bandNames = bandNames.ToList().AsReadOnly();

        // Feature names: raw bands + membership degrees per class per band + firing strengths
        var names = new List<string>();

        // Raw spectral values
        foreach (var band in _bandNames)
            names.Add($"Raw_{band}");

        // Membership degrees per class per band
        foreach (var rule in ruleSet.Rules)
            foreach (var band in _bandNames)
                names.Add($"MF_{rule.ClassName}_{band}");

        // Firing strengths per class
        foreach (var rule in ruleSet.Rules)
            names.Add($"Strength_{rule.ClassName}");

        FeatureNames = names.AsReadOnly();
    }

    /// <summary>
    /// Extracts a feature vector from pixel band values.
    /// </summary>
    /// <param name="bandValues">Pixel values per band.</param>
    /// <returns>Feature vector combining raw values, membership degrees, and firing strengths.</returns>
    public float[] ExtractFeatures(IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);

        var features = new float[FeatureNames.Count];
        var idx = 0;

        // Raw spectral values
        foreach (var band in _bandNames)
            features[idx++] = (float)bandValues[band];

        // Membership degrees per class per band
        foreach (var rule in _ruleSet.Rules)
            foreach (var band in _bandNames)
                features[idx++] = (float)rule.BandMembershipFunctions[band].Evaluate(bandValues[band]);

        // Firing strengths per class (min of all band memberships)
        foreach (var rule in _ruleSet.Rules)
            features[idx++] = (float)rule.Evaluate(bandValues);

        return features;
    }
}
