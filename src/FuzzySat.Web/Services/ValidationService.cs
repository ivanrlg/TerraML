using System.Globalization;
using System.Text;
using FuzzySat.Core.FuzzyLogic.Classification;
using FuzzySat.Core.FuzzyLogic.Inference;
using FuzzySat.Core.Training;
using FuzzySat.Core.Validation;

namespace FuzzySat.Web.Services;

/// <summary>
/// Service for validation operations: classify validation samples,
/// build confusion matrix, and export results as CSV.
/// Registered as singleton since it holds no mutable state.
/// </summary>
public sealed class ValidationService
{
    /// <summary>
    /// Classifies validation samples using the trained session and compares
    /// predicted vs actual labels to produce a confusion matrix and metrics.
    /// </summary>
    public (ConfusionMatrix Matrix, AccuracyMetrics Metrics) ValidateFromSamples(
        IReadOnlyList<LabeledPixelSample> validationSamples,
        TrainingSession session,
        ClassificationOptions options)
    {
        ArgumentNullException.ThrowIfNull(validationSamples);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(options);

        if (validationSamples.Count == 0)
            throw new ArgumentException("At least one validation sample is required.", nameof(validationSamples));

        // Build classifier from training session
        var ruleSet = ClassificationService.BuildRuleSet(
            session, options.MembershipFunctionType, options.AndOperator);
        var engine = new FuzzyInferenceEngine(ruleSet);
        var defuzzifier = options.DefuzzifierType == "Weighted Average"
            ? (Core.FuzzyLogic.Defuzzification.IDefuzzifier)new Core.FuzzyLogic.Defuzzification.WeightedAverageDefuzzifier()
            : new Core.FuzzyLogic.Defuzzification.MaxWeightDefuzzifier();
        var classifier = new FuzzyClassifier(engine, defuzzifier);

        // Classify each validation sample
        var actual = new List<string>(validationSamples.Count);
        var predicted = new List<string>(validationSamples.Count);

        foreach (var sample in validationSamples)
        {
            actual.Add(sample.ClassName);
            predicted.Add(classifier.ClassifyPixel(
                new Dictionary<string, double>(sample.BandValues)));
        }

        var matrix = new ConfusionMatrix(actual, predicted);
        var metrics = new AccuracyMetrics(matrix);

        return (matrix, metrics);
    }

    /// <summary>
    /// Exports a confusion matrix to CSV format with OA and Kappa in footer.
    /// </summary>
    public string ExportConfusionMatrixCsv(ConfusionMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var sb = new StringBuilder();
        var classes = matrix.ClassNames;

        // Header: empty cell + class names
        sb.Append("Actual\\Predicted");
        foreach (var cls in classes)
            sb.Append(',').Append(cls);
        sb.AppendLine(",Total");

        // Matrix rows
        for (var i = 0; i < classes.Count; i++)
        {
            sb.Append(classes[i]);
            for (var j = 0; j < classes.Count; j++)
                sb.Append(',').Append(matrix[classes[i], classes[j]]);
            sb.Append(',').Append(matrix.RowTotal(classes[i]));
            sb.AppendLine();
        }

        // Column totals row
        sb.Append("Total");
        for (var j = 0; j < classes.Count; j++)
            sb.Append(',').Append(matrix.ColumnTotal(classes[j]));
        sb.Append(',').Append(matrix.TotalSamples);
        sb.AppendLine();

        // Footer metrics
        sb.AppendLine();
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Overall Accuracy,{matrix.OverallAccuracy:F4}"));
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Kappa Coefficient,{matrix.KappaCoefficient:F4}"));
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Total Samples,{matrix.TotalSamples}"));
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Correct Count,{matrix.CorrectCount}"));

        return sb.ToString();
    }
}
