using FluentAssertions;
using FuzzySat.Core.Raster;

namespace FuzzySat.Core.Tests.Raster;

public class SpectralIndexCalculatorTests
{
    private const double Precision = 1e-10;

    [Fact]
    public void NormalizedDifference_KnownValues()
    {
        // (80 - 20) / (80 + 20) = 60/100 = 0.6
        var bandA = new Band("NIR", new double[,] { { 80.0 } });
        var bandB = new Band("Red", new double[,] { { 20.0 } });

        var result = SpectralIndexCalculator.NormalizedDifference(bandA, bandB, "NDVI");

        result[0, 0].Should().BeApproximately(0.6, Precision);
        result.Name.Should().Be("NDVI");
    }

    [Fact]
    public void NormalizedDifference_BothZero_ReturnsZero()
    {
        var bandA = new Band("A", new double[,] { { 0.0 } });
        var bandB = new Band("B", new double[,] { { 0.0 } });

        var result = SpectralIndexCalculator.NormalizedDifference(bandA, bandB, "Index");

        result[0, 0].Should().Be(0.0);
    }

    [Fact]
    public void NormalizedDifference_EqualBands_ReturnsZero()
    {
        var bandA = new Band("A", new double[,] { { 50.0 } });
        var bandB = new Band("B", new double[,] { { 50.0 } });

        var result = SpectralIndexCalculator.NormalizedDifference(bandA, bandB, "Index");

        result[0, 0].Should().BeApproximately(0.0, Precision);
    }

    [Fact]
    public void NormalizedDifference_ResultRange_NegOneToOne()
    {
        // When B >> A, result is negative
        var bandA = new Band("A", new double[,] { { 10.0 } });
        var bandB = new Band("B", new double[,] { { 90.0 } });

        var result = SpectralIndexCalculator.NormalizedDifference(bandA, bandB, "Index");

        result[0, 0].Should().BeInRange(-1.0, 1.0);
        result[0, 0].Should().BeApproximately(-0.8, Precision);
    }

    [Fact]
    public void Ndvi_HighVegetation_PositiveValue()
    {
        // Forest: high NIR, low Red → high NDVI
        var nir = new Band("NIR", new double[,] { { 200.0 } });
        var red = new Band("Red", new double[,] { { 50.0 } });

        var ndvi = SpectralIndexCalculator.Ndvi(nir, red);

        ndvi.Name.Should().Be("NDVI");
        ndvi[0, 0].Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void Ndwi_Water_PositiveValue()
    {
        // Water: high Green, low NIR → positive NDWI
        var green = new Band("Green", new double[,] { { 150.0 } });
        var nir = new Band("NIR", new double[,] { { 30.0 } });

        var ndwi = SpectralIndexCalculator.Ndwi(green, nir);

        ndwi.Name.Should().Be("NDWI");
        ndwi[0, 0].Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void Ndbi_BuiltUp_PositiveValue()
    {
        // Urban: high SWIR, low NIR → positive NDBI
        var swir = new Band("SWIR", new double[,] { { 180.0 } });
        var nir = new Band("NIR", new double[,] { { 60.0 } });

        var ndbi = SpectralIndexCalculator.Ndbi(swir, nir);

        ndbi.Name.Should().Be("NDBI");
        ndbi[0, 0].Should().BeGreaterThan(0.4);
    }

    [Fact]
    public void NormalizedDifference_MismatchedDimensions_Throws()
    {
        var bandA = new Band("A", new double[2, 2]);
        var bandB = new Band("B", new double[3, 2]);

        var act = () => SpectralIndexCalculator.NormalizedDifference(bandA, bandB, "Idx");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NormalizedDifference_MultiplePixels()
    {
        var nir = new Band("NIR", new double[,] { { 80, 200 }, { 30, 100 } });
        var red = new Band("Red", new double[,] { { 20, 50 }, { 70, 100 } });

        var ndvi = SpectralIndexCalculator.Ndvi(nir, red);

        ndvi.Rows.Should().Be(2);
        ndvi.Columns.Should().Be(2);
        ndvi[0, 0].Should().BeApproximately(0.6, Precision);
        ndvi[1, 1].Should().BeApproximately(0.0, Precision); // Equal → 0
    }
}
