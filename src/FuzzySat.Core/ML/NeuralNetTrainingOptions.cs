namespace FuzzySat.Core.ML;

/// <summary>
/// Hyperparameters for MLP neural network training.
/// </summary>
public sealed record NeuralNetTrainingOptions
{
    /// <summary>Maximum number of training epochs.</summary>
    public int MaxEpochs { get; init; } = 200;

    /// <summary>Mini-batch size.</summary>
    public int BatchSize { get; init; } = 64;

    /// <summary>Initial learning rate for Adam optimizer.</summary>
    public double LearningRate { get; init; } = 0.001;

    /// <summary>L2 weight decay for regularization.</summary>
    public double WeightDecay { get; init; } = 1e-4;

    /// <summary>Dropout rate for hidden layers.</summary>
    public double DropoutRate { get; init; } = 0.3;

    /// <summary>Early stopping patience (epochs without improvement).</summary>
    public int PatienceEpochs { get; init; } = 20;

    /// <summary>Fraction of training data reserved for validation.</summary>
    public double ValidationSplit { get; init; } = 0.2;

    /// <summary>Random seed for reproducibility.</summary>
    public int RandomSeed { get; init; } = 42;
}
