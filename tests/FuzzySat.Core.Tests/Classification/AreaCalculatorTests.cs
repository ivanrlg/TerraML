using FluentAssertions;
using FuzzySat.Core.Classification;

namespace FuzzySat.Core.Tests.Classification;

public class AreaCalculatorTests
{
    private static ClassificationResult CreateTestResult()
    {
        // 4x4 grid: 8 Water, 4 Urban, 4 Forest
        var classMap = new string[4, 4]
        {
            { "Water", "Water", "Urban", "Urban" },
            { "Water", "Water", "Urban", "Urban" },
            { "Water", "Water", "Forest", "Forest" },
            { "Water", "Water", "Forest", "Forest" }
        };
        var confidenceMap = new double[4, 4];
        for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
                confidenceMap[r, c] = 0.9;

        var classes = new List<LandCoverClass>
        {
            new() { Name = "Water", Code = 1 },
            new() { Name = "Urban", Code = 2 },
            new() { Name = "Forest", Code = 3 }
        };
        return new ClassificationResult(classMap, confidenceMap, classes);
    }

    [Fact]
    public void Calculate_WithGeoTransform_ComputesCorrectAreas()
    {
        var result = CreateTestResult();
        // Sentinel-2 10m resolution: pixel = 10m x 10m = 100 m²
        var geoTransform = new double[] { 0, 10.0, 0, 0, 0, -10.0 };

        var stats = AreaCalculator.Calculate(result, geoTransform);

        stats.Should().HaveCount(3);

        // Water: 8 pixels * 100 m² = 800 m² = 0.08 ha
        var water = stats.First(s => s.ClassName == "Water");
        water.PixelCount.Should().Be(8);
        water.AreaM2.Should().Be(800.0);
        water.AreaHectares.Should().Be(0.08);
        water.Percentage.Should().Be(50.0);

        // Urban: 4 pixels * 100 m² = 400 m² = 0.04 ha
        var urban = stats.First(s => s.ClassName == "Urban");
        urban.PixelCount.Should().Be(4);
        urban.AreaM2.Should().Be(400.0);
        urban.AreaHectares.Should().Be(0.04);
        urban.Percentage.Should().Be(25.0);
    }

    [Fact]
    public void Calculate_WithoutGeoTransform_ReturnsZeroArea()
    {
        var result = CreateTestResult();

        var stats = AreaCalculator.Calculate(result, geoTransform: null);

        stats.Should().HaveCount(3);

        var water = stats.First(s => s.ClassName == "Water");
        water.PixelCount.Should().Be(8);
        water.AreaM2.Should().Be(0.0);
        water.AreaHectares.Should().Be(0.0);
        water.Percentage.Should().Be(50.0);
    }

    [Fact]
    public void Calculate_SortedByPixelCountDescending()
    {
        var result = CreateTestResult();

        var stats = AreaCalculator.Calculate(result);

        stats[0].ClassName.Should().Be("Water");   // 8 pixels
        stats[1].ClassName.Should().Be("Urban");    // 4 pixels
        stats[2].ClassName.Should().Be("Forest");   // 4 pixels
    }

    [Fact]
    public void Calculate_PercentageSumsTo100()
    {
        var result = CreateTestResult();

        var stats = AreaCalculator.Calculate(result);

        stats.Sum(s => s.Percentage).Should().BeApproximately(100.0, 0.01);
    }

    [Fact]
    public void Calculate_Sentinel2_20m_Resolution()
    {
        var result = CreateTestResult();
        // 20m resolution: pixel = 20m x 20m = 400 m²
        var geoTransform = new double[] { 0, 20.0, 0, 0, 0, -20.0 };

        var stats = AreaCalculator.Calculate(result, geoTransform);

        var water = stats.First(s => s.ClassName == "Water");
        water.AreaM2.Should().Be(3200.0); // 8 * 400
        water.AreaHectares.Should().Be(0.32);
    }

    [Fact]
    public void Calculate_NullResult_Throws()
    {
        var act = () => AreaCalculator.Calculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
