using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Operators;

namespace FuzzySat.Core.FuzzyLogic.Rules;

/// <summary>
/// A single fuzzy classification rule for one land cover class.
/// Each rule maps band names to membership functions and evaluates
/// pixel values using AND (minimum) across all bands.
/// </summary>
public sealed class FuzzyRule
{
    /// <summary>
    /// Gets the land cover class name (e.g., "Urban", "Water", "Forest").
    /// </summary>
    public string ClassName { get; }

    /// <summary>
    /// Gets the band-to-membership-function mappings.
    /// </summary>
    public IReadOnlyDictionary<string, IMembershipFunction> BandMembershipFunctions { get; }

    /// <summary>
    /// Creates a new fuzzy rule for a land cover class.
    /// </summary>
    /// <param name="className">The land cover class name.</param>
    /// <param name="bandMembershipFunctions">Mapping of band names to their membership functions.</param>
    public FuzzyRule(string className, IDictionary<string, IMembershipFunction> bandMembershipFunctions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(className);
        ArgumentNullException.ThrowIfNull(bandMembershipFunctions);

        if (bandMembershipFunctions.Count == 0)
            throw new ArgumentException("At least one band membership function is required.", nameof(bandMembershipFunctions));

        ClassName = className;
        BandMembershipFunctions = new Dictionary<string, IMembershipFunction>(bandMembershipFunctions).AsReadOnly();
    }

    /// <summary>
    /// Evaluates the firing strength of this rule for the given pixel band values.
    /// Uses AND (minimum) across all band membership degrees, as defined in the thesis.
    /// </summary>
    /// <param name="bandValues">Pixel values per band (e.g., reflectance values 0-255).</param>
    /// <returns>The firing strength in [0, 1].</returns>
    public double Evaluate(IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);

        var degrees = BandMembershipFunctions.Select(kvp => kvp.Value.Evaluate(bandValues[kvp.Key]));
        return FuzzyOperators.And(degrees);
    }
}
