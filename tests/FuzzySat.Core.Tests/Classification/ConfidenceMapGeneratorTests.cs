using FluentAssertions;
using FuzzySat.Core.Classification;

namespace FuzzySat.Core.Tests.Classification;

public class ConfidenceMapGeneratorTests
{
    [Fact]
    public void ExtractConfidenceBand_CorrectValues()
    {
        var result = MakeResult();

        var band = ConfidenceMapGenerator.ExtractConfidenceBand(result);

        band.Name.Should().Be("Confidence");
        band.Rows.Should().Be(2);
        band.Columns.Should().Be(2);
        band[0, 0].Should().Be(0.9);
        band[1, 1].Should().Be(0.95);
    }

    [Fact]
    public void ExtractClassCodeBand_CorrectCodes()
    {
        var result = MakeResult();

        var band = ConfidenceMapGenerator.ExtractClassCodeBand(result);

        band.Name.Should().Be("ClassCode");
        band[0, 0].Should().Be(1); // Urban
        band[0, 1].Should().Be(2); // Water
        band[1, 0].Should().Be(3); // Forest
    }

    [Fact]
    public void ExtractConfidenceBand_NullResult_Throws()
    {
        var act = () => ConfidenceMapGenerator.ExtractConfidenceBand(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    private static ClassificationResult MakeResult()
    {
        var classMap = new string[,] { { "Urban", "Water" }, { "Forest", "Urban" } };
        var confMap = new double[,] { { 0.9, 0.8 }, { 0.7, 0.95 } };
        var classes = new[]
        {
            new LandCoverClass { Name = "Urban", Code = 1 },
            new LandCoverClass { Name = "Water", Code = 2 },
            new LandCoverClass { Name = "Forest", Code = 3 }
        };
        return new ClassificationResult(classMap, confMap, classes);
    }
}
