# EPICs Activos - FuzzySat

> **Ultima actualizacion**: 2026-03-30
> **Proposito**: Punto central de informacion de EPICs en desarrollo.
> Para EPICs completados ver [EPIC_HISTORY.md](EPIC_HISTORY.md)

---

## Resumen por Estado

| Estado | Cantidad | EPICs |
|--------|----------|-------|
| Completado | 2 | #1 Core Engine MVP, #3 Advanced Features |
| Casi Completado | 2 | #2 I/O & CLI (~80%), #4 ML Hybrid (~90%) |
| Parcial | 1 | #5 Blazor Web (~35%) |
| Documentacion | 1 | #6 Gap Analysis: Road to 100% |

---

## Epic #1 - Core Engine MVP — COMPLETADO

- **Status**: **COMPLETADO**
- **Priority**: P0-CRITICAL
- **Folder**: [epic-001-core-engine/](epic-001-core-engine/)
- **Micro-commits**: 15/15 completados
- **Tests**: 242 tests, 0 failures
- **Verificado**: 81.87% OA y K=0.7637 reproducidos

**Scope completado**: Motor de logica difusa completo: 4 membership functions (Gaussian,
Triangular, Trapezoidal, Bell), fuzzy rules, inference engine (AND=Min), defuzzifiers
(MaxWeight + WeightedAverage), training data extractor, confusion matrix, Kappa statistic.
Unit tests validados contra datos de la tesis original.

---

## Epic #2 - I/O & CLI — CASI COMPLETADO

- **Status**: **~80% completado**
- **Priority**: P1
- **Folder**: [epic-002-io-cli/](epic-002-io-cli/)
- **Micro-commits**: 10/11 completados
- **Pendiente**: VisualizeCommand, tests CLI

**Completado**: GDAL raster reader/writer, CLI structure, sample config, InfoCommand,
TrainCommand (CSV→JSON), ClassifyCommand (progress bar + GeoTIFF output con georeferencing),
ValidateCommand (confusion matrix + Kappa + per-class metrics). TrainingSessionDto compartido.
**Pendiente**: VisualizeCommand (false color composite → PNG), tests CLI.

---

## Epic #3 - Advanced Features — COMPLETADO

- **Status**: **COMPLETADO**
- **Priority**: P2
- **Folder**: [epic-003-advanced-features/](epic-003-advanced-features/)
- **Micro-commits**: 8/9 completados (config JSON es minor)

**Completado**: 3 MFs adicionales, operador producto, WeightedAverageDefuzzifier,
SpectralIndexCalculator (NDVI, NDWI, NDBI), ConfidenceMapGenerator, PcaTransformer
(ML.NET PCA con PredictionEngine cacheado, 9 tests).

---

## Epic #4 - ML Hybrid — CASI COMPLETADO

- **Status**: **~90% completado**
- **Priority**: P3
- **Folder**: [epic-004-ml-hybrid/](epic-004-ml-hybrid/)
- **Micro-commits**: 5/6 completados
- **Pendiente**: Benchmark orchestrator (fuzzy vs hybrid)

**Completado**: FuzzyFeatureExtractor (39-111 features), HybridClassifier (Random Forest +
SDCA MaximumEntropy), KMeansClusterer, 13 tests ML.
**Pendiente**: ClassifierBenchmark orchestrator (las piezas existen, falta el pegamento).

---

## Epic #5 - Blazor Web App — EN PROGRESO

- **Status**: **~70% completado** (subio de 60% tras Fase 3C)
- **Priority**: P3
- **Folder**: [epic-005-blazor-web/](epic-005-blazor-web/)
- **Security**: UNC rejection, OOM guard (50M px), path traversal, raster whitelist
- **Tests Web**: 41 tests (ProjectLoader + Raster + Classification + Validation)

**Completado**: Blazor setup, Layout, Home, ProjectSetup (save/load/RasterInfo),
ConfusionMatrixHeatmap (colores dinamicos), DI services, BandViewer (real stats/histogram/preview
via SkiaSharp), Training (CSV upload, spectral chart, extract/export session),
TrainingService, ClassificationService (async, 4 MF types, Product AND, progress reporting),
ValidationService (ConfusionMatrix from CSV samples, CSV export), Classification page con
FuzzyClassifier real, Validation page con confusion matrix real + CSV upload,
file-download.js interop.
**Siguiente**: Interactive Training Tool (seleccion de areas con mouse sobre imagen).
**Pendiente**: leaflet-interop.js, History page, API controllers.

---

## Epic #6 - Gap Analysis: Road to 100%

- **Status**: Documentacion (solo analisis, no codigo)
- **Priority**: P0 — Prerequisito para planificar trabajo restante
- **Folder**: [epic-006-gap-to-100/](epic-006-gap-to-100/)
- **Creado**: 2026-03-28

**Scope**: Auditoria exhaustiva del estado real del proyecto. Identifica 72 tareas en 7 fases
para llegar al 100%. Proyecto esta al ~45% global. Documenta decisiones tecnicas criticas.

**Documentos**:
- [00-overview.md](epic-006-gap-to-100/00-overview.md) — Inventario completo de gaps (14 issues Web)
- [01-plan.md](epic-006-gap-to-100/01-plan.md) — Roadmap priorizado por fases
- [02-technical-design.md](epic-006-gap-to-100/02-technical-design.md) — Decisiones tecnicas y riesgos
