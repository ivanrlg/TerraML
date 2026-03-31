using FluentAssertions;
using FuzzySat.Core.Training;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class ValidationServiceTests
{
    private readonly ValidationService _service = new();

    private static (List<LabeledPixelSample> Samples, TrainingSession Session) CreateTestData()
    {
        // Training samples to build the classifier
        var trainingsamples = new List<LabeledPixelSample>
        {
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 10, ["B2"] = 100 } },
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 12, ["B2"] = 95 } },
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 8, ["B2"] = 105 } },
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B1"] = 30, ["B2"] = 20 } },
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B1"] = 32, ["B2"] = 22 } },
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B1"] = 28, ["B2"] = 18 } },
        };
        var session = TrainingSession.CreateFromSamples(trainingsamples);

        // Validation samples (independent set)
        var validationSamples = new List<LabeledPixelSample>
        {
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 11, ["B2"] = 98 } },
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 9, ["B2"] = 102 } },
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B1"] = 31, ["B2"] = 21 } },
            new() { ClassName = "Urban", BandValues = new Dictionary<string, double> { ["B1"] = 29, ["B2"] = 19 } },
        };

        return (validationSamples, session);
    }

    [Fact]
    public void ValidateFromSamples_WithGoodData_ProducesHighAccuracy()
    {
        var (samples, session) = CreateTestData();
        var options = new ClassificationOptions("Gaussian", "Minimum", "Max Weight");

        var (matrix, metrics) = _service.ValidateFromSamples(samples, session, options);

        // With well-separated classes, we expect perfect classification
        matrix.OverallAccuracy.Should().Be(1.0);
        matrix.KappaCoefficient.Should().Be(1.0);
        matrix.TotalSamples.Should().Be(4);
        matrix.CorrectCount.Should().Be(4);
        matrix.ClassNames.Should().HaveCount(2);

        metrics.OverallAccuracy.Should().Be(1.0);
        metrics.PerClassMetrics.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateFromSamples_EmptySamples_ThrowsArgument()
    {
        var (_, session) = CreateTestData();
        var options = new ClassificationOptions();
        var empty = new List<LabeledPixelSample>();

        var act = () => _service.ValidateFromSamples(empty, session, options);
        act.Should().Throw<ArgumentException>().WithMessage("*At least one validation sample*");
    }

    [Fact]
    public void ValidateFromSamples_NullSession_ThrowsArgumentNull()
    {
        var samples = new List<LabeledPixelSample>
        {
            new() { ClassName = "Water", BandValues = new Dictionary<string, double> { ["B1"] = 10 } }
        };

        var act = () => _service.ValidateFromSamples(samples, null!, new ClassificationOptions());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExportConfusionMatrixCsv_ContainsExpectedContent()
    {
        var (samples, session) = CreateTestData();
        var options = new ClassificationOptions();
        var (matrix, _) = _service.ValidateFromSamples(samples, session, options);

        var csv = _service.ExportConfusionMatrixCsv(matrix);

        csv.Should().Contain("Actual\\Predicted");
        csv.Should().Contain("Water");
        csv.Should().Contain("Urban");
        csv.Should().Contain("Overall Accuracy");
        csv.Should().Contain("Kappa Coefficient");
        csv.Should().Contain("Total Samples,4");
    }

    [Fact]
    public void ExportConfusionMatrixCsv_NullMatrix_ThrowsArgumentNull()
    {
        var act = () => _service.ExportConfusionMatrixCsv(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateFromSamples_AllMfTypes_Succeed()
    {
        var (samples, session) = CreateTestData();

        foreach (var mfType in new[] { "Gaussian", "Triangular", "Trapezoidal", "Bell" })
        {
            var options = new ClassificationOptions(mfType, "Minimum", "Max Weight");
            var (matrix, _) = _service.ValidateFromSamples(samples, session, options);
            matrix.TotalSamples.Should().Be(4);
        }
    }
}
