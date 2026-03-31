# Epic #10 — Plan de Implementacion

> **Estimacion**: 4 PRs, ~20-25 micro-commits
> **Orden**: Pilar A (Input UX) primero, Pilar B (Homepage) despues

---

## Fase 1 — Refactoring: Componentizar ProjectSetup (PR #1)

**Objetivo**: Romper el monolito ProjectSetup.razor en componentes manejables antes de
cambiar la logica.

| # | Tarea | Detalle |
|---|-------|---------|
| 1.1 | Extraer InputRasterWizard.razor | Componente dedicado para todo el flujo de importacion raster |
| 1.2 | Extraer SensorPresetSelector.razor | Componente para seleccion y aplicacion de presets |
| 1.3 | Extraer BandConfigurationPanel.razor | Componente para la tabla de bandas (add/edit/remove) |
| 1.4 | Extraer LandCoverClassPanel.razor | Componente para clases de cobertura |
| 1.5 | Verificar compilacion y tests | ProjectSetup debe funcionar identico con los componentes extraidos |

**Commits estimados**: 5-6

---

## Fase 2 — Smart Import Wizard (PR #2)

**Objetivo**: Reemplazar los dos modos (Direct Path + Sentinel-2) con un wizard inteligente.

| # | Tarea | Detalle |
|---|-------|---------|
| 2.1 | Paso 1: Seleccionar fuente | Opciones: "Single raster file" / "Sentinel-2 folder" / "Multi-band folder" |
| 2.2 | Paso 2: Auto-deteccion | Detectar tipo de archivo, sensor, bandas disponibles automaticamente |
| 2.3 | Paso 3: Seleccion de bandas | Si Sentinel-2: agrupar por resolucion, seleccionar grupo. Si single file: mostrar bandas del raster |
| 2.4 | Auto-preset sincronizado | Al detectar Sentinel-2, preset = Sentinel-2. Bandas del preset = solo las seleccionadas |
| 2.5 | VRT automatico (oculto) | Si Sentinel-2 con multiples archivos, crear VRT en background sin boton manual |
| 2.6 | Custom preset auto-poblado | Si Custom o no-match, poblar preset con bandas reales del raster (nombres + indices) |
| 2.7 | Normalizar nombres de bandas | B01 everywhere (con leading zero). Actualizar preset definitions y Training/Classification |
| 2.8 | Tests para el wizard | Unit tests del nuevo flujo |

**Commits estimados**: 7-8

---

## Fase 3 — Soporte imagenes pre-stacked y conversion (PR #3)

**Objetivo**: Manejar imagenes grandes pre-apiladas (ej. Copernicus 600MB TIF) sin
herramientas externas.

| # | Tarea | Detalle |
|---|-------|---------|
| 3.1 | Detectar raster multiband automatico | Al cargar un TIF con N bandas, mostrar info de cada banda |
| 3.2 | Tool de conversion in-app | Boton para convertir formatos soportados por GDAL (jp2→tif, etc.) |
| 3.3 | Opcion de subsetting | Permitir seleccionar subset de bandas de un raster multiband |
| 3.4 | Progress indicator | Barra de progreso para operaciones largas (conversion, VRT) |

**Commits estimados**: 4-5

---

## Fase 4 — Homepage Redesign + Pagina Thesis (PR #4)

**Objetivo**: Home enfocado en workflow, datos de tesis en pagina dedicada.

| # | Tarea | Detalle |
|---|-------|---------|
| 4.1 | Crear pagina Thesis.razor | Mover datos de comparacion (OA, Kappa, tabla de clasificadores) a /thesis |
| 4.2 | Redisenar Home.razor | Pipeline diagram (fuzzificacion → inferencia → defuzzificacion), CTA para crear proyecto |
| 4.3 | Seccion "How it works" | Diagrama visual del proceso de clasificacion difusa + hibrido ML |
| 4.4 | Agregar a navegacion | Link a /thesis en sidebar |
| 4.5 | Tests de navegacion | Verificar que ambas paginas cargan y navegan correctamente |

**Commits estimados**: 5-6

---

## Dependencias entre Fases

```
Fase 1 (Componentizar) → Fase 2 (Smart Wizard) → Fase 3 (Pre-stacked)
                                                         ↓
                                           Fase 4 (Homepage) [independiente, puede ir en paralelo con Fase 3]
```

---

## Criterio de Exito Global

- [ ] Usuario puede importar Sentinel-2 sin confusion sobre presets o VRT
- [ ] Bandas del preset coinciden exactamente con las bandas importadas
- [ ] Nombres de bandas consistentes en toda la app (B01, B02, etc.)
- [ ] Imagenes pre-stacked grandes se manejan sin herramientas externas
- [ ] Homepage muestra workflow, no datos de tesis
- [ ] Datos de tesis accesibles en pagina dedicada
- [ ] Zero regresiones en flujo existente (Training, Classification, Validation)
