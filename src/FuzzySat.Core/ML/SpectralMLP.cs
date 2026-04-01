using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using TorchSharp;
using TorchSharp.Modules;

namespace FuzzySat.Core.ML;

/// <summary>
/// Multi-Layer Perceptron for per-pixel spectral classification.
/// Architecture: Linear→BN→ReLU→Dropout layers, ending with LogSoftmax.
/// Designed for tabular fuzzy-enriched features (14-111 floats).
/// </summary>
internal sealed class SpectralMLP : Module<Tensor, Tensor>
{
    private readonly ModuleList<Module<Tensor, Tensor>> _layers;
    private readonly double _dropoutRate;

    public SpectralMLP(int inputSize, int numClasses, double dropoutRate = 0.3)
        : base(nameof(SpectralMLP))
    {
        _dropoutRate = dropoutRate;

        // Choose architecture based on feature space size
        var hiddenSizes = inputSize > 50
            ? new[] { 256, 128, 64 }
            : new[] { 128, 64 };

        _layers = new ModuleList<Module<Tensor, Tensor>>();

        var prevSize = inputSize;
        foreach (var hiddenSize in hiddenSizes)
        {
            _layers.Add(Linear(prevSize, hiddenSize));
            _layers.Add(BatchNorm1d(hiddenSize));
            prevSize = hiddenSize;
        }

        // Output layer
        _layers.Add(Linear(prevSize, numClasses));

        RegisterComponents();
    }

    public override Tensor forward(Tensor input)
    {
        var x = input;

        // Process hidden layers in groups of 2 (Linear + BatchNorm) with ReLU + Dropout
        // Dispose intermediate tensors to prevent native memory leaks
        for (var i = 0; i < _layers.Count - 1; i += 2)
        {
            var prev = x;
            x = _layers[i].call(x);     // Linear
            if (!ReferenceEquals(prev, input)) prev.Dispose();

            prev = x;
            x = _layers[i + 1].call(x); // BatchNorm
            prev.Dispose();

            prev = x;
            x = functional.relu(x);
            prev.Dispose();

            prev = x;
            x = functional.dropout(x, _dropoutRate, training);
            prev.Dispose();
        }

        // Output layer
        var beforeOutput = x;
        x = _layers[^1].call(x);
        if (!ReferenceEquals(beforeOutput, input)) beforeOutput.Dispose();

        var beforeSoftmax = x;
        x = functional.log_softmax(x, dim: 1);
        beforeSoftmax.Dispose();

        return x;
    }
}
