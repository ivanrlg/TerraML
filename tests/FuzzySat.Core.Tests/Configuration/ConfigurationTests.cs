using System.Text.Json;
using FluentAssertions;
using FuzzySat.Core.Classification;
using FuzzySat.Core.Configuration;

namespace FuzzySat.Core.Tests.Configuration;

public class ConfigurationTests
{
    [Fact]
    public void BandConfiguration_StoresProperties()
    {
        var band = new BandConfiguration
        {
            Name = "VNIR1",
            SourceIndex = 0,
            Description = "Visible Near Infrared 1"
        };

        band.Name.Should().Be("VNIR1");
        band.SourceIndex.Should().Be(0);
        band.Description.Should().Be("Visible Near Infrared 1");
    }

    [Fact]
    public void ClassifierConfiguration_StoresAllProperties()
    {
        var config = MakeTestConfig();

        config.ProjectName.Should().Be("TestProject");
        config.Bands.Should().HaveCount(2);
        config.Classes.Should().HaveCount(3);
        config.InputRasterPath.Should().Be("input.tif");
        config.OutputRasterPath.Should().Be("output.tif");
    }

    [Fact]
    public void ClassifierConfiguration_JsonRoundTrip()
    {
        var config = MakeTestConfig();

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var deserialized = JsonSerializer.Deserialize<ClassifierConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        deserialized.Should().NotBeNull();
        deserialized!.ProjectName.Should().Be("TestProject");
        deserialized.Bands.Should().HaveCount(2);
        deserialized.Bands[0].Name.Should().Be("VNIR1");
        deserialized.Classes.Should().HaveCount(3);
        deserialized.Classes[0].Name.Should().Be("Urban");
        deserialized.InputRasterPath.Should().Be("input.tif");
    }

    [Fact]
    public void BandConfiguration_JsonRoundTrip()
    {
        var band = new BandConfiguration { Name = "SWIR1", SourceIndex = 3 };

        var json = JsonSerializer.Serialize(band);
        var deserialized = JsonSerializer.Deserialize<BandConfiguration>(json);

        deserialized!.Name.Should().Be("SWIR1");
        deserialized.SourceIndex.Should().Be(3);
        deserialized.Description.Should().BeNull();
    }

    private static ClassifierConfiguration MakeTestConfig()
    {
        return new ClassifierConfiguration
        {
            ProjectName = "TestProject",
            Bands =
            [
                new BandConfiguration { Name = "VNIR1", SourceIndex = 0, Description = "Visible NIR 1" },
                new BandConfiguration { Name = "SWIR1", SourceIndex = 3, Description = "Shortwave IR 1" }
            ],
            Classes =
            [
                new LandCoverClass { Name = "Urban", Code = 1, Color = "#FF0000" },
                new LandCoverClass { Name = "Water", Code = 2, Color = "#0000FF" },
                new LandCoverClass { Name = "Forest", Code = 3, Color = "#00FF00" }
            ],
            TrainingDataPath = "training.shp",
            InputRasterPath = "input.tif",
            OutputRasterPath = "output.tif"
        };
    }
}
