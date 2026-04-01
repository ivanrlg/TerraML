using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class EnsembleClassifierTests
{
    [Fact]
    public void MajorityVote_ClassifiesCorrectly()
    {
        var (extractor, samples) = MakeTrainingData();

        var rf = HybridClassifier.TrainRandomForest(samples, extractor, numberOfTrees: 10);
        var sdca = HybridClassifier.TrainSdca(samples, extractor);
        var lgbm = LightGbmClassifier.Train(samples, extractor);

        var ensemble = EnsembleClassifier.MajorityVote([rf, sdca, lgbm]);

        ensemble.ClassifyPixel(new Dictionary<string, double> { ["B1"] = 100.0, ["B2"] = 150.0 })
            .Should().Be("Urban");
        ensemble.ClassifyPixel(new Dictionary<string, double> { ["B1"] = 30.0, ["B2"] = 20.0 })
            .Should().Be("Water");
    }

    [Fact]
    public void WeightedVote_RespectWeights()
    {
        var (extractor, samples) = MakeTrainingData();

        var rf = HybridClassifier.TrainRandomForest(samples, extractor, numberOfTrees: 10);
        var sdca = HybridClassifier.TrainSdca(samples, extractor);

        // Heavily weight the first classifier
        var ensemble = new EnsembleClassifier([(rf, 10.0), (sdca, 1.0)]);

        var result = ensemble.ClassifyPixel(new Dictionary<string, double> { ["B1"] = 100.0, ["B2"] = 150.0 });
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_EmptyMembers_Throws()
    {
        var act = () => new EnsembleClassifier([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MajorityVote_SingleClassifier_SameAsBase()
    {
        var (extractor, samples) = MakeTrainingData();
        var rf = HybridClassifier.TrainRandomForest(samples, extractor, numberOfTrees: 10);

        var ensemble = EnsembleClassifier.MajorityVote([rf]);

        var pixel = new Dictionary<string, double> { ["B1"] = 100.0, ["B2"] = 150.0 };
        ensemble.ClassifyPixel(pixel).Should().Be(rf.ClassifyPixel(pixel));
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
