using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;

namespace FuzzySat.Core.Tests.FuzzyLogic.MembershipFunctions;

public class TriangularMembershipFunctionTests
{
    private const double Precision = 1e-10;

    [Fact]
    public void Evaluate_AtCenter_ReturnsOne()
    {
        var mf = new TriangularMembershipFunction("Test", 0.0, 50.0, 100.0);
        mf.Evaluate(50.0).Should().BeApproximately(1.0, Precision);
    }

    [Fact]
    public void Evaluate_AtLeftFoot_ReturnsZero()
    {
        var mf = new TriangularMembershipFunction("Test", 0.0, 50.0, 100.0);
        mf.Evaluate(0.0).Should().Be(0.0);
    }

    [Fact]
    public void Evaluate_AtRightFoot_ReturnsZero()
    {
        var mf = new TriangularMembershipFunction("Test", 0.0, 50.0, 100.0);
        mf.Evaluate(100.0).Should().Be(0.0);
    }

    [Fact]
    public void Evaluate_Midpoint_LeftSlope_ReturnsHalf()
    {
        var mf = new TriangularMembershipFunction("Test", 0.0, 50.0, 100.0);
        mf.Evaluate(25.0).Should().BeApproximately(0.5, Precision);
    }

    [Fact]
    public void Evaluate_Midpoint_RightSlope_ReturnsHalf()
    {
        var mf = new TriangularMembershipFunction("Test", 0.0, 50.0, 100.0);
        mf.Evaluate(75.0).Should().BeApproximately(0.5, Precision);
    }

    [Fact]
    public void Evaluate_OutsideRange_ReturnsZero()
    {
        var mf = new TriangularMembershipFunction("Test", 10.0, 50.0, 90.0);
        mf.Evaluate(5.0).Should().Be(0.0);
        mf.Evaluate(95.0).Should().Be(0.0);
    }

    [Theory]
    [InlineData(0.0, 50.0, 100.0)]
    [InlineData(-10.0, 0.0, 10.0)]
    [InlineData(100.0, 200.0, 300.0)]
    public void Evaluate_AlwaysBetweenZeroAndOne(double left, double center, double right)
    {
        var mf = new TriangularMembershipFunction("Test", left, center, right);
        for (var x = left - 10; x <= right + 10; x += 1)
            mf.Evaluate(x).Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void Constructor_LeftGreaterThanCenter_Throws()
    {
        var act = () => new TriangularMembershipFunction("Test", 60.0, 50.0, 100.0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_CenterGreaterThanRight_Throws()
    {
        var act = () => new TriangularMembershipFunction("Test", 0.0, 100.0, 50.0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Width_ReturnsRightMinusLeft()
    {
        var mf = new TriangularMembershipFunction("Test", 10.0, 50.0, 90.0);
        mf.Width.Should().BeApproximately(80.0, Precision);
    }
}
