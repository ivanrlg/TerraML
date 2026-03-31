using FluentAssertions;
using FuzzySat.Core.Raster;
using FuzzySat.Core.Training;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class ClassificationServiceTests
{
    private readonly ClassificationService _service = new();

    private static (MultispectralImage Image, TrainingSession Session) CreateTestData()
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

        // Create training samples for 2 classes:
        // "Water" clusters around B1=10, B2=100
        // "Urban" clusters around B1=30, B2=20
        var samples = new List<LabeledPixelSample>
        {
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 10, ["B2"] = 100 } },
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 12, ["B2"] = 95 } },
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 8, ["B2"] = 105 } },
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B1"] = 30, ["B2"] = 20 } },
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B1"] = 32, ["B2"] = 22 } },
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B1"] = 28, ["B2"] = 18 } },
        };

        var session = TrainingSession.CreateFromSamples(samples);
        return (image, session);
    }

    [Fact]
    public void Classify_WithGaussianMF_ProducesValidResult()
    {
        var (image, session) = CreateTestData();
        var options = new ClassificationOptions(MembershipFunctionType: "Gaussian", AndOperator: "Minimum", DefuzzifierType: "Max Weight");

        var result = _service.Classify(image, session, options);

        result.Rows.Should().Be(3);
        result.Columns.Should().Be(3);
        result.Classes.Should().HaveCount(2);

        // Top-left pixel (B1=10, B2=100) should be Water
        result.GetClass(0, 0).Should().Be("Water");
        // Top-right pixel (B1=30, B2=20) should be Urban
        result.GetClass(0, 2).Should().Be("Urban");

        // All confidences should be in [0, 1]
        for (var r = 0; r < result.Rows; r++)
            for (var c = 0; c < result.Columns; c++)
                result.GetConfidence(r, c).Should().BeInRange(0, 1);
    }

    [Theory]
    [InlineData("Gaussian")]
    [InlineData("Triangular")]
    [InlineData("Trapezoidal")]
    [InlineData("Bell")]
    public void Classify_AllMfTypes_Succeed(string mfType)
    {
        var (image, session) = CreateTestData();
        var options = new ClassificationOptions(MembershipFunctionType: mfType, AndOperator: "Minimum", DefuzzifierType: "Max Weight");

        var result = _service.Classify(image, session, options);

        result.Rows.Should().Be(3);
        result.Columns.Should().Be(3);
        // Top-left should still be Water regardless of MF type
        result.GetClass(0, 0).Should().Be("Water");
    }

    [Fact]
    public void Classify_ProductAndOperator_ProducesValidResult()
    {
        var (image, session) = CreateTestData();
        var options = new ClassificationOptions(MembershipFunctionType: "Gaussian", AndOperator: "Product", DefuzzifierType: "Max Weight");

        var result = _service.Classify(image, session, options);

        result.Rows.Should().Be(3);
        result.GetClass(0, 0).Should().Be("Water");
        result.GetClass(0, 2).Should().Be("Urban");
    }

    [Fact]
    public void Classify_ProductAnd_WeightedAverage_UsesDefuzzifier()
    {
        // With only 2 well-separated classes, both defuzzifiers produce the same winner.
        // This test verifies that the ProductAnd path actually calls the defuzzifier
        // (i.e., produces a valid result) rather than silently using MaxWeight.
        var (image, session) = CreateTestData();

        var optionsMaxWeight = new ClassificationOptions(MembershipFunctionType: "Gaussian", AndOperator: "Product", DefuzzifierType: "Max Weight");
        var optionsWeightedAvg = new ClassificationOptions(MembershipFunctionType: "Gaussian", AndOperator: "Product", DefuzzifierType: "Weighted Average");

        var resultMW = _service.Classify(image, session, optionsMaxWeight);
        var resultWA = _service.Classify(image, session, optionsWeightedAvg);

        // Both should produce valid results (not crash)
        resultMW.Rows.Should().Be(3);
        resultWA.Rows.Should().Be(3);

        // Corner pixels with strong class separation should agree
        resultMW.GetClass(0, 0).Should().Be("Water");
        resultWA.GetClass(0, 0).Should().Be("Water");
        resultMW.GetClass(0, 2).Should().Be("Urban");
        resultWA.GetClass(0, 2).Should().Be("Urban");
    }

    [Fact]
    public void Classify_WeightedAverageDefuzzifier_ProducesValidResult()
    {
        var (image, session) = CreateTestData();
        var options = new ClassificationOptions(MembershipFunctionType: "Gaussian", AndOperator: "Minimum", DefuzzifierType: "Weighted Average");

        var result = _service.Classify(image, session, options);

        result.Rows.Should().Be(3);
        result.Columns.Should().Be(3);
    }

    [Fact]
    public void Classify_ReportsProgress()
    {
        var (image, session) = CreateTestData();
        var options = new ClassificationOptions();
        var completedEvent = new ManualResetEventSlim(false);
        var reports = new List<ClassificationProgress>();

        var progress = new Progress<ClassificationProgress>(p =>
        {
            reports.Add(p);
            if (p.Stage == "Complete")
                completedEvent.Set();
        });

        _service.Classify(image, session, options, progress);

        // Wait for async Progress<T> callbacks (posts to captured SynchronizationContext)
        completedEvent.Wait(TimeSpan.FromSeconds(2));

        reports.Should().NotBeEmpty();
        reports.Should().Contain(p => p.Stage == "Complete");
    }

    [Fact]
    public void Classify_WithCancellation_ThrowsOperationCancelled()
    {
        var (image, session) = CreateTestData();
        var options = new ClassificationOptions();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _service.Classify(image, session, options, cancellationToken: cts.Token);
        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void Classify_NullImage_ThrowsArgumentNull()
    {
        var (_, session) = CreateTestData();
        var act = () => _service.Classify(null!, session, new ClassificationOptions());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Classify_UnknownMfType_ThrowsArgument()
    {
        var (image, session) = CreateTestData();
        var options = new ClassificationOptions(MembershipFunctionType: "InvalidType", AndOperator: "Minimum", DefuzzifierType: "Max Weight");
        var act = () => _service.Classify(image, session, options);
        act.Should().Throw<ArgumentException>().WithMessage("*Unknown MF type*");
    }
}
