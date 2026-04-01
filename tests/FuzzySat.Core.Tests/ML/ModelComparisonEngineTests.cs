using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.Classification;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class ModelComparisonEngineTests
{
    [Fact]
    public void Compare_TwoMethods_ReturnsSortedResults()
    {
        var (extractor, samples) = MakeTrainingData();

        var factories = new List<(string Name,
            Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier> Factory)>
        {
            ("Random Forest", fold => HybridClassifier.TrainRandomForest(fold, extractor, numberOfTrees: 10)),
            ("SDCA", fold => HybridClassifier.TrainSdca(fold, extractor))
        };

        var result = ModelComparisonEngine.Compare(samples, factories, numberOfFolds: 3);

        result.Results.Should().HaveCount(2);
        result.BestModel.Should().NotBeNull();
        result.BestModel.MeanKappa.Should().BeGreaterThanOrEqualTo(result.Results[1].MeanKappa);
    }

    [Fact]
    public void Compare_ReportsProgress()
    {
        var (extractor, samples) = MakeTrainingData();
        var messages = new List<string>();

        var factories = new List<(string Name,
            Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier> Factory)>
        {
            ("RF", fold => HybridClassifier.TrainRandomForest(fold, extractor, numberOfTrees: 10))
        };

        ModelComparisonEngine.Compare(samples, factories, numberOfFolds: 3,
            progress: new Progress<string>(m => messages.Add(m)));

        messages.Should().NotBeEmpty();
    }

    [Fact]
    public void Compare_EmptyFactories_Throws()
    {
        var (_, samples) = MakeTrainingData();

        var act = () => ModelComparisonEngine.Compare(samples,
            new List<(string, Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier>)>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compare_AllMethodsHavePositiveMetrics()
    {
        var (extractor, samples) = MakeTrainingData();

        var factories = new List<(string Name,
            Func<IReadOnlyList<(string Label, IDictionary<string, double> BandValues)>, IClassifier> Factory)>
        {
            ("RF", fold => HybridClassifier.TrainRandomForest(fold, extractor, numberOfTrees: 10)),
            ("SDCA", fold => HybridClassifier.TrainSdca(fold, extractor)),
            ("LightGBM", fold => LightGbmClassifier.Train(fold, extractor))
        };

        var result = ModelComparisonEngine.Compare(samples, factories, numberOfFolds: 3);

        foreach (var r in result.Results)
        {
            r.MeanOA.Should().BeGreaterThan(0.5);
            r.MeanKappa.Should().BeGreaterThan(0.0);
            r.TrainingTimeMs.Should().BeGreaterThanOrEqualTo(0);
        }
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
