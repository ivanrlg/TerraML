# Epic #6 — Technical Design: Decisiones, Riesgos, y Detalles

**Creado**: 2026-03-28
**Tipo**: Documento tecnico de referencia

---

## 1. Core — PCA (unico gap significativo)

### Contexto
PCA (Principal Component Analysis) esta mencionado en Epic #3 como feature planificada
pero no tiene implementacion. Con Sentinel-2 (13 bandas) y el feature vector expandido
(hasta 111 dimensiones), PCA puede reducir dimensionalidad antes del ML pipeline.

### Decision de Diseno
- **Opcion A**: Implementar PCA from scratch con algebra lineal (eigenvalues de matriz de covarianza)
- **Opcion B**: Usar ML.NET `PrincipalComponentAnalysis` estimator
- **Recomendacion**: Opcion B. ML.NET ya esta como dependencia. Reimplementar PCA
  no aporta valor educativo suficiente para justificar el esfuerzo.

### Ubicacion
- `FuzzySat.Core/ML/PcaTransformer.cs`
- Interface: `IPcaTransformer` con metodos `Fit(data)` y `Transform(data)`

### Riesgo
- Bajo. PCA es un "nice to have" para optimizacion, no bloquea funcionalidad core.

---

## 2. CLI — Arquitectura de Wiring

### Contexto
Los 4 comandos CLI existen como stubs. Las clases Core que necesitan ya estan
implementadas. El trabajo es puramente de integracion.

### Patron Propuesto
Cada comando sigue el mismo flujo:

```
Command Handler
  ├── 1. Leer config JSON (ClassifierConfiguration)
  ├── 2. Instanciar servicios Core necesarios
  ├── 3. Ejecutar logica Core
  ├── 4. Formatear output con Spectre.Console
  └── 5. Manejar errores con mensajes claros
```

### Detalle por Comando

**`info <raster-path>`**
```
GdalRasterReader.ReadInfo(path)
  → SpectreTable con: dimensiones, bandas, proyeccion, driver, extent
```

**`train --config <path> --samples <csv-path> --output <session-path>`**
```
1. Leer ClassifierConfiguration desde JSON
2. Leer CSV de samples (formato: class,band1,band2,...,bandN)
3. TrainingSession.CreateFromSamples(samples)
4. Serializar session a JSON
5. Output: tabla de estadisticas por clase
```

**`classify --config <path> --session <path> --input <raster> --output <raster>`**
```
1. Leer config + session
2. session.BuildRuleSet() → FuzzyRuleSet
3. FuzzyClassifier(ruleSet, defuzzifier)
4. Iterar pixeles del raster con progress bar
5. GdalRasterWriter.Write(resultado)
6. Output: resumen de pixeles clasificados por clase
```

**`validate --classified <raster> --ground-truth <raster|csv>`**
```
1. Comparar pixel a pixel (o contra CSV de samples)
2. ConfusionMatrix.FromComparison(predicted, actual)
3. Output: tabla de confusion matrix + OA + Kappa
```

**`visualize --input <raster> --bands <indices> --output <png>`**
```
1. Leer bandas seleccionadas
2. Normalizar a 0-255
3. Generar PNG (false color, single band, o clasificado)
```

### Formato de CSV de Training Samples
```csv
class,vnir1,vnir2,swir1,swir2
Urban,128,95,142,110
Water,25,18,12,8
Forest,45,32,180,120
```

### Riesgo
- GDAL initialization puede fallar en sistemas sin las librerias nativas.
  El CLI debe dar un mensaje claro: "GDAL not found. Install MaxRev.Gdal..."

---

## 3. API — Arquitectura

### Contexto
El proyecto API es un template vacio. Necesita construirse desde cero pero
reutilizando 100% de la logica de Core.

### Stack Propuesto
- ASP.NET Core Minimal APIs o Controllers (Controllers preferido por consistencia con CLAUDE.md)
- Swagger via Swashbuckle o NSwag
- DTOs separados de modelos Core (evitar leak de internals)

### Endpoints Propuestos

```
GET  /api/health                          → 200 OK
GET  /api/raster/info?path={path}         → RasterInfoDto
POST /api/train                           → TrainingSessionDto
     Body: { config, samples[] }
POST /api/classify                        → ClassificationResultDto
     Body: { config, sessionPath, inputPath, outputPath }
GET  /api/validate?predicted={p}&truth={t} → ValidationResultDto
GET  /api/export/{sessionId}/{format}     → File download (csv, geotiff)
```

### Riesgo
- **File paths en API**: Exponer rutas del filesystem via API es un riesgo de seguridad.
  Considerar un sistema de "project workspace" donde la API solo accede a un directorio
  configurado. No aceptar rutas absolutas arbitrarias del cliente.
- **Tamano de rasters**: Clasificar un raster Sentinel-2 puede tomar minutos.
  Necesita ejecucion asincrona + polling o SignalR para progreso.

### Patron para Operaciones Largas
```
POST /api/classify → 202 Accepted + { jobId: "abc123" }
GET  /api/jobs/{jobId} → { status: "running", progress: 45 }
GET  /api/jobs/{jobId} → { status: "completed", resultPath: "..." }
```

---

## 4. Web — Decisiones Tecnicas Criticas

### 4.1 Leaflet.js Integration

**Problema**: Blazor Server no puede manipular DOM directamente. Leaflet.js es una
libreria JavaScript. Se necesita JS Interop.

**Solucion**: `leaflet-interop.js` + `LeafletMap.razor`

```
LeafletMap.razor (C#)
  ├── OnAfterRenderAsync → JSRuntime.InvokeVoidAsync("leafletInterop.initMap")
  ├── AddTileLayer() → JSRuntime.InvokeVoidAsync("leafletInterop.addTileLayer")
  ├── EnableDrawing() → JSRuntime.InvokeVoidAsync("leafletInterop.enableDrawing")
  └── OnPolygonDrawn ← DotNetObjectReference callback desde JS

leaflet-interop.js
  ├── initMap(elementId, options)
  ├── addTileLayer(url, options)
  ├── addGeoTiffLayer(url)  // via georaster-layer-for-leaflet
  ├── enableDrawing(options)
  ├── fitBounds(bounds)
  └── callbacks → DotNet.invokeMethodAsync(...)
```

**Dependencias JS adicionales**:
- `leaflet` (ya mencionado)
- `leaflet-draw` (dibujo de poligonos)
- `georaster-layer-for-leaflet` (renderizar GeoTIFF en browser)
- O alternativamente: renderizar tiles server-side y servirlos como PNG tiles

**Decision clave: Client-side vs Server-side rendering de rasters**

| Opcion | Pros | Contras |
|--------|------|---------|
| Client-side (georaster) | Interactivo, zoom fluido | Rasters grandes crashean el browser |
| Server-side (tile service) | Maneja rasters enormes | Mas complejo, latencia de red |

**Recomendacion**: Server-side tile rendering. Los rasters Sentinel-2 pueden ser
de varios GB. El browser no puede manejar eso.

### 4.2 Tile Service Architecture

```
Browser solicita: /tiles/{project}/{band}/{z}/{x}/{y}.png

TileController:
  1. Leer banda del raster con GdalRasterReader
  2. Extraer region correspondiente al tile (z/x/y)
  3. Normalizar valores a 0-255
  4. Renderizar como PNG (SkiaSharp o System.Drawing)
  5. Cache en memoria o disco
  6. Devolver PNG
```

**Dependencia nueva**: SkiaSharp (o ImageSharp) para renderizar tiles PNG.
Esto NO es una dependencia de Core (va en Web).

### 4.3 Drawing Tools → Training Samples

```
1. Usuario dibuja poligono en Leaflet (leaflet-draw)
2. JS callback envia GeoJSON a Blazor via DotNetObjectReference
3. Blazor convierte coordenadas geo → pixel (usando GeoTransform del raster)
4. Extrae valores de banda para cada pixel dentro del poligono
5. Crea LabeledPixelSample[] para la clase seleccionada
6. Actualiza spectral chart y estadisticas en tiempo real
```

**Complejidad**: La conversion de coordenadas geograficas a pixel requiere
la geotransform del raster (6 coeficientes de GDAL). GdalRasterReader ya
expone esto via RasterInfo.

### 4.4 Classification con Progreso Real

```
1. Usuario clickea "Run Classification"
2. Blazor crea ClassificationJob en background thread
3. Job itera pixeles, reporta progreso via callback
4. Blazor actualiza progress bar via StateHasChanged()
5. Al completar, genera resultado y overlay en mapa

Nota: Blazor Server usa SignalR internamente, asi que
StateHasChanged() es suficiente — no necesita un hub separado.
```

### 4.5 Resolucion de los 11 Issues Reportados

| # | Issue | Solucion Propuesta | Fase |
|---|-------|--------------------|------|
| 1 | Browse folder disabled | Usar `<InputFile>` de Blazor para upload | 3B.1 |
| 2 | Export CSV sin handler | Implementar descarga CSV con `IJSRuntime` | 3B.16 |
| 3 | Export Result sin handler | Implementar descarga GeoTIFF | 3B.14 |
| 4 | Extract Statistics disabled | Habilitar cuando hay samples dibujados | 3B.9 |
| 5 | Export Session disabled | Habilitar cuando hay session valida | 3B.11 |
| 6 | Drawing tools disabled | Habilitar cuando hay raster cargado + Leaflet activo | 3B.7 |
| 7 | Zoom/fit disabled | Reemplazar MapPlaceholder con LeafletMap real | 3A.2 |
| 8 | Dropdowns no actualizan | Conectar a tile service para cambiar banda/composite | 3B.3 |
| 9 | Metadata hardcodeada | Leer de RasterInfo real | 3B.4 |
| 10 | Spectral chart inventado | Calcular de samples reales | 3B.10 |
| 11 | Run es Task.Delay | Conectar a FuzzyClassifier real | 3B.12 |
| 12 | Save usa UserProfile hardcodeado | `IOptions<ProjectStorageOptions>` configurable | 3B.17 |
| 13 | Filename sanitization incompleta | Validar non-empty, length, no reserved names | 3B.18 |
| 14 | Info leak en toasts | Mensajes user-friendly + `ILogger` server-side | 3B.19 |

---

### 4.6 ProjectSetup — Fixes de Seguridad/Robustez (PR #12 Review)

Tres issues identificados por los bots de review (Copilot + Codex) en el PR que
implemento Save/Reset de ProjectSetup. Los tres son de esfuerzo bajo pero impacto
real en seguridad y robustez.

#### Issue #12: Path Hardcodeado a UserProfile

**Problema**: `Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)` resuelve
al perfil del **servidor** en Blazor Server. En contenedores, puede ser read-only o
compartido entre todos los usuarios del browser.

**Solucion recomendada**:
```csharp
// appsettings.json
{
  "ProjectStorage": {
    "BasePath": "" // empty = default to ApplicationData
  }
}

// ProjectStorageOptions.cs
public class ProjectStorageOptions
{
    public string BasePath { get; set; } = "";
}

// En el componente, inyectar IOptions<ProjectStorageOptions>
var basePath = string.IsNullOrEmpty(options.BasePath)
    ? Path.Combine(Environment.GetFolderPath(
        Environment.SpecialFolder.ApplicationData), "FuzzySat", "projects")
    : options.BasePath;
```

#### Issue #13: Sanitizacion de Filename Insuficiente

**Problema**: `string.Join("_", name.Split(Path.GetInvalidFileNameChars()))` puede producir:
- Nombre vacio (si el input solo tiene chars invalidos)
- Solo underscores `"___"`
- Nombres reservados de Windows: CON, PRN, AUX, NUL, COM1-COM9, LPT1-LPT9

**Solucion recomendada**:
```csharp
private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
{
    "CON", "PRN", "AUX", "NUL",
    "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
    "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
};

var safeName = string.Join("_", name.Trim().Split(Path.GetInvalidFileNameChars()));
safeName = safeName.Trim('_').Trim('.');

if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 100
    || ReservedNames.Contains(safeName))
{
    NotificationService.Notify(NotificationSeverity.Warning, "Invalid Name",
        "Please use a valid project name.");
    return;
}
```

#### Issue #14: Info Leak en Notificaciones

**Problema**: El toast de exito muestra `$"Saved to {filePath}"` exponiendo la ruta
completa del servidor (ej: `C:\Users\ivanr\FuzzySat\projects\...`). El toast de error
muestra `ex.Message` que puede contener stack traces o paths internos.

**Solucion recomendada**:
```csharp
// Inyectar ILogger<ProjectSetup>
@inject ILogger<ProjectSetup> Logger

// Exito: solo mostrar nombre del proyecto
NotificationService.Notify(NotificationSeverity.Success, "Saved",
    $"Configuration saved for project '{_projectName.Trim()}'.", duration: 5000);

// Error: mensaje generico en UI, detalles en log del servidor
catch (Exception ex)
{
    Logger.LogError(ex, "Failed to save project '{ProjectName}' to '{Path}'",
        _projectName, filePath);
    NotificationService.Notify(NotificationSeverity.Error, "Save Failed",
        "An error occurred while saving. Please try again.", duration: 6000);
}
```

---

## 5. Testing — Estrategia para Gaps

### Tests de Integracion (nuevos)
```
FuzzySat.Integration.Tests/
  ├── EndToEndPipelineTests.cs    — Train → Classify → Validate con raster sintetico
  ├── GdalRoundTripTests.cs       — Write GeoTIFF → Read → Verify pixel values
  └── HybridPipelineTests.cs      — Fuzzy features → ML.NET train → Predict
```

### Tests CLI (nuevos)
```
FuzzySat.CLI.Tests/
  ├── TrainCommandTests.cs        — Mock Core, verify output format
  ├── ClassifyCommandTests.cs     — Mock Core, verify progress output
  ├── ValidateCommandTests.cs     — Mock Core, verify table format
  └── InfoCommandTests.cs         — Mock GdalRasterReader, verify output
```

### Tests API (nuevos)
```
FuzzySat.Api.Tests/
  ├── InfoEndpointTests.cs        — WebApplicationFactory integration
  ├── TrainEndpointTests.cs       — POST with sample data
  ├── ClassifyEndpointTests.cs    — POST and poll for result
  └── ValidateEndpointTests.cs    — GET with known data
```

### Tests Web (opcionales, bajo ROI)
Los tests de componentes Blazor (bUnit) tienen alto costo de mantenimiento
y bajo ROI para un proyecto de este tamano. Priorizar tests de Core e integracion.

---

## 6. CI/CD — Decisiones

### Dockerfile (Multi-stage)
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY . .
RUN dotnet publish src/FuzzySat.Web -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# GDAL native libs needed
RUN apt-get update && apt-get install -y libgdal-dev
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FuzzySat.Web.dll"]
```

**Riesgo GDAL en Docker**: Las librerias nativas de GDAL son complicadas en
contenedores. Puede requerir un base image personalizado o instalacion manual.
Alternativa: usar `ghcr.io/osgeo/gdal` como base image.

### GitHub Release Workflow
```yaml
on:
  push:
    tags: ['v*']
jobs:
  release:
    - dotnet publish (win-x64, linux-x64, osx-x64)
    - gh release create con binarios
```

---

## 7. Riesgos Globales

| # | Riesgo | Impacto | Mitigacion |
|---|--------|---------|------------|
| 1 | GDAL nativo en Docker | Alto — puede bloquear deployment | Probar early con osgeo/gdal base image |
| 2 | Rasters grandes en browser | Alto — crash de memoria | Server-side tile rendering |
| 3 | Leaflet JS interop complejidad | Medio — debugging dificil | Aislar en un solo archivo JS, tests manuales |
| 4 | ML.NET en Linux | Bajo — deberia funcionar | CI ya corre en ubuntu, validar |
| 5 | EPICs desactualizados confunden | Medio — trabajo duplicado | Fase 0 los reconcilia |
| 6 | PCA scope creep | Bajo — nice to have | Implementar minimo viable via ML.NET |
| 7 | No hay rasters de prueba | Medio — no se puede demo | Generar raster sintetico pequeno (100x100 px) |

---

## 8. Dependencias Nuevas Necesarias

| Paquete | Proyecto | Proposito | Fase |
|---------|----------|-----------|------|
| SkiaSharp o SixLabors.ImageSharp | Web | Renderizar tiles PNG | 3A |
| leaflet (npm/CDN) | Web | Mapa interactivo | 3A |
| leaflet-draw (npm/CDN) | Web | Dibujo de poligonos | 3A |
| Swashbuckle.AspNetCore | Api | Swagger/OpenAPI | 4 |
| bUnit (opcional) | Tests | Tests de Blazor components | N/A |

**Nota**: Leaflet y leaflet-draw son JS, se incluyen via CDN o wwwroot/, no NuGet.

---

## 9. Lo que NO Necesita Hacerse

Para evitar scope creep, estas cosas **no son necesarias para 100%**:

1. **Mobile app** — Web responsive es suficiente
2. **Multi-user auth** — Herramienta single-user por ahora
3. **Database** — Filesystem + JSON es suficiente para el scope
4. **Cloud deployment** — Docker local es el target
5. **Real-time collaboration** — Single user workflow
6. **Plugin system** — Las 4 MFs y 2 operadores cubren el scope de la tesis
7. **Internationalization (i18n)** — Ingles como idioma unico de la UI
8. **GPU acceleration** — CPU es suficiente para el tamano de rasters target
