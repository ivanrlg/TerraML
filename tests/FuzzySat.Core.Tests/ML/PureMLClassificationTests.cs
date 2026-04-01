using FluentAssertions;
using FuzzySat.Core.ML;

namespace FuzzySat.Core.Tests.ML;

/// <summary>
/// Verifies that each ML classifier works correctly with RawFeatureExtractor
/// (raw band values only, no fuzzy membership functions).
/// </summary>
public class PureMLClassificationTests
{
    [Fact]
    public void TrainAndClassify_RandomForest_WithRawExtractor()
    {
        var (extractor, samples) = MakeTrainingData();

        var classifier = HybridClassifier.TrainRandomForest(samples, extractor, numberOfTrees: 50);

        classifier.ClassifyPixel(UrbanPixel()).Should().Be("Urban");
        classifier.ClassifyPixel(WaterPixel()).Should().Be("Water");
    }

    [Fact]
    public void TrainAndClassify_Sdca_WithRawExtractor()
    {
        var (extractor, samples) = MakeTrainingData();

        var classifier = HybridClassifier.TrainSdca(samples, extractor);

        classifier.ClassifyPixel(UrbanPixel()).Should().Be("Urban");
        classifier.ClassifyPixel(WaterPixel()).Should().Be("Water");
    }

    [Fact]
    public void TrainAndClassify_LightGbm_WithRawExtractor()
    {
        var (extractor, samples) = MakeTrainingData();

        var classifier = LightGbmClassifier.Train(samples, extractor);

        classifier.ClassifyPixel(UrbanPixel()).Should().Be("Urban");
        classifier.ClassifyPixel(WaterPixel()).Should().Be("Water");
    }

    [Fact]
    public void TrainAndClassify_Svm_WithRawExtractor()
    {
        var (extractor, samples) = MakeTrainingData();

        var classifier = SvmClassifier.Train(samples, extractor);

        classifier.ClassifyPixel(UrbanPixel()).Should().Be("Urban");
        classifier.ClassifyPixel(WaterPixel()).Should().Be("Water");
    }

    [Fact]
    public void TrainAndClassify_LogisticRegression_WithRawExtractor()
    {
        var (extractor, samples) = MakeTrainingData();

        var classifier = LogisticRegressionClassifier.Train(samples, extractor);

        classifier.ClassifyPixel(UrbanPixel()).Should().Be("Urban");
        classifier.ClassifyPixel(WaterPixel()).Should().Be("Water");
    }

    [Fact]
    public void TrainAndClassify_NeuralNet_WithRawExtractor()
    {
        var (extractor, samples) = MakeTrainingData();
        var options = new NeuralNetTrainingOptions { MaxEpochs = 50, PatienceEpochs = 10 };

        using var classifier = NeuralNetClassifier.Train(samples, extractor, options);

        classifier.ClassifyPixel(UrbanPixel()).Should().Be("Urban");
        classifier.ClassifyPixel(WaterPixel()).Should().Be("Water");
    }

    private static IDictionary<string, double> UrbanPixel() =>
        new Dictionary<string, double> { ["B1"] = 100.0, ["B2"] = 150.0 };

    private static IDictionary<string, double> WaterPixel() =>
        new Dictionary<string, double> { ["B1"] = 30.0, ["B2"] = 20.0 };

    private static (RawFeatureExtractor, List<(string, IDictionary<string, double>)>) MakeTrainingData()
    {
        var extractor = new RawFeatureExtractor(["B1", "B2"]);
        var rng = new Random(42);
        var samples = new List<(string Label, IDictionary<string, double> BandValues)>();
        for (var i = 0; i < 50; i++)
        {
            samples.Add(("Urban", new Dictionary<string, double>
            {
                ["B1"] = 100.0 + rng.NextDouble() * 20 - 10,
                ["B2"] = 150.0 + rng.NextDouble() * 20 - 10
            }));
            samples.Add(("Water", new Dictionary<string, double>
            {
                ["B1"] = 30.0 + rng.NextDouble() * 16 - 8,
                ["B2"] = 20.0 + rng.NextDouble() * 16 - 8
            }));
        }
        return (extractor, samples);
    }
}
