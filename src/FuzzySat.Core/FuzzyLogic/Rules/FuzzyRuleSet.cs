namespace FuzzySat.Core.FuzzyLogic.Rules;

/// <summary>
/// A collection of fuzzy rules, one per land cover class.
/// Evaluates all rules for a pixel and returns ordered firing strengths.
/// </summary>
public sealed class FuzzyRuleSet
{
    /// <summary>
    /// Gets all rules in this set, in insertion order.
    /// </summary>
    public IReadOnlyList<FuzzyRule> Rules { get; }

    /// <summary>
    /// Creates a new rule set from a collection of fuzzy rules.
    /// </summary>
    /// <param name="rules">The rules to include. Must not be empty and must have unique class names.</param>
    public FuzzyRuleSet(IEnumerable<FuzzyRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var ruleList = rules.ToList();

        if (ruleList.Count == 0)
            throw new ArgumentException("At least one rule is required.", nameof(rules));

        if (ruleList.Any(r => r is null))
            throw new ArgumentException("All rules must be non-null.", nameof(rules));

        var duplicates = ruleList.GroupBy(r => r.ClassName).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new ArgumentException($"Duplicate class names: {string.Join(", ", duplicates)}", nameof(rules));

        Rules = ruleList.AsReadOnly();
    }

    /// <summary>
    /// Evaluates all rules and returns firing strengths in rule order.
    /// Order is preserved for deterministic tie-breaking in defuzzification.
    /// </summary>
    /// <param name="bandValues">Pixel values per band.</param>
    /// <returns>Ordered list of (className, firingStrength) pairs.</returns>
    public IReadOnlyList<KeyValuePair<string, double>> EvaluateAll(IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);

        return Rules
            .Select(rule => new KeyValuePair<string, double>(rule.ClassName, rule.Evaluate(bandValues)))
            .ToList()
            .AsReadOnly();
    }
}
