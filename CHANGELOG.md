# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Fuzzy Logic Engine**: Gaussian, Triangular, Trapezoidal, and Generalized
  Bell membership functions with full operator support (AND=Min, AND=Product,
  OR=Max, NOT)
- **Inference Pipeline**: FuzzyInferenceEngine with MaxWeight and
  WeightedAverage defuzzifiers
- **Training System**: TrainingDataExtractor and TrainingSession for computing
  per-class/band statistics and building FuzzyRuleSets
- **Raster I/O**: GdalRasterReader/Writer for GeoTIFF with full geospatial
  metadata support
- **Spectral Indices**: NDVI, NDWI, NDBI derived band calculation
- **Validation**: ConfusionMatrix with Overall Accuracy, Cohen's Kappa, and
  per-class producer's/user's accuracy
- **Hybrid ML Pipeline**: ML.NET integration with FuzzyFeatureExtractor,
  HybridClassifier (Random Forest + SDCA), and KMeansClusterer
- **CLI**: 5 commands (classify, train, validate, info, visualize) with Spectre.Console
  rich output
- **Blazor Web App**: 6-page wizard flow with Leaflet maps, band viewer,
  training area drawing, and classification pipeline
- **Test Suite**: 484 tests (xUnit + FluentAssertions) validating core
  algorithms against thesis reference values
- **CI/CD**: GitHub Actions pipeline with build, test, and automated code
  review

[Unreleased]: https://github.com/ivanrlg/FuzzySat/commits/main
