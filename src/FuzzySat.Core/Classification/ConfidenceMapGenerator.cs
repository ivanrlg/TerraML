using FuzzySat.Core.Raster;

namespace FuzzySat.Core.Classification;

/// <summary>
/// Generates confidence-related derived products from classification results.
/// </summary>
public static class ConfidenceMapGenerator
{
    /// <summary>
    /// Extracts the confidence map (winner firing strength) as a Band.
    /// Values in [0, 1] where higher = more confident classification.
    /// </summary>
    public static Band ExtractConfidenceBand(ClassificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var data = new double[result.Rows, result.Columns];
        for (var row = 0; row < result.Rows; row++)
            for (var col = 0; col < result.Columns; col++)
                data[row, col] = result.GetConfidence(row, col);

        return new Band("Confidence", data);
    }

    /// <summary>
    /// Extracts the class code map as a Band (integer codes as doubles).
    /// </summary>
    public static Band ExtractClassCodeBand(ClassificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var classCodeMap = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var lc in result.Classes)
            classCodeMap[lc.Name] = lc.Code;

        var data = new double[result.Rows, result.Columns];
        for (var row = 0; row < result.Rows; row++)
            for (var col = 0; col < result.Columns; col++)
            {
                var className = result.GetClass(row, col);
                if (!classCodeMap.TryGetValue(className, out var code))
                    throw new InvalidOperationException(
                        $"Unknown class '{className}' at row {row}, column {col}.");
                data[row, col] = code;
            }

        return new Band("ClassCode", data);
    }
}
