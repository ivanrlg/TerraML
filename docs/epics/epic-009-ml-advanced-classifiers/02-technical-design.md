# Epic #9 — Technical Design: Advanced ML Classifiers & Ensemble Methods

---

## 1. Contexto

FuzzySat clasifica pixeles satelitales usando logica difusa. El Epic #4 agrego ML.NET con
Random Forest y SDCA como clasificadores hibridos (features fuzzy + ML). El pipeline actual:

```
TrainingData → FuzzyFeatureExtractor → HybridClassifier.Train() → IClassifier
                                                                      ↓
                              MultispectralImage → ClassifyPixel() → ClassificationResult
```

Este epic extiende el pipeline con mas trainers y metodos ensemble, sin romper la arquitectura.

---

## 2. Decision: Refactoring de HybridClassifier

### Problema

`HybridClassifier.Train()` (lineas 82-126) contiene pipeline ML.NET generico que es 95%
reutilizable. Solo cambia el `trainerFactory`. Copiar ese codigo para cada nuevo classifier
seria violacion de DRY.

### Opciones

| Opcion | Pros | Contras |
|--------|------|---------|
| A) Copiar Train() en cada classifier | Simple, sin refactoring | Code duplication, bugs se propagan |
| B) Extraer MlClassifierBase abstracta | DRY, facil agregar trainers | Requiere refactoring existente |
| C) Extender HybridClassifier con mas factories | Minimo cambio | Clase crece indefinidamente |

### Decision: Opcion B — MlClassifierBase

```csharp
// src/FuzzySat.Core/ML/MlClassifierBase.cs
public abstract class MlClassifierBase : IClassifier
{
    private readonly object _lock = new();
    private readonly PredictionEngine<PixelFeatureData, PixelPrediction> _engine;
    private readonly FuzzyFeatureExtractor _extractor;

    public string ClassifyPixel(IDictionary<string, double> bandValues)
    {
        var features = _extractor.ExtractFeatures(bandValues);
        lock (_lock) { return _engine.Predict(new PixelFeatureData { Features = features }).PredictedLabel; }
    }

    protected static T TrainBase<T>(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> samples,
        FuzzyFeatureExtractor extractor,
        Func<MLContext, int, IEstimator<ITransformer>> trainerFactory,
        Func<MLContext, ITransformer, FuzzyFeatureExtractor, SchemaDefinition, T> constructor)
        where T : MlClassifierBase
    { /* pipeline compartido */ }
}
```

`HybridClassifier` se convierte en facade que delega a `MlClassifierBase` internamente.
API publica (`TrainRandomForest()`, `TrainSdca()`) no cambia. Tests existentes pasan.

---

## 3. Decision: Nuevos Clasificadores ML.NET

### LightGBM

- **Trainer**: `mlContext.MulticlassClassification.Trainers.LightGbm(numberOfLeaves, numberOfIterations, learningRate)`
- **NuGet**: `Microsoft.ML.LightGbm 5.0.0` (misma familia que ML.NET existente)
- **Ventaja**: Gradient boosting, generalmente el mejor clasificador tabular
- **Defaults**: leaves=31, iterations=100, lr=0.1

### SVM

- **Trainer**: `mlContext.MulticlassClassification.Trainers.OneVersusAll(mlContext.BinaryClassification.Trainers.LinearSvm())`
- **NuGet**: Ya incluido en `Microsoft.ML 5.0.0`
- **Ventaja**: Clasico para clasificacion espectral, buen rendimiento en alta dimension

### Logistic Regression

- **Trainer**: `mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy()`
- **NuGet**: Ya incluido en `Microsoft.ML 5.0.0`
- **Ventaja**: Baseline solido, rapido, produce probabilidades calibradas

### Patron comun

Cada classifier es una clase de ~80-100 lineas que extiende `MlClassifierBase`:

```csharp
public sealed class LightGbmClassifier : MlClassifierBase
{
    private LightGbmClassifier(...) : base(...) { }

    public static LightGbmClassifier Train(
        IReadOnlyList<(string Label, IDictionary<string, double> BandValues)> samples,
        FuzzyFeatureExtractor extractor,
        int numberOfLeaves = 31,
        int numberOfIterations = 100,
        double learningRate = 0.1)
    {
        return TrainBase(samples, extractor,
            (ctx, _) => ctx.MulticlassClassification.Trainers.LightGbm(
                numberOfLeaves: numberOfLeaves,
                numberOfIterations: numberOfIterations,
                learningRate: learningRate),
            (ctx, model, ext, schema) => new LightGbmClassifier(ctx, model, ext, schema));
    }
}
```

---

## 4. Decision: TorchSharp Directo (no ML.NET TorchSharp)

### Problema

Se necesita una red neuronal para clasificacion per-pixel de datos espectrales. El input es
un feature vector tabular de 14-111 floats (no una imagen).

### Opciones

| Opcion | Pros | Contras |
|--------|------|---------|
| A) Microsoft.ML.TorchSharp | Integra con pipeline ML.NET | Disenado para image classification (ResNet), no per-pixel tabular |
| B) TorchSharp directo | Control total de arquitectura | API diferente a ML.NET, nuevo pattern |
| C) ONNX Runtime | Cross-framework | Requiere exportar modelo desde otro framework |

### Decision: Opcion B — TorchSharp directo

`Microsoft.ML.TorchSharp` usa transfer learning con ResNet/MobileNet para clasificar imagenes
completas. FuzzySat clasifica **pixeles individuales** con features ya extraidas por
`FuzzyFeatureExtractor`. Un MLP personalizado es la herramienta correcta.

### Arquitectura MLP

```
Input(N) → Linear(N, 128) → BatchNorm1d(128) → ReLU → Dropout(0.3)
         → Linear(128, 64) → BatchNorm1d(64)  → ReLU → Dropout(0.3)
         → Linear(64, C)   → LogSoftmax
```

Donde N = feature count (14-111), C = numero de clases (2-7+).

Para feature spaces grandes (>50 features), capa adicional:

```
Input(N) → Linear(N, 256) → BN → ReLU → Drop(0.3)
         → Linear(256, 128) → BN → ReLU → Drop(0.3)
         → Linear(128, 64) → BN → ReLU → Drop(0.2)
         → Linear(64, C) → LogSoftmax
```

### Justificacion de MLP (no CNN/Transformer)

- Features son **tabulares** (raw bands + membership degrees + firing strengths)
- No hay estructura **espacial** (pixeles individuales, no patches)
- No hay estructura **secuencial** (bandas no tienen orden inherente)
- MLP con ~25K-50K parametros es apropiado para datasets pequenos (150-3500 muestras)
- Literatura academica (Mountrakis et al. 2011) confirma MLPs comparables a arquitecturas
  mas profundas para clasificacion espectral per-pixel con features ingenierizados

### NuGet Packages

```xml
<PackageReference Include="TorchSharp" Version="0.105.0" />
<PackageReference Include="TorchSharp-cpu" Version="0.105.0"
                  Condition="'$(OS)' == 'Windows_NT'" />
```

**Nota**: TorchSharp-cpu agrega ~200 MB de binarios nativos (libtorch). El proyecto ya
tiene precedente con GDAL (~100+ MB). GPU es opcional via `TorchSharp-cuda-windows`.

---

## 5. Estrategia de Entrenamiento para Datasets Pequenos

FuzzySat entrena con poligonos dibujados por el usuario: tipicamente 50-500 pixeles por clase,
3-7 clases → 150-3500 muestras totales. Esto es pequeno para redes neuronales.

### Mitigaciones

| Tecnica | Implementacion |
|---------|---------------|
| Red pequena | 2-3 capas, ~25K-50K params (no mas) |
| Dropout | 0.3 en capas ocultas |
| Weight decay | L2 regularization 1e-4 (Adam optimizer) |
| BatchNorm | Regularizacion implicita via estadisticas de mini-batch |
| Early stopping | Monitorear val_loss con patience=20 epochs, restaurar mejores pesos |
| LR scheduling | ReduceLROnPlateau: factor=0.5, patience=10 epochs, LR inicial=0.001 |
| Class weighting | NLLLoss con pesos inversamente proporcionales a frecuencia de clase |
| Data augmentation | Gaussian noise N(0, 0.02*stddev), band dropout (prob 0.1) |
| Validation split | 20% estratificado para early stopping |

### Expectativa realista

Con datasets pequenos, MLP puede **no** superar Random Forest o LightGBM. Esto es aceptable —
el objetivo es ofrecer la opcion, no garantizar que siempre sea superior. El Model Comparison
Framework (Fase 6) permitira al usuario decidir empiricamente.

---

## 6. Batch Prediction para Imagenes Grandes

### Problema

Una imagen Sentinel-2 puede ser 10980x10980 pixeles = ~120M pixeles. Per-pixel con
`ClassifyPixel()` y allocacion de diccionarios seria extremadamente lento.

### Solucion

`NeuralNetClassifier.ClassifyBatch(float[][] features, int batchSize = 4096)`:

1. Stack feature vectors en tensor `[N, F]`
2. Procesar en batches de `batchSize` para limitar memoria
3. Forward pass con `torch.no_grad()` en eval mode
4. `argmax` sobre output → indices de clase
5. Mapear indices a labels

**Memoria por batch**: 4096 pixeles * 111 features * 4 bytes = ~1.8 MB (insignificante).

El Web service clasificara **por filas** en lugar de pixel-a-pixel:

```csharp
for (var row = 0; row < image.Rows; row++)
{
    var rowFeatures = ExtractRowFeatures(image, row, extractor);
    var predictions = classifier.ClassifyBatch(rowFeatures);
    FillClassMap(classMap, row, predictions);
}
```

---

## 7. Ensemble Methods

### Voting (EnsembleClassifier)

```csharp
public sealed class EnsembleClassifier : IClassifier
{
    private readonly IReadOnlyList<(IClassifier Classifier, double Weight)> _members;

    public string ClassifyPixel(IDictionary<string, double> bandValues)
    {
        var votes = new Dictionary<string, double>();
        foreach (var (classifier, weight) in _members)
        {
            var prediction = classifier.ClassifyPixel(bandValues);
            votes[prediction] = votes.GetValueOrDefault(prediction) + weight;
        }
        return votes.MaxBy(kv => kv.Value).Key;
    }
}
```

**MajorityVote**: weight=1.0 para todos.
**WeightedVote**: weight=MeanOA de cross-validation para cada classifier.

### Stacking (StackingClassifier)

1. **Level 0**: K-fold split de datos de entrenamiento
   - Para cada fold: entrenar N base classifiers, predecir out-of-fold → vector de N predicciones
   - Resultado: N columnas de predicciones out-of-fold (one-hot encoded) para todo el dataset
2. **Level 1**: Entrenar meta-learner (LogisticRegression) sobre las predicciones level-0
3. **Inference**: Para nuevo pixel → N base classifiers predicen → concatenar → meta-learner decide

### Prevencion de Data Leakage

El stacking usa predicciones **out-of-fold** exclusivamente. Cada base classifier solo predice
datos que NO uso para entrenarse. Esto previene que el meta-learner aprenda a confiar ciegamente
en un classifier sobreajustado.

---

## 8. Model Comparison Framework

### Flujo

```
ModelComparisonEngine.Compare(
    trainingSamples,
    featureExtractor,
    classifierFactories: [
        ("Pure Fuzzy",    factory),
        ("Random Forest", factory),
        ("SDCA",          factory),
        ("LightGBM",      factory),
        ("SVM",           factory),
        ("Logistic Reg",  factory),
        ("MLP Neural Net", factory)
    ],
    folds: 5
) → ModelComparisonResult
```

### Resultado

```csharp
public sealed record ClassifierResult(
    string Name,
    double MeanOA,
    double StdOA,
    double MeanKappa,
    double StdKappa,
    long TrainingTimeMs);

public sealed class ModelComparisonResult
{
    public IReadOnlyList<ClassifierResult> Results { get; }  // ordenados por MeanKappa desc
    public ClassifierResult BestModel => Results[0];
}
```

### UI (ModelComparison.razor)

- Checkboxes para seleccionar metodos a comparar
- Dropdown: 3-fold, 5-fold, 10-fold
- Tabla Radzen sortable con columnas: Rank, Name, OA (±std), Kappa (±std), Time
- Bar chart Radzen: OA por metodo con barras de error
- Boton "Use Best Model" → navega a Classification con metodo pre-seleccionado

---

## 9. Persistencia de Modelos

### ML.NET (Random Forest, SDCA, LightGBM, SVM, LR)

- `mlContext.Model.Save(model, schema, stream)` → .zip
- `mlContext.Model.Load(stream)` → ITransformer
- Almacenamiento: `{projectDir}/models/{method-name}.mlnet.zip`

### TorchSharp (Neural Network)

- `model.save(path)` → .pt (pesos)
- Metadata JSON: `{ inputSize, hiddenSizes, numClasses, classLabels, dropoutRate }`
- Almacenamiento: `{projectDir}/models/neural-net.pt` + `neural-net.meta.json`

### Extension de IProjectRepository

```csharp
// Nuevos metodos en IProjectRepository
Task SaveTrainedModelAsync(string projectName, string methodName, byte[] modelBytes);
Task<byte[]?> LoadTrainedModelAsync(string projectName, string methodName);
Task SaveModelMetadataAsync(string projectName, string methodName, string metadataJson);
Task<string?> LoadModelMetadataAsync(string projectName, string methodName);
```

---

## 10. Integracion Web

### Switch de Metodo (HybridClassificationService)

```csharp
var classifier = options.ClassificationMethod switch
{
    "Random Forest"      => HybridClassifier.TrainRandomForest(mlSamples, extractor),
    "SDCA"               => HybridClassifier.TrainSdca(mlSamples, extractor),
    "LightGBM"           => LightGbmClassifier.Train(mlSamples, extractor),
    "SVM"                => SvmClassifier.Train(mlSamples, extractor),
    "Logistic Regression" => LogisticRegressionClassifier.Train(mlSamples, extractor),
    "MLP Neural Network"  => NeuralNetClassifier.Train(mlSamples, extractor, nnOptions, progress, ct),
    _ => throw new ArgumentException($"Unknown method: '{options.ClassificationMethod}'")
};
```

### Progress Reporting para Neural Network

```
Epoch 1/200 - Loss: 2.3456 - Val Acc: 35.2%    [0-15% de la barra]
Epoch 50/200 - Loss: 0.4521 - Val Acc: 82.1%
Early stop at epoch 87 - Best Val Acc: 84.3%
Classifying pixels: row 500/5000                 [15-100% de la barra]
```

---

## 11. Thread Safety

| Componente | Estrategia |
|------------|-----------|
| ML.NET classifiers | `lock(_lock)` alrededor de `PredictionEngine.Predict()` (pattern existente) |
| NeuralNetClassifier.ClassifyPixel | `lock(_lock)` + `torch.no_grad()` |
| NeuralNetClassifier.ClassifyBatch | Single thread (llamado desde Task.Run en Web service) |
| Training (todos) | Single background thread via Task.Run |
| EnsembleClassifier | Delegates a miembros; cada miembro tiene su propio lock |

---

## 12. Riesgos Globales

| # | Riesgo | Impacto | Probabilidad | Mitigacion |
|---|--------|---------|-------------|------------|
| 1 | TorchSharp incompatible con .NET 10 | Alto | Media | Verificar `dotnet restore` + `dotnet build` en primer commit de Fase 4. Si falla, diferir NN hasta release compatible |
| 2 | Package size TorchSharp ~200 MB | Medio | Cierta | Documentar en README. GDAL ya establece precedente (~100+ MB). Considerar proyecto separado opcional |
| 3 | Overfitting NN en datasets pequenos | Medio | Alta | Regularizacion agresiva + early stopping + red pequena. Aceptar que RF/LightGBM pueden ser mejores |
| 4 | Breaking changes en HybridClassifier API | Alto | Baja | Facade pattern preserva `TrainRandomForest()` y `TrainSdca()` exactamente como estan |
| 5 | Memory leaks tensores TorchSharp | Medio | Media | `IDisposable` en NeuralNetClassifier, `using` blocks en toda operacion de tensor, finalizer como safety net |
| 6 | LightGBM native conflicts con FastTree | Bajo | Baja | Ambos de familia 5.0.0. FastTree usa LightGBM internamente; deberian coexistir |
| 7 | Stacking data leakage | Alto | Baja | K-fold out-of-fold predictions exclusivamente para level-0. Bien documentado en literatura |
| 8 | Blazor UI freeze durante training | Medio | Baja | Todo training en `Task.Run()` con progress reporting. Pattern ya probado en ClassificationService |

---

## 13. Dependencias Nuevas Necesarias

| Package | Version | Tamano | Justificacion |
|---------|---------|--------|---------------|
| `Microsoft.ML.LightGbm` | 5.0.0 | ~20 MB | Gradient boosting (Fase 2) |
| `TorchSharp` | 0.105.0 | ~5 MB | API TorchSharp managed (Fase 4) |
| `TorchSharp-cpu` | 0.105.0 | ~200 MB | Runtime nativo libtorch CPU (Fase 4) |

SVM (`LinearSvm`) y Logistic Regression (`LbfgsMaximumEntropy`) ya estan incluidos en
`Microsoft.ML 5.0.0` que ya es dependencia del proyecto.

---

## 14. Lo que NO Necesita Hacerse

- **No** crear nuevo proyecto/assembly para ML — todo va en `FuzzySat.Core/ML/`
- **No** cambiar `IClassifier` interface — `ClassifyPixel()` es suficiente
- **No** crear nueva interfaz para ensemble — implementa `IClassifier` directamente
- **No** implementar AutoML/hyperparameter tuning — defaults razonables son suficientes
- **No** agregar soporte GPU obligatorio — CPU es el default
- **No** implementar CNNs/Transformers — MLP es suficiente para features tabulares
- **No** cambiar el formato de `ClassificationResult` — todos los classifiers producen el mismo output
- **No** modificar el pipeline de entrenamiento fuzzy — `TrainingSession` y `FuzzyFeatureExtractor` se reutilizan tal cual
