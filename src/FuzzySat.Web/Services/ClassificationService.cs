using FuzzySat.Core.Classification;
using FuzzySat.Core.FuzzyLogic.Defuzzification;
using FuzzySat.Core.FuzzyLogic.Inference;
using FuzzySat.Core.FuzzyLogic.MembershipFunctions;
using FuzzySat.Core.FuzzyLogic.Operators;
using FuzzySat.Core.FuzzyLogic.Rules;
using FuzzySat.Core.Raster;
using FuzzySat.Core.Training;

namespace FuzzySat.Web.Services;

/// <summary>
/// Bridges Core's FuzzyClassifier to async Web operations with progress reporting.
/// Supports multiple MF types, AND operators, and defuzzifiers.
/// Registered as singleton since it holds no mutable state.
/// </summary>
public sealed class ClassificationService
{
    /// <summary>
    /// Classifies a multispectral image using fuzzy inference with progress reporting.
    /// Must be called from a background thread (via Task.Run) to avoid blocking Blazor UI.
    /// </summary>
    public ClassificationResult Classify(
        MultispectralImage image,
        TrainingSession session,
        ClassificationOptions options,
        IProgress<ClassificationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(options);

        // Stage 1: Build rule set
        progress?.Report(new ClassificationProgress("Building membership functions", 0, image.Rows, 0));
        var ruleSet = BuildRuleSet(session, options.MembershipFunctionType);

        // Stage 2: Create inference engine and defuzzifier
        progress?.Report(new ClassificationProgress("Initializing inference engine", 0, image.Rows, 5));
        var engine = new FuzzyInferenceEngine(ruleSet);
        var defuzzifier = CreateDefuzzifier(options.DefuzzifierType);
        var useProductAnd = options.AndOperator == "Product";

        // Stage 3: Classify pixel-by-pixel with progress
        var classMap = new string[image.Rows, image.Columns];
        var confidenceMap = new double[image.Rows, image.Columns];

        for (var row = 0; row < image.Rows; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var col = 0; col < image.Columns; col++)
            {
                var pixel = image.GetPixelVector(row, col);
                var bandValues = (IDictionary<string, double>)pixel.BandValues;

                InferenceResult result;
                if (useProductAnd)
                {
                    result = InferWithProductAnd(ruleSet, bandValues);
                }
                else
                {
                    result = engine.Infer(bandValues);
                }

                classMap[row, col] = defuzzifier.Defuzzify(result);
                confidenceMap[row, col] = result.WinnerStrength;
            }

            // Report progress every 10 rows to avoid excessive UI updates
            if (row % 10 == 0 || row == image.Rows - 1)
            {
                var pct = 10.0 + (row + 1.0) / image.Rows * 85.0; // 10-95%
                progress?.Report(new ClassificationProgress(
                    "Classifying pixels", row + 1, image.Rows, pct));
            }
        }

        // Stage 4: Build result
        progress?.Report(new ClassificationProgress("Building result", image.Rows, image.Rows, 98));

        var classes = session.ClassNames
            .Select((name, i) => new LandCoverClass { Name = name, Code = i + 1 })
            .ToList();

        var classificationResult = new ClassificationResult(classMap, confidenceMap, classes);
        progress?.Report(new ClassificationProgress("Complete", image.Rows, image.Rows, 100));

        return classificationResult;
    }

    /// <summary>
    /// Classifies a single pixel's band values, returning the predicted class name.
    /// Used by ValidationService for per-sample classification.
    /// </summary>
    internal string ClassifyPixel(
        FuzzyRuleSet ruleSet,
        IDefuzzifier defuzzifier,
        bool useProductAnd,
        FuzzyInferenceEngine engine,
        IDictionary<string, double> bandValues)
    {
        var result = useProductAnd
            ? InferWithProductAnd(ruleSet, bandValues)
            : engine.Infer(bandValues);
        return defuzzifier.Defuzzify(result);
    }

    /// <summary>
    /// Evaluates all rules using Product AND and constructs an InferenceResult
    /// so the selected defuzzifier is properly applied.
    /// </summary>
    private static InferenceResult InferWithProductAnd(
        FuzzyRuleSet ruleSet, IDictionary<string, double> bandValues)
    {
        var strengths = new List<KeyValuePair<string, double>>(ruleSet.Rules.Count);
        foreach (var rule in ruleSet.Rules)
        {
            var degrees = rule.BandMembershipFunctions
                .Select(kvp => kvp.Value.Evaluate(bandValues[kvp.Key]));
            var strength = FuzzyOperators.ProductAnd(degrees);
            strengths.Add(new KeyValuePair<string, double>(rule.ClassName, strength));
        }
        return new InferenceResult(strengths);
    }

    /// <summary>
    /// Builds a FuzzyRuleSet supporting all 4 MF types.
    /// Converts mean/stddev from training statistics to the appropriate MF parameters.
    /// </summary>
    internal static FuzzyRuleSet BuildRuleSet(TrainingSession session, string mfType)
    {
        var rules = new List<FuzzyRule>();

        foreach (var className in session.ClassNames)
        {
            var stats = session.Statistics[className];
            var bandMFs = new Dictionary<string, IMembershipFunction>();

            foreach (var bandName in session.BandNames)
            {
                var mean = stats.MeanPerBand[bandName];
                var stddev = stats.StdDevPerBand[bandName];

                // Ensure stddev is positive (avoid degenerate MFs)
                if (stddev < 1e-10) stddev = 1e-10;

                var mfName = $"{className}_{bandName}";

                bandMFs[bandName] = mfType switch
                {
                    "Gaussian" => new GaussianMembershipFunction(mfName, mean, stddev),
                    "Triangular" => new TriangularMembershipFunction(
                        mfName,
                        left: mean - 2.0 * stddev,
                        center: mean,
                        right: mean + 2.0 * stddev),
                    "Trapezoidal" => new TrapezoidalMembershipFunction(
                        mfName,
                        a: mean - 2.5 * stddev,
                        b: mean - 0.5 * stddev,
                        c: mean + 0.5 * stddev,
                        d: mean + 2.5 * stddev),
                    "Bell" => new BellMembershipFunction(mfName, mean, stddev, slope: 2.0),
                    _ => throw new ArgumentException($"Unknown MF type: '{mfType}'.", nameof(mfType))
                };
            }

            rules.Add(new FuzzyRule(className, bandMFs));
        }

        return new FuzzyRuleSet(rules);
    }

    internal static IDefuzzifier CreateDefuzzifier(string type) => type switch
    {
        "Max Weight" => new MaxWeightDefuzzifier(),
        "Weighted Average" => new WeightedAverageDefuzzifier(),
        _ => throw new ArgumentException($"Unknown defuzzifier type: '{type}'.", nameof(type))
    };
}

/// <summary>
/// Options for configuring the classification pipeline.
/// </summary>
public record ClassificationOptions(
    string ClassificationMethod = "Pure Fuzzy",
    string MembershipFunctionType = "Gaussian",
    string AndOperator = "Minimum",
    string DefuzzifierType = "Max Weight");

/// <summary>
/// Progress information during classification.
/// </summary>
public record ClassificationProgress(
    string Stage,
    int CurrentRow,
    int TotalRows,
    double Percentage);
