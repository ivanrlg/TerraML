using FluentAssertions;
using FuzzySat.Core.Raster;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class RasterServiceRgbTests
{
    private readonly RasterService _service = new();

    private static (Band Red, Band Green, Band Blue) CreateTestBands()
    {
        // 4x4 bands with distinct value ranges per channel
        var redData = new double[4, 4];
        var greenData = new double[4, 4];
        var blueData = new double[4, 4];

        for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
            {
                redData[r, c] = r * 50 + c * 10;       // 0-180
                greenData[r, c] = 200 - r * 30 - c * 5; // 55-200
                blueData[r, c] = c * 80;                 // 0-240
            }

        return (new Band("Red", redData), new Band("Green", greenData), new Band("Blue", blueData));
    }

    [Fact]
    public void RenderRgbComposite_ProducesPngBytes()
    {
        var (red, green, blue) = CreateTestBands();
        var rStats = _service.ComputeBandStatistics(red);
        var gStats = _service.ComputeBandStatistics(green);
        var bStats = _service.ComputeBandStatistics(blue);

        var png = _service.RenderRgbComposite(red, green, blue, rStats, gStats, bStats);

        png.Should().NotBeEmpty();
        // PNG magic bytes
        png[0].Should().Be(0x89);
        png[1].Should().Be(0x50); // 'P'
        png[2].Should().Be(0x4E); // 'N'
        png[3].Should().Be(0x47); // 'G'
    }

    [Fact]
    public void RenderRgbComposite_DifferentFromGrayscale()
    {
        var (red, green, blue) = CreateTestBands();
        var rStats = _service.ComputeBandStatistics(red);
        var gStats = _service.ComputeBandStatistics(green);
        var bStats = _service.ComputeBandStatistics(blue);

        var rgbPng = _service.RenderRgbComposite(red, green, blue, rStats, gStats, bStats);
        var grayPng = _service.RenderBandPreview(red, rStats);

        // RGB should be larger than grayscale (4 bytes/pixel vs 1)
        rgbPng.Length.Should().BeGreaterThan(grayPng.Length);
    }

    [Fact]
    public void RenderRgbComposite_NullBand_ThrowsArgumentNull()
    {
        var (red, green, blue) = CreateTestBands();
        var stats = _service.ComputeBandStatistics(red);

        var act = () => _service.RenderRgbComposite(null!, green, blue, stats, stats, stats);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RenderRgbComposite_SameBandAllChannels_ProducesGrayscaleLookingImage()
    {
        var (red, _, _) = CreateTestBands();
        var stats = _service.ComputeBandStatistics(red);

        // Same band in all 3 channels should produce a valid PNG (grayscale-like)
        var png = _service.RenderRgbComposite(red, red, red, stats, stats, stats);
        png.Should().NotBeEmpty();
    }
}
