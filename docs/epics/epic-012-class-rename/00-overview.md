# Epic #12 - Class Rename Support

> **Status**: Planificado
> **Priority**: P2
> **Creado**: 2026-04-01

---

## Objetivo

Permitir al usuario renombrar clases de cobertura (Land Cover Classes) despues de crearlas,
propagando el cambio a todos los artefactos dependientes (training samples, sesion, resultados).

## Problema Actual

Las clases se crean con nombres genericos (`Class1`, `Class2`, etc.) y el nombre actua como
clave primaria en todo el sistema:

- `LabeledPixelSample.ClassName` — muestras de entrenamiento
- `TrainingSession.Statistics` — diccionario con nombre como key
- CSV export/import — primera columna es el nombre
- Resultados de clasificacion y confusion matrix

No existe ningun mecanismo para cambiar el nombre despues de la creacion. El campo `Code` (int)
existe pero no se usa como identificador para lookups.

## Alcance

### Incluido
- Boton de edicion inline en `LandCoverClassPanel` (ProjectSetup)
- Metodo `RenameClass(oldName, newName)` en `ProjectStateService`
- Propagacion cascada a: samples, sesion de entrenamiento, colores de clasificacion
- Validacion: nombre unico, no vacio, no duplicado
- Re-persistencia automatica de artefactos afectados
- Tests unitarios para rename cascading

### Excluido
- Reordenar clases
- Merge/combinar clases
- Cambiar el `Code` numerico (se mantiene inmutable)
- Delete de clases (feature separada)

## Criterios de Aceptacion

- [ ] Usuario puede hacer click en nombre de clase y editarlo inline
- [ ] Rename propaga a training samples existentes
- [ ] Rename propaga a sesion de entrenamiento (Statistics dictionary)
- [ ] Rename propaga a ClassColors en ProjectStateService
- [ ] Validacion impide nombres duplicados o vacios
- [ ] Artefactos se re-persisten automaticamente
- [ ] Tests cubren rename con y sin datos de entrenamiento
- [ ] Build y tests pasan
