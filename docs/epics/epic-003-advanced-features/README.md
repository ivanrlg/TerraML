# Epic #3 - Advanced Features

**Status**: Planificado
**Priority**: P2
**Depends on**: Epic #2 (I/O & CLI)
**Estimated effort**: TBD

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

## Micro-Commits Planificados

- [ ] MC#1: TriangularMembershipFunction + tests
- [ ] MC#2: TrapezoidalMembershipFunction + tests
- [ ] MC#3: BellMembershipFunction + tests
- [ ] MC#4: ProductAndOperator + tests
- [ ] MC#5: WeightedAverageDefuzzifier + CentroidDefuzzifier + tests
- [ ] MC#6: SpectralIndexCalculator (NDVI, NDWI, NDBI) + tests
- [ ] MC#7: PCA implementation + tests
- [ ] MC#8: ConfidenceMapGenerator + tests
- [ ] MC#9: Configuracion de metodos en JSON

## Acceptance Criteria

- [ ] Todas las MF types producen curvas correctas
- [ ] Clasificacion con producto vs min muestra diferencias medibles
- [ ] NDVI calculado correctamente para datos conocidos
- [ ] Confidence map se genera como raster valido
