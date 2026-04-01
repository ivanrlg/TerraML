using FluentAssertions;
using FuzzySat.Core.Raster;

namespace FuzzySat.Core.Tests.Raster;

public class SensorDetectorTests
{
    [Theory]
    [InlineData(13, "Sentinel-2")]
    [InlineData(12, "Sentinel-2 (no B10)")]
    [InlineData(7, "Landsat 8/9")]
    [InlineData(8, "Landsat 8/9")]
    [InlineData(6, "Landsat 5/7")]
    [InlineData(4, "NAIP")]
    public void DetectFromBandCount_KnownCounts_ReturnsExpectedSensor(int bandCount, string expected)
    {
        SensorDetector.DetectFromBandCount(bandCount).Should().Be(expected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(100)]
    public void DetectFromBandCount_UnknownCounts_ReturnsNull(int bandCount)
    {
        SensorDetector.DetectFromBandCount(bandCount).Should().BeNull();
    }

    [Fact]
    public void GetBandNames_Sentinel2_Returns13Names()
    {
        var names = SensorDetector.GetBandNames("Sentinel-2");
        names.Should().NotBeNull();
        names!.Should().HaveCount(13);
        names[0].Should().Be("B01");
        names[12].Should().Be("B12");
    }

    [Fact]
    public void GetBandNames_NAIP_Returns4Names()
    {
        var names = SensorDetector.GetBandNames("NAIP");
        names.Should().NotBeNull();
        names!.Should().HaveCount(4);
        names.Should().Contain("NIR");
    }

    [Fact]
    public void GetBandNames_UnknownSensor_ReturnsNull()
    {
        SensorDetector.GetBandNames("UnknownSensor").Should().BeNull();
    }
}
