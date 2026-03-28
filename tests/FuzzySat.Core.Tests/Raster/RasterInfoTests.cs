using FluentAssertions;
using FuzzySat.Core.Raster;

namespace FuzzySat.Core.Tests.Raster;

public class RasterInfoTests
{
    [Fact]
    public void RasterInfo_StoresAllProperties()
    {
        var info = new RasterInfo(
            filePath: "image.tif",
            rows: 512,
            columns: 256,
            bandCount: 4,
            dataType: "Float64",
            driverName: "GTiff",
            projection: "EPSG:4326");

        info.FilePath.Should().Be("image.tif");
        info.Rows.Should().Be(512);
        info.Columns.Should().Be(256);
        info.BandCount.Should().Be(4);
        info.DataType.Should().Be("Float64");
        info.DriverName.Should().Be("GTiff");
        info.Projection.Should().Be("EPSG:4326");
    }

    [Fact]
    public void RasterInfo_NullProjection_IsAllowed()
    {
        var info = new RasterInfo("test.tif", 100, 100, 1, "Byte", "GTiff");

        info.Projection.Should().BeNull();
    }

    [Fact]
    public void RasterInfo_InvalidRows_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new RasterInfo("test.tif", 0, 100, 1, "Byte", "GTiff");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RasterInfo_InvalidBandCount_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new RasterInfo("test.tif", 100, 100, 0, "Byte", "GTiff");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RasterInfo_EmptyFilePath_ThrowsArgumentException()
    {
        var act = () => new RasterInfo("", 100, 100, 1, "Byte", "GTiff");

        act.Should().Throw<ArgumentException>();
    }
}
