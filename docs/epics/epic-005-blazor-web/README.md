# Epic #5 - Blazor Web Application

**Status**: PARCIAL (~35% — UI scaffolding listo, funcionalidad real minima)
**Priority**: P3
**Depends on**: Epic #2 (minimo), Epic #3 (ideal)
**Estimated effort**: Muy alto (Leaflet integration, tile service, SignalR, wiring completo)

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

## Micro-Commits — Estado Real

### Completado (UI scaffolding)
- [x] MC#1: Blazor Server project setup + dependencies (Radzen 10.0.6)
- [x] MC#2: MainLayout + NavMenu (sidebar, breadcrumbs, responsive)
- [x] MC#3: Home page (hero, stats thesis, workflow cards, comparisons)
- [x] MC#4: ProjectSetup page — **FUNCIONAL**: save JSON, reset, sensor presets, Radzen toasts
- [x] MC#12: ConfusionMatrixHeatmap component — **REAL**: reusable, color-coded, tooltips

### Parcial (UI existe, datos hardcodeados/mock)
- [ ] MC#6: BandViewer page — **MOCK**: dropdown funciona pero stats/histogram hardcodeados
- [ ] MC#8: TrainingEditor page — **MOCK**: clases hardcodeadas, spectral chart inventado
- [ ] MC#9: SpectralChart + MembershipFunctionChart — **PARCIAL**: usa RadzenChart inline, no componentes reusables
- [ ] MC#11: Classification page — **MOCK**: `Task.Delay` simula progreso, no clasifica
- [ ] MC#13: ValidationResults page — **MOCK**: metricas de tesis hardcodeadas
- [ ] MC#15: Program.cs service registration — **PARCIAL**: solo Radzen registrado, no Core services

### No Implementado
- [ ] MC#5: TileService + TileController — **NO EXISTE**: necesario para servir bandas como tiles
- [ ] MC#7: leaflet-interop.js — **NO EXISTE**: JS interop para Leaflet, solo MapPlaceholder
- [ ] MC#10: ClassificationService (async + SignalR progress) — **NO EXISTE**
- [ ] MC#14: History page — **NO EXISTE**

## Issues Conocidos (de PR review y auditoria)

### Botones muertos (0 feedback al usuario):
1. ProjectSetup: Browse folder icon disabled
2. Validation: Export CSV sin click handler
3. Classification: Export Result sin click handler
4. Training: Extract Statistics permanentemente disabled
5. Training: Export Session permanentemente disabled

### Controles disabled sin contexto:
6. Training: 5 drawing tools disabled
7. MapPlaceholder: Zoom/fit disabled

### Mocks que parecen reales:
8. BandViewer: Dropdowns cambian variable pero no actualizan nada
9. BandViewer: Metadata/stats/histogram 100% hardcodeados
10. Training: Spectral chart con datos inventados
11. Classification: Run es simulacion con Task.Delay

### Seguridad/Robustez (PR #12 review):
12. ProjectSetup: Save usa UserProfile hardcodeado (server-side en Blazor Server)
13. ProjectSetup: Sanitizacion de filename incompleta (nombres reservados Windows)
14. ProjectSetup: Info leak — toast expone ruta del servidor y exception cruda

## Acceptance Criteria

- [ ] Workflow completo funciona end-to-end en browser
- [ ] Map interactivo muestra bandas y overlay clasificado
- [ ] Training areas se dibujan y estadisticas se calculan
- [ ] Progreso de clasificacion se muestra en tiempo real
- [ ] Confusion matrix es color-coded e interactiva (parcial: ConfusionMatrixHeatmap existe)
- [ ] Responsive: funciona en 1024x768
