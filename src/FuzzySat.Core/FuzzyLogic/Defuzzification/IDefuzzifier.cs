using FuzzySat.Core.FuzzyLogic.Inference;

namespace FuzzySat.Core.FuzzyLogic.Defuzzification;

/// <summary>
/// Converts a fuzzy inference result into a crisp classification decision.
/// </summary>
public interface IDefuzzifier
{
    /// <summary>
    /// Defuzzifies an inference result into the winning land cover class name.
    /// </summary>
    /// <param name="result">The inference result containing all firing strengths.</param>
    /// <returns>The name of the selected land cover class.</returns>
    string Defuzzify(InferenceResult result);
}
