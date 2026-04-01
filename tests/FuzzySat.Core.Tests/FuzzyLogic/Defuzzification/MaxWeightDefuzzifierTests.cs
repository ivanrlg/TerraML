using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.Defuzzification;
using FuzzySat.Core.FuzzyLogic.Inference;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;

namespace FuzzySat.Core.Tests.FuzzyLogic.Defuzzification;

public class MaxWeightDefuzzifierTests
{
    private readonly MaxWeightDefuzzifier _defuzzifier = new();

    [Fact]
    public void Defuzzify_ClearWinner_ReturnsHighestClass()
    {
        var result = MakeResult(("Urban", 0.9), ("Water", 0.2), ("Forest", 0.5));

        _defuzzifier.Defuzzify(result).Should().Be("Urban");
    }

    [Fact]
    public void Defuzzify_TiedClasses_ReturnsFirstInOrder()
    {
        var result = MakeResult(("Urban", 0.7), ("Water", 0.7), ("Forest", 0.3));

        _defuzzifier.Defuzzify(result).Should().Be("Urban");
    }

    [Fact]
    public void Defuzzify_AllZeroStrengths_ReturnsFirstClass()
    {
        var result = MakeResult(("Urban", 0.0), ("Water", 0.0), ("Forest", 0.0));

        _defuzzifier.Defuzzify(result).Should().Be("Urban");
    }

    [Fact]
    public void Defuzzify_SingleClass_ReturnsThatClass()
    {
        var result = MakeResult(("Water", 0.42));

        _defuzzifier.Defuzzify(result).Should().Be("Water");
    }

    [Fact]
    public void Defuzzify_NullResult_ThrowsArgumentNullException()
    {
        var act = () => _defuzzifier.Defuzzify(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Defuzzify_VeryCloseStrengths_ReturnsHighest()
    {
        var result = MakeResult(("Urban", 0.500000), ("Water", 0.500001));

        _defuzzifier.Defuzzify(result).Should().Be("Water");
    }

    [Fact]
    public void Defuzzify_FullPipeline_EndToEnd()
    {
        // Build complete pipeline: MFs → Rules → Engine → Infer → Defuzzify
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
            }),
            new FuzzyRule("Forest", new Dictionary<string, IMembershipFunction>
            {
                ["B1"] = new GaussianMembershipFunction("F_B1", 70.0, 12.0),
                ["B2"] = new GaussianMembershipFunction("F_B2", 85.0, 15.0)
            })
        ]);

        var engine = new FuzzyInferenceEngine(rules);

        // Pixel matching Urban profile
        var inferenceResult = engine.Infer(new Dictionary<string, double>
        {
            ["B1"] = 102.0,
            ["B2"] = 148.0
        });

        var winner = _defuzzifier.Defuzzify(inferenceResult);

        winner.Should().Be("Urban");
    }

    [Fact]
    public void Defuzzify_ThesisScenario_ForestPixel()
    {
        // 4-band scenario mimicking ASTER imagery classification
        var rules = new FuzzyRuleSet([
            new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
            {
                ["VNIR1"] = new GaussianMembershipFunction("U_V1", 130.0, 18.0),
                ["VNIR2"] = new GaussianMembershipFunction("U_V2", 110.0, 15.0),
                ["SWIR1"] = new GaussianMembershipFunction("U_S1", 160.0, 22.0),
                ["SWIR2"] = new GaussianMembershipFunction("U_S2", 140.0, 20.0)
            }),
            new FuzzyRule("Water", new Dictionary<string, IMembershipFunction>
            {
                ["VNIR1"] = new GaussianMembershipFunction("W_V1", 25.0, 8.0),
                ["VNIR2"] = new GaussianMembershipFunction("W_V2", 15.0, 6.0),
                ["SWIR1"] = new GaussianMembershipFunction("W_S1", 10.0, 5.0),
                ["SWIR2"] = new GaussianMembershipFunction("W_S2", 8.0, 4.0)
            }),
            new FuzzyRule("Forest", new Dictionary<string, IMembershipFunction>
            {
                ["VNIR1"] = new GaussianMembershipFunction("F_V1", 75.0, 12.0),
                ["VNIR2"] = new GaussianMembershipFunction("F_V2", 95.0, 14.0),
                ["SWIR1"] = new GaussianMembershipFunction("F_S1", 85.0, 16.0),
                ["SWIR2"] = new GaussianMembershipFunction("F_S2", 70.0, 13.0)
            })
        ]);

        var engine = new FuzzyInferenceEngine(rules);

        // Pixel values matching Forest spectral signature
        var result = engine.Infer(new Dictionary<string, double>
        {
            ["VNIR1"] = 77.0,
            ["VNIR2"] = 93.0,
            ["SWIR1"] = 83.0,
            ["SWIR2"] = 72.0
        });

        _defuzzifier.Defuzzify(result).Should().Be("Forest");
        result.WinnerStrength.Should().BeGreaterThan(0.7);
    }

    private static InferenceResult MakeResult(params (string Class, double Strength)[] entries)
    {
        var strengths = entries
            .Select(e => new KeyValuePair<string, double>(e.Class, e.Strength))
            .ToList();
        return new InferenceResult(strengths);
    }
}
