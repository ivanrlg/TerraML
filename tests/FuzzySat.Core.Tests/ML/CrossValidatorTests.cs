using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class CrossValidatorTests
{
    [Fact]
    public void Evaluate_ThreeFold_ReturnsMeanAndStd()
    {
        var (extractor, samples) = MakeTrainingData();

        var result = CrossValidator.Evaluate(
            samples,
            fold => HybridClassifier.TrainRandomForest(fold, extractor, numberOfTrees: 10),
            numberOfFolds: 3);

        result.NumberOfFolds.Should().Be(3);
        result.FoldMetrics.Should().HaveCount(3);
        result.MeanOverallAccuracy.Should().BeGreaterThan(0.5);
        result.MeanKappa.Should().BeGreaterThan(0.0);
        result.StdOverallAccuracy.Should().BeGreaterThanOrEqualTo(0.0);
        result.StdKappa.Should().BeGreaterThanOrEqualTo(0.0);
    }

    [Fact]
    public void Evaluate_FiveFold_AllFoldsProduceMetrics()
    {
        var (extractor, samples) = MakeTrainingData();

        var result = CrossValidator.Evaluate(
            samples,
            fold => HybridClassifier.TrainSdca(fold, extractor, maximumNumberOfIterations: 50),
            numberOfFolds: 5);

        result.NumberOfFolds.Should().Be(5);
        foreach (var fold in result.FoldMetrics)
        {
            fold.OverallAccuracy.Should().BeInRange(0.0, 1.0);
            fold.TotalSamples.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void Evaluate_HighQualityData_HighAccuracy()
    {
        var (extractor, samples) = MakeTrainingData();

        var result = CrossValidator.Evaluate(
            samples,
            fold => HybridClassifier.TrainRandomForest(fold, extractor, numberOfTrees: 20),
            numberOfFolds: 3);

        // Well-separated classes should give high accuracy
        result.MeanOverallAccuracy.Should().BeGreaterThan(0.8);
        result.MeanKappa.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void Evaluate_TooFewFolds_Throws()
    {
        var (extractor, samples) = MakeTrainingData();

        var act = () => CrossValidator.Evaluate(
            samples,
            fold => HybridClassifier.TrainRandomForest(fold, extractor),
            numberOfFolds: 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Evaluate_TooFewSamples_Throws()
    {
        var (extractor, _) = MakeTrainingData();
        var tinySamples = new List<(string Label, IDictionary<string, double> BandValues)>
        {
            ("Urban", new Dictionary<string, double> { ["B1"] = 100.0, ["B2"] = 150.0 }),
            ("Water", new Dictionary<string, double> { ["B1"] = 30.0, ["B2"] = 20.0 })
        };

        var act = () => CrossValidator.Evaluate(
            tinySamples,
            fold => HybridClassifier.TrainRandomForest(fold, extractor),
            numberOfFolds: 5);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Evaluate_SingleClass_Throws()
    {
        var (extractor, _) = MakeTrainingData();
        var singleClassSamples = Enumerable.Range(0, 10)
            .Select(i => ((string)"Urban", (IDictionary<string, double>)new Dictionary<string, double>
            {
                ["B1"] = 100.0 + i,
                ["B2"] = 150.0 + i
            }))
            .ToList();

        var act = () => CrossValidator.Evaluate(
            singleClassSamples,
            fold => HybridClassifier.TrainRandomForest(fold, extractor),
            numberOfFolds: 3);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CrossValidationResult_SingleFold_StdIsZero()
    {
        var matrix = new Core.Validation.ConfusionMatrix(
            ["A", "B", "A"], ["A", "B", "A"]);
        var metrics = new Core.Validation.AccuracyMetrics(matrix);

        var result = new CrossValidationResult([metrics]);

        result.StdOverallAccuracy.Should().Be(0.0);
        result.StdKappa.Should().Be(0.0);
    }

    private static (FuzzyFeatureExtractor, List<(string, IDictionary<string, double>)>) MakeTrainingData()
    {
        var extractor = MakeExtractor();
        var rng = new Random(42);

        var samples = new List<(string Label, IDictionary<string, double> BandValues)>();

        for (var i = 0; i < 50; i++)
        {
            samples.Add(("Urban", new Dictionary<string, double>
            {
                ["B1"] = 100.0 + rng.NextDouble() * 20 - 10,
                ["B2"] = 150.0 + rng.NextDouble() * 20 - 10
            }));
            samples.Add(("Water", new Dictionary<string, double>
            {
                ["B1"] = 30.0 + rng.NextDouble() * 16 - 8,
                ["B2"] = 20.0 + rng.NextDouble() * 16 - 8
            }));
        }

        return (extractor, samples);
    }

    private static FuzzyFeatureExtractor MakeExtractor()
    {
        var rules = new FuzzyRuleSet([
            new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
            {
                ["B1"] = new GaussianMembershipFunction("U_B1", 100.0, 10.0),
                ["B2"] = new GaussianMembershipFunction("U_B2", 150.0, 10.0)
            }),
            new FuzzyRule("Water", new Dictionary<string, IMembershipFunction>
            {
                ["B1"] = new GaussianMembershipFunction("W_B1", 30.0, 8.0),
                ["B2"] = new GaussianMembershipFunction("W_B2", 20.0, 8.0)
            })
        ]);

        return new FuzzyFeatureExtractor(rules, ["B1", "B2"]);
    }
}
