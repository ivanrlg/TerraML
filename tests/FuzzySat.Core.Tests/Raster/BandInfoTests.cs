using FluentAssertions;
using FuzzySat.Core.Raster;

namespace FuzzySat.Core.Tests.Raster;

public class BandInfoTests
{
    [Fact]
    public void BandInfo_RecordProperties_SetCorrectly()
    {
        var info = new BandInfo(1, "UInt16", "Coastal Aerosol", "Gray");

        info.Index.Should().Be(1);
        info.DataType.Should().Be("UInt16");
        info.Description.Should().Be("Coastal Aerosol");
        info.ColorInterpretation.Should().Be("Gray");
    }

    [Fact]
    public void BandInfo_NullableProperties_AcceptNull()
    {
        var info = new BandInfo(3, "Float64", null, null);

        info.Description.Should().BeNull();
        info.ColorInterpretation.Should().BeNull();
    }

    [Fact]
    public void BandInfo_EqualityByValue()
    {
        var a = new BandInfo(1, "UInt16", "Blue", null);
        var b = new BandInfo(1, "UInt16", "Blue", null);

        a.Should().Be(b);
    }

    [Fact]
    public void RasterInfo_Bands_DefaultsToEmpty()
    {
        var info = new RasterInfo("test.tif", 100, 100, 3, "UInt16", "GTiff");
        info.Bands.Should().BeEmpty();
    }

    [Fact]
    public void RasterInfo_Bands_PreservesProvidedList()
    {
        var bands = new List<BandInfo>
        {
            new(1, "UInt16", "Red", "Red"),
            new(2, "UInt16", "Green", "Green"),
            new(3, "UInt16", "Blue", "Blue")
        };

        var info = new RasterInfo("test.tif", 100, 100, 3, "UInt16", "GTiff", bands: bands);

        info.Bands.Should().HaveCount(3);
        info.Bands[0].Description.Should().Be("Red");
    }
}
