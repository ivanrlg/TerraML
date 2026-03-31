# Epic #8 — Diseño Técnico

---

## 1. Arquitectura

### Capas de Responsabilidad

```
FuzzySat.Core/Persistence/       <- Interfaces + DTOs (sin dependencias Web)
  IProjectRepository.cs
  ClassificationResultDto.cs
  ClassificationOptionsDto.cs
  ValidationResultDto.cs

FuzzySat.Core/Training/          <- DTOs existentes (reutilizados)
  TrainingRegion.cs              <- Movido desde Web
  TrainingSessionDto.cs          <- Ya existe

FuzzySat.Web/Services/           <- Implementaciones concretas
  FileProjectRepository.cs      <- IProjectRepository con file I/O
  ProjectPersistenceService.cs   <- Auto-save orquestador
```

### Principio: Core no depende de Web
- `IProjectRepository` en Core define el contrato
- `FileProjectRepository` en Web implementa con file system
- DTOs en Core son POCOs sin dependencias de I/O

---

## 2. IProjectRepository — Contrato

```csharp
public interface IProjectRepository
{
    // Training regions
    Task SaveTrainingRegionsAsync(string projectName, IReadOnlyList<TrainingRegion> regions);
    Task<List<TrainingRegion>?> LoadTrainingRegionsAsync(string projectName);

    // Training samples (CSV format for interoperability)
    Task SaveTrainingSamplesCsvAsync(string projectName, string csvContent);
    Task<string?> LoadTrainingSamplesCsvAsync(string projectName);

    // Training session
    Task SaveTrainingSessionAsync(string projectName, TrainingSessionDto session);
    Task<TrainingSessionDto?> LoadTrainingSessionAsync(string projectName);

    // Classification result (binary compressed)
    Task SaveClassificationResultAsync(string projectName,
        ClassificationResultDto metadata, string[,] classMap, double[,] confidenceMap);
    Task<(ClassificationResultDto Metadata, string[,] ClassMap, double[,] ConfidenceMap)?> 
        LoadClassificationResultAsync(string projectName);

    // Classification options
    Task SaveClassificationOptionsAsync(string projectName, ClassificationOptionsDto options);
    Task<ClassificationOptionsDto?> LoadClassificationOptionsAsync(string projectName);

    // Validation result
    Task SaveValidationResultAsync(string projectName, ValidationResultDto result);
    Task<ValidationResultDto?> LoadValidationResultAsync(string projectName);

    // Utility
    Task<bool> HasPersistedDataAsync(string projectName);
}
```

---

## 3. Formatos de Almacenamiento

### 3.1 Training Regions (JSON)
```json
[
  {
    "className": "Urban",
    "color": "#FF0000",
    "startRow": 100, "startCol": 200,
    "endRow": 150, "endCol": 250
  }
]
```

### 3.2 Training Samples (CSV)
Formato identico al export existente de `TrainingService.ExportSamplesCsv()`:
```csv
class,VNIR1,VNIR2,SWIR1
Urban,150.5,120.3,80.1
Water,30.2,25.8,15.5
```

### 3.3 Training Session (JSON)
Usa `TrainingSessionDto` existente — sin cambios.

### 3.4 Classification Result (Binary + GZip)

Formato binario custom para eficiencia:

```
Header:
  int32   rows
  int32   columns
  int32   classCount
  string[] classNames  (each: int32 length + UTF8 bytes)

Body (per pixel, row-major):
  byte    classIndex   (index into classNames, max 255 classes)
  float32 confidence   (firing strength, 4 bytes)

Total per pixel: 5 bytes
Compressed with GZip (spatial autocorrelation = excellent compression)
```

Ejemplo: imagen 5000x5000 = 25M pixels x 5 bytes = 125MB raw -> ~5-15MB compressed

### 3.5 Classification Options (JSON)
```json
{
  "membershipFunctionType": "Gaussian",
  "andOperator": "Minimum",
  "defuzzifierType": "MaxWeight",
  "classificationMethod": "PureFuzzy"
}
```

### 3.6 Validation Result (JSON)
```json
{
  "classNames": ["Urban", "Water", "Forest"],
  "matrix": [[45,2,3],[1,48,1],[2,1,47]],
  "overallAccuracy": 0.9333,
  "kappaCoefficient": 0.9000,
  "totalSamples": 150,
  "correctCount": 140,
  "perClassMetrics": [
    {"className":"Urban","producersAccuracy":0.90,"usersAccuracy":0.9375,"actualCount":50,"predictedCount":48}
  ]
}
```

---

## 4. Auto-Save (ProjectPersistenceService)

### Estrategia
- Servicio **scoped** (1 por circuit Blazor)
- Suscribe a `ProjectStateService.OnStateChanged`
- Compara **referencias** de objetos (no deep equality) para detectar que cambio
- **Debounce 500ms**: al recibir cambio, espera 500ms; si llega otro cambio, reinicia timer
- I/O ejecutado en `Task.Run()` para no bloquear UI
- `CancellationTokenSource` cancelado al cambiar proyecto

### Flujo
```
OnStateChanged fired
  -> Compare references: _lastRegions != state.TrainingRegions?
  -> If changed, mark dirty flag
  -> Reset debounce timer (500ms)
  -> Timer fires -> snapshot data on sync context
  -> Task.Run: save dirty artifacts to disk
  -> Clear dirty flags
```

### Inicializacion
Inyectado en `MainLayout.razor` para que se construya al inicio del circuit:
```razor
@inject ProjectPersistenceService Persistence
```

---

## 5. Restauracion de Estado

En `ProjectSetup.razor.LoadSelectedProject()`, despues de cargar `ClassifierConfiguration`:

```csharp
ProjectState.BeginBatch();
try
{
    ProjectState.Configuration = config;
    
    // Restore training regions
    var regions = await Repo.LoadTrainingRegionsAsync(projectName);
    if (regions is not null) ProjectState.TrainingRegions = regions;

    // Restore training samples
    var csv = await Repo.LoadTrainingSamplesCsvAsync(projectName);
    if (csv is not null)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var (samples, _, _) = TrainingService.LoadSamplesFromCsv(stream);
        ProjectState.TrainingSamples = samples;
    }

    // Restore training session
    var sessionDto = await Repo.LoadTrainingSessionAsync(projectName);
    if (sessionDto is not null) ProjectState.TrainingSession = sessionDto.ToSession();

    // Restore classification
    var classResult = await Repo.LoadClassificationResultAsync(projectName);
    if (classResult is not null) { /* reconstruct ClassificationResult */ }

    // Restore validation
    var validation = await Repo.LoadValidationResultAsync(projectName);
    if (validation is not null)
        ProjectState.ConfusionMatrix = ConfusionMatrix.FromPersistedData(
            validation.ClassNames, validation.ToMatrix());
}
finally { ProjectState.EndBatch(); }
```

---

## 6. Riesgos y Mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigacion |
|--------|-------------|---------|------------|
| Auto-save bloquea UI | Media | Alto | Debounce + Task.Run para I/O |
| ClassificationResult muy grande | Media | Medio | Binary+GZip, ~5-15MB para 5000x5000 |
| Cambio de proyecto durante save | Baja | Alto | CancellationToken cancelado al cambiar |
| Corrupcion de archivo | Baja | Alto | Try-catch + log warning, return null |
| Path traversal | Baja | Critico | Reutilizar ResolveSafePath de ProjectLoaderService |

---

## 7. Dependencias

- **Ninguna nueva**: Todo se implementa con `System.Text.Json`, `System.IO.Compression`, y APIs de .NET existentes
- Se reutiliza `ProjectStorageOptions` y la logica de path de `ProjectLoaderService`
- `TrainingSessionDto` ya existe y se reutiliza
