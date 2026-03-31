using System.Text.Json;
using FluentAssertions;
using FuzzySat.Core.Persistence;
using FuzzySat.Core.Validation;

namespace FuzzySat.Core.Tests.Persistence;

public class PersistenceDtoTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ClassificationResultDto_RoundTrips_ViaJson()
    {
        var dto = new ClassificationResultDto
        {
            Rows = 100,
            Columns = 200,
            ClassNames = ["Urban", "Water", "Forest"],
            ClassCodes = [1, 2, 3],
            ClassColors = ["#FF0000", "#0000FF", "#00FF00"]
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ClassificationResultDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Rows.Should().Be(100);
        deserialized.Columns.Should().Be(200);
        deserialized.ClassNames.Should().BeEquivalentTo(["Urban", "Water", "Forest"]);
        deserialized.ClassCodes.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void ClassificationOptionsDto_RoundTrips_ViaJson()
    {
        var dto = new ClassificationOptionsDto
        {
            MembershipFunctionType = "Gaussian",
            AndOperator = "Minimum",
            DefuzzifierType = "MaxWeight",
            ClassificationMethod = "PureFuzzy"
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ClassificationOptionsDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.MembershipFunctionType.Should().Be("Gaussian");
        deserialized.AndOperator.Should().Be("Minimum");
    }

    [Fact]
    public void ValidationResultDto_RoundTrips_ViaJson()
    {
        var dto = new ValidationResultDto
        {
            ClassNames = ["Urban", "Water"],
            Matrix = [[45, 5], [3, 47]],
            OverallAccuracy = 0.92,
            KappaCoefficient = 0.84,
            TotalSamples = 100,
            CorrectCount = 92,
            PerClassMetrics =
            [
                new ClassMetricDto
                {
                    ClassName = "Urban", ProducersAccuracy = 0.90,
                    UsersAccuracy = 0.9375, ActualCount = 50, PredictedCount = 48
                },
                new ClassMetricDto
                {
                    ClassName = "Water", ProducersAccuracy = 0.94,
                    UsersAccuracy = 0.9038, ActualCount = 50, PredictedCount = 52
                }
            ]
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ValidationResultDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.ClassNames.Should().HaveCount(2);
        deserialized.Matrix.Should().HaveCount(2);
        deserialized.OverallAccuracy.Should().BeApproximately(0.92, 0.001);
        deserialized.PerClassMetrics.Should().HaveCount(2);
    }

    [Fact]
    public void ValidationResultDto_ToMatrix_ReturnsCorrect2DArray()
    {
        var dto = new ValidationResultDto
        {
            ClassNames = ["A", "B", "C"],
            Matrix = [[10, 1, 2], [0, 15, 3], [1, 2, 12]]
        };

        var matrix = dto.ToMatrix();

        matrix.GetLength(0).Should().Be(3);
        matrix.GetLength(1).Should().Be(3);
        matrix[0, 0].Should().Be(10);
        matrix[1, 1].Should().Be(15);
        matrix[2, 2].Should().Be(12);
        matrix[0, 2].Should().Be(2);
    }

    [Fact]
    public void ConfusionMatrix_FromPersistedData_ReproducesMetrics()
    {
        // Create original from raw labels
        var actual = new[] { "A", "A", "A", "B", "B", "B", "C", "C", "C", "C" };
        var predicted = new[] { "A", "A", "B", "B", "B", "A", "C", "C", "C", "B" };
        var original = new ConfusionMatrix(actual, predicted);

        // Extract matrix data
        var n = original.ClassNames.Count;
        var matrixData = new int[n, n];
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                matrixData[i, j] = original[original.ClassNames[i], original.ClassNames[j]];

        // Reconstruct
        var restored = ConfusionMatrix.FromPersistedData(original.ClassNames, matrixData);

        restored.ClassNames.Should().BeEquivalentTo(original.ClassNames);
        restored.TotalSamples.Should().Be(original.TotalSamples);
        restored.CorrectCount.Should().Be(original.CorrectCount);
        restored.OverallAccuracy.Should().BeApproximately(original.OverallAccuracy, 1e-10);
        restored.KappaCoefficient.Should().BeApproximately(original.KappaCoefficient, 1e-10);

        foreach (var cn in original.ClassNames)
        {
            restored.ProducersAccuracy(cn).Should().BeApproximately(original.ProducersAccuracy(cn), 1e-10);
            restored.UsersAccuracy(cn).Should().BeApproximately(original.UsersAccuracy(cn), 1e-10);
        }
    }

    [Fact]
    public void ConfusionMatrix_FromPersistedData_EmptyClassNames_Throws()
    {
        var act = () => ConfusionMatrix.FromPersistedData([], new int[0, 0]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ConfusionMatrix_FromPersistedData_MismatchedDimensions_Throws()
    {
        var act = () => ConfusionMatrix.FromPersistedData(["A", "B"], new int[3, 3]);
        act.Should().Throw<ArgumentException>();
    }
}
