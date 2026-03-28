namespace FuzzySat.Core.FuzzyLogic.Inference;

/// <summary>
/// Infers fuzzy classification for a pixel given its spectral band values.
/// </summary>
public interface IInferenceEngine
{
    /// <summary>
    /// Evaluates all fuzzy rules and returns the inference result for one pixel.
    /// </summary>
    /// <param name="bandValues">Pixel values per band name (e.g., reflectance values).</param>
    /// <returns>The inference result containing all firing strengths and the winner.</returns>
    InferenceResult Infer(IDictionary<string, double> bandValues);
}
