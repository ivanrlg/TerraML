using FuzzySat.Core.FuzzyLogic.Defuzzification;
using FuzzySat.Core.FuzzyLogic.Inference;

namespace FuzzySat.Core.FuzzyLogic.Classification;

/// <summary>
/// Orchestrates the full fuzzy classification pipeline:
/// band values → inference engine → inference result → defuzzifier → class name.
/// </summary>
public sealed class FuzzyClassifier : IClassifier
{
    private readonly IInferenceEngine _engine;
    private readonly IDefuzzifier _defuzzifier;

    /// <summary>
    /// Creates a new fuzzy classifier.
    /// </summary>
    /// <param name="engine">The inference engine containing the fuzzy rule set.</param>
    /// <param name="defuzzifier">The defuzzification strategy. Defaults to MaxWeightDefuzzifier.</param>
    public FuzzyClassifier(IInferenceEngine engine, IDefuzzifier? defuzzifier = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engine = engine;
        _defuzzifier = defuzzifier ?? new MaxWeightDefuzzifier();
    }

    /// <inheritdoc />
    public string ClassifyPixel(IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);
        var result = _engine.Infer(bandValues);
        return _defuzzifier.Defuzzify(result);
    }
}
