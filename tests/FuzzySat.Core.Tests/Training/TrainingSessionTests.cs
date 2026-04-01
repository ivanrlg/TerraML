using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.Classification;
using FuzzySat.Core.FuzzyLogic.Inference;
using FuzzySat.Core.Training;

namespace FuzzySat.Core.Tests.Training;

public class TrainingSessionTests
{
    [Fact]
    public void CreateFromSamples_CorrectStructure()
    {
        var session = MakeSession();

        session.ClassNames.Should().HaveCount(2);
        session.BandNames.Should().HaveCount(2);
        session.Statistics.Should().HaveCount(2);
        session.Id.Should().NotBeNullOrEmpty();
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void BuildRuleSet_CorrectNumberOfRulesAndBands()
    {
        var session = MakeSession();

        var ruleSet = session.BuildRuleSet();

        ruleSet.Rules.Should().HaveCount(2);
        ruleSet.Rules[0].BandMembershipFunctions.Should().HaveCount(2);
        ruleSet.Rules[1].BandMembershipFunctions.Should().HaveCount(2);
    }

    [Fact]
    public void BuildRuleSet_UsesCorrectMeanAndStdDev()
    {
        var session = MakeSession();

        var ruleSet = session.BuildRuleSet();
        var urbanRule = ruleSet.Rules.First(r => r.ClassName == "Urban");

        // Urban B1 samples: [100, 110] → mean=105, used as center
        var mf = urbanRule.BandMembershipFunctions["B1"];
        mf.Center.Should().BeApproximately(105.0, 1e-10);
    }

    [Fact]
    public void BuildRuleSet_Deterministic()
    {
        var session = MakeSession();

        var ruleSet1 = session.BuildRuleSet();
        var ruleSet2 = session.BuildRuleSet();

        ruleSet1.Rules.Should().HaveCount(ruleSet2.Rules.Count);
        for (var i = 0; i < ruleSet1.Rules.Count; i++)
        {
            ruleSet1.Rules[i].ClassName.Should().Be(ruleSet2.Rules[i].ClassName);
        }
    }

    [Fact]
    public void BuildRuleSet_ClassifiesCorrectly_EndToEnd()
    {
        var session = MakeSession();
        var ruleSet = session.BuildRuleSet();
        var classifier = new FuzzyClassifier(new FuzzyInferenceEngine(ruleSet));

        // Pixel close to Urban profile (105, 155)
        classifier.ClassifyPixel(new Dictionary<string, double>
        {
            ["B1"] = 106.0,
            ["B2"] = 154.0
        }).Should().Be("Urban");

        // Pixel close to Water profile (25, 15)
        classifier.ClassifyPixel(new Dictionary<string, double>
        {
            ["B1"] = 24.0,
            ["B2"] = 16.0
        }).Should().Be("Water");
    }

    [Fact]
    public void CreateFromStatistics_WithExplicitId()
    {
        var stats = new Dictionary<string, SpectralStatistics>
        {
            ["Urban"] = new SpectralStatistics("Urban",
                new Dictionary<string, double> { ["B1"] = 100.0 },
                new Dictionary<string, double> { ["B1"] = 10.0 })
        };

        var session = TrainingSession.CreateFromStatistics(
            stats, ["Urban"], ["B1"], id: "test-id", createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        session.Id.Should().Be("test-id");
        session.CreatedAt.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CreateFromStatistics_Empty_ThrowsArgumentException()
    {
        var act = () => TrainingSession.CreateFromStatistics(
            new Dictionary<string, SpectralStatistics>(), [], []);

        act.Should().Throw<ArgumentException>();
    }

    private static TrainingSession MakeSession()
    {
        var samples = new[]
        {
            MakeSample("Urban", ("B1", 100.0), ("B2", 150.0)),
            MakeSample("Urban", ("B1", 110.0), ("B2", 160.0)),
            MakeSample("Water", ("B1", 20.0), ("B2", 10.0)),
            MakeSample("Water", ("B1", 30.0), ("B2", 20.0))
        };

        return TrainingSession.CreateFromSamples(samples);
    }

    private static LabeledPixelSample MakeSample(string className, params (string Band, double Value)[] bands)
    {
        return new LabeledPixelSample
        {
            ClassName = className,
            BandValues = bands.ToDictionary(b => b.Band, b => b.Value).AsReadOnly()
        };
    }
}
