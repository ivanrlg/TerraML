# Epic #9 — Plan de Implementacion

> **Branch**: `feature/epic-009-ml-advanced-classifiers`
> **Estimado**: ~24 micro-commits, 6 PRs
> **Referencia**: Issue [#31](https://github.com/ivanrlg/FuzzySat/issues/31)

---

## Dependencias entre Fases

```
Fase 1 (Base Refactor) ──→ Fase 2 (LightGBM)
                       ──→ Fase 3 (SVM + LR)
                       ──→ Fase 4 (Neural Net)
Fases 2 + 3 + 4       ──→ Fase 5 (Ensemble)
Fases 1 + 2 + 3       ──→ Fase 6 (Comparison)
```

---

## Fase 1: Refactoring Base + Cross-Validation (PR #1, ~3 commits)

### 1A: Extraer MlClassifierBase

- **Archivo**: `src/FuzzySat.Core/ML/MlClassifierBase.cs` (~120 lineas)
- **Detalle**: Clase abstracta que extrae el pipeline compartido de `HybridClassifier.Train()`
  (lineas 82-126). Encapsula: MLContext creation (seed 42), SchemaDefinition dinamico,
  LoadFromEnumerable, MapValueToKey/MapKeyToValue pipeline, PredictionEngine con lock.
  Las subclases solo proveen el `IEstimator<ITransformer>` trainer.

### 1B: Refactorizar HybridClassifier

- **Archivo**: `src/FuzzySat.Core/ML/HybridClassifier.cs` (~80 lineas cambio)
- **Detalle**: Internamente usa `MlClassifierBase` pero mantiene la API publica existente
  (`TrainRandomForest()`, `TrainSdca()`) como facade. Tests existentes pasan sin cambios.

### 1C: Cross-Validator

- **Archivos**:
  - `src/FuzzySat.Core/ML/CrossValidator.cs` (~120 lineas)
  - `src/FuzzySat.Core/ML/CrossValidationResult.cs` (~40 lineas)
  - `tests/FuzzySat.Core.Tests/ML/CrossValidatorTests.cs` (~80 lineas)
- **Detalle**: Split estratificado por clase en k folds. Entrena classifier factory por fold.
  Computa ConfusionMatrix/AccuracyMetrics por fold. Agrega: MeanOA, StdOA, MeanKappa, StdKappa.

**Commits**:
1. `refactor(core): extract MlClassifierBase from HybridClassifier`
2. `refactor(core): adapt HybridClassifier to use MlClassifierBase`
3. `feat(core): add stratified k-fold CrossValidator`

---

## Fase 2: LightGBM Classifier (PR #2, ~3 commits)

### 2A: NuGet + Classifier

- **Archivo**: `src/FuzzySat.Core/FuzzySat.Core.csproj` — agregar `Microsoft.ML.LightGbm 5.0.0`
- **Archivo**: `src/FuzzySat.Core/ML/LightGbmClassifier.cs` (~100 lineas)
- **Detalle**: Extiende `MlClassifierBase`. Trainer:
  `mlContext.MulticlassClassification.Trainers.LightGbm(numberOfLeaves, numberOfIterations, learningRate)`.
  Factory: `static LightGbmClassifier TrainLightGbm(samples, extractor, leaves=31, iterations=100, lr=0.1)`.

### 2B: Tests

- **Archivo**: `tests/FuzzySat.Core.Tests/ML/LightGbmClassifierTests.cs` (~80 lineas)
- **Detalle**: Mismo patron que HybridClassifierTests: datos sinteticos 2 clases, verifica
  clasificacion correcta, test de argumentos invalidos.

### 2C: Web Integration

- **Archivo**: `src/FuzzySat.Web/Services/HybridClassificationService.cs` — agregar case `"LightGBM"`
- **Archivo**: `src/FuzzySat.Web/Components/Pages/Classification.razor` — agregar a dropdown
- **Detalle**: Nuevo entry en `_classificationMethods` list con icono y descripcion.

**Commits**:
1. `feat(core): add LightGBM classifier with ML.NET`
2. `test(core): add LightGBM classifier tests`
3. `feat(web): integrate LightGBM in classification UI`

---

## Fase 3: SVM + Logistic Regression (PR #3, ~4 commits)

### 3A: SVM Classifier

- **Archivo**: `src/FuzzySat.Core/ML/SvmClassifier.cs` (~80 lineas)
- **Detalle**: Extiende `MlClassifierBase`. Trainer:
  `mlContext.MulticlassClassification.Trainers.OneVersusAll(mlContext.BinaryClassification.Trainers.LinearSvm())`.
  No requiere NuGet nuevo (incluido en Microsoft.ML 5.0.0).

### 3B: Logistic Regression Classifier

- **Archivo**: `src/FuzzySat.Core/ML/LogisticRegressionClassifier.cs` (~80 lineas)
- **Detalle**: Extiende `MlClassifierBase`. Trainer:
  `mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy()`.
  No requiere NuGet nuevo.

### 3C: Tests

- **Archivos**:
  - `tests/FuzzySat.Core.Tests/ML/SvmClassifierTests.cs` (~60 lineas)
  - `tests/FuzzySat.Core.Tests/ML/LogisticRegressionClassifierTests.cs` (~60 lineas)

### 3D: Web Integration

- **Archivo**: `src/FuzzySat.Web/Services/HybridClassificationService.cs` — agregar cases
- **Archivo**: `src/FuzzySat.Web/Components/Pages/Classification.razor` — agregar a dropdown

**Commits**:
1. `feat(core): add SVM classifier (LinearSvm/OVA)`
2. `feat(core): add Logistic Regression classifier (LbfgsMaximumEntropy)`
3. `test(core): add SVM and Logistic Regression tests`
4. `feat(web): integrate SVM and Logistic Regression in UI`

---

## Fase 4: Neural Network / TorchSharp (PR #4, ~5 commits)

### 4A: NuGet Setup

- **Archivo**: `src/FuzzySat.Core/FuzzySat.Core.csproj`
- **Detalle**: Agregar:
  ```xml
  <PackageReference Include="TorchSharp" Version="0.105.0" />
  <PackageReference Include="TorchSharp-cpu" Version="0.105.0"
                    Condition="'$(OS)' == 'Windows_NT'" />
  ```
  Verificar `dotnet restore` + `dotnet build` exitoso antes de continuar.

### 4B: DTOs de Entrenamiento

- **Archivos**:
  - `src/FuzzySat.Core/ML/NeuralNetTrainingOptions.cs` (~30 lineas)
  - `src/FuzzySat.Core/ML/TrainingProgressInfo.cs` (~20 lineas)
- **Detalle**: Records con hiperparametros (MaxEpochs=200, BatchSize=64, LR=0.001,
  WeightDecay=1e-4, DropoutRate=0.3, PatienceEpochs=20, ValidationSplit=0.2, RandomSeed=42).

### 4C: SpectralMLP + NeuralNetClassifier

- **Archivos**:
  - `src/FuzzySat.Core/ML/SpectralMLP.cs` (~80 lineas)
  - `src/FuzzySat.Core/ML/NeuralNetClassifier.cs` (~180 lineas)
- **Detalle**:
  - `SpectralMLP` — nn.Module: `Linear(N,128)→BN→ReLU→Drop(0.3)→Linear(128,64)→BN→ReLU→Drop(0.3)→Linear(64,C)→LogSoftmax`
  - `NeuralNetClassifier` — implementa IClassifier + IDisposable. Metodos:
    - `static Train(samples, extractor, options, progress?, ct)` — factory
    - `ClassifyPixel(bandValues)` — single pixel (thread-safe con lock)
    - `ClassifyBatch(float[][])` — batch prediction para imagenes completas
    - `PredictProbabilities(bandValues)` — para confidence maps
    - `Save(path)` / `static Load(path, extractor)` — persistencia
  - Training: Adam optimizer, NLLLoss con class weights, early stopping, LR scheduling
  - Data augmentation: Gaussian noise injection, band dropout (prob 0.1)

### 4D: Tests

- **Archivo**: `tests/FuzzySat.Core.Tests/ML/NeuralNetClassifierTests.cs` (~100 lineas)
- **Detalle**: Entrenar con datos sinteticos, verificar clasificacion, test save/load round-trip,
  test batch prediction, test argumentos invalidos.

### 4E: Web Integration

- **Archivo**: `src/FuzzySat.Web/Services/HybridClassificationService.cs` o nuevo
  `NeuralNetClassificationService.cs` — con training progress reporting
- **Archivo**: `src/FuzzySat.Web/Components/Pages/Classification.razor` — dropdown + progress de epochs

**Commits**:
1. `chore(core): add TorchSharp NuGet packages`
2. `feat(core): add NeuralNetTrainingOptions and TrainingProgressInfo DTOs`
3. `feat(core): implement SpectralMLP and NeuralNetClassifier`
4. `test(core): add NeuralNetClassifier tests`
5. `feat(web): integrate MLP Neural Network in classification UI`

---

## Fase 5: Ensemble Methods (PR #5, ~5 commits)

### 5A: Voting Ensemble

- **Archivo**: `src/FuzzySat.Core/ML/EnsembleClassifier.cs` (~120 lineas)
- **Detalle**: Implementa IClassifier. Constructor recibe `IReadOnlyList<(IClassifier, double weight)>`.
  Estrategias: MajorityVote (peso=1 para todos), WeightedVote (peso proporcional a OA en CV).

### 5B: Stacking Ensemble

- **Archivo**: `src/FuzzySat.Core/ML/StackingClassifier.cs` (~150 lineas)
- **Detalle**: Level 0: N clasificadores base producen predicciones out-of-fold (k-fold para
  evitar data leakage). Level 1: meta-learner (LogisticRegression) entrenado sobre predicciones
  concatenadas de base classifiers. Implementa IClassifier.

### 5C: Tests

- **Archivos**:
  - `tests/FuzzySat.Core.Tests/ML/EnsembleClassifierTests.cs` (~60 lineas)
  - `tests/FuzzySat.Core.Tests/ML/StackingClassifierTests.cs` (~60 lineas)

### 5D: Web Service

- **Archivo**: `src/FuzzySat.Web/Services/EnsembleClassificationService.cs` (~100 lineas)
- **Detalle**: Orquesta entrenamiento de multiples clasificadores con progress reporting.
  Reporta: "Training classifier 1/N...", "Training meta-learner...", "Classifying pixels...".

### 5E: UI

- **Archivo**: `src/FuzzySat.Web/Components/Pages/Classification.razor`
- **Detalle**: Agregar "Ensemble (Voting)" y "Ensemble (Stacking)" al dropdown.
  Cuando se selecciona ensemble, mostrar checkboxes para seleccionar clasificadores base.

**Commits**:
1. `feat(core): add EnsembleClassifier with voting strategies`
2. `feat(core): add StackingClassifier with meta-learner`
3. `test(core): add Ensemble and Stacking tests`
4. `feat(web): add EnsembleClassificationService`
5. `feat(web): add ensemble method selection UI`

---

## Fase 6: Model Comparison Framework (PR #6, ~4 commits)

### 6A: Comparison Engine

- **Archivo**: `src/FuzzySat.Core/ML/ModelComparisonEngine.cs` (~130 lineas)
- **Detalle**: Recibe lista de `(string Name, Func<..., IClassifier> Factory)`. Ejecuta
  CrossValidator para cada uno. Retorna `ModelComparisonResult` con ranking por OA/Kappa.

### 6B: Result Types

- **Archivo**: `src/FuzzySat.Core/ML/ModelComparisonResult.cs` (~60 lineas)
- **Detalle**: `ClassifierResult { Name, MeanOA, StdOA, MeanKappa, StdKappa, TrainingTimeMs }`.
  Property `BestModel` retorna el de mayor MeanKappa.

### 6C: Web Service + Pagina

- **Archivos**:
  - `src/FuzzySat.Web/Services/ModelComparisonService.cs` (~80 lineas)
  - `src/FuzzySat.Web/Components/Pages/ModelComparison.razor` (~250 lineas)
- **Detalle**: Pagina con:
  - Seleccion de metodos a comparar (checkboxes)
  - Numero de folds (dropdown: 3, 5, 10)
  - Boton "Run Comparison"
  - Tabla de resultados ordenable (Name, OA +/- std, Kappa +/- std, Time)
  - Bar chart (Radzen) de OA por metodo
  - Boton "Use Best Model" que navega a Classification con el metodo seleccionado

### 6D: Tests

- **Archivo**: `tests/FuzzySat.Core.Tests/ML/ModelComparisonEngineTests.cs` (~80 lineas)

**Commits**:
1. `feat(core): add ModelComparisonEngine with k-fold CV`
2. `feat(core): add ModelComparisonResult types`
3. `feat(web): add ModelComparison page with chart`
4. `test(core): add ModelComparisonEngine tests`

---

## Resumen de Esfuerzo por Fase

| Fase | Tareas | Commits | Lineas Nuevas (est.) | Prioridad |
|------|--------|---------|---------------------|-----------|
| 1 — Base Refactor | 3 | 3 | ~360 | P0 (prerequisito) |
| 2 — LightGBM | 3 | 3 | ~280 | P1 |
| 3 — SVM + LR | 4 | 4 | ~330 | P1 |
| 4 — Neural Net | 5 | 5 | ~490 | P2 |
| 5 — Ensemble | 5 | 5 | ~490 | P2 |
| 6 — Comparison | 4 | 4 | ~600 | P2 |
| **Total** | **24** | **24** | **~2550** | |

---

## Verificacion

1. `dotnet build` despues de cada commit
2. `dotnet test` — todos los tests existentes + nuevos pasan
3. Test E2E manual:
   - Cargar proyecto con datos de entrenamiento existentes
   - Clasificar con cada metodo individualmente (Fuzzy, RF, SDCA, LightGBM, SVM, LR, MLP)
   - Verificar que resultados se persisten y restauran
   - Clasificar con ensemble (Voting con 3 base classifiers)
   - Clasificar con ensemble (Stacking con 3 base classifiers)
   - Ejecutar Model Comparison con 5-fold CV
   - Verificar tabla comparativa con OA, Kappa, tiempo
   - Click "Use Best Model" y verificar que navega correctamente

---

## Siguiente Paso

Ver [02-technical-design.md](02-technical-design.md) para decisiones de arquitectura y riesgos.
