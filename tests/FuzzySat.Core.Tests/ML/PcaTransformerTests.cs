using FluentAssertions;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class PcaTransformerTests
{
    [Fact]
    public void Fit_ReducesDimensionality()
    {
        // 4D data → 2 components
        var data = GenerateSyntheticData(dimension: 4, count: 50);
        var pca = PcaTransformer.Fit(data, rank: 2);

        pca.Rank.Should().Be(2);
        pca.OriginalDimension.Should().Be(4);
    }

    [Fact]
    public void Transform_ReturnsCorrectDimension()
    {
        var data = GenerateSyntheticData(dimension: 4, count: 50);
        var pca = PcaTransformer.Fit(data, rank: 2);

        var result = pca.Transform(data[0]);

        result.Length.Should().Be(2);
    }

    [Fact]
    public void TransformBatch_ReturnsAllVectors()
    {
        var data = GenerateSyntheticData(dimension: 4, count: 30);
        var pca = PcaTransformer.Fit(data, rank: 2);

        var results = pca.TransformBatch(data);

        results.Count.Should().Be(30);
        results.All(r => r.Length == 2).Should().BeTrue();
    }

    [Fact]
    public void Transform_ProducesFiniteValues()
    {
        var data = GenerateSyntheticData(dimension: 6, count: 40);
        var pca = PcaTransformer.Fit(data, rank: 3);

        var result = pca.Transform(data[0]);

        result.All(v => float.IsFinite(v)).Should().BeTrue();
    }

    [Fact]
    public void Fit_WithRankEqualToDimension_ReturnsCorrectDimension()
    {
        var data = GenerateSyntheticData(dimension: 3, count: 20);
        var pca = PcaTransformer.Fit(data, rank: 3);

        var result = pca.Transform(data[0]);
        result.Length.Should().Be(3);
    }

    [Fact]
    public void Fit_ThrowsWhenRankExceedsDimension()
    {
        var data = GenerateSyntheticData(dimension: 3, count: 20);

        var act = () => PcaTransformer.Fit(data, rank: 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Fit_ThrowsWhenEmpty()
    {
        var act = () => PcaTransformer.Fit([], rank: 2);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Transform_ThrowsOnWrongDimension()
    {
        var data = GenerateSyntheticData(dimension: 4, count: 20);
        var pca = PcaTransformer.Fit(data, rank: 2);

        var act = () => pca.Transform(new float[] { 1f, 2f }); // wrong dimension

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Fit_ThrowsOnInconsistentVectorLengths()
    {
        var data = new List<float[]>
        {
            new float[] { 1f, 2f, 3f },
            new float[] { 4f, 5f }  // wrong length
        };

        var act = () => PcaTransformer.Fit(data, rank: 2);

        act.Should().Throw<ArgumentException>();
    }

    private static List<float[]> GenerateSyntheticData(int dimension, int count)
    {
        var rng = new Random(42);
        var data = new List<float[]>(count);
        for (var i = 0; i < count; i++)
        {
            var vector = new float[dimension];
            for (var j = 0; j < dimension; j++)
                vector[j] = (float)(rng.NextDouble() * 100 + j * 10);
            data.Add(vector);
        }
        return data;
    }
}
