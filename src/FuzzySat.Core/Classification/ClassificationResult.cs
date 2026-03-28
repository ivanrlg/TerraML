using FuzzySat.Core.Raster;

namespace FuzzySat.Core.Classification;

/// <summary>
/// The result of classifying a multispectral image: a 2D grid of class labels
/// with per-pixel confidence (firing strength of the winning class).
/// </summary>
public sealed class ClassificationResult
{
    private readonly string[,] _classMap;
    private readonly double[,] _confidenceMap;

    /// <summary>Gets the number of rows.</summary>
    public int Rows { get; }

    /// <summary>Gets the number of columns.</summary>
    public int Columns { get; }

    /// <summary>Gets the land cover classes used in classification.</summary>
    public IReadOnlyList<LandCoverClass> Classes { get; }

    /// <summary>
    /// Creates a classification result from class and confidence maps.
    /// </summary>
    public ClassificationResult(
        string[,] classMap,
        double[,] confidenceMap,
        IEnumerable<LandCoverClass> classes)
    {
        ArgumentNullException.ThrowIfNull(classMap);
        ArgumentNullException.ThrowIfNull(confidenceMap);
        ArgumentNullException.ThrowIfNull(classes);

        if (classMap.GetLength(0) != confidenceMap.GetLength(0) ||
            classMap.GetLength(1) != confidenceMap.GetLength(1))
            throw new ArgumentException("Class map and confidence map must have the same dimensions.");

        Rows = classMap.GetLength(0);
        Columns = classMap.GetLength(1);
        _classMap = (string[,])classMap.Clone();
        _confidenceMap = (double[,])confidenceMap.Clone();
        Classes = classes.ToList().AsReadOnly();
    }

    /// <summary>Gets the predicted class name at [row, col].</summary>
    public string GetClass(int row, int col) => _classMap[row, col];

    /// <summary>Gets the confidence (firing strength) at [row, col].</summary>
    public double GetConfidence(int row, int col) => _confidenceMap[row, col];

    /// <summary>
    /// Classifies an entire multispectral image using the provided classifier.
    /// </summary>
    public static ClassificationResult ClassifyImage(
        MultispectralImage image,
        FuzzyLogic.Classification.IClassifier classifier,
        FuzzyLogic.Inference.IInferenceEngine engine,
        IReadOnlyList<LandCoverClass> classes)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(classifier);
        ArgumentNullException.ThrowIfNull(engine);

        var classMap = new string[image.Rows, image.Columns];
        var confidenceMap = new double[image.Rows, image.Columns];

        for (var row = 0; row < image.Rows; row++)
        {
            for (var col = 0; col < image.Columns; col++)
            {
                var pixel = image.GetPixelVector(row, col);
                var bandValues = (IDictionary<string, double>)pixel.BandValues;
                classMap[row, col] = classifier.ClassifyPixel(bandValues);

                var result = engine.Infer(bandValues);
                confidenceMap[row, col] = result.WinnerStrength;
            }
        }

        return new ClassificationResult(classMap, confidenceMap, classes);
    }
}
