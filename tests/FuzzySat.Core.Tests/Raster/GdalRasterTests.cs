using FluentAssertions;
using FuzzySat.Core.Classification;
using FuzzySat.Core.Raster;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace FuzzySat.Core.Tests.Raster;

public class GdalRasterTests : IDisposable
{
    private readonly string _tempDir;

    public GdalRasterTests()
    {
        GdalBase.ConfigureAll();
        _tempDir = Path.Combine(Path.GetTempPath(), $"fuzzysat_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void GdalRasterReader_Read_ReturnsCorrectImage()
    {
        var filePath = CreateTestGeoTiff(2, 3, 4, new double[] { 10, 20, 30, 40, 50, 60 });
        var reader = new GdalRasterReader();

        var image = reader.Read(filePath, ["B1", "B2"]);

        image.BandNames.Should().HaveCount(2);
        image.Rows.Should().Be(3);
        image.Columns.Should().Be(4);
        image.GetBand("B1")[0, 0].Should().Be(10.0);
        image.GetBand("B1")[0, 3].Should().Be(40.0);
    }

    [Fact]
    public void GdalRasterReader_Read_DefaultBandNames()
    {
        var filePath = CreateTestGeoTiff(1, 2, 2, new double[] { 1, 2, 3, 4 });
        var reader = new GdalRasterReader();

        var image = reader.Read(filePath);

        image.BandNames.Should().Contain("Band1");
    }

    [Fact]
    public void GdalRasterReader_ReadInfo_ReturnsMetadata()
    {
        var filePath = CreateTestGeoTiff(3, 10, 20, new double[200]);
        var reader = new GdalRasterReader();

        var info = reader.ReadInfo(filePath);

        info.FilePath.Should().Be(filePath);
        info.Rows.Should().Be(10);
        info.Columns.Should().Be(20);
        info.BandCount.Should().Be(3);
        info.DriverName.Should().Be("GTiff");
        info.DataType.Should().Be("Float64");
    }

    [Fact]
    public void GdalRasterReader_FileNotFound_ThrowsFileNotFoundException()
    {
        var reader = new GdalRasterReader();

        var act = () => reader.Read(Path.Combine(_tempDir, "nonexistent.tif"));

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void GdalRasterWriter_Write_CreatesValidGeoTiff()
    {
        var outputPath = Path.Combine(_tempDir, "output.tif");
        var classMap = new string[,] { { "Urban", "Water" }, { "Forest", "Urban" } };
        var confMap = new double[,] { { 0.9, 0.8 }, { 0.7, 0.95 } };
        var classes = new[]
        {
            new LandCoverClass { Name = "Urban", Code = 1 },
            new LandCoverClass { Name = "Water", Code = 2 },
            new LandCoverClass { Name = "Forest", Code = 3 }
        };
        var result = new ClassificationResult(classMap, confMap, classes);

        var writer = new GdalRasterWriter();
        writer.Write(outputPath, result);

        File.Exists(outputPath).Should().BeTrue();

        // Verify by reading back
        using var dataset = Gdal.Open(outputPath, Access.GA_ReadOnly);
        dataset.RasterXSize.Should().Be(2);
        dataset.RasterYSize.Should().Be(2);

        using var band = dataset.GetRasterBand(1);
        var buffer = new int[4];
        band.ReadRaster(0, 0, 2, 2, buffer, 2, 2, 0, 0);

        buffer[0].Should().Be(1); // Urban
        buffer[1].Should().Be(2); // Water
        buffer[2].Should().Be(3); // Forest
        buffer[3].Should().Be(1); // Urban
    }

    private string CreateTestGeoTiff(int bandCount, int rows, int cols, double[] band1Data)
    {
        var filePath = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.tif");
        using var driver = Gdal.GetDriverByName("GTiff");
        using var dataset = driver.Create(filePath, cols, rows, bandCount, DataType.GDT_Float64, null);

        for (var b = 1; b <= bandCount; b++)
        {
            using var band = dataset.GetRasterBand(b);
            var data = b == 1 ? band1Data : new double[rows * cols];
            band.WriteRaster(0, 0, cols, rows, data, cols, rows, 0, 0);
        }

        dataset.FlushCache();
        return filePath;
    }
}
