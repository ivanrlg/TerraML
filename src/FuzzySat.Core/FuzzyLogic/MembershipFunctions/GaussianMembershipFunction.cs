namespace FuzzySat.Core.FuzzyLogic.MembershipFunctions;

/// <summary>
/// Gaussian membership function: μ(x) = exp(-0.5 * ((x - center) / spread)²).
/// Used in the original thesis for all land cover classes.
/// </summary>
public sealed class GaussianMembershipFunction : IMembershipFunction
{
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public double Center { get; }

    /// <summary>
    /// Gets the spread (standard deviation) of the Gaussian curve.
    /// </summary>
    public double Spread { get; }

    /// <inheritdoc />
    public double Width => Spread;

    /// <summary>
    /// Creates a new Gaussian membership function.
    /// </summary>
    /// <param name="name">Human-readable name (e.g., "Urban_Band1").</param>
    /// <param name="center">The mean (peak) of the Gaussian curve.</param>
    /// <param name="spread">The standard deviation. Must be greater than zero.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="spread"/> is not positive.</exception>
    public GaussianMembershipFunction(string name, double center, double spread)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (spread <= 0)
            throw new ArgumentOutOfRangeException(nameof(spread), spread, "Spread must be greater than zero.");

        Name = name;
        Center = center;
        Spread = spread;
    }

    /// <inheritdoc />
    public double Evaluate(double x)
    {
        var z = (x - Center) / Spread;
        return Math.Exp(-0.5 * z * z);
    }

    /// <inheritdoc />
    public override string ToString() => $"Gaussian(center={Center}, spread={Spread})";
}
