using FluentAssertions;
using FuzzySat.Core.Training;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class TrainingServiceCsvExportTests
{
    private readonly TrainingService _service = new();

    [Fact]
    public void ExportSamplesCsv_ProducesValidCsvFormat()
    {
        var samples = new List<LabeledPixelSample>
        {
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 10.5, ["B2"] = 100.0 } },
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B1"] = 30.0, ["B2"] = 20.5 } },
        };
        var bandNames = new List<string> { "B1", "B2" };

        var csv = _service.ExportSamplesCsv(samples, bandNames);

        csv.Should().StartWith("class,B1,B2");
        csv.Should().Contain("Water,10.5,100");
        csv.Should().Contain("Urban,30,20.5");
    }

    [Fact]
    public void ExportSamplesCsv_RoundTrip_PreservesSamples()
    {
        var original = new List<LabeledPixelSample>
        {
            new() { ClassName = "Forest", BandValues = new Dictionary<string, double> { ["NIR"] = 45.5, ["SWIR"] = 12.3 } },
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["NIR"] = 8.1, ["SWIR"] = 3.2 } },
        };
        var bandNames = new List<string> { "NIR", "SWIR" };

        // Export
        var csv = _service.ExportSamplesCsv(original, bandNames);

        // Re-import
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        var (reimported, reimportedBands, warnings) = _service.LoadSamplesFromCsv(stream);

        warnings.Should().BeEmpty();
        reimportedBands.Should().BeEquivalentTo(bandNames);
        reimported.Should().HaveCount(2);
        reimported[0].ClassName.Should().Be("Forest");
        reimported[0].BandValues["NIR"].Should().BeApproximately(45.5, 0.01);
        reimported[1].ClassName.Should().Be("Water");
    }

    [Fact]
    public void ExportSamplesCsv_EmptySamples_ProducesHeaderOnly()
    {
        var csv = _service.ExportSamplesCsv([], new List<string> { "B1", "B2" });
        csv.Trim().Should().Be("class,B1,B2");
    }

    [Fact]
    public void ExportSamplesCsv_NullSamples_ThrowsArgumentNull()
    {
        var act = () => _service.ExportSamplesCsv(null!, []);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExportSamplesCsv_ClassNameWithComma_IsEscaped()
    {
        var samples = new List<LabeledPixelSample>
        {
            new() { ClassName = "Bare Soil, Rocky", BandValues = new Dictionary<string, double> { ["B1"] = 50 } },
        };

        var csv = _service.ExportSamplesCsv(samples, new List<string> { "B1" });
        csv.Should().Contain("\"Bare Soil, Rocky\"");
    }
}
