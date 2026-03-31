namespace FuzzySat.Core.Persistence;

/// <summary>
/// DTO for persisting a confusion matrix and accuracy metrics.
/// </summary>
public sealed class ValidationResultDto
{
    /// <summary>Ordered class names (rows/columns of the matrix).</summary>
    public List<string> ClassNames { get; set; } = [];

    /// <summary>Confusion matrix as jagged array [actual][predicted].</summary>
    public int[][] Matrix { get; set; } = [];

    /// <summary>Overall accuracy (correct / total).</summary>
    public double OverallAccuracy { get; set; }

    /// <summary>Cohen's Kappa coefficient.</summary>
    public double KappaCoefficient { get; set; }

    /// <summary>Total number of samples.</summary>
    public int TotalSamples { get; set; }

    /// <summary>Number of correctly classified samples.</summary>
    public int CorrectCount { get; set; }

    /// <summary>Per-class accuracy metrics.</summary>
    public List<ClassMetricDto> PerClassMetrics { get; set; } = [];

    /// <summary>
    /// Converts the jagged array back to a 2D array for ConfusionMatrix reconstruction.
    /// </summary>
    public int[,] ToMatrix()
    {
        var n = ClassNames.Count;
        if (Matrix.Length != n)
            throw new InvalidOperationException(
                $"Matrix has {Matrix.Length} rows but expected {n} (one per class).");

        var result = new int[n, n];
        for (var i = 0; i < n; i++)
        {
            if (Matrix[i].Length != n)
                throw new InvalidOperationException(
                    $"Matrix row {i} has {Matrix[i].Length} columns but expected {n}.");
            for (var j = 0; j < n; j++)
                result[i, j] = Matrix[i][j];
        }
        return result;
    }
}

/// <summary>
/// Per-class accuracy metrics DTO.
/// </summary>
public sealed class ClassMetricDto
{
    /// <summary>Class name.</summary>
    public string ClassName { get; set; } = "";

    /// <summary>Producer's accuracy (recall).</summary>
    public double ProducersAccuracy { get; set; }

    /// <summary>User's accuracy (precision).</summary>
    public double UsersAccuracy { get; set; }

    /// <summary>Number of actual (ground truth) samples for this class.</summary>
    public int ActualCount { get; set; }

    /// <summary>Number of predicted samples for this class.</summary>
    public int PredictedCount { get; set; }
}
