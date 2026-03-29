# Epic #2 - I/O & CLI

**Status**: PARCIAL (~55% completado)
**Priority**: P1
**Depends on**: Epic #1 (Core Engine MVP) — COMPLETADO
**Estimated effort**: Medio (Core I/O listo, falta cablear CLI)

---

## Problem

El motor de clasificacion necesita leer imagenes satelitales reales (GeoTIFF, multi-banda)
y una interfaz de linea de comandos para que usuarios puedan entrenar, clasificar y validar
sin necesidad de UI.

## Solution

1. Implementar GDAL raster reader/writer usando MaxRev.Gdal.Core
2. Crear CLI tool `fuzzysat` con System.CommandLine + Spectre.Console
3. JSON config persistence para proyectos y sesiones de entrenamiento

## Micro-Commits — Estado Real

### Core I/O (COMPLETADO)
- [x] MC#1: IRasterReader + IRasterWriter interfaces
- [x] MC#2: GdalRasterReader implementation (thread-safe GDAL init)
- [x] MC#3: GdalRasterWriter implementation (GTiff output)
- [x] MC#4: RasterInfo metadata model
- [x] MC#11: Sample config JSON (`samples/sample-project.json`)

### CLI (PARCIAL — stubs sin cablear)
- [x] MC#5: CLI Program.cs + root command setup (System.CommandLine funcional)
- [ ] MC#6: ClassifyCommand — **STUB**: imprime "not yet implemented"
- [ ] MC#7: TrainCommand — **STUB**: imprime "not yet implemented"
- [ ] MC#8: ValidateCommand — **STUB**: imprime "not yet implemented"
- [ ] MC#9: VisualizeCommand — **NO EXISTE**: archivo no creado
- [ ] MC#10: InfoCommand — **STUB**: imprime "not yet implemented"

## Lo que Falta para Completar

1. **Cablear TrainCommand** a TrainingSession.CreateFromSamples() + CSV reader
2. **Cablear ClassifyCommand** a FuzzyClassifier + GdalRasterReader/Writer
3. **Cablear ValidateCommand** a ConfusionMatrix + GdalRasterReader
4. **Cablear InfoCommand** a GdalRasterReader.ReadInfo()
5. **Crear VisualizeCommand** para false color composites (PNG output)
6. **Spectre.Console output** — tablas, progress bars, colores
7. **JSON config loading** — leer ClassifierConfiguration desde archivo
8. **Tests CLI** — tests unitarios para cada comando

## Acceptance Criteria

- [x] Lee GeoTIFF single-band y multi-band (GdalRasterReader funcional)
- [x] Escribe GeoTIFF clasificado con class codes (GdalRasterWriter funcional)
- [ ] CLI `fuzzysat classify` produce output correcto
- [ ] CLI `fuzzysat info` muestra metadata de banda
- [x] JSON config se serializa/deserializa correctamente
- [x] `dotnet build` exitoso
