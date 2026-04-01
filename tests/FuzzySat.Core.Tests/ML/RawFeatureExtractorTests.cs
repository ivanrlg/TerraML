using FluentAssertions;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class RawFeatureExtractorTests
{
    [Fact]
    public void FeatureNames_ReturnsCorrectList()
    {
        var names = new[] { "A", "B", "C" };
        var extractor = new RawFeatureExtractor(names);

        extractor.FeatureNames.Should().BeEquivalentTo(names, o => o.WithStrictOrdering());
    }

    [Fact]
    public void ExtractFeatures_PassesThroughValues()
    {
        var extractor = new RawFeatureExtractor(["X", "Y"]);
        var bandValues = new Dictionary<string, double> { ["X"] = 1.0, ["Y"] = 0.0 };

        var features = extractor.ExtractFeatures(bandValues);

        features.Should().HaveCount(2);
        features[0].Should().Be(1.0f);
        features[1].Should().Be(0.0f);
    }

    [Fact]
    public void ExtractFeatures_PreservesOrder()
    {
        var extractor = new RawFeatureExtractor(["C", "A", "B"]);
        var bandValues = new Dictionary<string, double> { ["A"] = 10.0, ["B"] = 20.0, ["C"] = 30.0 };

        var features = extractor.ExtractFeatures(bandValues);

        features[0].Should().Be(30.0f); // C
        features[1].Should().Be(10.0f); // A
        features[2].Should().Be(20.0f); // B
    }

    [Fact]
    public void ExtractFeatures_NullBandValues_Throws()
    {
        var extractor = new RawFeatureExtractor(["X"]);

        var act = () => extractor.ExtractFeatures(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullFeatureNames_Throws()
    {
        var act = () => new RawFeatureExtractor(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractFeatures_MissingKey_ThrowsWithBandName()
    {
        var extractor = new RawFeatureExtractor(["B1", "B2"]);
        var bandValues = new Dictionary<string, double> { ["B1"] = 1.0 }; // B2 missing

        var act = () => extractor.ExtractFeatures(bandValues);

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*B2*");
    }

    [Fact]
    public void Constructor_EmptyNames_Works()
    {
        var extractor = new RawFeatureExtractor([]);

        extractor.FeatureNames.Should().BeEmpty();
        extractor.ExtractFeatures(new Dictionary<string, double>()).Should().BeEmpty();
    }
}
