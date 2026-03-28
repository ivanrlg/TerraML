# Epic #1 - Core Engine MVP

**Status**: Planificado
**Priority**: P0-CRITICAL
**Depends on**: Scaffolding completado
**Estimated effort**: TBD

---

## Problem

FuzzySat necesita un motor de logica difusa completo que implemente fielmente el algoritmo
de la tesis original. Sin este motor, ningun otro componente (CLI, API, Web) puede funcionar.

## Solution

Implementar el pipeline completo de clasificacion fuzzy en `FuzzySat.Core`:
1. Membership functions (Gaussian como base, interfaz para extensibilidad)
2. Fuzzy rules y rule set
3. Inference engine con AND operator (minimum)
4. Max Weight defuzzifier
5. Training data extractor (mean + stddev per band per class)
6. Confusion matrix y Kappa statistic
7. Unit tests validados contra datos de la tesis

## Micro-Commits Planificados

- [ ] MC#1: Crear solution (.sln) y proyectos (.csproj) con NuGet packages
- [ ] MC#2: IMembershipFunction interface + GaussianMembershipFunction + tests
- [ ] MC#3: TriangularMembershipFunction + TrapezoidalMembershipFunction + tests
- [ ] MC#4: FuzzyOperators (And=Min, Or=Max) + tests
- [ ] MC#5: FuzzyRule + FuzzyRuleSet + tests
- [ ] MC#6: InferenceEngine + tests
- [ ] MC#7: IDefuzzifier + MaxWeightDefuzzifier + tests
- [ ] MC#8: IClassifier + FuzzyClassifier (orchestrator) + tests
- [ ] MC#9: SpectralStatistics + TrainingDataExtractor + tests
- [ ] MC#10: TrainingSession + JSON serialization + tests
- [ ] MC#11: ConfusionMatrix + tests con datos de la tesis
- [ ] MC#12: KappaStatistic + AccuracyMetrics + ValidationReport + tests
- [ ] MC#13: Band + MultispectralImage + PixelVector models
- [ ] MC#14: ClassificationResult + LandCoverClass models
- [ ] MC#15: ClassifierConfiguration + BandConfiguration + ProjectSettings

## Acceptance Criteria

- [ ] Todos los tests pasan
- [ ] GaussianMembershipFunction produce valores correctos para datos de la tesis
- [ ] MaxWeightDefuzzifier clasifica correctamente pixeles de ejemplo
- [ ] ConfusionMatrix reproduce metricas publicadas en la tesis (81.87% OA, K=0.7637)
- [ ] Kappa statistic es correcto para matrices conocidas
- [ ] Coverage >80% en Core
- [ ] `dotnet build` exitoso sin warnings

## Key Technical Decisions

- **No external fuzzy library**: El motor se implementa from scratch para fidelidad al algoritmo
- **Gaussian MF como default**: Otras MFs (triangular, trapezoidal) son extensiones futuras
- **AND = Min (no producto)**: El minimo requiere que el pixel satisfaga TODAS las bandas
- **Max Weight defuzzification**: Elimina dependencia del orden de clases

## Reference

- Tesis original, Capitulos 4-6
- Tabla de parametros de entrenamiento (7 clases, 4 bandas, 28 MFs)
- Benchmark: 81.87% OA vs Maximum Likelihood 74.27%
