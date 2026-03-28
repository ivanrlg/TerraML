namespace FuzzySat.Core.FuzzyLogic.MembershipFunctions;

/// <summary>
/// Defines a fuzzy membership function that maps a crisp input value
/// to a degree of membership in the range [0, 1].
/// </summary>
public interface IMembershipFunction
{
    /// <summary>
    /// Gets the human-readable name of this membership function (e.g., "Urban_Band1").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the center (peak) parameter of the membership function.
    /// For a Gaussian MF, this is the mean.
    /// </summary>
    double Center { get; }

    /// <summary>
    /// Gets the width parameter of the membership function.
    /// For a Gaussian MF, this is the standard deviation (spread).
    /// </summary>
    double Width { get; }

    /// <summary>
    /// Evaluates the membership degree for a given crisp input value.
    /// </summary>
    /// <param name="x">The crisp input value (e.g., a pixel's spectral reflectance).</param>
    /// <returns>A value in [0, 1] representing the degree of membership.</returns>
    double Evaluate(double x);
}
