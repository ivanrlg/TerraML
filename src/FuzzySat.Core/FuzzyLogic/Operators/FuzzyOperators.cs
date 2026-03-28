namespace FuzzySat.Core.FuzzyLogic.Operators;

/// <summary>
/// Standard fuzzy logic operators as defined in the thesis:
/// AND = minimum, OR = maximum, NOT = complement.
/// </summary>
public static class FuzzyOperators
{
    /// <summary>
    /// Fuzzy AND (minimum) of two membership degrees.
    /// </summary>
    public static double And(double a, double b)
    {
        ValidateMembershipValue(a, nameof(a));
        ValidateMembershipValue(b, nameof(b));
        return Math.Min(a, b);
    }

    /// <summary>
    /// Fuzzy OR (maximum) of two membership degrees.
    /// </summary>
    public static double Or(double a, double b)
    {
        ValidateMembershipValue(a, nameof(a));
        ValidateMembershipValue(b, nameof(b));
        return Math.Max(a, b);
    }

    /// <summary>
    /// Fuzzy NOT (complement) of a membership degree.
    /// </summary>
    public static double Not(double a)
    {
        ValidateMembershipValue(a, nameof(a));
        return 1.0 - a;
    }

    /// <summary>
    /// Fuzzy AND (minimum) across a collection of membership degrees.
    /// Used for multi-band rule evaluation: firing strength = min of all band memberships.
    /// </summary>
    public static double And(IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Aggregate(values, double.MaxValue, Math.Min);
    }

    /// <summary>
    /// Fuzzy OR (maximum) across a collection of membership degrees.
    /// </summary>
    public static double Or(IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Aggregate(values, double.MinValue, Math.Max);
    }

    private static double Aggregate(IEnumerable<double> values, double seed, Func<double, double, double> func)
    {
        var hasValues = false;
        var result = seed;

        foreach (var value in values)
        {
            ValidateMembershipValue(value, "value");
            result = func(result, value);
            hasValues = true;
        }

        if (!hasValues)
            throw new ArgumentException("Collection must contain at least one value.", nameof(values));

        return result;
    }

    private static void ValidateMembershipValue(double value, string paramName)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
            throw new ArgumentOutOfRangeException(paramName, value,
                "Membership degree must be a finite number in the range [0, 1].");
    }
}
