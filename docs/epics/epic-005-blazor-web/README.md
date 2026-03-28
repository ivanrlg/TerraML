# Epic #5 - Blazor Web Application

**Status**: Planificado
**Priority**: P3
**Depends on**: Epic #2 (minimo), Epic #3 (ideal)
**Estimated effort**: TBD

---

## Problem

CLI es util para power users pero una web UI es necesaria para hacer FuzzySat accesible
a investigadores y estudiantes sin experiencia en terminal.

## Solution

Blazor Server web app (server-side rendering porque GDAL es nativo y satellite processing
es CPU/memory intensive). Incluye:

1. Interactive map (Leaflet.js) para visualizar bandas y dibujar training areas
2. False color composite viewer
3. Training editor con charts de spectral curves y membership functions
4. Clasificacion con progreso en tiempo real via SignalR
5. Confusion matrix interactiva
6. Export: GeoTIFF, JSON, PDF

## Pages (Wizard Flow)

```
Home --> Project Setup --> Band Viewer --> Training Editor --> Classification --> Validation
                                                                                     |
                                                                                  History
```

## Micro-Commits Planificados

- [ ] MC#1: Blazor Server project setup + dependencies (Leaflet, Radzen)
- [ ] MC#2: MainLayout + NavMenu
- [ ] MC#3: Home page
- [ ] MC#4: ProjectSetup page (upload, band config, class definition)
- [ ] MC#5: TileService + TileController
- [ ] MC#6: BandViewer page con Leaflet map
- [ ] MC#7: leaflet-interop.js (map init, overlays, drawing)
- [ ] MC#8: TrainingEditor page (map + panel split)
- [ ] MC#9: SpectralChart + MembershipFunctionChart components
- [ ] MC#10: ClassificationService (async + SignalR progress)
- [ ] MC#11: Classification page (progress bar + result overlay)
- [ ] MC#12: ConfusionMatrixTable component
- [ ] MC#13: ValidationResults page
- [ ] MC#14: History page
- [ ] MC#15: Program.cs service registration

## Acceptance Criteria

- [ ] Workflow completo funciona end-to-end en browser
- [ ] Map interactivo muestra bandas y overlay clasificado
- [ ] Training areas se dibujan y estadisticas se calculan
- [ ] Progreso de clasificacion se muestra en tiempo real
- [ ] Confusion matrix es color-coded e interactiva
- [ ] Responsive: funciona en 1024x768
