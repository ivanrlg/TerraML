using FuzzySat.Core.FuzzyLogic.Inference;

namespace FuzzySat.Core.FuzzyLogic.Defuzzification;

/// <summary>
/// Weighted average defuzzifier: returns the class whose index is the weighted
/// average of all firing strengths. Falls back to MaxWeight when all strengths are zero.
/// Useful when classes have an ordinal relationship.
/// </summary>
public sealed class WeightedAverageDefuzzifier : IDefuzzifier
{
    /// <inheritdoc />
    public string Defuzzify(InferenceResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var totalWeight = 0.0;
        var weightedSum = 0.0;

        for (var i = 0; i < result.AllStrengths.Count; i++)
        {
            var strength = result.AllStrengths[i].Value;
            totalWeight += strength;
            weightedSum += i * strength;
        }

        if (totalWeight == 0.0)
            return result.WinnerClass; // Fallback: all zero → first class

        var index = (int)Math.Round(weightedSum / totalWeight);
        index = Math.Clamp(index, 0, result.AllStrengths.Count - 1);

        return result.AllStrengths[index].Key;
    }
}
