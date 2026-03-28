namespace FuzzySat.Core.FuzzyLogic.Inference;

/// <summary>
/// Holds the result of fuzzy inference for a single pixel: all firing strengths
/// and the winning class determined by maximum strength (first-in-order for ties).
/// </summary>
public sealed class InferenceResult
{
    /// <summary>
    /// Gets all firing strengths in rule evaluation order.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, double>> AllStrengths { get; }

    /// <summary>
    /// Gets the class name with the highest firing strength.
    /// In case of a tie, the first class in rule order wins.
    /// </summary>
    public string WinnerClass { get; }

    /// <summary>
    /// Gets the firing strength of the winning class.
    /// </summary>
    public double WinnerStrength { get; }

    /// <summary>
    /// Creates a new inference result from ordered firing strengths.
    /// </summary>
    /// <param name="strengths">Ordered list of (className, firingStrength) pairs.</param>
    public InferenceResult(IReadOnlyList<KeyValuePair<string, double>> strengths)
    {
        ArgumentNullException.ThrowIfNull(strengths);

        if (strengths.Count == 0)
            throw new ArgumentException("At least one class strength is required.", nameof(strengths));

        // Defensive copy to prevent post-construction drift
        var copy = new List<KeyValuePair<string, double>>(strengths);
        AllStrengths = copy.AsReadOnly();

        // Determine winner: highest strength, first-in-order wins ties
        var winner = copy[0];
        for (var i = 1; i < copy.Count; i++)
        {
            if (copy[i].Value > winner.Value)
                winner = copy[i];
        }

        WinnerClass = winner.Key;
        WinnerStrength = winner.Value;
    }
}
