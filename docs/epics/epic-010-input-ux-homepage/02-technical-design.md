# Epic #10 — Diseno Tecnico

---

## 1. Arquitectura del Smart Import Wizard

### Estado actual (problematico)

```
ProjectSetup.razor (~900 lineas)
├── _inputMode: "direct" | "sentinel2"     ← dos caminos que confunden
├── _presets: ASTER/Sentinel-2/Landsat/Custom  ← hardcoded, no sincronizado
├── _bands: List<BandEntry>                ← puede no coincidir con raster real
├── Sentinel2ImportService                 ← descubre bandas pero no sincroniza preset
└── Manual VRT button                      ← deberia ser automatico
```

### Propuesta: Wizard por pasos con componentes

```
ProjectSetup.razor (orquestador, ~200 lineas)
├── InputRasterWizard.razor
│   ├── Step 1: SourceSelector (file / folder / sentinel-2)
│   ├── Step 2: AutoDetector (analiza, detecta sensor, bandas)
│   ├── Step 3: BandSelector (filtro por resolucion, seleccion)
│   └── Step 4: Summary (confirmar configuracion)
├── SensorPresetSelector.razor (auto-sincronizado con wizard)
├── BandConfigurationPanel.razor (read-only si auto-detectado, editable si Custom)
└── LandCoverClassPanel.razor (sin cambios)
```

### Regla clave: El preset SIGUE a las bandas, no al reves

```
Flujo actual (roto):
  Usuario importa 4 bandas → aplica preset Sentinel-2 → preset dice 13 bandas → MISMATCH

Flujo nuevo:
  Usuario importa 4 bandas (10m) → sistema detecta Sentinel-2 
  → crea preset dinamico "Sentinel-2 (10m)" con 4 bandas → MATCH
```

---

## 2. Presets Dinamicos vs Estaticos

### Problema
Los presets actuales son estaticos:
```csharp
new("Sentinel-2", 13, "10m")  // siempre 13 bandas, no importa cuantas importes
```

### Solucion: Presets como templates + instancias

```csharp
// Template (lo que el sensor PUEDE tener)
record SensorTemplate(string Name, List<BandTemplate> AllBands);

// Instancia (lo que el usuario REALMENTE importo)
record SensorInstance(string Name, string Resolution, List<BandEntry> SelectedBands);

// Ejemplo:
// Template: Sentinel-2 tiene 13 bandas
// Instancia: Sentinel-2 (10m) tiene 4 bandas: B02, B03, B04, B08
```

El preset selector mostrara:
- "Sentinel-2 (10m) — 4 bands" si importo solo 10m
- "Sentinel-2 (all) — 10 bands" si importo todas las resoluciones
- "Custom — N bands" si no coincide con ningun sensor conocido

---

## 3. Normalizacion de Nombres de Bandas

### Problema
- `Sentinel2ImportService` produce: B01, B02, B03... B8A, B09, B10, B11, B12
- `ProjectSetup.ApplyPreset()` produce: B1, B2, B3... B8A, B9, B10, B11, B12
- Inconsistencia que puede causar errores en Training/Classification band lookup

### Decision: Usar formato con leading zero (B01-B12, B8A)

**Razon**: Es el formato oficial de ESA/Copernicus para Sentinel-2.

**Archivos a actualizar**:
- `ProjectSetup.razor` → ApplyPreset() para Sentinel-2
- `Sentinel2ImportService.cs` → ya usa B01 format (no cambiar)
- `Training.razor` → BandPreset filter (usa nombres de bandas para RGB composites)
- `RasterService.cs` → RGB composite presets

---

## 4. Soporte Imagenes Pre-stacked

### Caso de uso
El usuario tiene un TIF de ~600MB descargado de Copernicus con todas las bandas ya apiladas.

### Flujo propuesto
1. Usuario selecciona "Single raster file"
2. Sistema abre con GDAL, lee N bandas
3. Muestra tabla: Band 1 (sin nombre), Band 2, ... Band N
4. Si N coincide con sensor conocido (ej. 13 = Sentinel-2), sugerir preset
5. Si no, usar Custom con bandas genericas
6. Opcion de seleccionar subset de bandas (ej. solo las primeras 4)

### Conversion de formatos
- Usar `GdalRasterReader` existente para detectar formato
- Para conversion (ej. JP2 → TIF): usar `gdal_translate` via GDAL C# bindings
- Progress callback via `IProgress<double>` (ya usado en ClassificationService)

---

## 5. Homepage Redesign

### Pagina Home nueva

```
┌─────────────────────────────────────────────┐
│  FuzzySat — Fuzzy Logic Satellite Classifier │
│  [Create New Project]  [Open Existing]       │
├─────────────────────────────────────────────┤
│  HOW IT WORKS                                │
│  ┌──────┐  ┌──────┐  ┌──────┐  ┌──────┐    │
│  │Input │→ │Train │→ │Classify│→│Validate│   │
│  │Raster│  │Samples│  │(Fuzzy) │ │(Kappa) │  │
│  └──────┘  └──────┘  └──────┘  └──────┘    │
│                                              │
│  CLASSIFICATION METHODS                      │
│  ┌────────────────┐  ┌────────────────┐     │
│  │ Fuzzy Logic     │  │ Hybrid ML      │     │
│  │ - Membership Fns│  │ - Random Forest│     │
│  │ - Inference     │  │ - SDCA         │     │
│  │ - Defuzzify     │  │ - Ensemble     │     │
│  └────────────────┘  └────────────────┘     │
│                                              │
│  [View Original Thesis Results →]            │
└─────────────────────────────────────────────┘
```

### Pagina Thesis

```
┌─────────────────────────────────────────────┐
│  Original Thesis — ULA 2008                  │
│  Merida, Venezuela                           │
├─────────────────────────────────────────────┤
│  CLASSIFIER COMPARISON                       │
│  ┌────────────────────────────────────┐     │
│  │ Fuzzy Logic:    81.87% (K=0.7637) │ ★   │
│  │ Max Likelihood: 74.27% (K=0.6650) │     │
│  │ Decision Tree:  63.74% (K=0.5312) │     │
│  │ Min Distance:   56.14% (K=0.4233) │     │
│  └────────────────────────────────────┘     │
│                                              │
│  STUDY AREA: Merida, Venezuela               │
│  SENSOR: ASTER (4 VNIR bands)               │
│  CLASSES: Urban, Water, Forest, Agriculture  │
│                                              │
│  [About the Thesis]  [Back to Home]          │
└─────────────────────────────────────────────┘
```

---

## 6. Riesgos Tecnicos

| Riesgo | Probabilidad | Impacto | Mitigacion |
|--------|-------------|---------|-----------|
| Romper proyectos guardados al cambiar band names | Alta | Alto | Migration logic: si proyecto tiene B1→B12, mapear a B01→B12 al cargar |
| GDAL conversion lenta para 600MB TIF | Media | Medio | Progress bar + async + cancelation token |
| Componentizacion rompe state management | Media | Alto | Usar [CascadingParameter] y EventCallback para mantener flujo de datos |
| Presets dinamicos complican serialization | Baja | Bajo | Serializar como BandEntry[] — ya es el formato actual |

---

## 7. Archivos Principales a Modificar

| Archivo | Cambio |
|---------|--------|
| `ProjectSetup.razor` | Refactorizar en componentes, wizard flow |
| `Sentinel2ImportService.cs` | Retornar SensorInstance con bandas matched |
| `RasterService.cs` | RGB presets sincronizados con band names |
| `Training.razor` | Band preset filter usar nombres normalizados |
| `Home.razor` | Redisenar completamente |
| `NavMenu.razor` | Agregar link a /thesis |
| **(nuevo)** `Thesis.razor` | Pagina dedicada con datos de tesis |
| **(nuevo)** `InputRasterWizard.razor` | Componente wizard |
| **(nuevo)** `SensorPresetSelector.razor` | Componente presets |
| **(nuevo)** `BandConfigurationPanel.razor` | Componente bandas |
