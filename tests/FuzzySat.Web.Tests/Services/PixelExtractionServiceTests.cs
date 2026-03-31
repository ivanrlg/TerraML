using FluentAssertions;
using FuzzySat.Core.Raster;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class PixelExtractionServiceTests
{
    private readonly PixelExtractionService _service = new();

    private static MultispectralImage CreateTestImage()
    {
        // 5x5 image with 2 bands
        var band1Data = new double[5, 5];
        var band2Data = new double[5, 5];
        for (var r = 0; r < 5; r++)
            for (var c = 0; c < 5; c++)
            {
                band1Data[r, c] = r * 10 + c;       // 0-44
                band2Data[r, c] = 100 - (r * 10 + c); // 100-56
            }

        return new MultispectralImage([new Band("B1", band1Data), new Band("B2", band2Data)]);
    }

    [Fact]
    public void ExtractRegion_SinglePixel_ReturnsOneSample()
    {
        var image = CreateTestImage();

        var samples = _service.ExtractRegion(image, "Water", 0, 0, 0, 0);

        samples.Should().HaveCount(1);
        samples[0].ClassName.Should().Be("Water");
        samples[0].BandValues["B1"].Should().Be(0);
        samples[0].BandValues["B2"].Should().Be(100);
    }

    [Fact]
    public void ExtractRegion_2x3Rectangle_ReturnsSixSamples()
    {
        var image = CreateTestImage();

        var samples = _service.ExtractRegion(image, "Urban", 1, 1, 2, 3);

        samples.Should().HaveCount(6); // 2 rows × 3 cols
        samples.Should().AllSatisfy(s => s.ClassName.Should().Be("Urban"));

        // Check corner values
        samples[0].BandValues["B1"].Should().Be(11); // [1,1]
        samples[5].BandValues["B1"].Should().Be(23); // [2,3]
    }

    [Fact]
    public void ExtractRegion_ReversedCoordinates_NormalizesAutomatically()
    {
        var image = CreateTestImage();

        // End before start — should normalize
        var samples = _service.ExtractRegion(image, "Forest", 2, 3, 1, 1);

        samples.Should().HaveCount(6); // Same as 1,1→2,3
    }

    [Fact]
    public void ExtractRegion_OutOfBounds_ClampsToImageBounds()
    {
        var image = CreateTestImage();

        // Request beyond image bounds
        var samples = _service.ExtractRegion(image, "Water", -5, -5, 100, 100);

        // Should clamp to 0,0→4,4 = 25 pixels
        samples.Should().HaveCount(25);
    }

    [Fact]
    public void ExtractRegion_FullImage_ReturnsAllPixels()
    {
        var image = CreateTestImage();

        var samples = _service.ExtractRegion(image, "All", 0, 0, 4, 4);

        samples.Should().HaveCount(25);
    }

    [Fact]
    public void ExtractAllRegions_MultipleRegions_CombinesSamples()
    {
        var image = CreateTestImage();
        var regions = new List<TrainingRegion>
        {
            new("Water", "#0000FF", 0, 0, 0, 1),  // 2 pixels
            new("Urban", "#FF0000", 3, 3, 4, 4),   // 4 pixels
        };

        var samples = _service.ExtractAllRegions(image, regions);

        samples.Should().HaveCount(6);
        samples.Count(s => s.ClassName == "Water").Should().Be(2);
        samples.Count(s => s.ClassName == "Urban").Should().Be(4);
    }

    [Fact]
    public void ExtractAllRegions_EmptyList_ReturnsEmpty()
    {
        var image = CreateTestImage();

        var samples = _service.ExtractAllRegions(image, []);

        samples.Should().BeEmpty();
    }

    [Fact]
    public void ExtractRegion_NullImage_ThrowsArgumentNull()
    {
        var act = () => _service.ExtractRegion(null!, "Water", 0, 0, 1, 1);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractRegion_EmptyClassName_ThrowsArgument()
    {
        var image = CreateTestImage();
        var act = () => _service.ExtractRegion(image, "", 0, 0, 1, 1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TrainingRegion_PixelCount_IsCorrect()
    {
        var region = new TrainingRegion("Water", "#0000FF", 0, 0, 2, 3);
        region.PixelCount.Should().Be(12); // 3 rows × 4 cols
    }
}
