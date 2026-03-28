# Epic #2 - I/O & CLI

**Status**: Planificado
**Priority**: P1
**Depends on**: Epic #1 (Core Engine MVP)
**Estimated effort**: TBD

---

## Problem

El motor de clasificacion necesita leer imagenes satelitales reales (GeoTIFF, multi-banda)
y una interfaz de linea de comandos para que usuarios puedan entrenar, clasificar y validar
sin necesidad de UI.

## Solution

1. Implementar GDAL raster reader/writer usando MaxRev.Gdal.Core
2. Crear CLI tool `fuzzysat` con System.CommandLine + Spectre.Console
3. JSON config persistence para proyectos y sesiones de entrenamiento

## Micro-Commits Planificados

- [ ] MC#1: IRasterReader + IRasterWriter interfaces
- [ ] MC#2: GdalRasterReader implementation
- [ ] MC#3: GdalRasterWriter implementation
- [ ] MC#4: RasterInfo metadata model
- [ ] MC#5: CLI Program.cs + root command setup
- [ ] MC#6: ClassifyCommand
- [ ] MC#7: TrainCommand
- [ ] MC#8: ValidateCommand
- [ ] MC#9: VisualizeCommand (false color composite)
- [ ] MC#10: InfoCommand (band metadata)
- [ ] MC#11: Sample config JSON + samples/README.md

## Acceptance Criteria

- [ ] Lee GeoTIFF single-band y multi-band
- [ ] Escribe GeoTIFF clasificado con colores por clase
- [ ] CLI `fuzzysat classify` produce output correcto
- [ ] CLI `fuzzysat info` muestra metadata de banda
- [ ] JSON config se serializa/deserializa correctamente
- [ ] `dotnet build` y `dotnet test` exitosos
