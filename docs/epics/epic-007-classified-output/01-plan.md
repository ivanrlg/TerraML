# Epic #7 — Plan de Implementacion

> Ver [00-overview.md](00-overview.md) para contexto y alcance.

---

## Fase 1: Integracion Hybrid ML en Web (PR #1, ~5 commits)

### 1A — HybridClassificationService (Web Service)
**Archivo**: `src/FuzzySat.Web/Services/HybridClassificationService.cs` (NUEVO)

- Wrapper de `HybridClassifier.TrainRandomForest()` / `TrainSdca()` para Web
- Acepta TrainingSession + MultispectralImage + metodo elegido
- Extrae muestras de pixels etiquetados desde ProjectState.TrainingSamples
- Flujo: FuzzyRuleSet -> FuzzyFeatureExtractor -> entrena modelo ML -> clasifica pixel a pixel
- Retorna ClassificationResult (misma estructura que el path fuzzy)
- Reporta progreso via IProgress<ClassificationProgress>
- Reutiliza ClassificationService.BuildRuleSet() (ya es internal static)

### 1B — Actualizar ClassificationOptions
**Archivo**: `src/FuzzySat.Web/Services/ClassificationService.cs`

Agregar propiedad ClassificationMethod: "Pure Fuzzy" | "Random Forest" | "SDCA"

### 1C — Actualizar UI de Classification Page
**Archivo**: `src/FuzzySat.Web/Components/Pages/Classification.razor`

- 4ta tarjeta de configuracion: **Classification Method**
  - "Pure Fuzzy" — icono: functions, hint: "Direct fuzzy inference"
  - "Random Forest" — icono: forest, hint: "ML.NET + 100 decision trees"
  - "SDCA" — icono: model_training, hint: "Maximum Entropy classifier"
- Nota info cuando se selecciona hibrido
- Rutear a HybridClassificationService o ClassificationService segun seleccion
- Stages de progreso actualizados para hibrido
- Badge del metodo activo en resumen de resultados

### 1D — DI + Tests
- Registrar HybridClassificationService en Program.cs
- Unit tests

**Commits**:
1. `feat(web): Add HybridClassificationService wrapping ML.NET classifiers`
2. `feat(web): Add ClassificationMethod to ClassificationOptions`
3. `feat(web): Add method selector card to Classification page`
4. `feat(web): Wire hybrid classification into RunClassification flow`
5. `test: Add HybridClassificationService integration tests`

---

## Fase 2: Renderizado de Imagen Clasificada (PR #2, ~6 commits)

### 2A — ClassifiedImageRenderer (Core)
**Archivo**: `src/FuzzySat.Core/Visualization/ClassifiedImageRenderer.cs` (NUEVO)

- Genera PNG byte array desde ClassificationResult
- Mapea class name -> color hex desde LandCoverClass.Color
- Auto-asigna colores con paleta inteligente si Color es null
- Renderiza buffer RGBA (row-major) -> codifica a PNG via SkiaSharp
- Modo overlay: clasificada semi-transparente sobre original grayscale

### 2B — Classification Canvas JS Interop
**Archivo**: `src/FuzzySat.Web/wwwroot/js/classification-canvas.js` (NUEVO)

- Viewer Canvas (patron similar a training-selection.js)
- **Side-by-side**: Dos canvas, original izquierda + clasificada derecha
- Zoom (scroll wheel) + pan (Ctrl+drag) sincronizado entre ambos
- Toggle: Original | Classified | Overlay (alpha blend 50%)
- Tooltip hover: coordenadas pixel, nombre de clase, confianza %
- Leyenda de colores

**Archivo**: `src/FuzzySat.Web/Services/ClassificationViewService.cs` (NUEVO)
- Genera base64 PNG para imagen clasificada
- Genera base64 PNG para original (false color composite)

### 2C — Side-by-Side Viewer en Classification Page
**Archivo**: `src/FuzzySat.Web/Components/Pages/Classification.razor`

Post-clasificacion:
- Dos Canvas side-by-side, zoom/pan sincronizado
- Toggle bar con 3 modos
- Leyenda de colores horizontal
- Controles de zoom: +/-, reset, fit-to-view

### 2D — Colores Auto + Editables
- Auto-asignacion: keyword matching (water->azul, forest->verde) + fallback
- Editable: clic en swatch -> RadzenColorPicker
- Persiste en ProjectStateService.ClassColors
- Re-render inmediato al cambiar color

**Commits**:
1. `feat(core): Add ClassifiedImageRenderer with SkiaSharp PNG generation`
2. `feat(web): Add classification-canvas.js with zoom/pan/toggle`
3. `feat(web): Add ClassificationViewService for image rendering`
4. `feat(web): Add classified image viewer to Classification page`
5. `feat(web): Add color legend and class color picker`
6. `test: Add ClassifiedImageRenderer unit tests`

---

## Fase 3: Estadisticas de Area + GeoTIFF Export (PR #3, ~5 commits)

### 3A — AreaCalculator (Core)
**Archivo**: `src/FuzzySat.Core/Classification/AreaCalculator.cs` (NUEVO)

- Calcula area por clase usando GeoTransform (pixelWidth * pixelHeight)
- Record: ClassAreaStats(ClassName, PixelCount, AreaM2, AreaHa, Percentage)
- Si no hay GeoTransform, solo muestra pixel count (area = N/A)

### 3B — Tabla de Estadisticas Rica
**Archivo**: `src/FuzzySat.Web/Components/Pages/Classification.razor`

- Reemplaza grid simple con tabla: Clase | Color | Pixeles | Area m2 | Area ha | %
- Donut chart (Radzen) distribucion de area
- Bar chart area por clase con colores

### 3C — Boton Descarga GeoTIFF
- GdalRasterWriter.Write() a archivo temporal -> download via JS interop
- Color table y category names en metadata del GeoTIFF

### 3D — CSV Export Mejorado
- Agregar columnas de area al CSV existente

**Commits**:
1. `feat(core): Add AreaCalculator for per-class m2/ha statistics`
2. `feat(web): Replace pixel count grid with area statistics table`
3. `feat(web): Add area distribution charts (donut + bar)`
4. `feat(web): Add GeoTIFF download from Classification page`
5. `test: Add AreaCalculator unit tests`
