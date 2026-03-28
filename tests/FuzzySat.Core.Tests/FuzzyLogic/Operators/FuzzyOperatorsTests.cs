using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.Operators;

namespace FuzzySat.Core.Tests.FuzzyLogic.Operators;

public class FuzzyOperatorsTests
{
    private const double Precision = 1e-10;

    // --- AND (binary) ---

    [Theory]
    [InlineData(0.3, 0.7, 0.3)]
    [InlineData(0.0, 1.0, 0.0)]
    [InlineData(0.5, 0.5, 0.5)]
    [InlineData(1.0, 1.0, 1.0)]
    [InlineData(0.0, 0.0, 0.0)]
    public void And_TwoValues_ReturnsMinimum(double a, double b, double expected)
    {
        FuzzyOperators.And(a, b).Should().BeApproximately(expected, Precision);
    }

    // --- OR (binary) ---

    [Theory]
    [InlineData(0.3, 0.7, 0.7)]
    [InlineData(0.0, 1.0, 1.0)]
    [InlineData(0.5, 0.5, 0.5)]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(1.0, 1.0, 1.0)]
    public void Or_TwoValues_ReturnsMaximum(double a, double b, double expected)
    {
        FuzzyOperators.Or(a, b).Should().BeApproximately(expected, Precision);
    }

    // --- NOT ---

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(1.0, 0.0)]
    [InlineData(0.3, 0.7)]
    [InlineData(0.5, 0.5)]
    public void Not_Value_ReturnsComplement(double a, double expected)
    {
        FuzzyOperators.Not(a).Should().BeApproximately(expected, Precision);
    }

    // --- AND (collection) ---

    [Fact]
    public void And_Collection_ReturnsMinimumOfAll()
    {
        var values = new[] { 0.8, 0.3, 0.5, 0.9 };

        FuzzyOperators.And(values).Should().BeApproximately(0.3, Precision);
    }

    [Fact]
    public void And_SingleElement_ReturnsThatElement()
    {
        FuzzyOperators.And([0.42]).Should().BeApproximately(0.42, Precision);
    }

    // --- OR (collection) ---

    [Fact]
    public void Or_Collection_ReturnsMaximumOfAll()
    {
        var values = new[] { 0.8, 0.3, 0.5, 0.9 };

        FuzzyOperators.Or(values).Should().BeApproximately(0.9, Precision);
    }

    [Fact]
    public void Or_SingleElement_ReturnsThatElement()
    {
        FuzzyOperators.Or([0.42]).Should().BeApproximately(0.42, Precision);
    }

    // --- Empty collection ---

    [Fact]
    public void And_EmptyCollection_ThrowsArgumentException()
    {
        var act = () => FuzzyOperators.And(Array.Empty<double>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Or_EmptyCollection_ThrowsArgumentException()
    {
        var act = () => FuzzyOperators.Or(Array.Empty<double>());

        act.Should().Throw<ArgumentException>();
    }

    // --- Out of range ---

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void And_ValueOutOfRange_ThrowsArgumentOutOfRangeException(double bad)
    {
        var act = () => FuzzyOperators.And(bad, 0.5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Or_ValueOutOfRange_ThrowsArgumentOutOfRangeException(double bad)
    {
        var act = () => FuzzyOperators.Or(bad, 0.5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Not_ValueOutOfRange_ThrowsArgumentOutOfRangeException(double bad)
    {
        var act = () => FuzzyOperators.Not(bad);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void And_CollectionContainsInvalidValue_ThrowsArgumentOutOfRangeException()
    {
        var act = () => FuzzyOperators.And([0.5, 0.3, 1.5]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // --- ProductAnd ---

    [Theory]
    [InlineData(0.5, 0.5, 0.25)]
    [InlineData(1.0, 0.8, 0.8)]
    [InlineData(0.0, 0.9, 0.0)]
    [InlineData(1.0, 1.0, 1.0)]
    public void ProductAnd_TwoValues_ReturnsProduct(double a, double b, double expected)
    {
        FuzzyOperators.ProductAnd(a, b).Should().BeApproximately(expected, Precision);
    }

    [Fact]
    public void ProductAnd_Collection_ReturnsProductOfAll()
    {
        FuzzyOperators.ProductAnd([0.8, 0.5, 0.9]).Should().BeApproximately(0.36, Precision);
    }

    [Fact]
    public void ProductAnd_LowerThanMinAnd()
    {
        // Product AND is always <= Min AND for values in [0,1]
        var values = new[] { 0.8, 0.6, 0.9 };
        FuzzyOperators.ProductAnd(values).Should().BeLessThanOrEqualTo(FuzzyOperators.And(values));
    }

    [Fact]
    public void ProductAnd_EmptyCollection_Throws()
    {
        var act = () => FuzzyOperators.ProductAnd(Array.Empty<double>());
        act.Should().Throw<ArgumentException>();
    }
}
