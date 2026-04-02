# Epic #12 - Plan de Implementacion

> **Commits estimados**: 5-6
> **PRs**: 1

---

## Fase 1: Rename en ProjectStateService (backend)

**Commit 1**: Add RenameClass method to ProjectStateService

- Agregar `RenameClass(string oldName, string newName)` en `ProjectStateService`
- Actualizar `ClassifierConfiguration.Classes` (lista de `LandCoverClass`)
- Actualizar `TrainingSamples` — cambiar `ClassName` en todas las muestras afectadas
- Actualizar `TrainingSession.Statistics` — re-key el diccionario
- Actualizar `ClassColors` dictionary
- Validacion: nombre no vacio, no duplicado, clase existe

## Fase 2: Re-persistencia automatica

**Commit 2**: Auto-persist after rename

- Llamar `ProjectPersistenceService` para re-guardar artefactos modificados
- Re-persistir: config, samples CSV, sesion de entrenamiento
- Garantizar atomicidad (todo o nada)

## Fase 3: UI — Edit inline en LandCoverClassPanel

**Commit 3**: Add inline edit to LandCoverClassPanel

- Agregar boton de edicion (icono `edit`) por fila en el DataGrid
- Click activa modo edicion inline en la celda del nombre
- Enter o blur confirma, Escape cancela
- Notificar al padre via `ClassesChanged` callback
- Propagar rename via `ProjectStateService`

## Fase 4: Tests

**Commit 4**: Add unit tests for class rename

- Test: rename basico sin datos de entrenamiento
- Test: rename con samples existentes propaga correctamente
- Test: rename con sesion de entrenamiento re-keys statistics
- Test: rename a nombre duplicado falla con validacion
- Test: rename a nombre vacio falla con validacion

## Fase 5: Documentacion

**Commit 5**: Update ACTIVE_EPICS.md and close epic

- Actualizar ACTIVE_EPICS.md con Epic #12
- Marcar criterios de aceptacion como completados
