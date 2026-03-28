using Microsoft.ML.Data;

namespace FuzzySat.Core.ML;

/// <summary>
/// ML.NET input schema: features extracted from a pixel + label.
/// </summary>
public sealed class PixelFeatureData
{
    /// <summary>Gets or sets the class label.</summary>
    public string Label { get; set; } = "";

    /// <summary>Gets or sets the feature vector.</summary>
    public float[] Features { get; set; } = [];
}

/// <summary>
/// ML.NET prediction output.
/// </summary>
public sealed class PixelPrediction
{
    /// <summary>Gets or sets the predicted class label.</summary>
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = "";

    /// <summary>Gets or sets the prediction scores.</summary>
    [ColumnName("Score")]
    public float[] Score { get; set; } = [];
}
