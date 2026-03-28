# Project TODOs - FuzzySat

> **Archivo central de tracking de progreso.**
> Ultima actualizacion: 2026-03-28

---

## Estado Actual

> **EN PROGRESO**: Scaffolding del proyecto
> - Branch: `feature/init-project-scaffolding`
> - PR: Pendiente
> - Status: Creando estructura del repo, docs, CI

---

## Completado

### Scaffolding (2026-03-28)
- [x] Crear repositorio Git
- [x] Estructura de carpetas (src/, tests/, docs/, task/, samples/)
- [x] CLAUDE.md con metodologia completa
- [x] docs/claude/ (Golden Rules, Git Safety, Checklists)
- [x] docs/epics/ (ACTIVE_EPICS, EPIC_HISTORY, 5 epic folders)
- [x] docs/troubleshooting/ (templates)
- [x] README.md con Mermaid diagrams, badges, thesis citation
- [x] .gitignore (.NET + satellite imagery)
- [x] LICENSE (MIT)
- [x] .github/workflows/build.yml (CI skeleton)
- [x] task/todo.md (este archivo)
- [ ] Crear repo en GitHub (privado)
- [ ] Crear primer PR
- [ ] Review de bots
- [ ] Merge a main

---

## En Progreso

*(Nada adicional en progreso)*

---

## Roadmap

### Phase 1 - Epic #1: Core Engine MVP
- [ ] Crear solution (.sln) y proyectos (.csproj)
- [ ] IMembershipFunction + GaussianMembershipFunction + tests
- [ ] FuzzyOperators (And=Min, Or=Max) + tests
- [ ] FuzzyRule + FuzzyRuleSet + tests
- [ ] InferenceEngine + tests
- [ ] MaxWeightDefuzzifier + tests
- [ ] FuzzyClassifier (orchestrator) + tests
- [ ] SpectralStatistics + TrainingDataExtractor + tests
- [ ] ConfusionMatrix + KappaStatistic + tests (validar vs tesis)
- [ ] Band + MultispectralImage + PixelVector models
- [ ] ClassificationResult + LandCoverClass models
- [ ] Configuration models (JSON)

### Phase 2 - Epic #2: I/O & CLI
- [ ] GdalRasterReader + GdalRasterWriter
- [ ] CLI commands (train, classify, validate, visualize, info)
- [ ] Sample config JSON
- [ ] Integration tests con GeoTIFF de prueba

### Phase 3 - Epic #3: Advanced Features
- [ ] MFs adicionales (triangular, trapezoidal, bell)
- [ ] Operador producto
- [ ] Indices espectrales (NDVI, NDWI, NDBI)
- [ ] PCA
- [ ] Confidence maps

### Phase 4 - Epic #4: ML Hybrid
- [ ] ML.NET integration
- [ ] Fuzzy features -> ML pipeline
- [ ] K-Means clustering

### Phase 5 - Epic #5: Blazor Web App
- [ ] Blazor Server setup
- [ ] Leaflet.js map integration
- [ ] Training editor
- [ ] Classification with real-time progress
- [ ] Confusion matrix UI
- [ ] Export (GeoTIFF, JSON, PDF)
