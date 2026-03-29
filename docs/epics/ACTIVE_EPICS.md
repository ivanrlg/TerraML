# EPICs Activos - FuzzySat

> **Ultima actualizacion**: 2026-03-28
> **Proposito**: Punto central de informacion de EPICs en desarrollo.
> Para EPICs completados ver [EPIC_HISTORY.md](EPIC_HISTORY.md)

---

## Resumen por Estado

| Estado | Cantidad | EPICs |
|--------|----------|-------|
| En Progreso | 0 | - |
| Planificado | 5 | #1 Core Engine, #2 I/O & CLI, #3 Advanced, #4 ML Hybrid, #5 Blazor Web |
| Documentacion | 1 | #6 Gap Analysis: Road to 100% |
| Completado | 0 | - |

---

## Epic #1 - Core Engine MVP

- **Status**: Planificado
- **Priority**: P0-CRITICAL (fundamento de todo el proyecto)
- **Folder**: [epic-001-core-engine/](epic-001-core-engine/)
- **Depends on**: Scaffolding (este PR)
- **Estimado**: TBD
- **PRs**: Ninguno aun

**Scope**: Motor de logica difusa completo: membership functions (Gaussian), fuzzy rules,
inference engine (AND=Min), defuzzifier (Max Weight), training data extractor, confusion
matrix, Kappa statistic. Unit tests con valores de la tesis original.

---

## Epic #2 - I/O & CLI

- **Status**: Planificado
- **Priority**: P1
- **Folder**: [epic-002-io-cli/](epic-002-io-cli/)
- **Depends on**: Epic #1
- **Estimado**: TBD
- **PRs**: Ninguno aun

**Scope**: GDAL raster reader/writer (GeoTIFF), CLI commands (train, classify, validate,
visualize, info), JSON config persistence, sample configuration.

---

## Epic #3 - Advanced Features

- **Status**: Planificado
- **Priority**: P2
- **Folder**: [epic-003-advanced-features/](epic-003-advanced-features/)
- **Depends on**: Epic #2
- **Estimado**: TBD
- **PRs**: Ninguno aun

**Scope**: MFs adicionales (triangular, trapezoidal, bell), operador producto como
alternativa a min, indices espectrales (NDVI, NDWI, NDBI), PCA, confidence maps.

---

## Epic #4 - ML Hybrid

- **Status**: Planificado
- **Priority**: P3
- **Folder**: [epic-004-ml-hybrid/](epic-004-ml-hybrid/)
- **Depends on**: Epic #3
- **Estimado**: TBD
- **PRs**: Ninguno aun

**Scope**: ML.NET integration, membership degrees como features para neural network
o random forest, automated training area suggestion (K-Means).

---

## Epic #5 - Blazor Web App

- **Status**: Planificado
- **Priority**: P3
- **Folder**: [epic-005-blazor-web/](epic-005-blazor-web/)
- **Depends on**: Epic #2 (minimo), Epic #3 (ideal)
- **Estimado**: TBD
- **PRs**: Ninguno aun

**Scope**: Blazor Server web app con Leaflet.js maps, upload de imagery, training area
drawing, clasificacion con progreso en tiempo real, confusion matrix interactiva,
export GeoTIFF/JSON/PDF.

---

## Epic #6 - Gap Analysis: Road to 100%

- **Status**: Documentacion (solo analisis, no codigo)
- **Priority**: P0 — Prerequisito para planificar trabajo restante
- **Folder**: [epic-006-gap-to-100/](epic-006-gap-to-100/)
- **Depends on**: Ninguno
- **Creado**: 2026-03-28
- **PRs**: Ninguno (documentacion only)

**Scope**: Auditoria exhaustiva del estado real del proyecto vs lo documentado en EPICs
#1-#5. Cataloga todos los gaps para llegar al 100%: Core ~95% completo, CLI ~20%,
API ~0%, Web ~25%, CI/CD ~30%. Incluye roadmap priorizado de 69 tareas en 7 fases.
Documenta decisiones tecnicas criticas (Leaflet JS interop, tile rendering server-side,
API async pattern, GDAL en Docker).

**Documentos**:
- [00-overview.md](epic-006-gap-to-100/00-overview.md) — Inventario completo de gaps
- [01-plan.md](epic-006-gap-to-100/01-plan.md) — Roadmap priorizado por fases
- [02-technical-design.md](epic-006-gap-to-100/02-technical-design.md) — Decisiones tecnicas y riesgos
