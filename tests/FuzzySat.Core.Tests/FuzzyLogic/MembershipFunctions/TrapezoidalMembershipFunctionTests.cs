using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;

namespace FuzzySat.Core.Tests.FuzzyLogic.MembershipFunctions;

public class TrapezoidalMembershipFunctionTests
{
    private const double Precision = 1e-10;

    [Fact]
    public void Evaluate_InPlateau_ReturnsOne()
    {
        var mf = new TrapezoidalMembershipFunction("Test", 0.0, 20.0, 80.0, 100.0);
        mf.Evaluate(50.0).Should().BeApproximately(1.0, Precision);
        mf.Evaluate(20.0).Should().BeApproximately(1.0, Precision);
        mf.Evaluate(80.0).Should().BeApproximately(1.0, Precision);
    }

    [Fact]
    public void Evaluate_AtFeet_ReturnsZero()
    {
        var mf = new TrapezoidalMembershipFunction("Test", 0.0, 20.0, 80.0, 100.0);
        mf.Evaluate(0.0).Should().Be(0.0);
        mf.Evaluate(100.0).Should().Be(0.0);
    }

    [Fact]
    public void Evaluate_LeftSlope_Midpoint_ReturnsHalf()
    {
        var mf = new TrapezoidalMembershipFunction("Test", 0.0, 20.0, 80.0, 100.0);
        mf.Evaluate(10.0).Should().BeApproximately(0.5, Precision);
    }

    [Fact]
    public void Evaluate_RightSlope_Midpoint_ReturnsHalf()
    {
        var mf = new TrapezoidalMembershipFunction("Test", 0.0, 20.0, 80.0, 100.0);
        mf.Evaluate(90.0).Should().BeApproximately(0.5, Precision);
    }

    [Fact]
    public void Evaluate_OutsideRange_ReturnsZero()
    {
        var mf = new TrapezoidalMembershipFunction("Test", 10.0, 30.0, 70.0, 90.0);
        mf.Evaluate(5.0).Should().Be(0.0);
        mf.Evaluate(95.0).Should().Be(0.0);
    }

    [Fact]
    public void Constructor_InvalidOrder_Throws()
    {
        var act = () => new TrapezoidalMembershipFunction("Test", 50.0, 20.0, 80.0, 100.0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Center_ReturnsMidpointOfPlateau()
    {
        var mf = new TrapezoidalMembershipFunction("Test", 0.0, 20.0, 80.0, 100.0);
        mf.Center.Should().BeApproximately(50.0, Precision);
    }

    [Fact]
    public void Width_ReturnsDMinusA()
    {
        var mf = new TrapezoidalMembershipFunction("Test", 10.0, 30.0, 70.0, 90.0);
        mf.Width.Should().BeApproximately(80.0, Precision);
    }

    [Fact]
    public void EqualBC_DegeneratesToTriangle()
    {
        // b == c is allowed, creating a triangle shape
        var mf = new TrapezoidalMembershipFunction("Test", 0.0, 50.0, 50.0, 100.0);
        mf.Evaluate(50.0).Should().BeApproximately(1.0, Precision);
        mf.Evaluate(25.0).Should().BeApproximately(0.5, Precision);
    }
}
