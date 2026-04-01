using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.Classification;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class StackingClassifierTests
{
    [Fact]
    public void Train_ClassifiesCorrectly()
    {
        var (extractor, samples) = MakeTrainingData();

        var factories = new List<Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier>>
        {
            fold => HybridClassifier.TrainRandomForest(fold, extractor, numberOfTrees: 10),
            fold => HybridClassifier.TrainSdca(fold, extractor)
        };

        var stacking = StackingClassifier.Train(samples, factories, numberOfFolds: 3);

        stacking.ClassifyPixel(new Dictionary<string, double> { ["B1"] = 100.0, ["B2"] = 150.0 })
            .Should().Be("Urban");
        stacking.ClassifyPixel(new Dictionary<string, double> { ["B1"] = 30.0, ["B2"] = 20.0 })
            .Should().Be("Water");
    }

    [Fact]
    public void Train_EmptyFactories_Throws()
    {
        var (extractor, samples) = MakeTrainingData();
        var factories = new List<Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier>>();

        var act = () => StackingClassifier.Train(samples, factories);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Train_SingleBaseClassifier_Works()
    {
        var (extractor, samples) = MakeTrainingData();

        var factories = new List<Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier>>
        {
            fold => HybridClassifier.TrainRandomForest(fold, extractor, numberOfTrees: 10)
        };

        var stacking = StackingClassifier.Train(samples, factories, numberOfFolds: 3);

        var result = stacking.ClassifyPixel(new Dictionary<string, double> { ["B1"] = 100.0, ["B2"] = 150.0 });
        result.Should().NotBeNullOrEmpty();
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
