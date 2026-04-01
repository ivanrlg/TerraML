using FluentAssertions;
using FuzzySat.Core.Raster;
using FuzzySat.Core.Visualization;

namespace FuzzySat.Core.Tests.Visualization;

public class RgbCompositeRendererTests
{
    [Fact]
    public void RenderGrayscale_SmallBand_ProducesValidPng()
    {
        var data = new double[,] { { 0, 100 }, { 200, 255 } };
        var band = new Band("Test", data);

        var png = RgbCompositeRenderer.RenderGrayscale(band, 0, 255);

        png.Should().NotBeEmpty();
        // PNG magic bytes
        png[0].Should().Be(0x89);
        png[1].Should().Be((byte)'P');
        png[2].Should().Be((byte)'N');
        png[3].Should().Be((byte)'G');
    }

    [Fact]
    public void RenderGrayscale_WithStats_ProducesValidPng()
    {
        var data = new double[,] { { 10, 20 }, { 30, 40 } };
        var band = new Band("Test", data);
        var stats = new BandStatistics(10, 40, 25, 11.18, new long[256]);

        var png = RgbCompositeRenderer.RenderGrayscale(band, stats);

        png.Should().NotBeEmpty();
        png[0].Should().Be(0x89); // PNG header
    }

    [Fact]
    public void RenderGrayscale_ZeroRange_ProducesMidGray()
    {
        var data = new double[,] { { 5, 5 }, { 5, 5 } };
        var band = new Band("Uniform", data);

        var png = RgbCompositeRenderer.RenderGrayscale(band, 5, 5);

        png.Should().NotBeEmpty();
    }

    [Fact]
    public void RenderGrayscale_ZeroWidth_Throws()
    {
        var band = new Band("Test", new double[,] { { 1 } });
        var act = () => RgbCompositeRenderer.RenderGrayscale(band, 0, 1, maxWidth: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RenderGrayscale_NullBand_Throws()
    {
        var act = () => RgbCompositeRenderer.RenderGrayscale(null!, 0, 1);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RenderRgb_SmallBands_ProducesValidPng()
    {
        var red = new Band("R", new double[,] { { 255, 0 }, { 0, 255 } });
        var green = new Band("G", new double[,] { { 0, 255 }, { 0, 255 } });
        var blue = new Band("B", new double[,] { { 0, 0 }, { 255, 255 } });

        var rStats = new BandStatistics(0, 255, 127, 127, new long[256]);
        var gStats = new BandStatistics(0, 255, 127, 127, new long[256]);
        var bStats = new BandStatistics(0, 255, 127, 127, new long[256]);

        var png = RgbCompositeRenderer.RenderRgb(red, green, blue, rStats, gStats, bStats);

        png.Should().NotBeEmpty();
        png[0].Should().Be(0x89); // PNG header
    }

    [Fact]
    public void RenderRgb_MismatchedDimensions_Throws()
    {
        var big = new Band("Big", new double[4, 4]);
        var small = new Band("Small", new double[2, 2]);
        var stats = new BandStatistics(0, 1, 0.5, 0.3, new long[256]);

        var act = () => RgbCompositeRenderer.RenderRgb(big, small, big, stats, stats, stats);

        act.Should().Throw<ArgumentException>().WithMessage("*identical dimensions*");
    }

    [Fact]
    public void RenderRgb_ZeroHeight_Throws()
    {
        var band = new Band("Test", new double[,] { { 1 } });
        var stats = new BandStatistics(0, 1, 0.5, 0.3, new long[256]);

        var act = () => RgbCompositeRenderer.RenderRgb(band, band, band, stats, stats, stats,
            maxWidth: 100, maxHeight: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
