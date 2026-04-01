using FuzzySat.Core.FuzzyLogic.Classification;

namespace FuzzySat.Core.ML;

/// <summary>
/// Ensemble classifier that combines N base classifiers via voting.
/// Supports majority voting (equal weights) and weighted voting (e.g., by OA).
/// </summary>
public sealed class EnsembleClassifier : IClassifier
{
    private readonly IReadOnlyList<(IClassifier Classifier, double Weight)> _members;

    /// <summary>
    /// Creates an ensemble from a list of classifiers with weights.
    /// For majority voting, use weight=1.0 for all members.
    /// For weighted voting, use each classifier's OA or Kappa as weight.
    /// </summary>
    public EnsembleClassifier(IReadOnlyList<(IClassifier Classifier, double Weight)> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        if (members.Count == 0)
            throw new ArgumentException("At least one member classifier is required.", nameof(members));

        _members = members;
    }

    /// <summary>
    /// Creates a majority-voting ensemble (all weights = 1.0).
    /// </summary>
    public static EnsembleClassifier MajorityVote(IReadOnlyList<IClassifier> classifiers)
    {
        ArgumentNullException.ThrowIfNull(classifiers);
        return new EnsembleClassifier(classifiers.Select(c => (c, 1.0)).ToList());
    }

    /// <inheritdoc />
    public string ClassifyPixel(IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);

        var votes = new Dictionary<string, double>();
        foreach (var (classifier, weight) in _members)
        {
            var prediction = classifier.ClassifyPixel(bandValues);
            votes[prediction] = votes.GetValueOrDefault(prediction) + weight;
        }

        return votes.MaxBy(kv => kv.Value).Key;
    }
}
