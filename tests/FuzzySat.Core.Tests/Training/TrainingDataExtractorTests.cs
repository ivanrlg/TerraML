using FluentAssertions;
using FuzzySat.Core.Training;

namespace FuzzySat.Core.Tests.Training;

public class TrainingDataExtractorTests
{
    private const double Precision = 1e-10;

    // --- SpectralStatistics ---

    [Fact]
    public void SpectralStatistics_ValidInput_CreatesSuccessfully()
    {
        var stats = new SpectralStatistics("Urban",
            new Dictionary<string, double> { ["B1"] = 100.0 },
            new Dictionary<string, double> { ["B1"] = 15.0 });

        stats.ClassName.Should().Be("Urban");
        stats.MeanPerBand["B1"].Should().Be(100.0);
        stats.StdDevPerBand["B1"].Should().Be(15.0);
    }

    [Fact]
    public void SpectralStatistics_MismatchedBands_ThrowsArgumentException()
    {
        var act = () => new SpectralStatistics("Urban",
            new Dictionary<string, double> { ["B1"] = 100.0 },
            new Dictionary<string, double> { ["B2"] = 15.0 });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SpectralStatistics_ZeroStdDev_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SpectralStatistics("Urban",
            new Dictionary<string, double> { ["B1"] = 100.0 },
            new Dictionary<string, double> { ["B1"] = 0.0 });

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // --- TrainingDataExtractor ---

    [Fact]
    public void ExtractStatistics_SingleClassSingleBand_CorrectMean()
    {
        var samples = new[]
        {
            MakeSample("Urban", ("B1", 10.0)),
            MakeSample("Urban", ("B1", 20.0)),
            MakeSample("Urban", ("B1", 30.0))
        };

        var result = TrainingDataExtractor.ExtractStatistics(samples);

        result["Urban"].MeanPerBand["B1"].Should().BeApproximately(20.0, Precision);
    }

    [Fact]
    public void ExtractStatistics_SingleClassSingleBand_CorrectStdDev()
    {
        // Values: [10, 20, 30], mean=20, variance=((100+0+100)/3)=66.67, stddev=√66.67
        var samples = new[]
        {
            MakeSample("Urban", ("B1", 10.0)),
            MakeSample("Urban", ("B1", 20.0)),
            MakeSample("Urban", ("B1", 30.0))
        };

        var result = TrainingDataExtractor.ExtractStatistics(samples);
        var expectedStdDev = Math.Sqrt(200.0 / 3.0); // ≈ 8.1650

        result["Urban"].StdDevPerBand["B1"].Should().BeApproximately(expectedStdDev, Precision);
    }

    [Fact]
    public void ExtractStatistics_MultipleClasses_IsolatedStatistics()
    {
        var samples = new[]
        {
            MakeSample("Urban", ("B1", 100.0), ("B2", 150.0)),
            MakeSample("Urban", ("B1", 110.0), ("B2", 160.0)),
            MakeSample("Water", ("B1", 20.0), ("B2", 10.0)),
            MakeSample("Water", ("B1", 30.0), ("B2", 20.0))
        };

        var result = TrainingDataExtractor.ExtractStatistics(samples);

        result.Should().HaveCount(2);
        result["Urban"].MeanPerBand["B1"].Should().BeApproximately(105.0, Precision);
        result["Water"].MeanPerBand["B1"].Should().BeApproximately(25.0, Precision);
    }

    [Fact]
    public void ExtractStatistics_IdenticalValues_UsesEpsilonStdDev()
    {
        var samples = new[]
        {
            MakeSample("Urban", ("B1", 50.0)),
            MakeSample("Urban", ("B1", 50.0)),
            MakeSample("Urban", ("B1", 50.0))
        };

        var result = TrainingDataExtractor.ExtractStatistics(samples);

        // stddev = 0 gets replaced by epsilon
        result["Urban"].StdDevPerBand["B1"].Should().BeGreaterThan(0);
    }

    [Fact]
    public void ExtractStatistics_Empty_ThrowsArgumentException()
    {
        var act = () => TrainingDataExtractor.ExtractStatistics([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ExtractStatistics_MismatchedBands_ThrowsArgumentException()
    {
        var samples = new[]
        {
            MakeSample("Urban", ("B1", 100.0)),
            MakeSample("Urban", ("B2", 100.0))
        };

        var act = () => TrainingDataExtractor.ExtractStatistics(samples);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ExtractStatistics_KnownValues_MathematicallyCorrect()
    {
        // Values: [2, 4, 4, 4, 5, 5, 7, 9]
        // Mean = 40/8 = 5.0
        // Variance = ((9+1+1+1+0+0+4+16)/8) = 32/8 = 4.0
        // StdDev = 2.0
        var values = new double[] { 2, 4, 4, 4, 5, 5, 7, 9 };
        var samples = values.Select(v => MakeSample("Test", ("B1", v))).ToArray();

        var result = TrainingDataExtractor.ExtractStatistics(samples);

        result["Test"].MeanPerBand["B1"].Should().BeApproximately(5.0, Precision);
        result["Test"].StdDevPerBand["B1"].Should().BeApproximately(2.0, Precision);
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
