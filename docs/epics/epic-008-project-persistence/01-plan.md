# Epic #8 — Plan de Implementacion

> **Branch**: `feature/epic-008-project-persistence`
> **Estimado**: 6-7 micro-commits, 1 PR
> **Referencia**: Issue [#28](https://github.com/ivanrlg/FuzzySat/issues/28)

---

## Estructura de Almacenamiento

```
%APPDATA%/FuzzySat/projects/
  {project-name}.json                    # (existente) ClassifierConfiguration
  {project-name}/                        # NUEVO: directorio de datos del proyecto
    training-regions.json                # Regiones dibujadas (List<TrainingRegion>)
    training-samples.csv                 # Muestras etiquetadas (formato CSV round-trip)
    training-session.json                # TrainingSessionDto (estadisticas espectrales)
    classification-result.bin.gz         # Mapa de clases + confianza (binary, GZip)
    classification-options.json          # Parametros de clasificacion usados
    validation-result.json               # Matriz de confusion + metricas
```

---

## Fases

### Fase 1: Mover TrainingRegion a Core + Crear DTOs e Interfaz

**Commit**: `feat(core): move TrainingRegion to Core and create persistence DTOs`

| Accion | Archivo | Detalle |
|--------|---------|---------|
| Crear | `Core/Training/TrainingRegion.cs` | Mover record desde PixelExtractionService |
| Crear | `Core/Persistence/IProjectRepository.cs` | Interfaz del repositorio |
| Crear | `Core/Persistence/ClassificationResultDto.cs` | DTO: Rows, Columns, ClassNames |
| Crear | `Core/Persistence/ClassificationOptionsDto.cs` | DTO: MfType, AndOperator, Defuzzifier |
| Crear | `Core/Persistence/ValidationResultDto.cs` | DTO: Matrix, ClassNames, OA, Kappa |
| Modificar | `Web/Services/PixelExtractionService.cs` | Remover TrainingRegion record |
| Modificar | `Web/Components/Pages/Training.razor` | Actualizar using si necesario |

**Nota**: `TrainingSessionDto` ya existe en `Core/Training/` — se reutiliza.

### Fase 2: Factory Method para ConfusionMatrix

**Commit**: `feat(core): add ConfusionMatrix.FromPersistedData factory`

| Accion | Archivo | Detalle |
|--------|---------|---------|
| Modificar | `Core/Validation/ConfusionMatrix.cs` | Agregar `FromPersistedData(classNames, matrix)` |

### Fase 3: Implementar FileProjectRepository

**Commit**: `feat(web): implement FileProjectRepository`

| Accion | Archivo | Detalle |
|--------|---------|---------|
| Crear | `Web/Services/FileProjectRepository.cs` | Implementacion de IProjectRepository |

Metodos:
- Training regions: JSON serialize/deserialize
- Training samples: CSV string save/load
- Training session: JSON via TrainingSessionDto
- Classification result: Binary + GZip (byte class index + float32 confidence)
- Classification options: JSON
- Validation result: JSON

### Fase 4: Implementar ProjectPersistenceService

**Commit**: `feat(web): implement ProjectPersistenceService auto-save`

| Accion | Archivo | Detalle |
|--------|---------|---------|
| Crear | `Web/Services/ProjectPersistenceService.cs` | Auto-save con debounce 500ms |

Comportamiento:
- Scoped (mismo lifetime que ProjectStateService)
- Suscribe a OnStateChanged
- Compara referencias para detectar cambios
- I/O en background thread
- Cancela saves pendientes al cambiar proyecto

### Fase 5: Restaurar Estado + Wiring DI

**Commit**: `feat(web): restore project state on load`

| Accion | Archivo | Detalle |
|--------|---------|---------|
| Modificar | `Web/Components/Pages/ProjectSetup.razor` | Restaurar artefactos en LoadSelectedProject() |
| Modificar | `Web/Program.cs` | Registrar IProjectRepository + ProjectPersistenceService |
| Modificar | `Web/Components/Layout/MainLayout.razor` | Inyectar PersistenceService para inicializacion |

### Fase 6: Tests

**Commit**: `test: add persistence round-trip and repository tests`

| Accion | Archivo | Detalle |
|--------|---------|---------|
| Crear | `tests/Core.Tests/Persistence/PersistenceDtoTests.cs` | Round-trip DTOs |
| Crear | `tests/Core.Tests/Validation/ConfusionMatrixPersistenceTests.cs` | FromPersistedData tests |

---

## Verificacion

1. `dotnet build` despues de cada fase
2. `dotnet test` despues de fase 6
3. Test E2E manual:
   - Crear proyecto > cargar raster > dibujar regiones > entrenar
   - Cerrar navegador > reabrir > cargar proyecto
   - Verificar: regiones, muestras, sesion restauradas
   - Clasificar > validar > cerrar > reabrir
   - Verificar: clasificacion y validacion restauradas
