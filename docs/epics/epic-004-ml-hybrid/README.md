# Epic #4 - ML Hybrid

**Status**: CASI COMPLETADO (~90%)
**Priority**: P3
**Depends on**: Epic #3 (Advanced Features) — completado en lo relevante
**Estimated effort**: Bajo (falta benchmark orchestrator)

---

## Problem

La logica difusa pura puede mejorar combinandose con ML. Los membership degrees son features
excelentes para modelos supervisados.

## Solution

1. ML.NET integration
2. Usar fuzzy membership degrees como features para random forest y SDCA
3. Automated training area suggestion via K-Means clustering

## Micro-Commits — Estado Real

### Implementado
- [x] MC#1: ML.NET project setup + dependencies (ML 5.0.0 + FastTree 5.0.0 en Core.csproj)
- [x] MC#2: FuzzyFeatureExtractor — extrae raw bands + membership degrees + firing strengths
- [x] MC#3: HybridClassifier con Random Forest (FastForest/OVA) — thread-safe, configurable
- [x] MC#4: HybridClassifier con SDCA MaximumEntropy — alternativa a RF
- [x] MC#5: KMeansClusterer para training area suggestion — completo con predict

### Parcial
- [ ] MC#6: Benchmark comparison (pure fuzzy vs hybrid) — **no hay orchestrator dedicado**
  - Las piezas existen (ambos classifiers implementan IClassifier, ConfusionMatrix disponible)
  - Falta: clase `ClassifierBenchmark` que ejecute ambos y compare metricas

### Nota sobre MC#4 original
El plan original decia "Neural network classifier". Se implemento SDCA MaximumEntropy en su
lugar, que es mas apropiado para el tamano del dataset. Decision correcta.

## Lo que Falta para Completar

1. **ClassifierBenchmark** (opcional) — orchestrator que ejecute fuzzy puro vs hybrid y
   genere tabla comparativa de metricas. Las piezas Core existen; falta el pegamento.

## Acceptance Criteria

- [ ] Hybrid classifier mejora accuracy sobre fuzzy puro en al menos un dataset (pendiente: necesita datos reales)
- [x] K-Means sugiere areas de entrenamiento (implementado + tests con datos sinteticos)
- [x] Pipeline configurable: fuzzy-only via FuzzyClassifier, hybrid via HybridClassifier

## Feature Vectors

| Sensor | Bandas | Clases | Raw | Membership | Firing | Total |
|--------|--------|--------|-----|------------|--------|-------|
| ASTER | 4 | 7 | 4 | 28 | 7 | **39** |
| Sentinel-2 | 13 | 7 | 13 | 91 | 7 | **111** |

## Test Coverage

- FuzzyFeatureExtractorTests: 5 tests
- HybridClassifierTests: 3 tests (RF + SDCA + validation)
- KMeansClustererTests: 5 tests
- **Total ML tests: 13**
