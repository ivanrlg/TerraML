# Epic #4 - ML Hybrid

**Status**: Planificado
**Priority**: P3
**Depends on**: Epic #3 (Advanced Features)
**Estimated effort**: TBD

---

## Problem

La logica difusa pura puede mejorar combinandose con ML. Los membership degrees son features
excelentes para modelos supervisados.

## Solution

1. ML.NET integration
2. Usar fuzzy membership degrees como features para neural network o random forest
3. Automated training area suggestion via K-Means clustering

## Micro-Commits Planificados

- [ ] MC#1: ML.NET project setup + dependencies
- [ ] MC#2: Feature extractor (membership degrees -> ML features)
- [ ] MC#3: Random forest classifier using fuzzy features
- [ ] MC#4: Neural network classifier using fuzzy features
- [ ] MC#5: K-Means clustering for training area suggestion
- [ ] MC#6: Benchmark comparison (pure fuzzy vs hybrid)

## Acceptance Criteria

- [ ] Hybrid classifier mejora accuracy sobre fuzzy puro en al menos un dataset
- [ ] K-Means sugiere areas de entrenamiento razonables
- [ ] Pipeline es configurable (fuzzy-only, ML-only, hybrid)
