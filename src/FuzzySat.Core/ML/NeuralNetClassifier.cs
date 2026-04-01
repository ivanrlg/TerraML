using FuzzySat.Core.FuzzyLogic.Classification;
using TorchSharp;
using static TorchSharp.torch;

namespace FuzzySat.Core.ML;

/// <summary>
/// Neural network classifier using a TorchSharp MLP for per-pixel spectral classification.
/// Implements <see cref="IClassifier"/> and <see cref="IDisposable"/> for tensor cleanup.
/// Thread-safe for prediction via lock.
/// </summary>
public sealed class NeuralNetClassifier : IClassifier, IDisposable
{
    private readonly object _lock = new();
    private readonly SpectralMLP _model;
    private readonly FuzzyFeatureExtractor _featureExtractor;
    private readonly string[] _classLabels;
    private bool _disposed;

    private NeuralNetClassifier(
        SpectralMLP model,
        FuzzyFeatureExtractor featureExtractor,
        string[] classLabels)
    {
        _model = model;
        _featureExtractor = featureExtractor;
        _classLabels = classLabels;
    }

    /// <inheritdoc />
    public string ClassifyPixel(IDictionary<string, double> bandValues)
    {
        ArgumentNullException.ThrowIfNull(bandValues);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var features = _featureExtractor.ExtractFeatures(bandValues);

        lock (_lock)
        {
            _model.eval();
            using var noGrad = no_grad();
            using var input = tensor(features, [1, features.Length]);
            using var output = _model.call(input);
            var classIdx = output.argmax(1).item<long>();
            return _classLabels[classIdx];
        }
    }

    /// <summary>
    /// Classifies a batch of pre-extracted feature vectors.
    /// </summary>
    /// <param name="featureBatch">Array of feature vectors (one per pixel).</param>
    /// <returns>Array of predicted class labels.</returns>
    public string[] ClassifyBatch(float[][] featureBatch)
    {
        ArgumentNullException.ThrowIfNull(featureBatch);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (featureBatch.Length == 0)
            return [];

        var featureCount = featureBatch[0].Length;
        var flat = new float[featureBatch.Length * featureCount];
        for (var i = 0; i < featureBatch.Length; i++)
            Array.Copy(featureBatch[i], 0, flat, i * featureCount, featureCount);

        lock (_lock)
        {
            _model.eval();
            using var noGrad = no_grad();
            using var input = tensor(flat, [featureBatch.Length, featureCount]);
            using var output = _model.call(input);
            using var indices = output.argmax(1);
            var indexData = indices.data<long>().ToArray();

            var results = new string[featureBatch.Length];
            for (var i = 0; i < results.Length; i++)
                results[i] = _classLabels[indexData[i]];
            return results;
        }
    }

    /// <summary>
    /// Trains a neural network classifier on fuzzy-enriched features.
    /// </summary>
    /// <param name="trainingSamples">Labeled training data.</param>
    /// <param name="featureExtractor">Fuzzy feature extractor.</param>
    /// <param name="options">Training hyperparameters (optional, uses defaults).</param>
    /// <param name="progress">Optional progress callback receiving epoch info.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static NeuralNetClassifier Train(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> trainingSamples,
        FuzzyFeatureExtractor featureExtractor,
        NeuralNetTrainingOptions? options = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trainingSamples);
        ArgumentNullException.ThrowIfNull(featureExtractor);

        if (trainingSamples.Count == 0)
            throw new ArgumentException("At least one training sample is required.", nameof(trainingSamples));

        options ??= new NeuralNetTrainingOptions();
        var rng = new Random(options.RandomSeed);

        // Extract features and build label mapping
        var classLabels = trainingSamples.Select(s => s.Label).Distinct().OrderBy(s => s).ToArray();
        var labelToIndex = new Dictionary<string, int>();
        for (var i = 0; i < classLabels.Length; i++)
            labelToIndex[classLabels[i]] = i;

        var allFeatures = new List<float[]>();
        var allLabels = new List<long>();
        foreach (var (label, bandValues) in trainingSamples)
        {
            allFeatures.Add(featureExtractor.ExtractFeatures(bandValues));
            allLabels.Add(labelToIndex[label]);
        }

        var featureCount = featureExtractor.FeatureNames.Count;
        var numClasses = classLabels.Length;

        // Stratified train/validation split
        var (trainIdx, valIdx) = StratifiedSplit(allLabels, options.ValidationSplit, rng);

        // Build tensors
        var trainFeatures = BuildFeatureTensor(allFeatures, trainIdx, featureCount);
        var trainLabels = BuildLabelTensor(allLabels, trainIdx);
        var valFeatures = BuildFeatureTensor(allFeatures, valIdx, featureCount);
        var valLabels = BuildLabelTensor(allLabels, valIdx);

        // Compute class weights (inverse frequency)
        var classWeights = ComputeClassWeights(allLabels, trainIdx, numClasses);

        // Create model and optimizer
        var model = new SpectralMLP(featureCount, numClasses, options.DropoutRate);
        var optimizer = optim.Adam(model.parameters(), lr: options.LearningRate,
            weight_decay: options.WeightDecay);

        using var weightTensor = tensor(classWeights);
        var bestValLoss = double.MaxValue;
        var patienceCounter = 0;
        byte[]? bestModelState = null;

        // Training loop
        for (var epoch = 0; epoch < options.MaxEpochs; epoch++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Train
            model.train();
            var trainLoss = TrainEpoch(model, optimizer, trainFeatures, trainLabels,
                weightTensor, options.BatchSize, rng);

            // Validate
            model.eval();
            double valAcc;
            double valLoss;
            using (var noGrad = no_grad())
            {
                using var valOutput = model.call(valFeatures);
                using var loss = nn.functional.nll_loss(valOutput, valLabels, weight: weightTensor);
                valLoss = loss.item<float>();

                using var predicted = valOutput.argmax(1);
                using var correct = predicted.eq(valLabels);
                valAcc = correct.sum().item<long>() / (double)valIdx.Count;
            }

            progress?.Report($"Epoch {epoch + 1}/{options.MaxEpochs} - Loss: {trainLoss:F4} - Val Acc: {valAcc:P1}");

            // Early stopping
            if (valLoss < bestValLoss)
            {
                bestValLoss = valLoss;
                patienceCounter = 0;
                using var ms = new MemoryStream();
                model.save(ms);
                bestModelState = ms.ToArray();
            }
            else
            {
                patienceCounter++;
                if (patienceCounter >= options.PatienceEpochs)
                {
                    progress?.Report($"Early stop at epoch {epoch + 1} - Best Val Loss: {bestValLoss:F4}");
                    break;
                }
            }
        }

        // Restore best model
        if (bestModelState is not null)
        {
            using var ms = new MemoryStream(bestModelState);
            model.load(ms);
        }

        // Cleanup training tensors
        trainFeatures.Dispose();
        trainLabels.Dispose();
        valFeatures.Dispose();
        valLabels.Dispose();

        model.eval();
        return new NeuralNetClassifier(model, featureExtractor, classLabels);
    }

    private static double TrainEpoch(
        SpectralMLP model,
        optim.Optimizer optimizer,
        Tensor features,
        Tensor labels,
        Tensor classWeights,
        int batchSize,
        Random rng)
    {
        var n = (int)features.shape[0];
        var indices = Enumerable.Range(0, n).OrderBy(_ => rng.Next()).ToArray();
        var totalLoss = 0.0;
        var batches = 0;

        for (var start = 0; start < n; start += batchSize)
        {
            var end = Math.Min(start + batchSize, n);
            var batchIdx = indices[start..end];

            using var idxTensor = tensor(batchIdx.Select(i => (long)i).ToArray());
            using var batchFeatures = features.index_select(0, idxTensor);
            using var batchLabels = labels.index_select(0, idxTensor);

            using var output = model.call(batchFeatures);
            using var loss = nn.functional.nll_loss(output, batchLabels, weight: classWeights);

            optimizer.zero_grad();
            loss.backward();
            optimizer.step();

            totalLoss += loss.item<float>();
            batches++;
        }

        return batches > 0 ? totalLoss / batches : 0.0;
    }

    private static (List<int> Train, List<int> Val) StratifiedSplit(
        List<long> labels, double valFraction, Random rng)
    {
        var byClass = new Dictionary<long, List<int>>();
        for (var i = 0; i < labels.Count; i++)
        {
            if (!byClass.TryGetValue(labels[i], out var list))
            {
                list = [];
                byClass[labels[i]] = list;
            }
            list.Add(i);
        }

        var trainIdx = new List<int>();
        var valIdx = new List<int>();

        foreach (var classSamples in byClass.Values)
        {
            var shuffled = classSamples.OrderBy(_ => rng.Next()).ToList();
            var valCount = Math.Max(1, (int)(shuffled.Count * valFraction));
            valIdx.AddRange(shuffled.Take(valCount));
            trainIdx.AddRange(shuffled.Skip(valCount));
        }

        return (trainIdx, valIdx);
    }

    private static Tensor BuildFeatureTensor(List<float[]> allFeatures, List<int> indices, int featureCount)
    {
        var flat = new float[indices.Count * featureCount];
        for (var i = 0; i < indices.Count; i++)
            Array.Copy(allFeatures[indices[i]], 0, flat, i * featureCount, featureCount);
        return tensor(flat, [indices.Count, featureCount]);
    }

    private static Tensor BuildLabelTensor(List<long> allLabels, List<int> indices)
    {
        var data = indices.Select(i => allLabels[i]).ToArray();
        return tensor(data, [indices.Count], ScalarType.Int64);
    }

    private static float[] ComputeClassWeights(List<long> labels, List<int> trainIdx, int numClasses)
    {
        var counts = new int[numClasses];
        foreach (var i in trainIdx)
            counts[labels[i]]++;

        var total = (float)trainIdx.Count;
        var weights = new float[numClasses];
        for (var c = 0; c < numClasses; c++)
            weights[c] = counts[c] > 0 ? total / (numClasses * counts[c]) : 1.0f;
        return weights;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _model.Dispose();
            _disposed = true;
        }
    }
}
