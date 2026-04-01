using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class ClassifierBenchmarkTests
{
    [Fact]
    public void RunBenchmark_SingleHybridMethod_ReturnsOneResult()
    {
        var (ruleSet, samples) = MakeTrainingData();

        var result = ClassifierBenchmark.RunBenchmark(
            samples, ruleSet, ["B1", "B2"], ["Random Forest"], numberOfFolds: 3);

        result.Results.Should().HaveCount(1);
        result.BestModel.Name.Should().Be("Random Forest");
        result.BestModel.MeanOA.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void RunBenchmark_MultipleMethods_ReturnsSortedByKappa()
    {
        var (ruleSet, samples) = MakeTrainingData();

        var result = ClassifierBenchmark.RunBenchmark(
            samples, ruleSet, ["B1", "B2"],
            ["Random Forest", "SDCA"], numberOfFolds: 3);

        result.Results.Should().HaveCount(2);
        result.Results[0].MeanKappa.Should().BeGreaterThanOrEqualTo(result.Results[1].MeanKappa);
    }

    [Fact]
    public void RunBenchmark_PureMLMethod_WorksWithoutRuleSet()
    {
        var (_, samples) = MakeTrainingData();

        var result = ClassifierBenchmark.RunBenchmark(
            samples, ruleSet: null, ["B1", "B2"],
            ["ML: Random Forest"], numberOfFolds: 3);

        result.Results.Should().HaveCount(1);
        result.BestModel.Name.Should().Be("ML: Random Forest");
        result.BestModel.MeanOA.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void RunBenchmark_MixedHybridAndPureML_ReturnsBothResults()
    {
        var (ruleSet, samples) = MakeTrainingData();

        var result = ClassifierBenchmark.RunBenchmark(
            samples, ruleSet, ["B1", "B2"],
            ["Random Forest", "ML: SDCA"], numberOfFolds: 3);

        result.Results.Should().HaveCount(2);
        result.Results.Select(r => r.Name).Should()
            .Contain("Random Forest").And.Contain("ML: SDCA");
    }

    [Fact]
    public void RunBenchmark_UnknownMethod_ThrowsArgumentException()
    {
        var (ruleSet, samples) = MakeTrainingData();

        var act = () => ClassifierBenchmark.RunBenchmark(
            samples, ruleSet, ["B1", "B2"], ["NonExistent"], numberOfFolds: 3);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unknown classification method*NonExistent*");
    }

    [Fact]
    public void RunBenchmark_EmptySamples_ThrowsArgumentException()
    {
        var act = () => ClassifierBenchmark.RunBenchmark(
            [], ruleSet: null, ["B1", "B2"], ["ML: Random Forest"]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one training sample*");
    }

    [Fact]
    public void RunBenchmark_EmptyMethods_ThrowsArgumentException()
    {
        var (_, samples) = MakeTrainingData();

        var act = () => ClassifierBenchmark.RunBenchmark(
            samples, ruleSet: null, ["B1", "B2"], []);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one method name*");
    }

    [Fact]
    public void RunBenchmark_HybridWithoutRuleSet_ThrowsArgumentException()
    {
        var (_, samples) = MakeTrainingData();

        var act = () => ClassifierBenchmark.RunBenchmark(
            samples, ruleSet: null, ["B1", "B2"], ["Random Forest"]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*FuzzyRuleSet is required*");
    }

    [Fact]
    public void RunBenchmark_Cancellation_ThrowsOperationCancelled()
    {
        var (ruleSet, samples) = MakeTrainingData();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => ClassifierBenchmark.RunBenchmark(
            samples, ruleSet, ["B1", "B2"], ["Random Forest"],
            cancellationToken: cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    private static (FuzzyRuleSet, List<(string, IDictionary<string, double>)>) MakeTrainingData()
    {
        var ruleSet = new FuzzyRuleSet([
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
        return (ruleSet, samples);
    }
}
