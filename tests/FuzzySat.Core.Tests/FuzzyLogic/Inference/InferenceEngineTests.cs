using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.Inference;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;

namespace FuzzySat.Core.Tests.FuzzyLogic.Inference;

public class InferenceEngineTests
{
    private const double Precision = 1e-10;

    // --- InferenceResult ---

    [Fact]
    public void InferenceResult_WinnerClass_ReturnsHighestStrength()
    {
        var strengths = new List<KeyValuePair<string, double>>
        {
            new("Urban", 0.3),
            new("Water", 0.9),
            new("Forest", 0.5)
        };

        var result = new InferenceResult(strengths);

        result.WinnerClass.Should().Be("Water");
        result.WinnerStrength.Should().BeApproximately(0.9, Precision);
    }

    [Fact]
    public void InferenceResult_TiedStrengths_FirstClassWins()
    {
        var strengths = new List<KeyValuePair<string, double>>
        {
            new("Urban", 0.7),
            new("Water", 0.7),
            new("Forest", 0.3)
        };

        var result = new InferenceResult(strengths);

        result.WinnerClass.Should().Be("Urban");
    }

    [Fact]
    public void InferenceResult_AllZero_FirstClassWins()
    {
        var strengths = new List<KeyValuePair<string, double>>
        {
            new("Urban", 0.0),
            new("Water", 0.0),
            new("Forest", 0.0)
        };

        var result = new InferenceResult(strengths);

        result.WinnerClass.Should().Be("Urban");
        result.WinnerStrength.Should().Be(0.0);
    }

    [Fact]
    public void InferenceResult_SingleClass_ThatClassWins()
    {
        var strengths = new List<KeyValuePair<string, double>> { new("Water", 0.42) };

        var result = new InferenceResult(strengths);

        result.WinnerClass.Should().Be("Water");
        result.WinnerStrength.Should().BeApproximately(0.42, Precision);
    }

    [Fact]
    public void InferenceResult_AllStrengths_ContainsAllClasses()
    {
        var strengths = new List<KeyValuePair<string, double>>
        {
            new("Urban", 0.3), new("Water", 0.9), new("Forest", 0.5)
        };

        var result = new InferenceResult(strengths);

        result.AllStrengths.Should().HaveCount(3);
        result.AllStrengths.Select(s => s.Key).Should().ContainInOrder("Urban", "Water", "Forest");
    }

    [Fact]
    public void InferenceResult_Empty_ThrowsArgumentException()
    {
        var act = () => new InferenceResult(new List<KeyValuePair<string, double>>());

        act.Should().Throw<ArgumentException>();
    }

    // --- FuzzyInferenceEngine ---

    [Fact]
    public void Infer_ThreeClasses_IdentifiesWinner()
    {
        var engine = MakeThreeClassEngine();

        // Pixel at Urban center for all bands
        var bandValues = new Dictionary<string, double>
        {
            ["Band1"] = 100.0, ["Band2"] = 150.0
        };

        var result = engine.Infer(bandValues);

        result.WinnerClass.Should().Be("Urban");
        result.AllStrengths.Should().HaveCount(3);
    }

    [Fact]
    public void Infer_NullBandValues_ThrowsArgumentNullException()
    {
        var engine = MakeThreeClassEngine();

        var act = () => engine.Infer(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullRuleSet_ThrowsArgumentNullException()
    {
        var act = () => new FuzzyInferenceEngine(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Infer_ThesisScenario_CorrectWinner()
    {
        // Simulate: 2 bands, 3 classes, pixel closer to "Forest" profile
        var rules = new FuzzyRuleSet([
            new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
            {
                ["VNIR1"] = new GaussianMembershipFunction("Urban_V1", 120.0, 15.0),
                ["SWIR1"] = new GaussianMembershipFunction("Urban_S1", 180.0, 20.0)
            }),
            new FuzzyRule("Water", new Dictionary<string, IMembershipFunction>
            {
                ["VNIR1"] = new GaussianMembershipFunction("Water_V1", 30.0, 10.0),
                ["SWIR1"] = new GaussianMembershipFunction("Water_S1", 20.0, 8.0)
            }),
            new FuzzyRule("Forest", new Dictionary<string, IMembershipFunction>
            {
                ["VNIR1"] = new GaussianMembershipFunction("Forest_V1", 80.0, 12.0),
                ["SWIR1"] = new GaussianMembershipFunction("Forest_S1", 90.0, 15.0)
            })
        ]);

        var engine = new FuzzyInferenceEngine(rules);

        // Pixel values matching Forest profile
        var result = engine.Infer(new Dictionary<string, double>
        {
            ["VNIR1"] = 82.0, ["SWIR1"] = 88.0
        });

        result.WinnerClass.Should().Be("Forest");
        result.WinnerStrength.Should().BeGreaterThan(0.5);
    }

    private static FuzzyInferenceEngine MakeThreeClassEngine()
    {
        var rules = new FuzzyRuleSet([
            new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
            {
                ["Band1"] = new GaussianMembershipFunction("U_B1", 100.0, 10.0),
                ["Band2"] = new GaussianMembershipFunction("U_B2", 150.0, 10.0)
            }),
            new FuzzyRule("Water", new Dictionary<string, IMembershipFunction>
            {
                ["Band1"] = new GaussianMembershipFunction("W_B1", 50.0, 10.0),
                ["Band2"] = new GaussianMembershipFunction("W_B2", 30.0, 10.0)
            }),
            new FuzzyRule("Forest", new Dictionary<string, IMembershipFunction>
            {
                ["Band1"] = new GaussianMembershipFunction("F_B1", 70.0, 10.0),
                ["Band2"] = new GaussianMembershipFunction("F_B2", 90.0, 10.0)
            })
        ]);

        return new FuzzyInferenceEngine(rules);
    }
}
