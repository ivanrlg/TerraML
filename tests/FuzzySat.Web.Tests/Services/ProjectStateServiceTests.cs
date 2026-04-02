using FluentAssertions;
using FuzzySat.Core.Classification;
using FuzzySat.Core.Configuration;
using FuzzySat.Core.Training;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class ProjectStateServiceTests
{
    private readonly ProjectStateService _service = new();

    private static ClassifierConfiguration MakeConfig(params string[] classNames)
    {
        return new ClassifierConfiguration
        {
            ProjectName = "Test",
            Bands = [new BandConfiguration { Name = "B1", SourceIndex = 0 }],
            Classes = classNames.Select((n, i) => new LandCoverClass { Name = n, Code = i + 1, Color = "#808080" }).ToList()
        };
    }

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

    [Fact]
    public void BatchUpdate_FiresExactlyOneEvent()
    {
        var count = 0;
        _service.OnStateChanged += () => count++;

        _service.BeginBatch();
        _service.ExploreViewMode = "RGB";
        _service.ExploreBands = new ExploreBandSelection(0, 2, 1, 0);
        _service.TrainingRegions = [new TrainingRegion("Water", "#0000FF", 0, 0, 10, 10)];
        _service.TrainingSamples = null;
        _service.CachedImage = null;

        count.Should().Be(0, "no events should fire during batch");

        _service.EndBatch();

        count.Should().Be(1, "exactly one event after EndBatch");
    }

    [Fact]
    public void BatchUpdate_NestedBatches_FiresOnOuterEnd()
    {
        var count = 0;
        _service.OnStateChanged += () => count++;

        _service.BeginBatch();
        _service.BeginBatch();
        _service.ExploreViewMode = "Single";
        _service.EndBatch();

        count.Should().Be(0, "inner EndBatch should not fire");

        _service.EndBatch();

        count.Should().Be(1, "outer EndBatch fires once");
    }

    // --- RenameClass tests ---

    [Fact]
    public void RenameClass_UpdatesConfigurationClasses()
    {
        _service.Configuration = MakeConfig("Urban", "Water", "Forest");

        _service.RenameClass("Water", "Lake");

        _service.Configuration!.Classes.Should().Contain(c => c.Name == "Lake");
        _service.Configuration.Classes.Should().NotContain(c => c.Name == "Water");
        _service.Configuration.Classes.First(c => c.Name == "Lake").Code.Should().Be(2);
    }

    [Fact]
    public void RenameClass_UpdatesTrainingSamples()
    {
        _service.Configuration = MakeConfig("Urban", "Water");
        _service.TrainingSamples =
        [
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B1"] = 100 } },
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 10 } },
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 12 } }
        ];

        _service.RenameClass("Water", "Lake");

        _service.TrainingSamples.Should().HaveCount(3);
        _service.TrainingSamples!.Count(s => s.ClassName == "Lake").Should().Be(2);
        _service.TrainingSamples!.Count(s => s.ClassName == "Water").Should().Be(0);
    }

    [Fact]
    public void RenameClass_UpdatesTrainingRegions()
    {
        _service.Configuration = MakeConfig("Urban", "Water");
        _service.TrainingRegions =
        [
            new("Urban", "#FF0000", 0, 0, 10, 10),
            new("Water", "#0000FF", 5, 5, 15, 15)
        ];

        _service.RenameClass("Water", "Lake");

        _service.TrainingRegions!.Should().Contain(r => r.ClassName == "Lake");
        _service.TrainingRegions!.Should().NotContain(r => r.ClassName == "Water");
    }

    [Fact]
    public void RenameClass_UpdatesTrainingSession()
    {
        _service.Configuration = MakeConfig("Urban", "Water");

        var stats = new Dictionary<string, SpectralStatistics>
        {
            ["Urban"] = new("Urban",
                new Dictionary<string, double> { ["B1"] = 100 },
                new Dictionary<string, double> { ["B1"] = 5 }),
            ["Water"] = new("Water",
                new Dictionary<string, double> { ["B1"] = 10 },
                new Dictionary<string, double> { ["B1"] = 3 })
        };
        _service.TrainingSession = TrainingSession.CreateFromStatistics(
            stats, ["Urban", "Water"], ["B1"]);

        _service.RenameClass("Water", "Lake");

        _service.TrainingSession!.Statistics.Should().ContainKey("Lake");
        _service.TrainingSession.Statistics.Should().NotContainKey("Water");
        _service.TrainingSession.ClassNames.Should().Contain("Lake");
        _service.TrainingSession.Statistics["Lake"].ClassName.Should().Be("Lake");
    }

    [Fact]
    public void RenameClass_UpdatesClassColors()
    {
        _service.Configuration = MakeConfig("Urban", "Water");
        _service.ClassColors = new Dictionary<string, string>
        {
            ["Urban"] = "#FF0000",
            ["Water"] = "#0000FF"
        };

        _service.RenameClass("Water", "Lake");

        _service.ClassColors!.Should().ContainKey("Lake");
        _service.ClassColors!["Lake"].Should().Be("#0000FF");
        _service.ClassColors.Should().NotContainKey("Water");
    }

    [Fact]
    public void RenameClass_DuplicateName_Throws()
    {
        _service.Configuration = MakeConfig("Urban", "Water");

        var act = () => _service.RenameClass("Water", "Urban");

        act.Should().Throw<ArgumentException>().WithMessage("*already exists*");
    }

    [Fact]
    public void RenameClass_EmptyName_Throws()
    {
        _service.Configuration = MakeConfig("Urban", "Water");

        var act = () => _service.RenameClass("Water", "  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RenameClass_NonExistentClass_Throws()
    {
        _service.Configuration = MakeConfig("Urban", "Water");

        var act = () => _service.RenameClass("Forest", "Woods");

        act.Should().Throw<ArgumentException>().WithMessage("*not found*");
    }

    [Fact]
    public void RenameClass_SameName_NoOp()
    {
        _service.Configuration = MakeConfig("Urban", "Water");
        var count = 0;
        _service.OnStateChanged += () => count++;

        _service.RenameClass("Water", "Water");

        count.Should().Be(0, "same name should be a no-op");
    }

    [Fact]
    public void RenameClass_NoConfig_Throws()
    {
        var act = () => _service.RenameClass("Water", "Lake");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RenameClass_FiresExactlyOneStateChanged()
    {
        _service.Configuration = MakeConfig("Urban", "Water");
        _service.TrainingSamples =
        [
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 10 } }
        ];
        _service.TrainingRegions = [new("Water", "#0000FF", 0, 0, 10, 10)];

        var count = 0;
        _service.OnStateChanged += () => count++;

        _service.RenameClass("Water", "Lake");

        count.Should().Be(1, "batch should fire exactly one event");
    }
}
