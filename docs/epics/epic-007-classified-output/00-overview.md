# Epic #7 — Classified Image Output & Hybrid ML Integration

> **Status**: Planificado
> **Priority**: P1
> **Creado**: 2026-03-31
> **Prerequisito**: Completar PR actual de Epic #5

---

## Objetivo

Completar el ciclo de clasificacion de FuzzySat produciendo una **imagen clasificada visual**
(cada pixel coloreado por su clase de cobertura), integrando el **pipeline hibrido ML** en la
Web UI, y calculando **estadisticas de area** por clase.

En la tesis original de 2008, el entregable final era una nueva imagen satelital clasificada.
Este EPIC replica esa funcionalidad con tecnologia moderna.

---

## Alcance

| ID | Requerimiento | Criterio de Aceptacion |
|----|---------------|------------------------|
| R1 | Clasificacion hibrida ML en Web | Usuario puede elegir Pure Fuzzy / Random Forest / SDCA. Hybrid entrena con muestras existentes, luego clasifica. Indicador visual del metodo activo. |
| R2 | Generacion y visualizacion de imagen clasificada | Imagen coloreada renderizada en Canvas. Side-by-side: original vs clasificada. Zoom/pan sincronizado. Colores auto-asignados y editables. |
| R3 | Estadisticas de area por clase | Tabla: clase, pixeles, area (m2, ha), porcentaje. Charts. Incluido en CSV export. |
| R4 | Exportacion GeoTIFF desde Web | Boton para descargar el raster clasificado como GeoTIFF georeferenciado. |

---

## Codigo Existente a Reutilizar

| Componente | Ubicacion | Uso |
|-----------|----------|-----|
| HybridClassifier | `Core/ML/HybridClassifier.cs` | TrainRandomForest / TrainSdca |
| FuzzyFeatureExtractor | `Core/ML/FuzzyFeatureExtractor.cs` | Crear desde rule set + band names |
| GdalRasterWriter | `Core/Raster/GdalRasterWriter.cs` | Escribir GeoTIFF clasificado |
| ClassificationService.BuildRuleSet() | `Web/Services/ClassificationService.cs:132` | Construir reglas para feature extractor |
| RasterInfo.GeoTransform | `Core/Raster/RasterInfo.cs:33` | Tamano de pixel para calculo de area |
| training-selection.js | `Web/wwwroot/js/training-selection.js` | Patron Canvas zoom/pan |
| SkiaSharp | Dependencia existente (BandViewer) | Codificacion PNG |

---

## Entregables

- 3 Pull Requests (~16 commits total)
- PR #1: Integracion Hybrid ML en Web
- PR #2: Renderizado de imagen clasificada (side-by-side viewer)
- PR #3: Estadisticas de area + exportacion GeoTIFF
