using FluentAssertions;
using FuzzySat.Core.Validation;

namespace FuzzySat.Core.Tests.Validation;

public class AccuracyMetricsTests
{
    private const double Precision = 1e-4;

    [Fact]
    public void FromPerfectMatrix_AllMetricsCorrect()
    {
        var cm = new ConfusionMatrix(
            ["A", "A", "B", "B", "C", "C"],
            ["A", "A", "B", "B", "C", "C"]);

        var metrics = new AccuracyMetrics(cm);

        metrics.OverallAccuracy.Should().BeApproximately(1.0, Precision);
        metrics.KappaCoefficient.Should().BeApproximately(1.0, Precision);
        metrics.TotalSamples.Should().Be(6);
        metrics.CorrectCount.Should().Be(6);
        metrics.PerClassMetrics.Should().HaveCount(3);
    }

    [Fact]
    public void PerClassMetrics_ContainsAllClasses()
    {
        var cm = new ConfusionMatrix(
            ["A", "A", "A", "B", "B"],
            ["A", "A", "B", "B", "A"]);

        var metrics = new AccuracyMetrics(cm);

        metrics.PerClassMetrics.Should().HaveCount(2);
        metrics.PerClassMetrics.Select(m => m.ClassName).Should().Contain("A").And.Contain("B");
    }

    [Fact]
    public void PerClassMetrics_CorrectProducersAndUsersAccuracy()
    {
        var cm = new ConfusionMatrix(
            ["A", "A", "A", "B", "B"],
            ["A", "A", "B", "A", "B"]);

        var metrics = new AccuracyMetrics(cm);
        var classA = metrics.PerClassMetrics.First(m => m.ClassName == "A");
        var classB = metrics.PerClassMetrics.First(m => m.ClassName == "B");

        classA.ProducersAccuracy.Should().BeApproximately(2.0 / 3.0, Precision);
        classA.UsersAccuracy.Should().BeApproximately(2.0 / 3.0, Precision);
        classA.ActualCount.Should().Be(3);
        classA.PredictedCount.Should().Be(3);

        classB.ProducersAccuracy.Should().BeApproximately(1.0 / 2.0, Precision);
        classB.UsersAccuracy.Should().BeApproximately(1.0 / 2.0, Precision);
        classB.ActualCount.Should().Be(2);
        classB.PredictedCount.Should().Be(2);
    }

    [Fact]
    public void KnownMatrix_KappaMatchesHandCalculation()
    {
        // Same textbook example from ConfusionMatrixTests
        var actual = new[] { "A", "A", "A", "A", "A", "B", "B", "B", "B", "B" };
        var predicted = new[] { "A", "A", "A", "B", "B", "B", "B", "B", "A", "A" };

        var metrics = new AccuracyMetrics(new ConfusionMatrix(actual, predicted));

        metrics.OverallAccuracy.Should().BeApproximately(0.6, Precision);
        metrics.KappaCoefficient.Should().BeApproximately(0.2, Precision);
    }

    [Fact]
    public void Constructor_NullMatrix_ThrowsArgumentNullException()
    {
        var act = () => new AccuracyMetrics(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
