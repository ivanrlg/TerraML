# Epic #11 - Project History Page

> **Status**: COMPLETADO
> **Priority**: P2
> **Creado**: 2026-04-01

---

## Objetivo

Agregar una pagina de historial de proyectos (/history) que permita al usuario:
1. Ver todos los proyectos guardados con su estado y metricas
2. Cargar un proyecto existente para continuar trabajando
3. Eliminar proyectos que ya no necesite

## Alcance

### Implementado
- `ProjectSummary` record (Name, BandCount, ClassCount, Method, Accuracy, Kappa, LastModified, Status)
- `ProjectStatus` enum (Configured, Trained, Classified, Validated) — derivado de archivos de artefactos
- `ProjectLoaderService.GetProjectSummaries()` — agrega datos de config + artefactos
- `ProjectLoaderService.DeleteProject()` — elimina config + directorio de datos con proteccion path traversal
- `History.razor` — pagina con grid de tarjetas, badges de estado, metricas, timestamps relativos
- `History.razor.css` — estilos scoped siguiendo design system existente
- NavMenu: seccion TOOLS con link a History
- 11 tests nuevos (summaries + delete + path traversal)

### Decision: Leaflet Deferred
El enfoque canvas-based (SkiaSharp server-side + training-selection.js + classification-canvas.js)
es funcional y probado. Agregar Leaflet requeriria rearquitecturar toda la pipeline de renderizado
para zero ganancia funcional. Se marca como "Won't Do" y se cierra Epic #5.

## Criterios de Aceptacion
- [x] /history muestra todos los proyectos guardados
- [x] Tarjetas muestran estado, metricas, y timestamp
- [x] Boton Load navega a ProjectSetup con el proyecto
- [x] Boton Delete con dialogo de confirmacion
- [x] Estado vacio muestra CTA para crear proyecto
- [x] Tests pasan (build + test)
