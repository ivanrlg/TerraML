# Epic #7 — Diseno Tecnico

---

## Decision 1: Flujo del Pipeline Hibrido

El HybridClassifier ya existe en Core/ML/ pero no esta conectado a la Web.
La integracion requiere un nuevo servicio web que:

1. Tome el TrainingSession existente + las muestras de pixels etiquetados
2. Construya un FuzzyRuleSet (reutilizando ClassificationService.BuildRuleSet)
3. Cree un FuzzyFeatureExtractor desde el rule set
4. Extraiga features de las muestras de entrenamiento
5. Entrene el modelo ML.NET (Random Forest o SDCA)
6. Clasifique pixel a pixel usando HybridClassifier.ClassifyPixel()

**Riesgo**: El entrenamiento ML puede ser lento para imagenes grandes.
**Mitigacion**: Mostrar progreso granular, permitir cancelacion, entrenar solo con
las muestras (no toda la imagen).

---

## Decision 2: Side-by-Side Canvas vs Leaflet

**Elegido**: HTML5 Canvas side-by-side (no Leaflet)

**Razones**:
- Consistencia con el patron existente en training-selection.js
- Sin nuevas dependencias
- Control total sobre rendering de pixels individuales
- Leaflet seria mas util para georeferencing/tiles, no necesario aqui
- SkiaSharp ya esta en el proyecto para generar PNGs

---

## Decision 3: Colores Automaticos con Keyword Matching

Paleta inteligente que detecta nombres comunes:
- "water", "agua", "mar", "ocean" -> #3498DB (azul)
- "forest", "bosque", "tree" -> #27AE60 (verde)
- "urban", "urbano", "city", "ciudad" -> #E74C3C (rojo)
- "agriculture", "cultivo", "crop" -> #F39C12 (naranja)
- "bare", "suelo", "soil" -> #D4A574 (marron)
- "vegetation", "vegetacion" -> #2ECC71 (verde claro)
- "cloud", "nube" -> #ECF0F1 (gris claro)
- "shadow", "sombra" -> #34495E (gris oscuro)

Fallback: paleta colorblind-friendly para nombres no reconocidos.
Editable via RadzenColorPicker.

---

## Decision 4: Calculo de Area

GeoTransform de GDAL = [originX, pixelWidth, rotationX, originY, rotationY, pixelHeight]

- pixelWidth = GeoTransform[1] (metros para UTM)
- pixelHeight = abs(GeoTransform[5]) (negativo en GDAL)
- areaPerPixel = pixelWidth * pixelHeight (m2)
- areaHa = areaM2 / 10000

Para Sentinel-2: pixeles de 10m -> 100 m2 por pixel.

**Si no hay GeoTransform**: mostrar solo pixel count, area = "N/A".

---

## Decision 5: GeoTIFF Export desde Web

Flujo:
1. Usuario hace clic en "Download GeoTIFF"
2. Backend escribe a Path.GetTempFileName() con extension .tif
3. GdalRasterWriter.Write() con sourceInfo del raster original
4. Lee bytes del archivo temporal
5. Envia via JS interop downloadFile (blob)
6. Elimina archivo temporal

**Riesgo**: Archivos grandes en memoria.
**Mitigacion**: Ya hay un guard de 50M pixeles (OOM guard existente).

---

## Dependencias

| Dependencia | Estado | Uso |
|-------------|--------|-----|
| ML.NET 5.0.0 | Ya instalada | HybridClassifier |
| SkiaSharp | Ya instalada | PNG rendering |
| GDAL (MaxRev) | Ya instalada | GeoTIFF export |
| Radzen | Ya instalada | Charts, ColorPicker, UI |

No se requieren nuevas dependencias.
