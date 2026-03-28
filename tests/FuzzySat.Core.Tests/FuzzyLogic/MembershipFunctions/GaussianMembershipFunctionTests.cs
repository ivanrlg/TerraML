using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;

namespace FuzzySat.Core.Tests.FuzzyLogic.MembershipFunctions;

public class GaussianMembershipFunctionTests
{
    private const double Precision = 1e-10;

    // --- Mathematical Correctness ---

    [Fact]
    public void Evaluate_AtCenter_ReturnsOne()
    {
        var mf = new GaussianMembershipFunction("Test", center: 100.0, spread: 15.0);

        var result = mf.Evaluate(100.0);

        result.Should().BeApproximately(1.0, Precision);
    }

    [Fact]
    public void Evaluate_AtOneSigma_ReturnsExpectedValue()
    {
        var mf = new GaussianMembershipFunction("Test", center: 100.0, spread: 15.0);
        var expected = Math.Exp(-0.5); // ≈ 0.6065306597633104

        var result = mf.Evaluate(115.0); // center + 1*spread

        result.Should().BeApproximately(expected, Precision);
    }

    [Fact]
    public void Evaluate_AtTwoSigmas_ReturnsExpectedValue()
    {
        var mf = new GaussianMembershipFunction("Test", center: 100.0, spread: 15.0);
        var expected = Math.Exp(-2.0); // ≈ 0.1353352832366127

        var result = mf.Evaluate(130.0); // center + 2*spread

        result.Should().BeApproximately(expected, Precision);
    }

    [Fact]
    public void Evaluate_AtThreeSigmas_ReturnsNearZero()
    {
        var mf = new GaussianMembershipFunction("Test", center: 100.0, spread: 15.0);
        var expected = Math.Exp(-4.5); // ≈ 0.0111089965382423

        var result = mf.Evaluate(145.0); // center + 3*spread

        result.Should().BeApproximately(expected, Precision);
    }

    // --- Symmetry ---

    [Theory]
    [InlineData(5.0)]
    [InlineData(15.0)]
    [InlineData(30.0)]
    [InlineData(0.001)]
    public void Evaluate_IsSymmetricAroundCenter(double delta)
    {
        var mf = new GaussianMembershipFunction("Test", center: 120.0, spread: 20.0);

        var left = mf.Evaluate(120.0 - delta);
        var right = mf.Evaluate(120.0 + delta);

        left.Should().BeApproximately(right, Precision);
    }

    // --- Boundary / Edge Cases ---

    [Fact]
    public void Evaluate_VeryFarFromCenter_ReturnsNearZero()
    {
        var mf = new GaussianMembershipFunction("Test", center: 100.0, spread: 10.0);

        var result = mf.Evaluate(100.0 + 100.0 * 10.0); // 100 sigmas away

        result.Should().BeLessThan(1e-100);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(50.0)]
    [InlineData(100.0)]
    [InlineData(150.0)]
    [InlineData(200.0)]
    [InlineData(255.0)]
    [InlineData(-10.0)]
    [InlineData(1000.0)]
    public void Evaluate_AlwaysReturnsBetweenZeroAndOne(double x)
    {
        var mf = new GaussianMembershipFunction("Test", center: 120.0, spread: 25.0);

        var result = mf.Evaluate(x);

        result.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void Evaluate_WithNegativeInputs_WorksCorrectly()
    {
        var mf = new GaussianMembershipFunction("Test", center: -50.0, spread: 10.0);

        var result = mf.Evaluate(-50.0);

        result.Should().BeApproximately(1.0, Precision);
    }

    // --- Constructor Validation ---

    [Fact]
    public void Constructor_WithZeroSpread_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new GaussianMembershipFunction("Test", center: 100.0, spread: 0.0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("spread");
    }

    [Fact]
    public void Constructor_WithNegativeSpread_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new GaussianMembershipFunction("Test", center: 100.0, spread: -5.0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("spread");
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_WithNonFiniteSpread_ThrowsArgumentOutOfRangeException(double spread)
    {
        var act = () => new GaussianMembershipFunction("Test", center: 100.0, spread: spread);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("spread");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyName_ThrowsArgumentException(string? name)
    {
        var act = () => new GaussianMembershipFunction(name!, center: 100.0, spread: 10.0);

        act.Should().Throw<ArgumentException>();
    }

    // --- Thesis-Representative Validation ---

    [Theory]
    [InlineData(120.5, 15.3, 120.5, 1.0)]           // At center → 1.0
    [InlineData(120.5, 15.3, 135.8, 0.6065306597633104)] // At center + spread → exp(-0.5)
    [InlineData(120.5, 15.3, 105.2, 0.6065306597633104)] // At center - spread → exp(-0.5)
    [InlineData(120.5, 15.3, 151.1, 0.1353352832366128)] // At center + 2*spread → exp(-2)
    public void Evaluate_WithThesisParameters_ReturnsExpectedMembership(
        double center, double spread, double x, double expected)
    {
        var mf = new GaussianMembershipFunction("LandCover_Band1", center, spread);

        var result = mf.Evaluate(x);

        result.Should().BeApproximately(expected, 1e-8);
    }
}
