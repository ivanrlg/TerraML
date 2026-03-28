namespace FuzzySat.Core.FuzzyLogic.MembershipFunctions;

/// <summary>
/// Generalized bell-shaped membership function:
/// μ(x) = 1 / (1 + |((x - center) / width)|^(2*slope)).
/// Smoother than Gaussian with adjustable slope steepness.
/// </summary>
public sealed class BellMembershipFunction : IMembershipFunction
{
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public double Center { get; }

    /// <inheritdoc />
    public double Width { get; }

    /// <summary>Gets the slope parameter controlling steepness.</summary>
    public double Slope { get; }

    /// <summary>
    /// Creates a generalized bell membership function.
    /// </summary>
    /// <param name="name">Human-readable name.</param>
    /// <param name="center">Center of the bell curve.</param>
    /// <param name="width">Half-width at the crossover points. Must be positive.</param>
    /// <param name="slope">Slope steepness. Must be positive. Higher = steeper.</param>
    public BellMembershipFunction(string name, double center, double width, double slope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (!double.IsFinite(width) || width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be a finite positive number.");
        if (!double.IsFinite(slope) || slope <= 0)
            throw new ArgumentOutOfRangeException(nameof(slope), slope, "Slope must be a finite positive number.");
        if (!double.IsFinite(center))
            throw new ArgumentOutOfRangeException(nameof(center), center, "Center must be finite.");

        Name = name;
        Center = center;
        Width = width;
        Slope = slope;
    }

    /// <inheritdoc />
    public double Evaluate(double x)
    {
        var ratio = Math.Abs((x - Center) / Width);
        return 1.0 / (1.0 + Math.Pow(ratio, 2.0 * Slope));
    }

    /// <inheritdoc />
    public override string ToString() => $"Bell(center={Center}, width={Width}, slope={Slope})";
}
