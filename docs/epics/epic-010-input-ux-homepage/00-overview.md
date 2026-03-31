# Epic #10 — Input UX Redesign & Homepage Overhaul

> **Status**: En Desarrollo
> **Priority**: P1
> **Creado**: 2026-03-31
> **Prerequisito**: Epic #8 completado (PR #30)

---

## Objetivo

Redisenar la experiencia de entrada de datos raster (el punto mas debil del flujo actual segun
el usuario) y reorganizar el homepage para que se enfoque en el uso de la herramienta, no en
datos historicos de la tesis.

**Problema principal**: El flujo de importacion raster en ProjectSetup tiene dos caminos
(Direct Path vs Sentinel-2 Import) que se confunden entre si, con presets de sensores que no
coinciden con las bandas realmente importadas, y opciones que deberian ocultarse segun el
camino elegido.

---

## Alcance

### Pilar A — Input Raster UX (Prioridad ALTA)

| ID | Requerimiento | Criterio de Aceptacion |
|----|---------------|------------------------|
| A1 | Wizard inteligente de importacion | Flujo paso a paso que guia al usuario segun el tipo de datos (Sentinel-2, GeoTIFF multiband, etc.) |
| A2 | Auto-deteccion de sensor preset | Al importar Sentinel-2, el preset se selecciona automaticamente |
| A3 | Bandas sincronizadas con seleccion | Si selecciono 4 bandas de 10m, el preset muestra 4 bandas (no 13) |
| A4 | Ocultar VRT cuando es automatico | Si voy por Sentinel-2 import, no mostrar "Create VRT" como paso manual |
| A5 | Nombres de bandas consistentes | Normalizar B01/B1 — usar formato unico en toda la app |
| A6 | Custom preset auto-poblado | Al elegir Custom, auto-poblar con las bandas detectadas del raster |
| A7 | Soporte imagenes pre-stacked | Tool in-app para convertir imagenes grandes (ej. 600MB TIF de Copernicus) |

### Pilar B — Homepage Redesign (Prioridad MEDIA)

| ID | Requerimiento | Criterio de Aceptacion |
|----|---------------|------------------------|
| B1 | Pagina dedicada para tesis | Datos de la tesis 2008 (OA, Kappa, comparacion) en pagina `/thesis` |
| B2 | Home orientado a workflow | Diagrama del pipeline: fuzzificacion → inferencia → defuzzificacion → hibrido ML |
| B3 | Getting started | Guia visual de como empezar a usar FuzzySat |
| B4 | Navegacion actualizada | Agregar enlace a pagina Thesis en el sidebar |

---

## Fuera de Alcance

- Leaflet maps (pertenece a Epic #5)
- Nuevos clasificadores ML (pertenece a Epic #9)
- API controllers (pendiente en Epic #5)
- Cambios al Core engine

---

## Riesgos

| Riesgo | Mitigacion |
|--------|-----------|
| Refactoring de ProjectSetup.razor es grande (~900 lineas) | Dividir en componentes Razor separados (WizardStep1, WizardStep2, etc.) |
| Romper proyectos guardados existentes | Mantener compatibilidad con formato JSON actual |
| Band name normalization afecta Training y Classification | Hacer cambio atomico y actualizar todos los consumidores en el mismo PR |
