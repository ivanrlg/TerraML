using FluentAssertions;
using FuzzySat.Core.Raster;
using FuzzySat.Core.Visualization;

namespace FuzzySat.Core.Tests.Visualization;

public class BandStatisticsCalculatorTests
{
    [Fact]
    public void ComputeMinMax_KnownData_ReturnsCorrectValues()
    {
        var data = new double[,] { { 10, 20 }, { 30, 40 } };
        var band = new Band("Test", data);

        var (min, max) = BandStatisticsCalculator.ComputeMinMax(band);

        min.Should().Be(10);
        max.Should().Be(40);
    }

    [Fact]
    public void ComputeMinMax_UniformData_MinEqualsMax()
    {
        var data = new double[,] { { 5, 5 }, { 5, 5 } };
        var band = new Band("Uniform", data);

        var (min, max) = BandStatisticsCalculator.ComputeMinMax(band);

        min.Should().Be(5);
        max.Should().Be(5);
    }

    [Fact]
    public void ComputeMinMax_NullBand_Throws()
    {
        var act = () => BandStatisticsCalculator.ComputeMinMax(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Compute_KnownData_ReturnsCorrectStats()
    {
        // 4 pixels: 10, 20, 30, 40 → mean=25, stddev=sqrt(125)≈11.18
        var data = new double[,] { { 10, 20 }, { 30, 40 } };
        var band = new Band("Test", data);

        var stats = BandStatisticsCalculator.Compute(band);

        stats.Min.Should().Be(10);
        stats.Max.Should().Be(40);
        stats.Mean.Should().Be(25);
        stats.StdDev.Should().BeApproximately(11.180, 0.001);
        stats.Histogram.Should().HaveCount(256);
    }

    [Fact]
    public void Compute_UniformData_HistogramCentered()
    {
        var data = new double[,] { { 7, 7 }, { 7, 7 } };
        var band = new Band("Uniform", data);

        var stats = BandStatisticsCalculator.Compute(band);

        stats.Min.Should().Be(7);
        stats.Max.Should().Be(7);
        stats.StdDev.Should().Be(0);
        stats.Histogram[128].Should().Be(4); // all pixels in center bin
    }

    [Fact]
    public void Compute_NullBand_Throws()
    {
        var act = () => BandStatisticsCalculator.Compute(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
