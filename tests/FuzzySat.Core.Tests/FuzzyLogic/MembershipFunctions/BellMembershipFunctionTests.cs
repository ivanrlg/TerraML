using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;

namespace FuzzySat.Core.Tests.FuzzyLogic.MembershipFunctions;

public class BellMembershipFunctionTests
{
    private const double Precision = 1e-10;

    [Fact]
    public void Evaluate_AtCenter_ReturnsOne()
    {
        var mf = new BellMembershipFunction("Test", center: 50.0, width: 20.0, slope: 2.0);
        mf.Evaluate(50.0).Should().BeApproximately(1.0, Precision);
    }

    [Fact]
    public void Evaluate_AtWidth_ReturnsCrossover()
    {
        // At x = center ± width, |ratio| = 1, so μ = 1/(1+1) = 0.5
        var mf = new BellMembershipFunction("Test", center: 50.0, width: 20.0, slope: 2.0);
        mf.Evaluate(70.0).Should().BeApproximately(0.5, Precision);
        mf.Evaluate(30.0).Should().BeApproximately(0.5, Precision);
    }

    [Fact]
    public void Evaluate_FarFromCenter_ReturnsNearZero()
    {
        var mf = new BellMembershipFunction("Test", center: 50.0, width: 10.0, slope: 3.0);
        mf.Evaluate(200.0).Should().BeLessThan(0.001);
    }

    [Fact]
    public void Evaluate_IsSymmetric()
    {
        var mf = new BellMembershipFunction("Test", center: 100.0, width: 15.0, slope: 2.0);
        mf.Evaluate(90.0).Should().BeApproximately(mf.Evaluate(110.0), Precision);
    }

    [Fact]
    public void Evaluate_HigherSlope_SteeperCurve()
    {
        var gentle = new BellMembershipFunction("Gentle", center: 50.0, width: 20.0, slope: 1.0);
        var steep = new BellMembershipFunction("Steep", center: 50.0, width: 20.0, slope: 5.0);

        // At x = center + 1.5*width, steep should be lower (faster drop)
        steep.Evaluate(80.0).Should().BeLessThan(gentle.Evaluate(80.0));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(25.0)]
    [InlineData(50.0)]
    [InlineData(75.0)]
    [InlineData(100.0)]
    [InlineData(200.0)]
    public void Evaluate_AlwaysBetweenZeroAndOne(double x)
    {
        var mf = new BellMembershipFunction("Test", center: 50.0, width: 20.0, slope: 2.0);
        mf.Evaluate(x).Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void Constructor_ZeroWidth_Throws()
    {
        var act = () => new BellMembershipFunction("Test", 50.0, 0.0, 2.0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeSlope_Throws()
    {
        var act = () => new BellMembershipFunction("Test", 50.0, 20.0, -1.0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
