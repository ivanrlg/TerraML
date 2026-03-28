using FluentAssertions;
using FuzzySat.Core.Raster;

namespace FuzzySat.Core.Tests.Raster;

public class RasterModelTests
{
    // --- Band ---

    [Fact]
    public void Band_StoresDataCorrectly()
    {
        var data = new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } };
        var band = new Band("VNIR1", data);

        band.Name.Should().Be("VNIR1");
        band.Rows.Should().Be(2);
        band.Columns.Should().Be(2);
        band[0, 0].Should().Be(1.0);
        band[1, 1].Should().Be(4.0);
    }

    [Fact]
    public void Band_DefensiveCopy_OriginalMutationDoesNotAffect()
    {
        var data = new double[,] { { 10.0 } };
        var band = new Band("B1", data);

        data[0, 0] = 999.0;

        band[0, 0].Should().Be(10.0);
    }

    [Fact]
    public void Band_EmptyData_ThrowsArgumentException()
    {
        var act = () => new Band("B1", new double[0, 0]);

        act.Should().Throw<ArgumentException>();
    }

    // --- PixelVector ---

    [Fact]
    public void PixelVector_StoresBandValues()
    {
        var pv = new PixelVector(5, 10, new Dictionary<string, double>
        {
            ["VNIR1"] = 75.0, ["SWIR1"] = 85.0
        });

        pv.Row.Should().Be(5);
        pv.Column.Should().Be(10);
        pv.BandValues["VNIR1"].Should().Be(75.0);
        pv.BandValues["SWIR1"].Should().Be(85.0);
    }

    [Fact]
    public void PixelVector_EmptyBands_ThrowsArgumentException()
    {
        var act = () => new PixelVector(0, 0, new Dictionary<string, double>());

        act.Should().Throw<ArgumentException>();
    }

    // --- MultispectralImage ---

    [Fact]
    public void MultispectralImage_CreateFromBands()
    {
        var img = MakeTestImage();

        img.BandNames.Should().HaveCount(2);
        img.Rows.Should().Be(2);
        img.Columns.Should().Be(3);
    }

    [Fact]
    public void MultispectralImage_GetBand_ReturnsCorrectBand()
    {
        var img = MakeTestImage();

        img.GetBand("B1")[0, 0].Should().Be(10.0);
        img.GetBand("B2")[0, 0].Should().Be(50.0);
    }

    [Fact]
    public void MultispectralImage_GetPixelVector_ExtractsAllBands()
    {
        var img = MakeTestImage();

        var pv = img.GetPixelVector(1, 2);

        pv.Row.Should().Be(1);
        pv.Column.Should().Be(2);
        pv.BandValues.Should().HaveCount(2);
        pv.BandValues["B1"].Should().Be(16.0);
        pv.BandValues["B2"].Should().Be(56.0);
    }

    [Fact]
    public void MultispectralImage_MismatchedDimensions_ThrowsArgumentException()
    {
        var b1 = new Band("B1", new double[2, 3]);
        var b2 = new Band("B2", new double[3, 3]); // Different rows

        var act = () => new MultispectralImage([b1, b2]);

        act.Should().Throw<ArgumentException>().WithMessage("*dimensions*");
    }

    [Fact]
    public void MultispectralImage_DuplicateBandName_ThrowsArgumentException()
    {
        var b1 = new Band("B1", new double[2, 2]);
        var b2 = new Band("B1", new double[2, 2]);

        var act = () => new MultispectralImage([b1, b2]);

        act.Should().Throw<ArgumentException>().WithMessage("*Duplicate*");
    }

    [Fact]
    public void MultispectralImage_Empty_ThrowsArgumentException()
    {
        var act = () => new MultispectralImage([]);

        act.Should().Throw<ArgumentException>();
    }

    private static MultispectralImage MakeTestImage()
    {
        var b1 = new Band("B1", new double[,] { { 10, 11, 12 }, { 14, 15, 16 } });
        var b2 = new Band("B2", new double[,] { { 50, 51, 52 }, { 54, 55, 56 } });
        return new MultispectralImage([b1, b2]);
    }
}
