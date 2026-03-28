using FuzzySat.Core.FuzzyLogic.Rules;

namespace FuzzySat.Core.FuzzyLogic.Inference;

/// <summary>
/// Fuzzy inference engine that evaluates a rule set against pixel band values
/// and produces an inference result with firing strengths and winner identification.
/// </summary>
public sealed class FuzzyInferenceEngine : IInferenceEngine
{
    private readonly FuzzyRuleSet _ruleSet;

    /// <summary>
    /// Creates a new inference engine with the given rule set.
    /// </summary>
    public FuzzyInferenceEngine(FuzzyRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        _ruleSet = ruleSet;
    }

    /// <inheritdoc />
    public InferenceResult Infer(IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);
        var strengths = _ruleSet.EvaluateAll(bandValues);
        return new InferenceResult(strengths);
    }
}
