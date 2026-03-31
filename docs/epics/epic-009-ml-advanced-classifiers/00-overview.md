# Epic #9 — Advanced ML Classifiers & Ensemble Methods

> **Status**: Planificado
> **Priority**: P2
> **Creado**: 2026-03-31
> **Issue**: [#31](https://github.com/ivanrlg/FuzzySat/issues/31)
> **Prerequisito**: Epic #8 completado (persistencia de proyecto)

---

## Objetivo

Ampliar el pipeline de clasificacion hibrida de FuzzySat con clasificadores de machine learning
avanzados y metodos ensemble. Actualmente solo se dispone de Random Forest y SDCA (Epic #4).
Este epic agrega **LightGBM** (gradient boosting), **SVM**, **Logistic Regression**,
**MLP Neural Network** (via TorchSharp), metodos **Ensemble** (Voting y Stacking), y un
**framework de comparacion de modelos** con cross-validation.

El objetivo final es que el usuario pueda comparar automaticamente el rendimiento de cada metodo
contra el clasificador fuzzy original de la tesis (81.87% OA, Kappa 0.7637) y seleccionar el
mejor clasificador para su caso de uso.

---

## Alcance

| ID | Requerimiento | Criterio de Aceptacion |
|----|---------------|------------------------|
| R1 | LightGBM classifier | Clasifica pixeles usando gradient boosting con features fuzzy; test pasa con datos sinteticos |
| R2 | SVM classifier | Clasifica pixeles usando LinearSvm/OVA; test pasa con datos sinteticos |
| R3 | Logistic Regression classifier | Clasifica pixeles usando LbfgsMaximumEntropy; test pasa con datos sinteticos |
| R4 | MLP Neural Network | Red neuronal de 3 capas (TorchSharp) con BatchNorm, Dropout, early stopping; test pasa |
| R5 | Ensemble Voting | Combina N clasificadores por voto mayoritario o ponderado; test pasa |
| R6 | Ensemble Stacking | Meta-learner entrenado sobre predicciones out-of-fold de clasificadores base; test pasa |
| R7 | Cross-validation | K-fold estratificado que computa OA y Kappa por fold; test pasa |
| R8 | Model Comparison Framework | Ejecuta CV para todos los metodos y genera tabla comparativa con ranking |
| R9 | Integracion Web | Todos los metodos disponibles en la UI de clasificacion con progress reporting |
| R10 | Persistencia de modelos | Modelos entrenados se guardan/restauran via IProjectRepository |

---

## Codigo Existente a Reutilizar

| Componente | Ubicacion | Uso |
|------------|-----------|-----|
| `IClassifier` | `Core/FuzzyLogic/Classification/IClassifier.cs` | Todos los clasificadores lo implementan |
| `HybridClassifier` | `Core/ML/HybridClassifier.cs` | Patron Train + factory method; base para refactoring |
| `FuzzyFeatureExtractor` | `Core/ML/FuzzyFeatureExtractor.cs` | Pipeline de features (raw + MF degrees + firing strengths) |
| `HybridClassificationService` | `Web/Services/HybridClassificationService.cs` | Patron async Web con progress reporting |
| `ConfusionMatrix` + `AccuracyMetrics` | `Core/Validation/` | Metricas de validacion por fold y globales |
| `IProjectRepository` | `Core/Persistence/IProjectRepository.cs` | Interfaz de persistencia a extender |
| `FileProjectRepository` | `Web/Services/FileProjectRepository.cs` | Implementacion concreta de persistencia |
| `ClassificationOptionsDto` | `Core/Persistence/ClassificationOptionsDto.cs` | Ya tiene campo `ClassificationMethod` extensible |

---

## Fuera de Alcance

- CNNs sobre patches de imagen (requiere ventanas deslizantes, no per-pixel)
- Transfer learning con modelos pre-entrenados de imagenes (ResNet, etc.)
- GPU como requisito obligatorio (CPU es el default; GPU es opcional)
- Hyperparameter tuning automatico (AutoML) — se usaran defaults razonables
- Soporte para otros frameworks (ONNX, TensorFlow) — solo ML.NET y TorchSharp

---

## Metricas de Exito

1. **Todos los clasificadores** producen resultados de clasificacion validos en la UI Web
2. **Cross-validation** reporta OA y Kappa consistentes (stddev < 5% entre folds)
3. **Model Comparison** genera tabla ordenable con ranking automatico
4. **Tests**: minimo 2 tests por clasificador (entrenamiento + clasificacion correcta)
5. **Backward compatible**: fuzzy puro, Random Forest y SDCA siguen funcionando sin cambios
6. **Performance**: entrenamiento de MLP < 30 segundos para 1000 muestras en CPU

---

## Entregables

| PR | Contenido | Commits Estimados |
|----|-----------|-------------------|
| PR #1 | Refactoring base: MlClassifierBase + CrossValidator | ~3 |
| PR #2 | LightGBM classifier + tests + Web | ~3 |
| PR #3 | SVM + Logistic Regression + tests + Web | ~4 |
| PR #4 | MLP Neural Network (TorchSharp) + tests + Web | ~5 |
| PR #5 | Ensemble methods (Voting + Stacking) + tests + Web | ~5 |
| PR #6 | Model Comparison Framework + UI + tests | ~4 |
| **Total** | | **~24 micro-commits, 6 PRs** |
