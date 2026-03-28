using FluentAssertions;
using FuzzySat.Core.Classification;
using FuzzySat.Core.FuzzyLogic.Classification;
using FuzzySat.Core.FuzzyLogic.Inference;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;
using FuzzySat.Core.Raster;

namespace FuzzySat.Core.Tests.Classification;

public class ClassificationResultTests
{
    [Fact]
    public void LandCoverClass_StoresProperties()
    {
        var lc = new LandCoverClass { Name = "Urban", Code = 1, Color = "#FF0000" };

        lc.Name.Should().Be("Urban");
        lc.Code.Should().Be(1);
        lc.Color.Should().Be("#FF0000");
    }

    [Fact]
    public void ClassificationResult_StoresClassAndConfidence()
    {
        var classMap = new string[,] { { "Urban", "Water" }, { "Forest", "Urban" } };
        var confMap = new double[,] { { 0.9, 0.8 }, { 0.7, 0.95 } };
        var classes = new[] { new LandCoverClass { Name = "Urban", Code = 1 } };

        var result = new ClassificationResult(classMap, confMap, classes);

        result.Rows.Should().Be(2);
        result.Columns.Should().Be(2);
        result.GetClass(0, 0).Should().Be("Urban");
        result.GetClass(1, 0).Should().Be("Forest");
        result.GetConfidence(0, 0).Should().Be(0.9);
    }

    [Fact]
    public void ClassificationResult_DefensiveCopy()
    {
        var classMap = new string[,] { { "Urban" } };
        var confMap = new double[,] { { 0.9 } };
        var result = new ClassificationResult(classMap, confMap, []);

        classMap[0, 0] = "Water";
        confMap[0, 0] = 0.1;

        result.GetClass(0, 0).Should().Be("Urban");
        result.GetConfidence(0, 0).Should().Be(0.9);
    }

    [Fact]
    public void ClassificationResult_MismatchedDimensions_Throws()
    {
        var act = () => new ClassificationResult(
            new string[2, 2], new double[3, 2], []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ClassifyImage_EndToEnd()
    {
        // 2x2 image, 2 bands, 2 classes
        var b1 = new Band("B1", new double[,] { { 100, 30 }, { 100, 30 } });
        var b2 = new Band("B2", new double[,] { { 150, 20 }, { 150, 20 } });
        var image = new MultispectralImage([b1, b2]);

        var rules = new FuzzyRuleSet([
            new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
            {
                ["B1"] = new GaussianMembershipFunction("U_B1", 100.0, 10.0),
                ["B2"] = new GaussianMembershipFunction("U_B2", 150.0, 10.0)
            }),
            new FuzzyRule("Water", new Dictionary<string, IMembershipFunction>
            {
                ["B1"] = new GaussianMembershipFunction("W_B1", 30.0, 8.0),
                ["B2"] = new GaussianMembershipFunction("W_B2", 20.0, 8.0)
            })
        ]);

        var engine = new FuzzyInferenceEngine(rules);
        var classifier = new FuzzyClassifier(engine);
        var classes = new[]
        {
            new LandCoverClass { Name = "Urban", Code = 1 },
            new LandCoverClass { Name = "Water", Code = 2 }
        };

        var result = ClassificationResult.ClassifyImage(image, classifier, engine, classes);

        result.Rows.Should().Be(2);
        result.Columns.Should().Be(2);
        result.GetClass(0, 0).Should().Be("Urban");
        result.GetClass(0, 1).Should().Be("Water");
        result.GetConfidence(0, 0).Should().BeGreaterThan(0.5);
        result.GetConfidence(0, 1).Should().BeGreaterThan(0.5);
    }
}
