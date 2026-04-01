using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;

namespace FuzzySat.Core.Tests.FuzzyLogic.Rules;

public class FuzzyRuleTests
{
    private const double Precision = 1e-10;

    private static IMembershipFunction MakeGaussian(string name, double center, double spread)
        => new GaussianMembershipFunction(name, center, spread);

    // --- FuzzyRule ---

    [Fact]
    public void Evaluate_SingleBand_ReturnsMembershipValue()
    {
        var rule = new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
        {
            ["Band1"] = MakeGaussian("Urban_B1", 120.0, 15.0)
        });

        var result = rule.Evaluate(new Dictionary<string, double> { ["Band1"] = 120.0 });

        result.Should().BeApproximately(1.0, Precision);
    }

    [Fact]
    public void Evaluate_MultipleBands_ReturnsMinimum()
    {
        var rule = new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
        {
            ["Band1"] = MakeGaussian("B1", 100.0, 10.0),
            ["Band2"] = MakeGaussian("B2", 80.0, 10.0),
            ["Band3"] = MakeGaussian("B3", 60.0, 10.0)
        });

        // Band1 at center (1.0), Band2 at center (1.0), Band3 at 1 sigma (exp(-0.5))
        var bandValues = new Dictionary<string, double>
        {
            ["Band1"] = 100.0,
            ["Band2"] = 80.0,
            ["Band3"] = 70.0
        };

        var result = rule.Evaluate(bandValues);

        result.Should().BeApproximately(Math.Exp(-0.5), Precision);
    }

    [Fact]
    public void Evaluate_OneBandFarFromCenter_ReturnsNearZero()
    {
        var rule = new FuzzyRule("Water", new Dictionary<string, IMembershipFunction>
        {
            ["Band1"] = MakeGaussian("B1", 100.0, 10.0),
            ["Band2"] = MakeGaussian("B2", 50.0, 5.0)
        });

        // Band1 at center (1.0), Band2 far away (near 0)
        var bandValues = new Dictionary<string, double>
        {
            ["Band1"] = 100.0,
            ["Band2"] = 200.0
        };

        rule.Evaluate(bandValues).Should().BeLessThan(1e-50);
    }

    [Fact]
    public void Evaluate_MissingBand_ThrowsKeyNotFoundException()
    {
        var rule = new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
        {
            ["Band1"] = MakeGaussian("B1", 100.0, 10.0)
        });

        var act = () => rule.Evaluate(new Dictionary<string, double> { ["Band2"] = 50.0 });

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Constructor_EmptyBands_ThrowsArgumentException()
    {
        var act = () => new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentException()
    {
        var act = () => new FuzzyRule(null!, new Dictionary<string, IMembershipFunction>
        {
            ["Band1"] = MakeGaussian("B1", 100.0, 10.0)
        });

        act.Should().Throw<ArgumentException>();
    }

    // --- FuzzyRuleSet ---

    [Fact]
    public void EvaluateAll_ReturnsAllClassStrengths()
    {
        var ruleSet = MakeThreeClassRuleSet();

        var bandValues = new Dictionary<string, double> { ["Band1"] = 100.0 };
        var results = ruleSet.EvaluateAll(bandValues);

        results.Should().HaveCount(3);
        results.Select(r => r.Key).Should().ContainInOrder("Urban", "Water", "Forest");
    }

    [Fact]
    public void EvaluateAll_CorrectFiringStrengths()
    {
        var ruleSet = MakeThreeClassRuleSet();

        // Pixel at 100.0 — closest to Urban center (100), then Water (80), then Forest (60)
        var bandValues = new Dictionary<string, double> { ["Band1"] = 100.0 };
        var results = ruleSet.EvaluateAll(bandValues);

        var urban = results.First(r => r.Key == "Urban").Value;
        var water = results.First(r => r.Key == "Water").Value;
        var forest = results.First(r => r.Key == "Forest").Value;

        urban.Should().BeApproximately(1.0, Precision);
        urban.Should().BeGreaterThan(water);
        water.Should().BeGreaterThan(forest);
    }

    [Fact]
    public void Constructor_DuplicateClassName_ThrowsArgumentException()
    {
        var rule1 = new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
        {
            ["Band1"] = MakeGaussian("B1", 100.0, 10.0)
        });
        var rule2 = new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
        {
            ["Band1"] = MakeGaussian("B1", 80.0, 10.0)
        });

        var act = () => new FuzzyRuleSet([rule1, rule2]);

        act.Should().Throw<ArgumentException>().WithMessage("*Duplicate*");
    }

    [Fact]
    public void Constructor_Empty_ThrowsArgumentException()
    {
        var act = () => new FuzzyRuleSet([]);

        act.Should().Throw<ArgumentException>();
    }

    private static FuzzyRuleSet MakeThreeClassRuleSet()
    {
        return new FuzzyRuleSet([
            new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
            {
                ["Band1"] = MakeGaussian("Urban_B1", 100.0, 10.0)
            }),
            new FuzzyRule("Water", new Dictionary<string, IMembershipFunction>
            {
                ["Band1"] = MakeGaussian("Water_B1", 80.0, 10.0)
            }),
            new FuzzyRule("Forest", new Dictionary<string, IMembershipFunction>
            {
                ["Band1"] = MakeGaussian("Forest_B1", 60.0, 10.0)
            })
        ]);
    }
}
