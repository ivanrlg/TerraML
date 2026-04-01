using FluentAssertions;
using FuzzySat.Core.FuzzyLogic.Classification;
using FuzzySat.Core.FuzzyLogic.Inference;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Rules;

namespace FuzzySat.Core.Tests.FuzzyLogic.Classification;

public class FuzzyClassifierTests
{
    [Fact]
    public void ClassifyPixel_ReturnsCorrectClass()
    {
        var classifier = MakeThreeClassClassifier();

        // Pixel at Urban center
        var result = classifier.ClassifyPixel(new Dictionary<string, double>
        {
            ["B1"] = 100.0,
            ["B2"] = 150.0
        });

        result.Should().Be("Urban");
    }

    [Fact]
    public void ClassifyPixel_DifferentPixel_ReturnsDifferentClass()
    {
        var classifier = MakeThreeClassClassifier();

        // Pixel at Water center
        var result = classifier.ClassifyPixel(new Dictionary<string, double>
        {
            ["B1"] = 30.0,
            ["B2"] = 20.0
        });

        result.Should().Be("Water");
    }

    [Fact]
    public void ClassifyPixel_Deterministic_SameInputSameOutput()
    {
        var classifier = MakeThreeClassClassifier();
        var pixel = new Dictionary<string, double> { ["B1"] = 72.0, ["B2"] = 88.0 };

        var result1 = classifier.ClassifyPixel(pixel);
        var result2 = classifier.ClassifyPixel(pixel);

        result1.Should().Be(result2);
    }

    [Fact]
    public void ClassifyPixel_NullBandValues_ThrowsArgumentNullException()
    {
        var classifier = MakeThreeClassClassifier();

        var act = () => classifier.ClassifyPixel(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullEngine_ThrowsArgumentNullException()
    {
        var act = () => new FuzzyClassifier(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_DefaultDefuzzifier_Works()
    {
        var engine = MakeEngine();

        // No defuzzifier specified — should use MaxWeightDefuzzifier
        var classifier = new FuzzyClassifier(engine);
        var result = classifier.ClassifyPixel(new Dictionary<string, double>
        {
            ["B1"] = 100.0,
            ["B2"] = 150.0
        });

        result.Should().Be("Urban");
    }

    [Fact]
    public void ClassifyPixel_FourBands_ThesisScenario()
    {
        var rules = new FuzzyRuleSet([
            new FuzzyRule("Urban", new Dictionary<string, IMembershipFunction>
            {
                ["VNIR1"] = new GaussianMembershipFunction("U_V1", 130.0, 18.0),
                ["VNIR2"] = new GaussianMembershipFunction("U_V2", 110.0, 15.0),
                ["SWIR1"] = new GaussianMembershipFunction("U_S1", 160.0, 22.0),
                ["SWIR2"] = new GaussianMembershipFunction("U_S2", 140.0, 20.0)
            }),
            new FuzzyRule("Water", new Dictionary<string, IMembershipFunction>
            {
                ["VNIR1"] = new GaussianMembershipFunction("W_V1", 25.0, 8.0),
                ["VNIR2"] = new GaussianMembershipFunction("W_V2", 15.0, 6.0),
                ["SWIR1"] = new GaussianMembershipFunction("W_S1", 10.0, 5.0),
                ["SWIR2"] = new GaussianMembershipFunction("W_S2", 8.0, 4.0)
            }),
            new FuzzyRule("Forest", new Dictionary<string, IMembershipFunction>
            {
                ["VNIR1"] = new GaussianMembershipFunction("F_V1", 75.0, 12.0),
                ["VNIR2"] = new GaussianMembershipFunction("F_V2", 95.0, 14.0),
                ["SWIR1"] = new GaussianMembershipFunction("F_S1", 85.0, 16.0),
                ["SWIR2"] = new GaussianMembershipFunction("F_S2", 70.0, 13.0)
            })
        ]);

        var classifier = new FuzzyClassifier(new FuzzyInferenceEngine(rules));

        classifier.ClassifyPixel(new Dictionary<string, double>
        {
            ["VNIR1"] = 128.0,
            ["VNIR2"] = 112.0,
            ["SWIR1"] = 158.0,
            ["SWIR2"] = 138.0
        }).Should().Be("Urban");

        classifier.ClassifyPixel(new Dictionary<string, double>
        {
            ["VNIR1"] = 24.0,
            ["VNIR2"] = 14.0,
            ["SWIR1"] = 11.0,
            ["SWIR2"] = 9.0
        }).Should().Be("Water");

        classifier.ClassifyPixel(new Dictionary<string, double>
        {
            ["VNIR1"] = 76.0,
            ["VNIR2"] = 93.0,
            ["SWIR1"] = 84.0,
            ["SWIR2"] = 71.0
        }).Should().Be("Forest");
    }

    private static FuzzyClassifier MakeThreeClassClassifier()
    {
        return new FuzzyClassifier(MakeEngine());
    }

    private static FuzzyInferenceEngine MakeEngine()
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
            }),
            new FuzzyRule("Forest", new Dictionary<string, IMembershipFunction>
            {
                ["B1"] = new GaussianMembershipFunction("F_B1", 70.0, 12.0),
                ["B2"] = new GaussianMembershipFunction("F_B2", 85.0, 15.0)
            })
        ]);
        return new FuzzyInferenceEngine(rules);
    }
}
