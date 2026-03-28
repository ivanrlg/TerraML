using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.Defuzzification;
using FuzzySat.Core.FuzzyLogic.Inference;

namespace FuzzySat.Core.Tests.FuzzyLogic.Defuzzification;

public class WeightedAverageDefuzzifierTests
{
    private readonly WeightedAverageDefuzzifier _defuzzifier = new();

    [Fact]
    public void Defuzzify_ClearWinner_ReturnsThatClass()
    {
        var result = MakeResult(("A", 0.9), ("B", 0.1), ("C", 0.0));
        _defuzzifier.Defuzzify(result).Should().Be("A");
    }

    [Fact]
    public void Defuzzify_EqualWeights_ReturnsMiddleClass()
    {
        var result = MakeResult(("A", 0.5), ("B", 0.5), ("C", 0.5));
        // Weighted avg index = (0*0.5 + 1*0.5 + 2*0.5) / 1.5 = 1.0 → "B"
        _defuzzifier.Defuzzify(result).Should().Be("B");
    }

    [Fact]
    public void Defuzzify_AllZero_FallsBackToFirst()
    {
        var result = MakeResult(("A", 0.0), ("B", 0.0));
        _defuzzifier.Defuzzify(result).Should().Be("A");
    }

    [Fact]
    public void Defuzzify_WeightedTowardsLast()
    {
        var result = MakeResult(("A", 0.1), ("B", 0.1), ("C", 0.8));
        // Weighted avg = (0*0.1 + 1*0.1 + 2*0.8) / 1.0 = 1.7 → rounds to 2 → "C"
        _defuzzifier.Defuzzify(result).Should().Be("C");
    }

    [Fact]
    public void Defuzzify_NullResult_Throws()
    {
        var act = () => _defuzzifier.Defuzzify(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    private static InferenceResult MakeResult(params (string Class, double Strength)[] entries)
    {
        var strengths = entries
            .Select(e => new KeyValuePair<string, double>(e.Class, e.Strength))
            .ToList();
        return new InferenceResult(strengths);
    }
}
