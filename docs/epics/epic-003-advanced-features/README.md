# Epic #3 - Advanced Features

**Status**: CASI COMPLETADO (~85%)
**Priority**: P2
**Depends on**: Epic #2 (I/O & CLI) — parcial, pero features de Core no dependen de CLI
**Estimated effort**: Bajo (solo falta PCA)

---

## Problem

El MVP solo soporta Gaussian MF y Min operator. Para competir con clasificadores modernos
y soportar mas escenarios, se necesitan opciones adicionales.

## Solution

1. Membership functions adicionales (triangular, trapezoidal, bell-shaped)
2. Operador producto como alternativa a min para AND
3. Metodos de defuzzificacion configurables
4. Indices espectrales (NDVI, NDWI, NDBI) como bandas derivadas
5. PCA para reduccion de dimensionalidad
6. Confidence maps (raster de max weight por pixel)

## Micro-Commits — Estado Real

### Implementado
- [x] MC#1: TriangularMembershipFunction + tests
- [x] MC#2: TrapezoidalMembershipFunction + tests
- [x] MC#3: BellMembershipFunction (Generalized Bell) + tests
- [x] MC#4: ProductAndOperator + tests (en FuzzyOperators.cs)
- [x] MC#5: WeightedAverageDefuzzifier + tests — *Nota: CentroidDefuzzifier no implementado*
- [x] MC#6: SpectralIndexCalculator (NDVI, NDWI, NDBI) + tests
- [x] MC#8: ConfidenceMapGenerator + tests

### Pendiente
- [ ] MC#7: **PCA implementation** — no existe en el codebase
- [ ] MC#9: Configuracion de metodos en JSON — no verificado como feature explicita

## Lo que Falta para Completar

1. **PCA (Principal Component Analysis)** — reduccion de dimensionalidad para 13+ bandas
   - Recomendacion: usar ML.NET `PrincipalComponentAnalysis` estimator
   - Ubicacion: `FuzzySat.Core/ML/PcaTransformer.cs`
2. **CentroidDefuzzifier** (opcional) — mencionado en MC#5 pero no implementado
   - WeightedAverageDefuzzifier ya cubre este rol, evaluar si es necesario

## Acceptance Criteria

- [x] Todas las MF types producen curvas correctas (4 tipos verificados)
- [x] Clasificacion con producto vs min disponible (ProductAnd en FuzzyOperators)
- [x] NDVI calculado correctamente para datos conocidos
- [x] Confidence map se genera como raster valido
- [ ] PCA reduce dimensionalidad efectivamente
