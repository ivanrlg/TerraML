using FluentAssertions;
using FuzzySat.Core.Training;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class ProjectStateServiceTests
{
    private readonly ProjectStateService _service = new();

    [Fact]
    public void Reset_ClearsAllExploreState()
    {
        _service.ExploreViewMode = "RGB";
        _service.ExploreBands = new ExploreBandSelection(0, 2, 1, 0);
        _service.TrainingRegions = [new TrainingRegion("Water", "#0000FF", 0, 0, 10, 10)];
        _service.TrainingSamples = [new LabeledPixelSample { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B02"] = 1.0 } }];

        _service.Reset();

        _service.ExploreViewMode.Should().BeNull();
        _service.ExploreBands.Should().BeNull();
        _service.TrainingRegions.Should().BeNull();
        _service.TrainingSamples.Should().BeNull();
        _service.CachedImage.Should().BeNull();
    }

    [Fact]
    public void ExploreViewMode_FiresOnStateChanged()
    {
        var fired = false;
        _service.OnStateChanged += () => fired = true;

        _service.ExploreViewMode = "RGB";

        fired.Should().BeTrue();
        _service.ExploreViewMode.Should().Be("RGB");
    }

    [Fact]
    public void ExploreBands_FiresOnStateChanged()
    {
        var fired = false;
        _service.OnStateChanged += () => fired = true;

        var bands = new ExploreBandSelection(0, 2, 1, 0);
        _service.ExploreBands = bands;

        fired.Should().BeTrue();
        _service.ExploreBands.Should().Be(bands);
    }

    [Fact]
    public void TrainingRegions_FiresOnStateChanged()
    {
        var fired = false;
        _service.OnStateChanged += () => fired = true;

        var regions = new List<TrainingRegion>
        {
            new("Forest", "#00FF00", 5, 5, 20, 20)
        };
        _service.TrainingRegions = regions;

        fired.Should().BeTrue();
        _service.TrainingRegions.Should().HaveCount(1);
    }

    [Fact]
    public void TrainingSamples_FiresOnStateChanged()
    {
        var fired = false;
        _service.OnStateChanged += () => fired = true;

        var samples = new List<LabeledPixelSample>
        {
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B02"] = 100.0 } }
        };
        _service.TrainingSamples = samples;

        fired.Should().BeTrue();
        _service.TrainingSamples.Should().HaveCount(1);
    }

    [Fact]
    public void Reset_FiresOnStateChanged()
    {
        var count = 0;
        _service.OnStateChanged += () => count++;

        _service.ExploreViewMode = "Single";
        _service.Reset();

        count.Should().BeGreaterThanOrEqualTo(2);
    }
}
