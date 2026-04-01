namespace FuzzySat.Core.FuzzyLogic.MembershipFunctions;

/// <summary>
/// Triangular membership function defined by three points: left, center, right.
/// μ(x) = 0 for x ≤ left or x ≥ right, linearly rises to 1 at center.
/// </summary>
public sealed class TriangularMembershipFunction : IMembershipFunction
{
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public double Center { get; }

    /// <summary>Gets the left foot of the triangle (μ = 0).</summary>
    public double Left { get; }

    /// <summary>Gets the right foot of the triangle (μ = 0).</summary>
    public double Right { get; }

    /// <inheritdoc />
    public double Width => Right - Left;

    /// <summary>
    /// Creates a triangular membership function.
    /// </summary>
    /// <param name="name">Human-readable name.</param>
    /// <param name="left">Left foot (μ = 0). Must be less than center.</param>
    /// <param name="center">Peak (μ = 1). Must be between left and right.</param>
    /// <param name="right">Right foot (μ = 0). Must be greater than center.</param>
    public TriangularMembershipFunction(string name, double left, double center, double right)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (!double.IsFinite(left) || !double.IsFinite(center) || !double.IsFinite(right))
            throw new ArgumentException("All parameters must be finite numbers.");

        if (left >= center)
            throw new ArgumentException("Left must be less than center.", nameof(left));
        if (center >= right)
            throw new ArgumentException("Center must be less than right.", nameof(right));

        Name = name;
        Left = left;
        Center = center;
        Right = right;
    }

    /// <inheritdoc />
    public double Evaluate(double x)
    {
        if (x <= Left || x >= Right)
            return 0.0;
        if (x <= Center)
            return (x - Left) / (Center - Left);
        return (Right - x) / (Right - Center);
    }

    /// <inheritdoc />
    public override string ToString() => $"Triangular(left={Left}, center={Center}, right={Right})";
}
