# Epic #12 - Diseno Tecnico

---

## Problema Central: Name-as-Key

El nombre de clase actua como foreign key en todo el sistema:

```
ClassifierConfiguration.Classes[].Name  <-- definicion
    |
    +-- LabeledPixelSample.ClassName     <-- training samples
    +-- TrainingSession.Statistics[key]  <-- estadisticas
    +-- ProjectStateService.ClassColors  <-- colores UI
    +-- CSV export (primera columna)     <-- persistencia
```

El `Code` (int) existe pero solo se usa para encoding del raster de salida, no para lookups.

## Estrategia: Rename Cascading

Un rename es una operacion atomica que actualiza todas las referencias en memoria y luego
re-persiste los artefactos afectados.

### Orden de actualizacion

1. Validar (nombre unico, no vacio, clase existe)
2. Actualizar `ClassifierConfiguration.Classes` — crear nuevo `LandCoverClass` con nuevo nombre
3. Actualizar `TrainingSamples` — filtrar por `ClassName == oldName`, reemplazar
4. Actualizar `TrainingSession.Statistics` — re-key dictionary
5. Actualizar `ClassColors` — re-key dictionary
6. Re-persistir artefactos via `ProjectPersistenceService`

### Archivos afectados

| Archivo | Cambio |
|---------|--------|
| `ProjectStateService.cs` | Nuevo metodo `RenameClass()` |
| `LandCoverClassPanel.razor` | Boton edit + inline editing |
| `ProjectSetup.razor` | Handler para rename callback |
| Tests nuevos | Rename cascading + validacion |

## Decisiones

- **No migrar a Code-as-key**: Seria un refactor masivo que cambia toda la arquitectura.
  El rename cascading es mas pragmatico y de menor riesgo.
- **Rename en cualquier punto del workflow**: No restringir a solo "antes de entrenar".
  Si hay datos, se propagan. El usuario debe poder corregir nombres en cualquier momento.
- **ClassEntry sigue siendo record**: Se crea un nuevo ClassEntry, no se muta el existente.

## Riesgos

| Riesgo | Mitigacion |
|--------|------------|
| Datos huerfanos si falla a mitad | Validar todo antes de empezar, operacion atomica en memoria |
| Clasificacion activa con nombre viejo | No hay clasificacion "activa" — results son snapshots |
| CSV guardado con nombre viejo | Re-persistir CSV como parte del rename |
