using FuzzySat.Core.Training;

namespace FuzzySat.Core.Persistence;

/// <summary>
/// Defines the contract for persisting and loading project artifacts
/// (training data, classification results, validation metrics).
/// </summary>
public interface IProjectRepository
{
    /// <summary>Saves training regions drawn by the user.</summary>
    Task SaveTrainingRegionsAsync(string projectName, IReadOnlyList<TrainingRegion> regions);

    /// <summary>Loads training regions, or null if none exist.</summary>
    Task<List<TrainingRegion>?> LoadTrainingRegionsAsync(string projectName);

    /// <summary>Saves training samples as CSV content.</summary>
    Task SaveTrainingSamplesCsvAsync(string projectName, string csvContent);

    /// <summary>Loads training samples CSV, or null if none exist.</summary>
    Task<string?> LoadTrainingSamplesCsvAsync(string projectName);

    /// <summary>Saves a training session.</summary>
    Task SaveTrainingSessionAsync(string projectName, TrainingSessionDto session);

    /// <summary>Loads a training session, or null if none exists.</summary>
    Task<TrainingSessionDto?> LoadTrainingSessionAsync(string projectName);

    /// <summary>Saves classification result (metadata + pixel data).</summary>
    Task SaveClassificationResultAsync(
        string projectName,
        ClassificationResultDto metadata,
        string[,] classMap,
        double[,] confidenceMap);

    /// <summary>Loads classification result, or null if none exists.</summary>
    Task<(ClassificationResultDto Metadata, string[,] ClassMap, double[,] ConfidenceMap)?>
        LoadClassificationResultAsync(string projectName);

    /// <summary>Saves classification options used.</summary>
    Task SaveClassificationOptionsAsync(string projectName, ClassificationOptionsDto options);

    /// <summary>Loads classification options, or null if none exist.</summary>
    Task<ClassificationOptionsDto?> LoadClassificationOptionsAsync(string projectName);

    /// <summary>Saves validation result (confusion matrix + metrics).</summary>
    Task SaveValidationResultAsync(string projectName, ValidationResultDto result);

    /// <summary>Loads validation result, or null if none exists.</summary>
    Task<ValidationResultDto?> LoadValidationResultAsync(string projectName);

    /// <summary>Checks whether any persisted data exists for this project.</summary>
    Task<bool> HasPersistedDataAsync(string projectName);
}
