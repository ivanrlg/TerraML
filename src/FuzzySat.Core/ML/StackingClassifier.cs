using FuzzySat.Core.FuzzyLogic.Classification;
using FuzzySat.Core.Validation;

namespace FuzzySat.Core.ML;

/// <summary>
/// Stacking ensemble: trains N base classifiers, then a meta-learner
/// (Logistic Regression) on their out-of-fold predictions.
/// Prevents data leakage by using k-fold for level-0 predictions.
/// </summary>
public sealed class StackingClassifier : IClassifier
{
    private readonly IReadOnlyList<IClassifier> _baseClassifiers;
    private readonly LogisticRegressionClassifier _metaLearner;
    private readonly FuzzyFeatureExtractor _featureExtractor;
    private readonly string[] _classLabels;

    private StackingClassifier(
        IReadOnlyList<IClassifier> baseClassifiers,
        LogisticRegressionClassifier metaLearner,
        FuzzyFeatureExtractor featureExtractor,
        string[] classLabels)
    {
        _baseClassifiers = baseClassifiers;
        _metaLearner = metaLearner;
        _featureExtractor = featureExtractor;
        _classLabels = classLabels;
    }

    /// <inheritdoc />
    public string ClassifyPixel(IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);

        // Get predictions from all base classifiers
        var metaFeatures = BuildMetaFeatures(bandValues);
        return _metaLearner.ClassifyPixel(metaFeatures);
    }

    /// <summary>
    /// Trains a stacking ensemble using out-of-fold predictions for meta-learner training.
    /// </summary>
    /// <param name="samples">All labeled training samples.</param>
    /// <param name="featureExtractor">Fuzzy feature extractor.</param>
    /// <param name="baseClassifierFactories">
    /// Factories that train base classifiers from a subset of samples.
    /// </param>
    /// <param name="numberOfFolds">Number of folds for out-of-fold predictions.</param>
    /// <param name="seed">Random seed.</param>
    public static StackingClassifier Train(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> samples,
        FuzzyFeatureExtractor featureExtractor,
        IReadOnlyList<Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier>> baseClassifierFactories,
        int numberOfFolds = 3,
        int seed = 42)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(featureExtractor);
        ArgumentNullException.ThrowIfNull(baseClassifierFactories);

        if (baseClassifierFactories.Count == 0)
            throw new ArgumentException("At least one base classifier factory is required.", nameof(baseClassifierFactories));
        if (samples.Count < numberOfFolds)
            throw new ArgumentException($"Need at least {numberOfFolds} samples for {numberOfFolds}-fold stacking.", nameof(samples));

        var classLabels = samples.Select(s => s.Label).Distinct().OrderBy(s => s).ToArray();
        var numBaseClassifiers = baseClassifierFactories.Count;
        var numClasses = classLabels.Length;

        // Create stratified folds
        var folds = CreateStratifiedFolds(samples, numberOfFolds, seed);

        // Generate out-of-fold predictions for meta-learner training
        var oofPredictions = new string[samples.Count][];
        for (var i = 0; i < samples.Count; i++)
            oofPredictions[i] = new string[numBaseClassifiers];

        // Map sample index to fold
        var sampleToFold = new int[samples.Count];
        var foldStartIdx = 0;
        var foldSampleIndices = new List<List<int>>();
        for (var f = 0; f < numberOfFolds; f++)
        {
            var indices = new List<int>();
            for (var j = 0; j < folds[f].Count; j++)
                indices.Add(foldStartIdx + j);
            foldSampleIndices.Add(indices);
            foldStartIdx += folds[f].Count;
        }

        // Reindex: we need original sample indices per fold
        // Since CreateStratifiedFolds shuffles, we need to track which original samples are in each fold
        var allFoldSamples = new List<(string Label, IDictionary<string, double> BandValues, int OrigIdx)>();
        var origIdx = 0;
        var foldMembership = new int[samples.Count];
        // Rebuild: assign fold membership based on stratified split
        var rng = new Random(seed);
        var byClass = samples
            .Select((s, i) => (s, i))
            .GroupBy(x => x.s.Label)
            .ToDictionary(g => g.Key, g => g.Select(x => x.i).OrderBy(_ => rng.Next()).ToList());

        var foldAssignment = new int[samples.Count];
        foreach (var classSamples in byClass.Values)
        {
            for (var i = 0; i < classSamples.Count; i++)
                foldAssignment[classSamples[i]] = i % numberOfFolds;
        }

        // For each fold as holdout, train base classifiers on remaining folds
        for (var holdoutFold = 0; holdoutFold < numberOfFolds; holdoutFold++)
        {
            var trainSamples = new List<(string Label, IDictionary<string, double> BandValues)>();
            var holdoutIndices = new List<int>();

            for (var i = 0; i < samples.Count; i++)
            {
                if (foldAssignment[i] == holdoutFold)
                    holdoutIndices.Add(i);
                else
                    trainSamples.Add(samples[i]);
            }

            // Train each base classifier and predict on holdout
            for (var b = 0; b < numBaseClassifiers; b++)
            {
                var classifier = baseClassifierFactories[b](trainSamples);
                foreach (var idx in holdoutIndices)
                    oofPredictions[idx][b] = classifier.ClassifyPixel(samples[idx].BandValues);
            }
        }

        // Build meta-features from OOF predictions (one-hot encoded)
        var metaFeatures = BuildMetaTrainingData(oofPredictions, samples, classLabels, numBaseClassifiers);

        // Build meta-feature extractor (identity — features are already computed)
        var metaRuleSet = BuildMetaRuleSet(classLabels, numBaseClassifiers, numClasses);
        var metaBandNames = Enumerable.Range(0, numBaseClassifiers * numClasses)
            .Select(i => $"Meta_{i}").ToList();
        var metaExtractor = new FuzzyFeatureExtractor(metaRuleSet, metaBandNames);

        // Train meta-learner on meta-features
        var metaLearner = LogisticRegressionClassifier.Train(metaFeatures, metaExtractor);

        // Train final base classifiers on ALL data
        var finalClassifiers = new List<IClassifier>();
        foreach (var factory in baseClassifierFactories)
            finalClassifiers.Add(factory(samples));

        return new StackingClassifier(finalClassifiers, metaLearner, featureExtractor, classLabels);
    }

    private IDictionary<string, double> BuildMetaFeatures(IDictionary<string, double> bandValues)
    {
        var metaFeatures = new Dictionary<string, double>();
        var idx = 0;
        foreach (var classifier in _baseClassifiers)
        {
            var prediction = classifier.ClassifyPixel(bandValues);
            for (var c = 0; c < _classLabels.Length; c++)
            {
                metaFeatures[$"Meta_{idx}"] = prediction == _classLabels[c] ? 1.0 : 0.0;
                idx++;
            }
        }
        return metaFeatures;
    }

    private static List<(string Label, IDictionary<string, double> BandValues)> BuildMetaTrainingData(
        string[][] oofPredictions,
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> samples,
        string[] classLabels,
        int numBaseClassifiers)
    {
        var result = new List<(string Label, IDictionary<string, double> BandValues)>();
        for (var i = 0; i < samples.Count; i++)
        {
            var features = new Dictionary<string, double>();
            var idx = 0;
            for (var b = 0; b < numBaseClassifiers; b++)
            {
                var pred = oofPredictions[i][b];
                for (var c = 0; c < classLabels.Length; c++)
                {
                    features[$"Meta_{idx}"] = pred == classLabels[c] ? 1.0 : 0.0;
                    idx++;
                }
            }
            result.Add((samples[i].Label, features));
        }
        return result;
    }

    private static FuzzySat.Core.FuzzyLogic.Rules.FuzzyRuleSet BuildMetaRuleSet(
        string[] classLabels, int numBaseClassifiers, int numClasses)
    {
        // Create a trivial rule set where membership functions are identity-like
        // (centered at 0.5 with wide spread to pass through features unchanged)
        var metaBandNames = Enumerable.Range(0, numBaseClassifiers * numClasses)
            .Select(i => $"Meta_{i}").ToList();

        var rules = classLabels.Select(label =>
            new FuzzySat.Core.FuzzyLogic.Rules.FuzzyRule(label,
                metaBandNames.ToDictionary(
                    b => b,
                    b => (FuzzySat.Core.FuzzyLogic.MembershipFunctions.IMembershipFunction)
                        new FuzzySat.Core.FuzzyLogic.MembershipFunctions.GaussianMembershipFunction(
                            $"MF_{label}_{b}", 0.5, 10.0)))).ToList();

        return new FuzzySat.Core.FuzzyLogic.Rules.FuzzyRuleSet(rules);
    }

    private static List<List<(string Label, IDictionary<string, double> BandValues)>> CreateStratifiedFolds(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> samples,
        int numberOfFolds, int seed)
    {
        var rng = new Random(seed);
        var byClass = samples
            .GroupBy(s => s.Label)
            .ToDictionary(g => g.Key, g => g.OrderBy(_ => rng.Next()).ToList());

        var folds = new List<List<(string Label, IDictionary<string, double> BandValues)>>();
        for (var i = 0; i < numberOfFolds; i++)
            folds.Add([]);

        foreach (var classSamples in byClass.Values)
        {
            for (var i = 0; i < classSamples.Count; i++)
                folds[i % numberOfFolds].Add(classSamples[i]);
        }

        return folds;
    }
}
