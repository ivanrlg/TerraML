using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class LogisticRegressionClassifierTests
{
    [Fact]
    public void Train_ClassifiesCorrectly()
    {
        var (extractor, samples) = MakeTrainingData();

        var classifier = LogisticRegressionClassifier.Train(samples, extractor, historySize: 20);

        classifier.ClassifyPixel(new Dictionary<string, double> { ["B1"] = 100.0, ["B2"] = 150.0 })
            .Should().Be("Urban");
        classifier.ClassifyPixel(new Dictionary<string, double> { ["B1"] = 30.0, ["B2"] = 20.0 })
            .Should().Be("Water");
    }

    [Fact]
    public void Train_EmptySamples_Throws()
    {
        var extractor = MakeExtractor();
        var samples = new List<(string, IDictionary<string, double>)>();

        var act = () => LogisticRegressionClassifier.Train(samples, extractor);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Train_InvalidIterations_Throws()
    {
        var (extractor, samples) = MakeTrainingData();

        var act = () => LogisticRegressionClassifier.Train(samples, extractor, historySize: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
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
