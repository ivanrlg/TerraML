using FluentAssertions;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class Sentinel2ImportServiceTests : IDisposable
{
    private readonly Sentinel2ImportService _service = new();
    private readonly string _tempDir;

    public Sentinel2ImportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FuzzySat_S2Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    #region DetectFormat

    [Fact]
    public void DetectFormat_DirectoryWithTiffs_ReturnsBandFolder()
    {
        File.WriteAllText(Path.Combine(_tempDir, "B02.tif"), "dummy");

        var result = _service.DetectFormat(_tempDir);

        result.Should().Be(Sentinel2ImportService.InputFormat.BandFolder);
    }

    [Fact]
    public void DetectFormat_DirectoryWithJp2_ReturnsBandFolder()
    {
        File.WriteAllText(Path.Combine(_tempDir, "B04_10m.jp2"), "dummy");

        var result = _service.DetectFormat(_tempDir);

        result.Should().Be(Sentinel2ImportService.InputFormat.BandFolder);
    }

    [Fact]
    public void DetectFormat_SafeDirectoryByName_ReturnsSafePackage()
    {
        var safeDir = Path.Combine(_tempDir, "S2C_something.SAFE");
        Directory.CreateDirectory(safeDir);

        var result = _service.DetectFormat(safeDir);

        result.Should().Be(Sentinel2ImportService.InputFormat.SafePackage);
    }

    [Fact]
    public void DetectFormat_SafeDirectoryByMetadata_ReturnsSafePackage()
    {
        File.WriteAllText(Path.Combine(_tempDir, "MTD_MSIL2A.xml"), "<xml/>");

        var result = _service.DetectFormat(_tempDir);

        result.Should().Be(Sentinel2ImportService.InputFormat.SafePackage);
    }

    [Fact]
    public void DetectFormat_ZipFile_ReturnsZipArchive()
    {
        var zipPath = Path.Combine(_tempDir, "images.zip");
        File.WriteAllText(zipPath, "dummy");

        var result = _service.DetectFormat(zipPath);

        result.Should().Be(Sentinel2ImportService.InputFormat.ZipArchive);
    }

    [Fact]
    public void DetectFormat_EmptyDirectory_ReturnsUnknown()
    {
        var result = _service.DetectFormat(_tempDir);

        result.Should().Be(Sentinel2ImportService.InputFormat.Unknown);
    }

    [Fact]
    public void DetectFormat_NonexistentPath_ReturnsUnknown()
    {
        var result = _service.DetectFormat(Path.Combine(_tempDir, "nope"));

        result.Should().Be(Sentinel2ImportService.InputFormat.Unknown);
    }

    [Fact]
    public void DetectFormat_NullPath_ThrowsArgumentException()
    {
        var act = () => _service.DetectFormat(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectFormat_UncPath_ThrowsArgumentException()
    {
        var act = () => _service.DetectFormat(@"\\server\share\data");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*UNC*");
    }

    #endregion

    #region DiscoverBands

    [Fact]
    public void DiscoverBands_EmptyFolder_ReturnsEmpty()
    {
        var result = _service.DiscoverBands(_tempDir);

        result.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverBands_NonBandFiles_AreSkipped()
    {
        // TCI (True Color Image) should be skipped — not a spectral band
        File.WriteAllText(Path.Combine(_tempDir, "TCI_10m.tif"), "dummy");
        File.WriteAllText(Path.Combine(_tempDir, "readme.txt"), "dummy");

        var result = _service.DiscoverBands(_tempDir);

        result.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverBands_NonexistentDirectory_ThrowsDirectoryNotFound()
    {
        var act = () => _service.DiscoverBands(Path.Combine(_tempDir, "nope"));

        act.Should().Throw<DirectoryNotFoundException>();
    }

    #endregion

    #region GetAvailableResolutions

    [Fact]
    public void GetAvailableResolutions_ReturnsDistinctSorted()
    {
        var bands = new List<Sentinel2ImportService.Sentinel2BandInfo>
        {
            CreateDummyBandInfo("B02", 10),
            CreateDummyBandInfo("B04", 10),
            CreateDummyBandInfo("B05", 20),
            CreateDummyBandInfo("B01", 60),
        };

        var result = Sentinel2ImportService.GetAvailableResolutions(bands);

        result.Should().Equal(10, 20, 60);
    }

    [Fact]
    public void GetAvailableResolutions_Empty_ReturnsEmpty()
    {
        var result = Sentinel2ImportService.GetAvailableResolutions([]);

        result.Should().BeEmpty();
    }

    #endregion

    #region BuildVrtAsync

    [Fact]
    public async Task BuildVrtAsync_NoBands_ThrowsArgumentException()
    {
        var options = new Sentinel2ImportService.ImportOptions(
            SelectedBands: [],
            OutputPath: Path.Combine(_tempDir, "output.vrt"));

        var act = () => _service.BuildVrtAsync(options);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BuildVrtAsync_MismatchedDimensions_ThrowsInvalidOperation()
    {
        var bands = new List<Sentinel2ImportService.Sentinel2BandInfo>
        {
            CreateDummyBandInfo("B02", 10, width: 1000, height: 1000),
            CreateDummyBandInfo("B05", 20, width: 500, height: 500),
        };
        var options = new Sentinel2ImportService.ImportOptions(
            SelectedBands: bands,
            OutputPath: Path.Combine(_tempDir, "output.vrt"));

        var act = () => _service.BuildVrtAsync(options);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*different dimensions*");
    }

    [Fact]
    public async Task BuildVrtAsync_ValidBands_CreatesVrtFile()
    {
        var bands = new List<Sentinel2ImportService.Sentinel2BandInfo>
        {
            CreateDummyBandInfo("B02", 10, width: 100, height: 100),
            CreateDummyBandInfo("B03", 10, width: 100, height: 100),
            CreateDummyBandInfo("B04", 10, width: 100, height: 100),
        };
        var outputPath = Path.Combine(_tempDir, "output.vrt");
        var options = new Sentinel2ImportService.ImportOptions(
            SelectedBands: bands,
            OutputPath: outputPath);

        var result = await _service.BuildVrtAsync(options);

        result.Should().Be(Path.GetFullPath(outputPath));
        File.Exists(result).Should().BeTrue();

        var content = await File.ReadAllTextAsync(result);
        content.Should().Contain("VRTDataset");
        content.Should().Contain("rasterXSize=\"100\"");
        content.Should().Contain("rasterYSize=\"100\"");
        content.Should().Contain("<Description>B02</Description>");
        content.Should().Contain("<Description>B03</Description>");
        content.Should().Contain("<Description>B04</Description>");
        content.Should().Contain("band=\"1\"");
        content.Should().Contain("band=\"2\"");
        content.Should().Contain("band=\"3\"");
    }

    [Fact]
    public async Task BuildVrtAsync_ReportsProgress()
    {
        var bands = new List<Sentinel2ImportService.Sentinel2BandInfo>
        {
            CreateDummyBandInfo("B02", 10, width: 50, height: 50),
            CreateDummyBandInfo("B03", 10, width: 50, height: 50),
        };
        var options = new Sentinel2ImportService.ImportOptions(
            SelectedBands: bands,
            OutputPath: Path.Combine(_tempDir, "progress.vrt"));

        var reports = new List<Sentinel2ImportService.ImportProgress>();
        var progress = new Progress<Sentinel2ImportService.ImportProgress>(r => reports.Add(r));

        await _service.BuildVrtAsync(options, progress);

        // Allow async progress reporting to flush
        await Task.Delay(100);

        reports.Should().NotBeEmpty();
        reports.Last().TotalBands.Should().Be(2);
    }

    [Fact]
    public async Task BuildVrtAsync_Cancellation_ThrowsOperationCanceled()
    {
        var bands = new List<Sentinel2ImportService.Sentinel2BandInfo>
        {
            CreateDummyBandInfo("B02", 10, width: 50, height: 50),
        };
        var options = new Sentinel2ImportService.ImportOptions(
            SelectedBands: bands,
            OutputPath: Path.Combine(_tempDir, "cancel.vrt"));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _service.BuildVrtAsync(options, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task BuildVrtAsync_IncludesProjectionAndGeoTransform()
    {
        var gt = new double[] { 100.0, 10.0, 0.0, 200.0, 0.0, -10.0 };
        var bands = new List<Sentinel2ImportService.Sentinel2BandInfo>
        {
            new("B04", "/fake/B04.tif", 10, 100, 100, "UInt16", "EPSG:32619", gt),
        };
        var outputPath = Path.Combine(_tempDir, "geo.vrt");
        var options = new Sentinel2ImportService.ImportOptions(bands, outputPath);

        await _service.BuildVrtAsync(options);

        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("<SRS>EPSG:32619</SRS>");
        content.Should().Contain("<GeoTransform>");
    }

    #endregion

    private static Sentinel2ImportService.Sentinel2BandInfo CreateDummyBandInfo(
        string bandName, int resolution, int width = 100, int height = 100)
    {
        return new Sentinel2ImportService.Sentinel2BandInfo(
            BandName: bandName,
            FilePath: $"/fake/{bandName}.tif",
            ResolutionMeters: resolution,
            Width: width,
            Height: height,
            DataType: "UInt16",
            Projection: "EPSG:32619",
            GeoTransform: [0, resolution, 0, 0, 0, -resolution]);
    }
}
