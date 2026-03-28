using FuzzySat.Core.FuzzyLogic.Inference;

namespace FuzzySat.Core.FuzzyLogic.Defuzzification;

/// <summary>
/// Max Weight (winner-takes-all) defuzzifier as defined in the thesis.
/// Returns the class with the highest firing strength.
/// In case of a tie, the first class in rule order wins (deterministic).
/// </summary>
public sealed class MaxWeightDefuzzifier : IDefuzzifier
{
    /// <inheritdoc />
    public string Defuzzify(InferenceResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.WinnerClass;
    }
}
