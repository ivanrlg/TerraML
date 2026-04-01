using FluentAssertions;
using FuzzySat.Core.Raster;
using FuzzySat.Core.Training;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class HybridClassificationServiceTests
{
    private readonly HybridClassificationService _service = new();

    private static (MultispectralImage Image, TrainingSession Session, List<LabeledPixelSample> Samples) CreateTestData()
    {
        // Create a small 3x3 image with 2 bands
        var band1Data = new double[3, 3]
        {
            { 10, 20, 30 },
            { 15, 25, 35 },
            { 12, 22, 32 }
        };
        var band2Data = new double[3, 3]
        {
            { 100, 50, 20 },
            { 90, 55, 25 },
            { 95, 48, 22 }
        };

        var band1 = new Band("B1", band1Data);
        var band2 = new Band("B2", band2Data);
        var image = new MultispectralImage([band1, band2]);

        // Training samples: "Water" clusters around B1=10, B2=100; "Urban" around B1=30, B2=20
        var rng = new Random(42);
        var samples = new List<LabeledPixelSample>();
        for (var i = 0; i < 30; i++)
        {
            samples.Add(new LabeledPixelSample
            {
                ClassName = "Water",
                BandValues = new Dictionary<string, double>
                {
                    ["B1"] = 10.0 + rng.NextDouble() * 6 - 3,
                    ["B2"] = 100.0 + rng.NextDouble() * 10 - 5
                }
            });
            samples.Add(new LabeledPixelSample
            {
                ClassName = "Urban",
                BandValues = new Dictionary<string, double>
                {
                    ["B1"] = 30.0 + rng.NextDouble() * 6 - 3,
                    ["B2"] = 20.0 + rng.NextDouble() * 10 - 5
                }
            });
        }

        var session = TrainingSession.CreateFromSamples(samples);
        return (image, session, samples);
    }

    [Fact]
    public void Classify_RandomForest_ProducesValidResult()
    {
        var (image, session, samples) = CreateTestData();
        var options = new ClassificationOptions(ClassificationMethod: "Random Forest");

        var result = _service.Classify(image, session, samples, options);

        result.Rows.Should().Be(3);
        result.Columns.Should().Be(3);
        result.Classes.Should().HaveCount(2);

        // Top-left pixel (B1=10, B2=100) should be Water
        result.GetClass(0, 0).Should().Be("Water");
        // Bottom-right pixel (B1=32, B2=22) should be Urban
        result.GetClass(2, 2).Should().Be("Urban");
    }

    [Fact]
    public void Classify_Sdca_ProducesValidResult()
    {
        var (image, session, samples) = CreateTestData();
        var options = new ClassificationOptions(ClassificationMethod: "SDCA");

        var result = _service.Classify(image, session, samples, options);

        result.Rows.Should().Be(3);
        result.Columns.Should().Be(3);
        result.Classes.Should().HaveCount(2);

        // Top-left pixel (B1=10, B2=100) should be Water
        result.GetClass(0, 0).Should().Be("Water");
    }

    [Fact]
    public void Classify_ReportsProgress()
    {
        var (image, session, samples) = CreateTestData();
        var options = new ClassificationOptions(ClassificationMethod: "Random Forest");
        var completedEvent = new ManualResetEventSlim(false);
        var reports = new List<ClassificationProgress>();

        var progress = new Progress<ClassificationProgress>(p =>
        {
            reports.Add(p);
            if (p.Stage == "Complete")
                completedEvent.Set();
        });

        _service.Classify(image, session, samples, options, progress);

        completedEvent.Wait(TimeSpan.FromSeconds(5));

        reports.Should().NotBeEmpty();
        reports.Should().Contain(p => p.Stage == "Training ML model");
        reports.Should().Contain(p => p.Stage == "Complete");
    }

    [Fact]
    public void Classify_WithCancellation_ThrowsOperationCancelled()
    {
        var (image, session, samples) = CreateTestData();
        var options = new ClassificationOptions(ClassificationMethod: "Random Forest");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _service.Classify(image, session, samples, options, cancellationToken: cts.Token);
        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void Classify_EmptySamples_ThrowsArgument()
    {
        var (image, session, _) = CreateTestData();
        var options = new ClassificationOptions(ClassificationMethod: "Random Forest");

        var act = () => _service.Classify(image, session, [], options);
        act.Should().Throw<ArgumentException>().WithMessage("*training sample*");
    }

    [Fact]
    public void Classify_UnknownMethod_ThrowsArgument()
    {
        var (image, session, samples) = CreateTestData();
        var options = new ClassificationOptions(ClassificationMethod: "InvalidMethod");

        var act = () => _service.Classify(image, session, samples, options);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown classification method*");
    }
}
