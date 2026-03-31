using FuzzySat.Core.Classification;
using FuzzySat.Core.FuzzyLogic.Classification;
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
        var ruleSet = BuildRuleSet(session, options.MembershipFunctionType, options.AndOperator);

        // Stage 2: Create inference engine
        progress?.Report(new ClassificationProgress("Initializing inference engine", 0, image.Rows, 5));
        var engine = new FuzzyInferenceEngine(ruleSet);
        var defuzzifier = CreateDefuzzifier(options.DefuzzifierType);

        // Stage 3: Classify pixel-by-pixel with progress
        var classMap = new string[image.Rows, image.Columns];
        var confidenceMap = new double[image.Rows, image.Columns];
        var useProductAnd = options.AndOperator == "Product";

        for (var row = 0; row < image.Rows; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var col = 0; col < image.Columns; col++)
            {
                var pixel = image.GetPixelVector(row, col);
                var bandValues = (IDictionary<string, double>)pixel.BandValues;

                if (useProductAnd)
                {
                    // Custom evaluation using ProductAnd instead of default Min AND
                    var bestClass = "";
                    var bestStrength = -1.0;
                    foreach (var rule in ruleSet.Rules)
                    {
                        var degrees = rule.BandMembershipFunctions
                            .Select(kvp => kvp.Value.Evaluate(bandValues[kvp.Key]));
                        var strength = FuzzyOperators.ProductAnd(degrees);

                        if (strength > bestStrength)
                        {
                            bestStrength = strength;
                            bestClass = rule.ClassName;
                        }
                    }
                    classMap[row, col] = bestClass;
                    confidenceMap[row, col] = bestStrength;
                }
                else
                {
                    // Standard evaluation using Min AND (via inference engine)
                    var result = engine.Infer(bandValues);
                    classMap[row, col] = defuzzifier.Defuzzify(result);
                    confidenceMap[row, col] = result.WinnerStrength;
                }
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

        var result2 = new ClassificationResult(classMap, confidenceMap, classes);
        progress?.Report(new ClassificationProgress("Complete", image.Rows, image.Rows, 100));

        return result2;
    }

    /// <summary>
    /// Builds a FuzzyRuleSet supporting all 4 MF types.
    /// Converts mean/stddev from training statistics to the appropriate MF parameters.
    /// </summary>
    internal static FuzzyRuleSet BuildRuleSet(TrainingSession session, string mfType, string andOperator)
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

    private static IDefuzzifier CreateDefuzzifier(string type) => type switch
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
