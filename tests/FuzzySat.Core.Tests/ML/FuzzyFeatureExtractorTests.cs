using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

public class FuzzyFeatureExtractorTests
{
    [Fact]
    public void FeatureNames_ContainsRawAndMembershipAndStrength()
    {
        var extractor = MakeExtractor();

        // 2 raw bands + 2 classes * 2 bands MF + 2 strengths = 2 + 4 + 2 = 8
        extractor.FeatureNames.Should().HaveCount(8);
        extractor.FeatureNames.Should().Contain("Raw_B1");
        extractor.FeatureNames.Should().Contain("MF_Urban_B1");
        extractor.FeatureNames.Should().Contain("Strength_Urban");
    }

    [Fact]
    public void ExtractFeatures_ReturnsCorrectLength()
    {
        var extractor = MakeExtractor();
        var features = extractor.ExtractFeatures(new Dictionary<string, double>
        {
            ["B1"] = 100.0,
            ["B2"] = 150.0
        });

        features.Should().HaveCount(8);
    }

    [Fact]
    public void ExtractFeatures_RawValuesFirst()
    {
        var extractor = MakeExtractor();
        var features = extractor.ExtractFeatures(new Dictionary<string, double>
        {
            ["B1"] = 100.0,
            ["B2"] = 150.0
        });

        features[0].Should().BeApproximately(100.0f, 0.001f); // Raw_B1
        features[1].Should().BeApproximately(150.0f, 0.001f); // Raw_B2
    }

    [Fact]
    public void ExtractFeatures_AtCenter_MembershipIsOne()
    {
        var extractor = MakeExtractor();
        // Pixel at Urban center: B1=100, B2=150
        var features = extractor.ExtractFeatures(new Dictionary<string, double>
        {
            ["B1"] = 100.0,
            ["B2"] = 150.0
        });

        // MF_Urban_B1 should be ~1.0 (at center)
        features[2].Should().BeApproximately(1.0f, 0.01f);
        // MF_Urban_B2 should be ~1.0 (at center)
        features[3].Should().BeApproximately(1.0f, 0.01f);
    }

    [Fact]
    public void ExtractFeatures_AllValuesFinite()
    {
        var extractor = MakeExtractor();
        var features = extractor.ExtractFeatures(new Dictionary<string, double>
        {
            ["B1"] = 75.0,
            ["B2"] = 80.0
        });

        features.Should().AllSatisfy(f => float.IsFinite(f).Should().BeTrue());
    }

    private static FuzzyFeatureExtractor MakeExtractor()
    {
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

        return new FuzzyFeatureExtractor(rules, ["B1", "B2"]);
    }
}
