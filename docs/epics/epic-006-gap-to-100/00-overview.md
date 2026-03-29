# Epic #6 — Gap Analysis: Road to 100%

**Status**: Documentacion (solo analisis, no codigo)
**Priority**: P0 — Prerequisito para planificar el trabajo restante
**Creado**: 2026-03-28
**Tipo**: Auditoria tecnica + roadmap

---

## Objetivo

Catalogar de forma exhaustiva **todo lo que falta** para que FuzzySat sea un producto
completo y funcional al 100%. Esto incluye: codigo no conectado, UI mock/hardcodeada,
funcionalidades ausentes, y gaps entre lo que los EPICs originales (#1–#5) prometieron
vs lo que realmente existe hoy.

---

## Hallazgo Principal: Los EPICs Originales Estan Desactualizados

Los EPICs #1–#5 fueron escritos durante la fase de scaffolding y asumen que **nada**
esta implementado. La realidad en 2026-03-28 es muy diferente:

| Epic | Nombre | Estado Documentado | Estado Real | Gap |
|------|--------|--------------------|-------------|-----|
| #1 | Core Engine MVP | Planificado | **~95% implementado** | Tests ya pasan, clases existen |
| #2 | I/O & CLI | Planificado | **~50% implementado** | Core I/O listo, CLI stubs |
| #3 | Advanced Features | Planificado | **~80% implementado** | 4 MFs, 2 operadores, indices existen |
| #4 | ML Hybrid | Planificado | **~90% implementado** | FuzzyFeatureExtractor, HybridClassifier, KMeans existen |
| #5 | Blazor Web | Planificado | **~30% implementado** | UI mockups, no conectada a Core |

**Implicacion**: Los micro-commits originales de cada EPIC necesitan reconciliarse con
la realidad. Muchos items ya estan hechos. El trabajo restante es diferente al planeado.

---

## Inventario de Gaps por Area

### A. Core Engine (FuzzySat.Core) — 95% Completo

**Lo que EXISTE y FUNCIONA:**
- 4 Membership Functions: Gaussian, Triangular, Trapezoidal, Bell
- 2 AND Operators: Minimum, Product
- 2 Defuzzifiers: MaxWeight, WeightedAverage
- Inference Engine completo
- FuzzyClassifier (orquestador)
- Training: SpectralStatistics, TrainingDataExtractor, TrainingSession
- Validation: ConfusionMatrix, AccuracyMetrics, Kappa
- Raster: GdalRasterReader, GdalRasterWriter, SpectralIndexCalculator
- ML: FuzzyFeatureExtractor, HybridClassifier (Random Forest + SDCA)
- Configuration: BandConfiguration, ClassifierConfiguration
- 24 archivos de test con cobertura real

**Lo que FALTA:**
1. PCA (Principal Component Analysis) — mencionado en Epic #3, no implementado
2. Visualization module — carpeta existe pero contenido no verificado
3. K-Means Clusterer — existe en tests, verificar implementacion completa
4. ConfidenceMapGenerator — existe, verificar end-to-end
5. Reconciliar EPICs #1-#4 como "completados" en EPIC_HISTORY.md

---

### B. CLI (FuzzySat.CLI) — 20% Completo

**Lo que EXISTE:**
- Program.cs con System.CommandLine registrando 4 comandos
- TrainCommand.cs, ClassifyCommand.cs, ValidateCommand.cs, InfoCommand.cs
- Argument parsing funciona

**Lo que FALTA (4 comandos son stubs):**
1. `train` — Imprime "not yet implemented", no llama a TrainingSession
2. `classify` — Imprime "not yet implemented", no llama a FuzzyClassifier
3. `validate` — Imprime "not yet implemented", no llama a ConfusionMatrix
4. `info` — Imprime "not yet implemented", no llama a GdalRasterReader
5. `visualize` — Comando no existe (mencionado en Epic #2)
6. Output con Spectre.Console — No hay tablas, progress bars, ni colores
7. JSON config loading — No conectado (ClassifierConfiguration existe en Core)

**Esfuerzo estimado**: Medio. Las clases Core existen; falta cablear.

---

### C. API (FuzzySat.Api) — 0% Completo

**Lo que EXISTE:**
- Program.cs con endpoint `/weatherforecast` (template default de ASP.NET)
- **CERO endpoints de FuzzySat**
- **No referencia a FuzzySat.Core**
- No hay carpeta Controllers/

**Lo que FALTA:**
1. Referencia a FuzzySat.Core en .csproj
2. Controllers: TrainController, ClassifyController, ValidateController, InfoController
3. DTOs para request/response
4. Middleware de error handling
5. Swagger/OpenAPI documentacion
6. Endpoints RESTful para todo el pipeline:
   - POST /api/train — recibir samples, devolver session
   - POST /api/classify — recibir config, ejecutar clasificacion
   - GET /api/validate — confusion matrix y metricas
   - GET /api/info/{rasterPath} — metadata de imagen
7. SignalR hub para progreso de clasificacion (opcional)

**Esfuerzo estimado**: Alto. Todo desde cero.

---

### D. Web — Botones Muertos, Mocks, y Seguridad (14 Issues Identificados)

#### D.1 Botones que el usuario clickea y NO pasa nada (0 feedback):

| # | Pagina | Elemento | Problema |
|---|--------|----------|----------|
| 1 | ProjectSetup | Browse folder icon | Disabled, tooltip escondido |
| 2 | Validation | Export CSV | Sin click handler |
| 3 | Classification | Export Result | Sin click handler (se habilita post-run) |
| 4 | Training | Extract Statistics | `Disabled="true"` permanente |
| 5 | Training | Export Session | `Disabled="true"` permanente |

#### D.2 Controles disabled sin contexto claro:

| # | Pagina | Elementos | Cantidad |
|---|--------|-----------|----------|
| 6 | Training | Drawing tools (polygon, rect, point, edit, delete) | 5 botones |
| 7 | MapPlaceholder | Zoom in/out, fit extent | 3 botones |

#### D.3 Cosas que parecen dinamicas pero son mock:

| # | Pagina | Elemento | Problema |
|---|--------|----------|----------|
| 8 | BandViewer | Dropdowns | Cambian variable pero no actualizan nada visible |
| 9 | BandViewer | Metadata/stats/histogram | 100% hardcodeados |
| 10 | Training | Spectral chart | Datos inventados, no de samples reales |
| 11 | Classification | Run button | Simulacion con `Task.Delay`, no clasifica nada |

#### D.4 Issues de seguridad/robustez (hallazgos de PR #12 review):

Identificados por bots de review (Copilot + Codex) en el PR de Save/Reset de ProjectSetup:

| # | Pagina | Issue | Fuente |
|---|--------|-------|--------|
| 12 | ProjectSetup | Save usa `Environment.SpecialFolder.UserProfile` hardcodeado — en Blazor Server esto es server-side, problematico en multi-user y contenedores | Copilot + Codex |
| 13 | ProjectSetup | Sanitizacion de filename no maneja nombres vacios, solo underscores, ni nombres reservados de Windows (CON, NUL, PRN) | Copilot |
| 14 | ProjectSetup | Toast de exito expone ruta completa del servidor (`Saved to C:\Users\...`) y toast de error muestra `ex.Message` crudo — info leak | Copilot |

**Esfuerzo estimado**: Muy alto para issues 1-11 (requiere Leaflet.js real, carga de rasters, SignalR).
Bajo para issues 12-14 (fixes puntuales en ProjectSetup.razor).

---

### E. Web — Funcionalidades Ausentes para 100%

1. **Leaflet.js real** — Reemplazar MapPlaceholder con mapa interactivo real
2. **Carga de rasters** — Upload o browse de archivos GeoTIFF/Sentinel-2
3. **Visualizacion de bandas** — Renderizar bandas reales en el mapa
4. **Composites** — True Color, False Color IR, NDVI como overlays reales
5. **Drawing tools** — Dibujar poligonos de entrenamiento sobre el mapa
6. **Extraccion de estadisticas** — Calcular mean/stddev de areas seleccionadas
7. **Clasificacion real** — Conectar Run a FuzzyClassifier con progreso SignalR
8. **Resultado en mapa** — Overlay del raster clasificado sobre Leaflet
9. **Validation real** — Generar ConfusionMatrix de resultados reales
10. **Export CSV** — Exportar confusion matrix como CSV
11. **Export GeoTIFF** — Exportar raster clasificado
12. **Export PDF** — Reporte de validacion (mencionado en Epic #5)
13. **History page** — Pagina de historial (mencionada en Epic #5, no existe)
14. **File browser** — Dialogo nativo o alternativa web para seleccionar archivos
15. **leaflet-interop.js** — JS interop para comunicacion Blazor ↔ Leaflet

---

### F. Testing — Gaps

1. **Integration tests** — No hay tests que prueben el pipeline end-to-end
2. **CLI tests** — No hay tests para los comandos CLI
3. **API tests** — No hay tests para endpoints (no existen aun)
4. **Web tests** — No hay tests de componentes Blazor
5. **GDAL test fixtures** — Tests de GDAL pueden fallar sin archivos de prueba
6. **Code coverage reporting** — coverlet esta en .csproj pero no hay reporte en CI

---

### G. CI/CD & DevOps — Gaps

1. **CI basico** — Solo build + test, no hay:
   - Code coverage threshold
   - Lint / format check
   - Security scan (dependabot, CodeQL)
2. **No hay CD** — No deployment pipeline
3. **No hay Docker** — Ni Dockerfile ni docker-compose
4. **No hay release workflow** — No publish de NuGet, no GitHub Releases

---

### H. Documentacion & UX — Gaps

1. **User documentation** — No hay guia de usuario para la Web app
2. **API documentation** — No hay Swagger (API no existe)
3. **CONTRIBUTING.md** — No existe
4. **CHANGELOG.md** — No existe
5. **Sample data** — sample-project.json existe pero no hay rasters de ejemplo
6. **README badges** — CI badge apunta a workflow que puede no funcionar

---

## Metricas de Completitud

| Area | Completado | Faltante | % Completo |
|------|-----------|----------|------------|
| Core Engine | 40+ clases | PCA, reconciliacion EPICs | 95% |
| Unit Tests | 24 archivos | Integration, CLI, API, Web | 85% |
| CLI | Estructura | 5 comandos sin cablear | 20% |
| API | Template | Todo | 0% |
| Web UI | 5 paginas mock | 15 funcionalidades reales | 25% |
| CI/CD | Build+Test | Coverage, CD, Docker | 30% |
| Documentacion | CLAUDE.md, EPICs | User docs, CHANGELOG, CONTRIBUTING | 40% |
| **GLOBAL** | | | **~45%** |

---

## Siguiente Paso

Ver [01-plan.md](01-plan.md) para el roadmap priorizado de como llegar al 100%.
