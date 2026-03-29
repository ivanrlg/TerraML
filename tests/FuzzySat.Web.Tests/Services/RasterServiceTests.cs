using FluentAssertions;
using FuzzySat.Core.Raster;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class RasterServiceTests
{
    private readonly RasterService _service = new();

    [Fact]
    public void GetInfo_NonExistentPath_ThrowsFileNotFound()
    {
        var act = () => _service.GetInfo("/nonexistent/path.tif");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void GetInfo_UnsupportedExtension_ThrowsArgumentException()
    {
        // Create a temp file with unsupported extension
        var tempFile = Path.GetTempFileName(); // creates .tmp file
        try
        {
            var act = () => _service.GetInfo(tempFile);
            act.Should().Throw<ArgumentException>().WithMessage("*Unsupported raster format*");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetInfo_NullPath_ThrowsArgumentException()
    {
        var act = () => _service.GetInfo(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetInfo_EmptyPath_ThrowsArgumentException()
    {
        var act = () => _service.GetInfo("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ComputeBandStatistics_KnownData_ReturnsCorrectValues()
    {
        // 3x3 band with known values: 1,2,3,4,5,6,7,8,9
        var data = new double[3, 3];
        var val = 1.0;
        for (var r = 0; r < 3; r++)
            for (var c = 0; c < 3; c++)
                data[r, c] = val++;

        var band = new Band("Test", data);
        var stats = _service.ComputeBandStatistics(band);

        stats.Min.Should().Be(1.0);
        stats.Max.Should().Be(9.0);
        stats.Mean.Should().Be(5.0);
        stats.StdDev.Should().BeApproximately(Math.Sqrt(60.0 / 9.0), 1e-10);
        stats.Histogram.Should().HaveCount(256);
        stats.Histogram.Sum().Should().Be(9); // all 9 pixels accounted for
    }

    [Fact]
    public void ComputeBandStatistics_UniformBand_HandlesZeroRange()
    {
        var data = new double[2, 2];
        for (var r = 0; r < 2; r++)
            for (var c = 0; c < 2; c++)
                data[r, c] = 42.0;

        var band = new Band("Uniform", data);
        var stats = _service.ComputeBandStatistics(band);

        stats.Min.Should().Be(42.0);
        stats.Max.Should().Be(42.0);
        stats.Mean.Should().Be(42.0);
        stats.StdDev.Should().Be(0.0);
        stats.Histogram[128].Should().Be(4); // all pixels in middle bin
    }

    [Fact]
    public void ComputeBandStatistics_NullBand_Throws()
    {
        var act = () => _service.ComputeBandStatistics(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
