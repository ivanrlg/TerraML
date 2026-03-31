namespace FuzzySat.Core.Validation;

/// <summary>
/// Confusion matrix for evaluating classification accuracy.
/// Rows represent actual (ground truth) classes, columns represent predicted classes.
/// </summary>
public sealed class ConfusionMatrix
{
    private readonly int[,] _matrix;
    private readonly Dictionary<string, int> _classIndex;
    private readonly int[] _rowTotals;
    private readonly int[] _colTotals;

    /// <summary>Gets the ordered list of class names.</summary>
    public IReadOnlyList<string> ClassNames { get; }

    /// <summary>Gets the total number of samples in the matrix.</summary>
    public int TotalSamples { get; }

    /// <summary>Gets the number of correctly classified samples (diagonal sum).</summary>
    public int CorrectCount { get; }

    /// <summary>Overall Accuracy = correct / total.</summary>
    public double OverallAccuracy { get; }

    /// <summary>
    /// Cohen's Kappa coefficient: κ = (Po - Pe) / (1 - Pe).
    /// Returns 1.0 when Pe = 1.0 and Po = 1.0 (perfect single-class agreement).
    /// </summary>
    public double KappaCoefficient { get; }

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

        // Validate labels
        for (var i = 0; i < actualList.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(actualList[i]))
                throw new ArgumentException($"Actual label at index {i} is null or whitespace.", nameof(actual));
            if (string.IsNullOrWhiteSpace(predictedList[i]))
                throw new ArgumentException($"Predicted label at index {i} is null or whitespace.", nameof(predicted));
        }

        var classNames = actualList.Union(predictedList).Distinct()
            .OrderBy(c => c, StringComparer.Ordinal).ToList();
        ClassNames = classNames.AsReadOnly();
        TotalSamples = actualList.Count;

        _classIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < classNames.Count; i++)
            _classIndex[classNames[i]] = i;

        var count = classNames.Count;
        _matrix = new int[count, count];
        for (var i = 0; i < actualList.Count; i++)
            _matrix[_classIndex[actualList[i]], _classIndex[predictedList[i]]]++;

        // Cache row/column totals and diagonal sum
        _rowTotals = new int[count];
        _colTotals = new int[count];
        var correctCount = 0;
        for (var i = 0; i < count; i++)
        {
            for (var j = 0; j < count; j++)
            {
                _rowTotals[i] += _matrix[i, j];
                _colTotals[j] += _matrix[i, j];
            }
            correctCount += _matrix[i, i];
        }
        CorrectCount = correctCount;
        OverallAccuracy = (double)correctCount / TotalSamples;

        // Compute Kappa
        var n = (double)TotalSamples;
        var pe = 0.0;
        for (var i = 0; i < count; i++)
            pe += ((double)_rowTotals[i] * _colTotals[i]) / (n * n);

        if (Math.Abs(1.0 - pe) < 1e-15)
            KappaCoefficient = OverallAccuracy >= 1.0 - 1e-15 ? 1.0 : 0.0;
        else
            KappaCoefficient = (OverallAccuracy - pe) / (1.0 - pe);
    }

    /// <summary>
    /// Reconstructs a confusion matrix from persisted data (class names and matrix values).
    /// </summary>
    /// <param name="classNames">Ordered class names.</param>
    /// <param name="matrix">The confusion matrix [actual, predicted].</param>
    public static ConfusionMatrix FromPersistedData(
        IReadOnlyList<string> classNames,
        int[,] matrix)
    {
        ArgumentNullException.ThrowIfNull(classNames);
        ArgumentNullException.ThrowIfNull(matrix);

        if (classNames.Count == 0)
            throw new ArgumentException("At least one class name is required.", nameof(classNames));

        var count = classNames.Count;
        if (matrix.GetLength(0) != count || matrix.GetLength(1) != count)
            throw new ArgumentException(
                $"Matrix dimensions ({matrix.GetLength(0)}x{matrix.GetLength(1)}) " +
                $"must match class count ({count}).", nameof(matrix));

        return new ConfusionMatrix(classNames, matrix);
    }

    /// <summary>Private constructor for reconstruction from persisted data.</summary>
    private ConfusionMatrix(IReadOnlyList<string> classNames, int[,] matrix)
    {
        var count = classNames.Count;
        ClassNames = classNames.ToList().AsReadOnly();

        _classIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
            _classIndex[classNames[i]] = i;

        _matrix = (int[,])matrix.Clone();

        _rowTotals = new int[count];
        _colTotals = new int[count];
        var correctCount = 0;
        for (var i = 0; i < count; i++)
        {
            for (var j = 0; j < count; j++)
            {
                _rowTotals[i] += _matrix[i, j];
                _colTotals[j] += _matrix[i, j];
            }
            correctCount += _matrix[i, i];
        }

        TotalSamples = _rowTotals.Sum();
        CorrectCount = correctCount;
        OverallAccuracy = TotalSamples == 0 ? 0.0 : (double)correctCount / TotalSamples;

        var n = (double)TotalSamples;
        var pe = 0.0;
        if (n > 0)
        {
            for (var i = 0; i < count; i++)
                pe += ((double)_rowTotals[i] * _colTotals[i]) / (n * n);
        }

        if (Math.Abs(1.0 - pe) < 1e-15)
            KappaCoefficient = OverallAccuracy >= 1.0 - 1e-15 ? 1.0 : 0.0;
        else
            KappaCoefficient = (OverallAccuracy - pe) / (1.0 - pe);
    }

    /// <summary>Gets the count at [actualClass, predictedClass].</summary>
    public int this[string actualClass, string predictedClass]
    {
        get
        {
            ValidateClassName(actualClass, nameof(actualClass));
            ValidateClassName(predictedClass, nameof(predictedClass));
            return _matrix[_classIndex[actualClass], _classIndex[predictedClass]];
        }
    }

    /// <summary>Gets the row total (actual count) for a class.</summary>
    public int RowTotal(string className)
    {
        ValidateClassName(className, nameof(className));
        return _rowTotals[_classIndex[className]];
    }

    /// <summary>Gets the column total (predicted count) for a class.</summary>
    public int ColumnTotal(string className)
    {
        ValidateClassName(className, nameof(className));
        return _colTotals[_classIndex[className]];
    }

    /// <summary>Producer's accuracy (recall) for a class = TP / row total.</summary>
    public double ProducersAccuracy(string className)
    {
        ValidateClassName(className, nameof(className));
        var idx = _classIndex[className];
        return _rowTotals[idx] == 0 ? 0.0 : (double)_matrix[idx, idx] / _rowTotals[idx];
    }

    /// <summary>User's accuracy (precision) for a class = TP / column total.</summary>
    public double UsersAccuracy(string className)
    {
        ValidateClassName(className, nameof(className));
        var idx = _classIndex[className];
        return _colTotals[idx] == 0 ? 0.0 : (double)_matrix[idx, idx] / _colTotals[idx];
    }

    private void ValidateClassName(string className, string paramName)
    {
        if (!_classIndex.ContainsKey(className))
            throw new ArgumentException($"Unknown class name: '{className}'.", paramName);
    }
}
