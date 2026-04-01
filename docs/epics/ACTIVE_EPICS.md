# EPICs Activos - FuzzySat

> **Ultima actualizacion**: 2026-04-01 (post PR #38 — Homepage redesign)
> **Proposito**: Punto central de informacion de EPICs en desarrollo.
> Para EPICs completados ver [EPIC_HISTORY.md](EPIC_HISTORY.md)

---

## Resumen por Estado

| Estado | Cantidad | EPICs |
|--------|----------|-------|
| Completado | 5 | #1 Core Engine MVP, #3 Advanced Features, #7 Classified Output, #8 Project Persistence, #9 Advanced ML Classifiers |
| Casi Completado | 2 | #2 I/O & CLI (~80%), #4 ML Hybrid (~90%) |
| Parcial | 1 | #5 Blazor Web (~85%) |
| En Progreso | 1 | #10 Input UX Redesign & Homepage (~75%) |
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

- **Status**: **~78% completado** (subio de 70% tras Fase 3D)
- **Priority**: P3
- **Folder**: [epic-005-blazor-web/](epic-005-blazor-web/)
- **Security**: UNC rejection, OOM guard (50M px), path traversal, raster whitelist
- **Tests Web**: 51 tests (ProjectLoader + Raster + Classification + Validation + PixelExtraction)

**Completado**: Blazor setup, Layout, Home, ProjectSetup (save/load/RasterInfo),
ConfusionMatrixHeatmap (colores dinamicos), DI services, BandViewer (real stats/histogram/preview
via SkiaSharp), Training interactivo (dibujar rectangulos sobre imagen de banda, dual mode
draw/csv, spectral chart, extract/export session), PixelExtractionService,
training-selection.js (canvas overlay), TrainingService, ClassificationService (async, 4 MF
types, Product AND, progress reporting), ValidationService (ConfusionMatrix from CSV samples,
CSV export), Classification page con FuzzyClassifier real, Validation page con confusion matrix
real + CSV upload, file-download.js interop.
**Pendiente**: Leaflet maps, History page, API controllers.
**Herramientas**: S2Preprocess (Sentinel-2 SAFE → multiband GeoTIFF).

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

---

## Epic #7 - Classified Image Output & Hybrid ML — COMPLETADO

- **Status**: **COMPLETADO** (PR #29 mergeado 2026-03-31)
- **Priority**: P1
- **Folder**: [epic-007-classified-output/](epic-007-classified-output/)
- **GitHub Issue**: [#27](https://github.com/ivanrlg/FuzzySat/issues/27) (closed)
- **Creado**: 2026-03-31
- **Micro-commits**: 12 commits, 1 PR
- **Tests**: 19 nuevos (6 Hybrid + 7 Renderer + 6 AreaCalculator), 354 total

**Completado**: HybridClassificationService (Random Forest + SDCA en Web), ClassifiedImageRenderer
(colores auto+editables, keyword matching bilingue), classification-canvas.js (side-by-side viewer
con zoom/pan sincronizado, tooltip hover, 4 modos de vista), AreaCalculator (m2/ha por clase),
tabla de estadisticas con charts (pie + bar), exportacion GeoTIFF, CSV mejorado con area,
color picker editable, ProjectStateService.ClassColors.

**Documentos**:
- [00-overview.md](epic-007-classified-output/00-overview.md) — Objetivo y alcance
- [01-plan.md](epic-007-classified-output/01-plan.md) — Plan de implementacion por fases
- [02-technical-design.md](epic-007-classified-output/02-technical-design.md) — Decisiones tecnicas

---

## Epic #8 - Full Project Persistence — COMPLETADO

- **Status**: **COMPLETADO** (PR #30 mergeado 2026-03-31)
- **Priority**: P0
- **Folder**: [epic-008-project-persistence/](epic-008-project-persistence/)
- **GitHub Issue**: [#28](https://github.com/ivanrlg/FuzzySat/issues/28) (closed)
- **Creado**: 2026-03-31

**Completado**: Persistencia automatica completa de todos los artefactos del proyecto (regiones,
muestras, sesion de entrenamiento, clasificacion, validacion). Zero data loss al cerrar navegador.
Auto-save, auto-load, compatibilidad retroactiva con proyectos existentes.

**Documentos**:
- [00-overview.md](epic-008-project-persistence/00-overview.md) — Objetivo y alcance
- [01-plan.md](epic-008-project-persistence/01-plan.md) — Plan de implementacion por fases
- [02-technical-design.md](epic-008-project-persistence/02-technical-design.md) — Decisiones tecnicas

---

## Epic #9 - Advanced ML Classifiers & Ensemble Methods — COMPLETADO

- **Status**: **COMPLETADO**
- **Priority**: P2
- **Folder**: [epic-009-ml-advanced-classifiers/](epic-009-ml-advanced-classifiers/)
- **GitHub PR**: [#33](https://github.com/ivanrlg/FuzzySat/pull/33), [#35](https://github.com/ivanrlg/FuzzySat/pull/35), [#37](https://github.com/ivanrlg/FuzzySat/pull/37) (all merged)
- **Tests**: 302 Core tests

**Implementado**:
- 7 clasificadores: Random Forest, SDCA, LightGBM, SVM, Logistic Regression, MLP Neural Network, KMeans
- 2 ensembles: Voting (majority + weighted), Stacking (meta-learner)
- IFeatureExtractor interface + RawFeatureExtractor (fix #34 — meta-learner degraded features)
- Pure ML mode: 6 clasificadores con raw bands sin fuzzy features (issue #36)
- CrossValidator (stratified k-fold) + ModelComparisonEngine + Blazor UI
- MlClassifierBase abstract class (shared ML.NET pipeline)
- UI agrupada por categoria: Fuzzy / Hybrid / Pure ML / Ensemble

**Documentos**:
- [00-overview.md](epic-009-ml-advanced-classifiers/00-overview.md) — Objetivo y alcance
- [01-plan.md](epic-009-ml-advanced-classifiers/01-plan.md) — Plan de implementacion por fases
- [02-technical-design.md](epic-009-ml-advanced-classifiers/02-technical-design.md) — Decisiones tecnicas

---

## Epic #10 - Input UX Redesign & Homepage Overhaul — EN PROGRESO

- **Status**: **~75% completado** (Fases 1-2-4 mergeadas, Fase 3 pendiente)
- **Priority**: P1
- **Folder**: [epic-010-input-ux-homepage/](epic-010-input-ux-homepage/)
- **GitHub PR**: [#32](https://github.com/ivanrlg/FuzzySat/pull/32) (merged), [#38](https://github.com/ivanrlg/FuzzySat/pull/38) (merged)
- **Creado**: 2026-03-31
- **Prerequisito**: Epic #8 completado (PR #30)

**Objetivo**: Redisenar el flujo de importacion raster (punto mas debil del UX actual) y
reorganizar el homepage para enfocarse en el uso de la herramienta, moviendo datos de la
tesis a una pagina dedicada.

**Completado (PR #32)**:
- Fase 1: Componentizar ProjectSetup.razor (848→280 lineas, 5 componentes modulares)
- Fase 2: Smart Import Wizard (auto-detect Sentinel-2, auto-build VRT, preset dinamico,
  band names B01 normalizados, CancellationToken, IDisposable)
- Fixes adicionales: spectral chart auto-scaling, thinner selection borders,
  new project state reset, 3 rondas de review bot resueltas

**Completado (PR #38)**:
- Fase 4: Homepage profesional — stats de herramienta (6 ML models, 3 modos, 4 MF types,
  13 bands), workflow actualizado (5 pasos matching NavMenu), about section con mencion
  de tesis, link a Copernicus Browser, step badges consistentes en todas las paginas

**Pendiente**:
3. Soporte imagenes pre-stacked + conversion in-app

**Documentos**:
- [00-overview.md](epic-010-input-ux-homepage/00-overview.md) — Objetivo y alcance
- [01-plan.md](epic-010-input-ux-homepage/01-plan.md) — Plan de implementacion por fases
- [02-technical-design.md](epic-010-input-ux-homepage/02-technical-design.md) — Decisiones tecnicas
