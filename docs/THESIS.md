# Original Thesis

> **Desarrollo de un Clasificador de Imagenes Satelitales Basado en Logica Difusa**
> *(Development of a Satellite Image Classifier Based on Fuzzy Logic)*

---

## Overview

| Field | Detail |
|:---|:---|
| **Author** | Ivan Ramon Jose Labrador Gonzalez |
| **Advisor** | Prof. Alexander Parra Uzcategui |
| **Co-advisor** | Prof. Ernesto A. Ponsot R. |
| **Institution** | Universidad de Los Andes |
| **Department** | Investigacion de Operaciones |
| **Location** | Merida, Venezuela |
| **Date** | November 2008 |
| **Type** | Bachelor's Thesis (Proyecto de Grado) |
| **Pages** | 105 |

---

## Abstract

The thesis developed a hard classifier for multispectral satellite images using fuzzy logic,
based on spectral pattern recognition. The classifier was built as a Fuzzy Inference System
where each pixel is assigned to a land cover class based on its spectral similarity across
multiple bands. The implementation was done in MATLAB using its Fuzzy Logic Toolbox.

The system was designed for the Institute of Photogrammetry at Universidad de Los Andes,
using ASTER sensor imagery (aboard NASA's Terra satellite) of the city of Merida and
its surroundings as a case study.

---

## Methodology

### Fuzzy Inference System Design

The classifier follows a four-stage pipeline:

1. **Input**: Multi-band spectral values from ASTER imagery (4 bands: VNIR1, VNIR2, SWIR1, SWIR2)
2. **Fuzzification**: Each pixel's spectral value is evaluated through Gaussian membership
   functions (one per land cover class per band), where the mean and standard deviation
   are derived from training areas selected by the user
3. **Inference**: Fuzzy AND (minimum operator) across all bands per class produces a
   firing strength for each class
4. **Defuzzification**: The class with the highest firing strength is assigned to the pixel
   (max-weight method)

### Land Cover Classes

Seven classes were defined for the Merida study area:
- Laguna (Water bodies)
- Nucleos Urbanos (Urban areas)
- Matorral (Shrubland)
- Suelo Descubierto (Bare soil)
- Pastizales (Grassland)
- Bosque Denso (Dense forest)
- Bosque Medio (Medium forest)

### Validation

Classification quality was assessed using confusion matrices, Overall Accuracy
(Fiabilidad Global), and Cohen's Kappa coefficient. A stratified random sample of
171 ground-truth points was used for validation against a reference image.

---

## Results

The fuzzy logic classifier was compared against three traditional classifiers available
in the IDRISI GIS software, all evaluated on the same ASTER imagery and ground-truth sample:

| Classifier | Overall Accuracy | Kappa |
|:---|:---:|:---:|
| **Fuzzy Logic** (thesis) | **81.87%** | **0.7637** |
| Maximum Likelihood | 74.27% | 0.6650 |
| Decision Tree (CART) | 63.74% | 0.5312 |
| Minimum Distance | 56.14% | 0.4233 |

> **Note**: These results were obtained on a specific ASTER image of Merida, Venezuela,
> with 7 land cover classes and 4 spectral bands. Performance on other imagery, regions,
> or class configurations may differ. The thesis also noted that Maximum Likelihood
> showed the greatest similarity to the fuzzy classifier (89.39% agreement between the two).

---

## How Terra ML Extends the Original Work

Terra ML preserves the core fuzzy inference algorithm from the thesis and extends it
significantly:

| Aspect | Original Thesis (2008) | Terra ML (2026) |
|:---|:---|:---|
| **Platform** | MATLAB + IDRISI (proprietary) | C# / .NET 10 + GDAL (open source) |
| **Imagery** | ASTER (4 bands, 15m) | Any GeoTIFF including Sentinel-2 (13 bands, 10m) |
| **MF Types** | Gaussian only | Gaussian, Triangular, Trapezoidal, Generalized Bell |
| **ML Integration** | None | ML.NET Random Forest + SDCA using fuzzy features |
| **Classification Modes** | Fuzzy logic only | Fuzzy, Hybrid (fuzzy+ML), Pure ML |
| **Interface** | MATLAB GUI | Blazor Web App + CLI + REST API |
| **License** | Academic only | MIT (open source) |
| **Sharing** | Not possible | GitHub, anyone can use it |

---

## Citation

```bibtex
@thesis{labrador2008fuzzy,
  title      = {Desarrollo de un Clasificador de Imagenes Satelitales
                Basado en Logica Difusa},
  author     = {Labrador Gonzalez, Ivan Ramon Jose},
  year       = {2008},
  month      = {November},
  school     = {Universidad de Los Andes},
  address    = {Merida, Venezuela},
  type       = {Bachelor's Thesis},
  department = {Investigacion de Operaciones},
  pages      = {105}
}
```

---

<p align="center">
  <em>
    Terra ML is inspired by this thesis but is an independent open-source project.<br/>
    The original thesis results are specific to the ASTER dataset and study area used.
  </em>
</p>
