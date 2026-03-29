# Project TODOs - FuzzySat

> **Archivo central de tracking de progreso.**
> Ultima actualizacion: 2026-03-29
> **Reconciliado**: Alineado con auditoria de codigo real (Epic #6 gap analysis)

---

## Estado Actual

> **Proyecto al ~58% global** (subio de 55% tras Fase 3A)
> - Core Engine: **100%** (PCA implementado, 242 Core tests)
> - CLI: **80%** funcional (4 comandos wired, falta VisualizeCommand)
> - API: 0% (template default)
> - Web: **42%** (DI services, security fixes, 18 Web tests, falta wiring pages)
> - CI/CD: 30% (solo build+test)
> - Tests totales: **260** (242 Core + 18 Web)

---

## Completado

### Scaffolding (2026-03-28)
- [x] Crear repositorio Git + GitHub (privado)
- [x] Estructura de carpetas (src/, tests/, docs/, task/, samples/)
- [x] CLAUDE.md con metodologia completa
- [x] docs/claude/ (Golden Rules, Git Safety, Checklists)
- [x] docs/epics/ (ACTIVE_EPICS, EPIC_HISTORY, 5 epic folders)
- [x] README.md con Mermaid diagrams, badges, thesis citation
- [x] .gitignore, LICENSE (MIT), CI workflow
- [x] Merge a main

### Epic #1 — Core Engine MVP (COMPLETADO)
- [x] Solution (.sln) y 4 proyectos (.csproj) con NuGet packages
- [x] IMembershipFunction + GaussianMembershipFunction + tests
- [x] TriangularMembershipFunction + TrapezoidalMembershipFunction + BellMF + tests
- [x] FuzzyOperators (And=Min, Or=Max, ProductAnd) + tests
- [x] FuzzyRule + FuzzyRuleSet + tests
- [x] InferenceEngine + InferenceResult + tests
- [x] MaxWeightDefuzzifier + WeightedAverageDefuzzifier + tests
- [x] FuzzyClassifier (orchestrator) + tests
- [x] SpectralStatistics + TrainingDataExtractor + tests
- [x] TrainingSession + JSON serialization + BuildRuleSet + tests
- [x] ConfusionMatrix (81.87% OA, K=0.7637 verificado) + tests
- [x] AccuracyMetrics + ClassMetrics + tests
- [x] Band + MultispectralImage + PixelVector models + tests
- [x] ClassificationResult + LandCoverClass + ConfidenceMapGenerator + tests
- [x] ClassifierConfiguration + BandConfiguration + tests

### Epic #2 — I/O (Core portion completada)
- [x] IRasterReader + GdalRasterReader (thread-safe GDAL init)
- [x] IRasterWriter + GdalRasterWriter (GTiff output)
- [x] RasterInfo metadata model
- [x] CLI Program.cs + System.CommandLine setup
- [x] Sample config JSON (samples/sample-project.json)

### Epic #3 — Advanced Features (mayoria completada)
- [x] TriangularMembershipFunction + tests
- [x] TrapezoidalMembershipFunction + tests
- [x] BellMembershipFunction (Generalized Bell) + tests
- [x] ProductAndOperator + tests
- [x] WeightedAverageDefuzzifier + tests
- [x] SpectralIndexCalculator (NDVI, NDWI, NDBI) + tests
- [x] ConfidenceMapGenerator + tests

### Epic #4 — ML Hybrid (mayoria completada)
- [x] ML.NET dependencies (ML 5.0.0 + FastTree 5.0.0)
- [x] FuzzyFeatureExtractor (raw bands + membership degrees + firing strengths)
- [x] HybridClassifier — Random Forest (FastForest/OVA)
- [x] HybridClassifier — SDCA MaximumEntropy
- [x] KMeansClusterer + tests

### Epic #5 — Blazor Web (UI scaffolding)
- [x] Blazor Server project + Radzen 10.0.6
- [x] MainLayout + NavMenu (sidebar, breadcrumbs)
- [x] Home page (hero, stats, workflow cards)
- [x] ProjectSetup page — FUNCIONAL (save JSON, reset, presets)
- [x] ConfusionMatrixHeatmap component (reusable, color-coded)

### Epic #5 — UI Deep Orbit Redesign
- [x] Dark theme "Deep Orbit" con CSS variables
- [x] Responsive layout

### Epic #6 — Gap Analysis Documentation
- [x] 00-overview.md (inventario de 14 issues)
- [x] 01-plan.md (72 tareas en 7 fases)
- [x] 02-technical-design.md (decisiones tecnicas y riesgos)

### Fase 0 — Reconciliacion de EPICs (PR #14)
- [x] Auditoria de los 5 EPICs contra codigo real
- [x] Actualizar READMEs con estados reales
- [x] Actualizar ACTIVE_EPICS.md y EPIC_HISTORY.md
- [x] Actualizar task/todo.md (este archivo)

### Fase 1 — Core 100% (PR #15)
- [x] PCA implementation (PcaTransformer via ML.NET, 9 tests)
- [x] GeoTransform en RasterInfo + GdalRasterReader/Writer (preserva georeferencing)
- [x] TrainingSessionDto compartido en Core (FromSession/ToSession)

### Fase 2 — CLI Wiring (PR #15)
- [x] Cablear InfoCommand a GdalRasterReader.ReadInfo() + Spectre table
- [x] Cablear TrainCommand a TrainingSession + CSV parser + JSON export
- [x] Cablear ClassifyCommand a FuzzyInferenceEngine + GdalRasterWriter + progress bar
- [x] Cablear ValidateCommand a ConfusionMatrix + per-class metrics table
- [x] Spectre.Console output (tablas, progress bars, colores)
- [x] CSV validation (header check, band name trimming, duplicates)

---

## Pendiente — Roadmap Priorizado

> Ver [Epic #6 plan](../docs/epics/epic-006-gap-to-100/01-plan.md) para detalle completo.

### Fase 2 — CLI Pendiente
- [ ] Crear VisualizeCommand (false color composite → PNG)
- [ ] Tests CLI

### Fase 3A — Web Infrastructure (PR #17) — COMPLETADA
- [x] ProjectStateService (scoped, cross-page state bus)
- [x] RasterService (singleton, GDAL wrapper, band statistics, path validation)
- [x] ProjectLoaderService (save/load/list, path traversal defense)
- [x] ProjectStorageOptions (configurable via appsettings.json)
- [x] BandStatistics record (IReadOnlyList<long> histogram)
- [x] Fix #12: Path configurable (IOptions, defaults to ApplicationData)
- [x] Fix #13: Filename sanitization (reserved names, extensions, length)
- [x] Fix #14: Info leak en toasts (ILogger server-side)
- [x] Security: path traversal defense (ResolveSafePath + directory separator rejection)
- [x] Security: raster extension whitelist (.tif, .tiff, .img, .hdf, .nc, .jp2, .vrt)
- [x] 18 Web tests (ProjectLoaderServiceTests + RasterServiceTests)

### Fase 3B — BandViewer + Training (Siguiente)
- [ ] Wire Program.cs, _Imports.razor, appsettings.json con DI services
- [ ] Wire ProjectSetup.razor con ProjectStateService + RasterInfo display
- [ ] SkiaSharp band preview rendering (server-side PNG)
- [ ] BandViewer con bandas reales (stats, histogram, preview image)
- [ ] TrainingService + CSV upload para training samples
- [ ] Training page con datos reales (spectral chart, extract stats, export)

### Fase 3C — Classification + Validation (Pendiente)
- [ ] ClassificationService (async, IProgress, CancellationToken)
- [ ] Classification conectada a FuzzyClassifier con progreso real
- [ ] Classification result preview (colored PNG) + export GeoTIFF
- [ ] Validation con ConfusionMatrix real + ground truth CSV upload
- [ ] Export CSV real

### Fase 3D — Leaflet Maps (Deferred)
- [ ] leaflet-interop.js + LeafletMap.razor
- [ ] Drawing tools para training areas
- [ ] History page

### Fase 4 — API (P2)
- [ ] Eliminar weather forecast template
- [ ] Controllers: Info, Train, Classify, Validate, Export
- [ ] DTOs + Swagger/OpenAPI
- [ ] Error handling middleware
- [ ] Tests API

### Fase 5 — CI/CD (P2)
- [ ] Code coverage threshold en CI
- [ ] Format check (dotnet format)
- [ ] Dockerfile multi-stage
- [ ] docker-compose.yml
- [ ] GitHub Release workflow
- [ ] Dependabot + CodeQL

### Fase 6 — Docs & Polish (P3)
- [ ] CONTRIBUTING.md
- [ ] CHANGELOG.md
- [ ] User Guide (Web)
- [ ] CLI Reference
- [ ] Sample raster data (sintetico, descargable)
- [ ] README update (screenshots, badges reales)
