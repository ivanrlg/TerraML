# Epic #6 — Plan: Road to 100%

**Creado**: 2026-03-28
**Tipo**: Roadmap priorizado (solo documentacion)

---

## Principios del Plan

1. **Valor incremental** — Cada fase entrega funcionalidad usable, no solo "mas codigo"
2. **Core-first** — Cerrar gaps del Core antes de cablear interfaces
3. **No reescribir** — Aprovechar todo lo que ya existe (95% del Core)
4. **Honest UI** — Botones muertos son peor que botones ausentes

---

## Fase 0: Reconciliacion de EPICs (Pre-requisito)

> **Objetivo**: Alinear la documentacion con la realidad.

| # | Tarea | Detalle |
|---|-------|---------|
| 0.1 | Auditar Epic #1 micro-commits | Marcar como completados los que ya existen |
| 0.2 | Auditar Epic #2 micro-commits | Separar lo hecho (Core I/O) de lo pendiente (CLI wiring) |
| 0.3 | Auditar Epic #3 micro-commits | Marcar MFs, operadores, indices como completados |
| 0.4 | Auditar Epic #4 micro-commits | Marcar ML pipeline como completado |
| 0.5 | Auditar Epic #5 micro-commits | Separar UI scaffolding (hecho) de funcionalidad real (pendiente) |
| 0.6 | Actualizar ACTIVE_EPICS.md | Reflejar estados reales |
| 0.7 | Mover EPICs completados a EPIC_HISTORY.md | #1, #3, #4 estan ~completados |
| 0.8 | Actualizar task/todo.md | Reconciliar con realidad |

**Entregable**: Documentacion que refleja la verdad.

---

## Fase 1: Core — Cerrar el 5% Restante

> **Objetivo**: Core 100% completo, sin deuda tecnica.
> **Prioridad**: P0
> **Depende de**: Fase 0

| # | Tarea | Detalle | Esfuerzo |
|---|-------|---------|----------|
| 1.1 | Implementar PCA | `FuzzySat.Core/FuzzyLogic/FeatureExtraction/PcaTransformer.cs` | Medio |
| 1.2 | Verificar Visualization module | Revisar si `Core/Visualization/` tiene contenido real | Bajo |
| 1.3 | Verificar KMeansClusterer | Confirmar que la implementacion es completa, no solo tests | Bajo |
| 1.4 | Verificar ConfidenceMapGenerator | Test end-to-end con datos sinteticos | Bajo |
| 1.5 | Code coverage report | Agregar coverlet report al CI, verificar >80% | Bajo |

**Entregable**: Core con 100% de funcionalidad prometida y coverage verificado.

---

## Fase 2: CLI — Cablear los 5 Comandos

> **Objetivo**: CLI funcional que ejecuta el pipeline completo desde terminal.
> **Prioridad**: P1
> **Depende de**: Fase 1

| # | Tarea | Detalle | Esfuerzo |
|---|-------|---------|----------|
| 2.1 | Cablear `info` command | Leer raster con GdalRasterReader, mostrar metadata con Spectre | Bajo |
| 2.2 | Cablear `train` command | Leer CSV/JSON de samples, crear TrainingSession, guardar | Medio |
| 2.3 | Cablear `classify` command | Cargar config + session, ejecutar FuzzyClassifier, escribir GeoTIFF | Medio |
| 2.4 | Cablear `validate` command | Comparar raster clasificado vs ground truth, mostrar ConfusionMatrix | Medio |
| 2.5 | Implementar `visualize` command | Generar PNG de bandas o resultado clasificado | Medio |
| 2.6 | Spectre.Console output | Tablas coloreadas, progress bars, tree views para info | Bajo |
| 2.7 | JSON config loading | Leer ClassifierConfiguration desde archivo, pasar a comandos | Bajo |
| 2.8 | Tests CLI | Tests unitarios para cada comando (mocking Core) | Medio |

**Entregable**: `dotnet run --project src/FuzzySat.CLI -- classify --config project.json` funciona end-to-end.

---

## Fase 3: Web — Eliminar Mocks y Conectar a Core

> **Objetivo**: Cada boton de la Web hace algo real o no existe.
> **Prioridad**: P1
> **Depende de**: Fase 1

### Fase 3A: Infraestructura Web (pre-requisito para todo lo demas)

| # | Tarea | Detalle | Esfuerzo |
|---|-------|---------|----------|
| 3A.1 | leaflet-interop.js | JS interop: initMap, addLayer, enableDrawing, fitBounds | Alto |
| 3A.2 | Reemplazar MapPlaceholder | Componente LeafletMap.razor real con JS interop | Alto |
| 3A.3 | Servicio de raster tiles | TileService que renderiza bandas como tiles PNG para Leaflet | Alto |
| 3A.4 | File upload/browse | InputFile de Blazor o solucion equivalente para seleccionar rasters | Medio |
| 3A.5 | SignalR hub para progreso | ClassificationHub que reporta % de progreso real | Medio |

### Fase 3B: Paginas — Conectar a Core

| # | Tarea | Detalle | Esfuerzo |
|---|-------|---------|----------|
| 3B.1 | ProjectSetup — Browse real | Conectar browse button a file upload o InputFile | Medio |
| 3B.2 | ProjectSetup — Cargar raster | Al seleccionar archivo, leer metadata con GdalRasterReader | Medio |
| 3B.3 | BandViewer — Bandas reales | Renderizar bandas del raster cargado en Leaflet | Alto |
| 3B.4 | BandViewer — Stats reales | Calcular min/max/mean/stddev de la banda seleccionada | Medio |
| 3B.5 | BandViewer — Histograma real | Histograma calculado de valores reales del raster | Medio |
| 3B.6 | BandViewer — Composites reales | True Color, False Color IR, NDVI como overlays | Alto |
| 3B.7 | Training — Drawing tools | Habilitar polygon/rect/point drawing en Leaflet | Alto |
| 3B.8 | Training — Samples de poligonos | Extraer pixeles dentro de poligonos dibujados | Alto |
| 3B.9 | Training — Extract Statistics real | Calcular SpectralStatistics de los samples extraidos | Medio |
| 3B.10 | Training — Spectral chart real | Graficar curvas espectrales reales de las clases | Medio |
| 3B.11 | Training — Export Session real | Serializar TrainingSession a JSON, descargar | Bajo |
| 3B.12 | Classification — Run real | Ejecutar FuzzyClassifier con progreso via SignalR | Alto |
| 3B.13 | Classification — Resultado en mapa | Overlay del raster clasificado en Leaflet | Alto |
| 3B.14 | Classification — Export Result real | Descargar GeoTIFF clasificado | Medio |
| 3B.15 | Validation — Datos reales | Generar ConfusionMatrix de clasificacion real | Medio |
| 3B.16 | Validation — Export CSV real | Descargar confusion matrix como CSV | Bajo |
| 3B.17 | ProjectSetup — Path configurable | Mover de UserProfile a `IOptions<ProjectStorageOptions>` configurable | Bajo |
| 3B.18 | ProjectSetup — Validar filename | Sanitizar + validar nombre (non-empty, length, no reserved names) | Bajo |
| 3B.19 | ProjectSetup — Fix info leak | Mensajes user-friendly en toasts, log detalles server-side con `ILogger` | Bajo |

### Fase 3C: Paginas Nuevas

| # | Tarea | Detalle | Esfuerzo |
|---|-------|---------|----------|
| 3C.1 | History page | Lista de sesiones/proyectos anteriores | Medio |
| 3C.2 | Help/About page | Documentacion in-app, links a tesis y repo | Bajo |

**Entregable**: Web app funcional end-to-end donde un usuario puede cargar una imagen,
definir clases, dibujar training areas, clasificar, y ver resultados reales.

---

## Fase 4: API — Construir desde Cero

> **Objetivo**: REST API completa para integracion programatica.
> **Prioridad**: P2
> **Depende de**: Fase 1

| # | Tarea | Detalle | Esfuerzo |
|---|-------|---------|----------|
| 4.1 | Eliminar weather forecast | Limpiar el template default | Bajo |
| 4.2 | Agregar referencia a Core | .csproj referencia FuzzySat.Core | Bajo |
| 4.3 | DTOs | Request/response models para cada endpoint | Medio |
| 4.4 | InfoController | GET /api/raster/info — metadata de imagen | Bajo |
| 4.5 | TrainController | POST /api/train — crear session desde samples | Medio |
| 4.6 | ClassifyController | POST /api/classify — ejecutar clasificacion | Medio |
| 4.7 | ValidateController | GET /api/validate — confusion matrix | Bajo |
| 4.8 | ExportController | GET /api/export/{format} — descargar resultados | Medio |
| 4.9 | Error handling middleware | Manejo consistente de errores | Bajo |
| 4.10 | Swagger/OpenAPI | Documentacion interactiva de API | Bajo |
| 4.11 | SignalR hub (opcional) | Progreso de clasificacion en tiempo real | Medio |
| 4.12 | Tests de API | Integration tests con WebApplicationFactory | Medio |

**Entregable**: API REST documentada con Swagger, testeable con curl/Postman.

---

## Fase 5: CI/CD & DevOps

> **Objetivo**: Pipeline profesional de CI/CD.
> **Prioridad**: P2
> **Depende de**: Fases 1-4

| # | Tarea | Detalle | Esfuerzo |
|---|-------|---------|----------|
| 5.1 | Code coverage en CI | Publicar reporte de coverlet, threshold 80% | Bajo |
| 5.2 | Format check | `dotnet format --verify-no-changes` en CI | Bajo |
| 5.3 | Dockerfile | Multi-stage build para Web y API | Medio |
| 5.4 | docker-compose.yml | Web + API + volumen para rasters | Bajo |
| 5.5 | GitHub Release workflow | Publish en tag, generar binarios | Medio |
| 5.6 | Dependabot | Security updates automaticos | Bajo |
| 5.7 | CodeQL analysis | Scan de seguridad en CI | Bajo |

**Entregable**: CI/CD completo con coverage, Docker, y releases automaticos.

---

## Fase 6: Documentacion & Polish

> **Objetivo**: Proyecto listo para open source publico.
> **Prioridad**: P3
> **Depende de**: Fases 1-5

| # | Tarea | Detalle | Esfuerzo |
|---|-------|---------|----------|
| 6.1 | CONTRIBUTING.md | Guia para contribuidores | Bajo |
| 6.2 | CHANGELOG.md | Historial de cambios desde v0.1.0 | Bajo |
| 6.3 | User Guide (Web) | Documentacion de como usar la Web app | Medio |
| 6.4 | API Reference | Generado desde Swagger + ejemplos | Bajo |
| 6.5 | CLI Reference | Help text + ejemplos de uso | Bajo |
| 6.6 | Sample raster data | Raster sintetico pequeno para demos (no commiteado, descargable) | Medio |
| 6.7 | README update | Badges reales, screenshots, GIF demo | Medio |
| 6.8 | Verificar README badges | CI badge, coverage badge, license badge funcionales | Bajo |

**Entregable**: Proyecto publicable con documentacion completa.

---

## Resumen de Esfuerzo por Fase

| Fase | Tareas | Esfuerzo Global | Prioridad |
|------|--------|-----------------|-----------|
| 0 - Reconciliacion | 8 | Bajo | P0 |
| 1 - Core 100% | 5 | Bajo-Medio | P0 |
| 2 - CLI Wiring | 8 | Medio | P1 |
| 3 - Web Real | 24 | **Muy Alto** | P1 |
| 4 - API | 12 | Alto | P2 |
| 5 - CI/CD | 7 | Medio | P2 |
| 6 - Docs & Polish | 8 | Medio | P3 |
| **TOTAL** | **72 tareas** | | |

---

## Dependencias Criticas

```
Fase 0 (Reconciliacion)
  └──> Fase 1 (Core 100%)
         ├──> Fase 2 (CLI)          [independiente de Web]
         ├──> Fase 3 (Web)          [independiente de CLI]
         │      ├── 3A (Infra)
         │      ├── 3B (Paginas)    [depende de 3A]
         │      └── 3C (Nuevas)     [depende de 3A]
         └──> Fase 4 (API)          [independiente de CLI y Web]

Fase 5 (CI/CD) ──> puede empezar en paralelo con Fase 2+
Fase 6 (Docs)  ──> despues de que todo funcione
```

**Ruta critica**: Fase 0 → Fase 1 → Fase 3A → Fase 3B (la Web es el mayor bloque de trabajo).

---

## Siguiente Paso

Ver [02-technical-design.md](02-technical-design.md) para decisiones tecnicas y riesgos por area.
