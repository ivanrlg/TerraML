using FuzzySat.Core.ML;
using FuzzySat.Core.Training;

namespace FuzzySat.Web.Services;

/// <summary>
/// Thin adapter that bridges Core's <see cref="ClassifierBenchmark"/> to the Web layer.
/// Handles MF-type-specific rule set building via <see cref="ClassificationService"/>,
/// then delegates comparison to Core.
/// </summary>
public sealed class ModelComparisonService
{
    /// <summary>
    /// Runs model comparison with the specified methods and fold count.
    /// Must be called from a background thread.
    /// </summary>
    public ModelComparisonResult Compare(
        TrainingSession session,
        IReadOnlyList<LabeledPixelSample> trainingSamples,
        string membershipFunctionType,
        IReadOnlyList<string> selectedMethods,
        int numberOfFolds,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(trainingSamples);
        ArgumentNullException.ThrowIfNull(selectedMethods);

        if (trainingSamples.Count == 0)
            throw new ArgumentException("At least one training sample is required.", nameof(trainingSamples));

        // Build rule set in Web layer (handles MF type selection)
        var hasHybrid = selectedMethods.Any(m => !m.StartsWith("ML: ", StringComparison.Ordinal));
        FuzzySat.Core.FuzzyLogic.Rules.FuzzyRuleSet? ruleSet = null;
        if (hasHybrid)
        {
            progress?.Report("Building rule set...");
            ruleSet = ClassificationService.BuildRuleSet(session, membershipFunctionType);
        }

        var samples = trainingSamples
            .Select(s => (s.ClassName, (IDictionary<string, double>)new Dictionary<string, double>(s.BandValues)))
            .ToList();

        return ClassifierBenchmark.RunBenchmark(
            samples, ruleSet, session.BandNames, selectedMethods,
            numberOfFolds, progress, cancellationToken);
    }
}
