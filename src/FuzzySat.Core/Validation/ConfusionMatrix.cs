namespace FuzzySat.Core.Validation;

/// <summary>
/// Confusion matrix for evaluating classification accuracy.
/// Rows represent actual (ground truth) classes, columns represent predicted classes.
/// </summary>
public sealed class ConfusionMatrix
{
    private readonly int[,] _matrix;
    private readonly Dictionary<string, int> _classIndex;

    /// <summary>Gets the ordered list of class names.</summary>
    public IReadOnlyList<string> ClassNames { get; }

    /// <summary>Gets the total number of samples in the matrix.</summary>
    public int TotalSamples { get; }

    /// <summary>
    /// Creates a confusion matrix from paired actual/predicted classifications.
    /// </summary>
    /// <param name="actual">Ground truth class labels.</param>
    /// <param name="predicted">Predicted class labels.</param>
    public ConfusionMatrix(IEnumerable<string> actual, IEnumerable<string> predicted)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(predicted);

        var actualList = actual.ToList();
        var predictedList = predicted.ToList();

        if (actualList.Count == 0)
            throw new ArgumentException("At least one sample is required.", nameof(actual));
        if (actualList.Count != predictedList.Count)
            throw new ArgumentException("Actual and predicted must have the same length.", nameof(predicted));

        var classNames = actualList.Union(predictedList).Distinct().OrderBy(c => c).ToList();
        ClassNames = classNames.AsReadOnly();
        TotalSamples = actualList.Count;

        _classIndex = new Dictionary<string, int>();
        for (var i = 0; i < classNames.Count; i++)
            _classIndex[classNames[i]] = i;

        _matrix = new int[classNames.Count, classNames.Count];
        for (var i = 0; i < actualList.Count; i++)
            _matrix[_classIndex[actualList[i]], _classIndex[predictedList[i]]]++;
    }

    /// <summary>Gets the count at [actualClass, predictedClass].</summary>
    public int this[string actualClass, string predictedClass]
        => _matrix[_classIndex[actualClass], _classIndex[predictedClass]];

    /// <summary>Gets the number of correctly classified samples (diagonal sum).</summary>
    public int CorrectCount
    {
        get
        {
            var sum = 0;
            for (var i = 0; i < ClassNames.Count; i++)
                sum += _matrix[i, i];
            return sum;
        }
    }

    /// <summary>Overall Accuracy = correct / total.</summary>
    public double OverallAccuracy => (double)CorrectCount / TotalSamples;

    /// <summary>Gets the row total (actual count) for a class.</summary>
    public int RowTotal(string className)
    {
        var row = _classIndex[className];
        var sum = 0;
        for (var col = 0; col < ClassNames.Count; col++)
            sum += _matrix[row, col];
        return sum;
    }

    /// <summary>Gets the column total (predicted count) for a class.</summary>
    public int ColumnTotal(string className)
    {
        var col = _classIndex[className];
        var sum = 0;
        for (var row = 0; row < ClassNames.Count; row++)
            sum += _matrix[row, col];
        return sum;
    }

    /// <summary>Producer's accuracy (recall) for a class = TP / row total.</summary>
    public double ProducersAccuracy(string className)
    {
        var rowTotal = RowTotal(className);
        return rowTotal == 0 ? 0.0 : (double)this[className, className] / rowTotal;
    }

    /// <summary>User's accuracy (precision) for a class = TP / column total.</summary>
    public double UsersAccuracy(string className)
    {
        var colTotal = ColumnTotal(className);
        return colTotal == 0 ? 0.0 : (double)this[className, className] / colTotal;
    }

    /// <summary>
    /// Cohen's Kappa coefficient: κ = (Po - Pe) / (1 - Pe),
    /// where Po = overall accuracy and Pe = expected agreement by chance.
    /// </summary>
    public double KappaCoefficient
    {
        get
        {
            var n = (double)TotalSamples;
            var po = OverallAccuracy;

            var pe = 0.0;
            for (var i = 0; i < ClassNames.Count; i++)
            {
                var rowSum = 0.0;
                var colSum = 0.0;
                for (var j = 0; j < ClassNames.Count; j++)
                {
                    rowSum += _matrix[i, j];
                    colSum += _matrix[j, i];
                }
                pe += (rowSum * colSum) / (n * n);
            }

            return Math.Abs(1.0 - pe) < 1e-15 ? 0.0 : (po - pe) / (1.0 - pe);
        }
    }
}
