using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class HybridClassifierTests
{
    [Fact]
    public void TrainRandomForest_ClassifiesCorrectly()
    {
        var (extractor, samples) = MakeTrainingData();

        var classifier = HybridClassifier.TrainRandomForest(samples, extractor, numberOfTrees: 10);

        // Pixel near Urban center
        classifier.ClassifyPixel(new Dictionary<string, double> { ["B1"] = 100.0, ["B2"] = 150.0 })
            .Should().Be("Urban");
    }

    [Fact]
    public void TrainSdca_ClassifiesCorrectly()
    {
        var (extractor, samples) = MakeTrainingData();

        var classifier = HybridClassifier.TrainSdca(samples, extractor, maximumNumberOfIterations: 50);

        // Pixel near Water center
        classifier.ClassifyPixel(new Dictionary<string, double> { ["B1"] = 30.0, ["B2"] = 20.0 })
            .Should().Be("Water");
    }

    [Fact]
    public void TrainRandomForest_EmptySamples_Throws()
    {
        var extractor = MakeExtractor();
        var samples = new List<(string, IDictionary<string, double>)>();

        var act = () => HybridClassifier.TrainRandomForest(samples, extractor);

        act.Should().Throw<ArgumentException>();
    }

    private static (FuzzyFeatureExtractor, List<(string, IDictionary<string, double>)>) MakeTrainingData()
    {
        var extractor = MakeExtractor();
        var rng = new Random(42);

        var samples = new List<(string Label, IDictionary<string, double> BandValues)>();

        // Generate synthetic training data
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
