using FluentAssertions;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class KMeansClustererTests
{
    [Fact]
    public void Train_ProducesClusterer()
    {
        var samples = GenerateSamples();

        var clusterer = KMeansClusterer.Train(samples, numberOfClusters: 3);

        clusterer.NumberOfClusters.Should().Be(3);
    }

    [Fact]
    public void Predict_ReturnsClusterId()
    {
        var samples = GenerateSamples();
        var clusterer = KMeansClusterer.Train(samples, numberOfClusters: 3);

        var clusterId = clusterer.Predict([100.0f, 150.0f]);

        clusterId.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Predict_SimilarPixels_SameCluster()
    {
        var samples = GenerateSamples();
        var clusterer = KMeansClusterer.Train(samples, numberOfClusters: 3);

        var c1 = clusterer.Predict([100.0f, 150.0f]);
        var c2 = clusterer.Predict([102.0f, 148.0f]);

        c1.Should().Be(c2);
    }

    [Fact]
    public void Train_EmptySamples_Throws()
    {
        var act = () => KMeansClusterer.Train([], numberOfClusters: 3);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Train_TooFewClusters_Throws()
    {
        var act = () => KMeansClusterer.Train([[1.0f, 2.0f]], numberOfClusters: 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static List<float[]> GenerateSamples()
    {
        var rng = new Random(42);
        var samples = new List<float[]>();

        // 3 distinct clusters
        for (var i = 0; i < 30; i++)
        {
            samples.Add([100.0f + (float)(rng.NextDouble() * 10), 150.0f + (float)(rng.NextDouble() * 10)]);
            samples.Add([30.0f + (float)(rng.NextDouble() * 10), 20.0f + (float)(rng.NextDouble() * 10)]);
            samples.Add([70.0f + (float)(rng.NextDouble() * 10), 85.0f + (float)(rng.NextDouble() * 10)]);
        }

        return samples;
    }
}
