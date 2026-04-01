namespace FuzzySat.Core.FuzzyLogic.MembershipFunctions;

/// <summary>
/// Trapezoidal membership function defined by four points: a, b, c, d.
/// μ(x) = 0 for x ≤ a or x ≥ d, rises linearly from a to b,
/// stays at 1 from b to c, falls linearly from c to d.
/// </summary>
public sealed class TrapezoidalMembershipFunction : IMembershipFunction
{
    /// <inheritdoc />
    public string Name { get; }

    /// <summary>Gets the left foot (μ = 0).</summary>
    public double A { get; }

    /// <summary>Gets the left shoulder (μ = 1 starts).</summary>
    public double B { get; }

    /// <summary>Gets the right shoulder (μ = 1 ends).</summary>
    public double C { get; }

    /// <summary>Gets the right foot (μ = 0).</summary>
    public double D { get; }

    /// <inheritdoc />
    public double Center => (B + C) / 2.0;

    /// <inheritdoc />
    public double Width => D - A;

    /// <summary>
    /// Creates a trapezoidal membership function.
    /// </summary>
    /// <param name="name">Human-readable name.</param>
    /// <param name="a">Left foot. Must satisfy a &lt; b &lt;= c &lt; d.</param>
    /// <param name="b">Left shoulder.</param>
    /// <param name="c">Right shoulder.</param>
    /// <param name="d">Right foot.</param>
    public TrapezoidalMembershipFunction(string name, double a, double b, double c, double d)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (!double.IsFinite(a) || !double.IsFinite(b) || !double.IsFinite(c) || !double.IsFinite(d))
            throw new ArgumentException("All parameters must be finite numbers.");

        if (a >= b)
            throw new ArgumentException("a must be less than b.", nameof(a));
        if (b > c)
            throw new ArgumentException("b must be less than or equal to c.", nameof(b));
        if (c >= d)
            throw new ArgumentException("c must be less than d.", nameof(c));

        Name = name;
        A = a;
        B = b;
        C = c;
        D = d;
    }

    /// <inheritdoc />
    public double Evaluate(double x)
    {
        if (x <= A || x >= D)
            return 0.0;
        if (x >= B && x <= C)
            return 1.0;
        if (x < B)
            return (x - A) / (B - A);
        return (D - x) / (D - C);
    }

    /// <inheritdoc />
    public override string ToString() => $"Trapezoidal(a={A}, b={B}, c={C}, d={D})";
}
