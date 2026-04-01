using System.IO.Compression;
using System.Text;
using System.Text.Json;
using FuzzySat.Core.Persistence;
using FuzzySat.Core.Training;
using Microsoft.Extensions.Options;

namespace FuzzySat.Web.Services;

/// <summary>
/// File-based implementation of <see cref="IProjectRepository"/>.
/// Stores project artifacts as JSON, CSV, and compressed binary files
/// in a per-project subdirectory under the project storage path.
/// All writes are atomic (write to .tmp, then move).
/// </summary>
public sealed class FileProjectRepository : IProjectRepository
{
    private readonly string _projectDir;
    private readonly ILogger<FileProjectRepository> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public FileProjectRepository(IOptions<ProjectStorageOptions> options, ILogger<FileProjectRepository> logger)
    {
        _projectDir = Path.GetFullPath(options.Value.GetEffectivePath());
        _logger = logger;
    }

    // --- Training Regions ---

    public async Task SaveTrainingRegionsAsync(string projectName, IReadOnlyList<TrainingRegion> regions)
    {
        var path = ResolveDataPath(projectName, "training-regions.json");
        var json = JsonSerializer.Serialize(regions, JsonOptions);
        await WriteFileAtomicAsync(path, json);
    }

    public async Task<List<TrainingRegion>?> LoadTrainingRegionsAsync(string projectName)
    {
        var path = ResolveDataPath(projectName, "training-regions.json");
        return await ReadJsonAsync<List<TrainingRegion>>(path);
    }

    // --- Training Samples (CSV) ---

    public async Task SaveTrainingSamplesCsvAsync(string projectName, string csvContent)
    {
        var path = ResolveDataPath(projectName, "training-samples.csv");
        await WriteFileAtomicAsync(path, csvContent);
    }

    public async Task<string?> LoadTrainingSamplesCsvAsync(string projectName)
    {
        var path = ResolveDataPath(projectName, "training-samples.csv");
        return await ReadTextAsync(path);
    }

    // --- Training Session ---

    public async Task SaveTrainingSessionAsync(string projectName, TrainingSessionDto session)
    {
        var path = ResolveDataPath(projectName, "training-session.json");
        var json = JsonSerializer.Serialize(session, JsonOptions);
        await WriteFileAtomicAsync(path, json);
    }

    public async Task<TrainingSessionDto?> LoadTrainingSessionAsync(string projectName)
    {
        var path = ResolveDataPath(projectName, "training-session.json");
        return await ReadJsonAsync<TrainingSessionDto>(path);
    }

    // --- Classification Result (Binary + GZip) ---

    public async Task SaveClassificationResultAsync(
        string projectName,
        ClassificationResultDto metadata,
        string[,] classMap,
        double[,] confidenceMap)
    {
        var dir = EnsureDataDirectory(projectName);

        // Save metadata as JSON (atomic)
        var metaPath = Path.Combine(dir, "classification-meta.json");
        var metaJson = JsonSerializer.Serialize(metadata, JsonOptions);
        await WriteFileAtomicAsync(metaPath, metaJson);

        // Save pixel data as compressed binary (atomic via tmp)
        var dataPath = Path.Combine(dir, "classification-result.bin.gz");
        var tmpPath = dataPath + ".tmp";

        await using (var fileStream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write))
        await using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
        await using (var writer = new BinaryWriter(gzipStream, Encoding.UTF8, leaveOpen: false))
        {
            var rows = classMap.GetLength(0);
            var cols = classMap.GetLength(1);
            var classNames = metadata.ClassNames;

            if (classNames.Count > 255)
                throw new ArgumentException($"Cannot persist more than 255 classes (got {classNames.Count}).");

            var classIndex = new Dictionary<string, byte>(StringComparer.Ordinal);
            for (var i = 0; i < classNames.Count; i++)
                classIndex[classNames[i]] = (byte)i;

            writer.Write(rows);
            writer.Write(cols);

            for (var r = 0; r < rows; r++)
                for (var c = 0; c < cols; c++)
                {
                    var className = classMap[r, c];
                    var idx = classIndex.GetValueOrDefault(className, (byte)0);
                    writer.Write(idx);
                    writer.Write((float)confidenceMap[r, c]);
                }
        }

        File.Move(tmpPath, dataPath, overwrite: true);
    }

    public async Task<(ClassificationResultDto Metadata, string[,] ClassMap, double[,] ConfidenceMap)?>
        LoadClassificationResultAsync(string projectName)
    {
        var dir = ResolveDataDir(projectName);
        var metaPath = Path.Combine(dir, "classification-meta.json");
        var dataPath = Path.Combine(dir, "classification-result.bin.gz");

        if (!File.Exists(metaPath) || !File.Exists(dataPath))
            return null;

        try
        {
            var metaJson = await File.ReadAllTextAsync(metaPath);
            var metadata = JsonSerializer.Deserialize<ClassificationResultDto>(metaJson, JsonOptions);
            if (metadata is null || metadata.ClassNames.Count == 0)
            {
                _logger.LogWarning("Classification metadata for '{Project}' is null or has no class names", projectName);
                return null;
            }

            await using var fileStream = new FileStream(dataPath, FileMode.Open, FileAccess.Read);
            await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new BinaryReader(gzipStream, Encoding.UTF8, leaveOpen: false);

            var rows = reader.ReadInt32();
            var cols = reader.ReadInt32();
            var classNames = metadata.ClassNames;

            var classMap = new string[rows, cols];
            var confidenceMap = new double[rows, cols];
            var outOfRangeCount = 0;

            for (var r = 0; r < rows; r++)
                for (var c = 0; c < cols; c++)
                {
                    var idx = reader.ReadByte();
                    var confidence = reader.ReadSingle();
                    if (idx < classNames.Count)
                    {
                        classMap[r, c] = classNames[idx];
                    }
                    else
                    {
                        classMap[r, c] = classNames[0];
                        outOfRangeCount++;
                    }
                    confidenceMap[r, c] = confidence;
                }

            if (outOfRangeCount > 0)
                _logger.LogWarning("Classification data for '{Project}' had {Count} pixels with out-of-range class index",
                    projectName, outOfRangeCount);

            return (metadata, classMap, confidenceMap);
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException)
        {
            _logger.LogWarning(ex, "Failed to load classification result for '{Project}'", projectName);
            return null;
        }
    }

    // --- Classification Options ---

    public async Task SaveClassificationOptionsAsync(string projectName, ClassificationOptionsDto options)
    {
        var path = ResolveDataPath(projectName, "classification-options.json");
        var json = JsonSerializer.Serialize(options, JsonOptions);
        await WriteFileAtomicAsync(path, json);
    }

    public async Task<ClassificationOptionsDto?> LoadClassificationOptionsAsync(string projectName)
    {
        var path = ResolveDataPath(projectName, "classification-options.json");
        return await ReadJsonAsync<ClassificationOptionsDto>(path);
    }

    // --- Validation Result ---

    public async Task SaveValidationResultAsync(string projectName, ValidationResultDto result)
    {
        var path = ResolveDataPath(projectName, "validation-result.json");
        var json = JsonSerializer.Serialize(result, JsonOptions);
        await WriteFileAtomicAsync(path, json);
    }

    public async Task<ValidationResultDto?> LoadValidationResultAsync(string projectName)
    {
        var path = ResolveDataPath(projectName, "validation-result.json");
        return await ReadJsonAsync<ValidationResultDto>(path);
    }

    // --- Explore State (band selection + view mode) ---

    public async Task SaveExploreStateAsync(string projectName, ExploreStateDto state)
    {
        var path = ResolveDataPath(projectName, "explore-state.json");
        var json = JsonSerializer.Serialize(state, JsonOptions);
        await WriteFileAtomicAsync(path, json);
    }

    public async Task<ExploreStateDto?> LoadExploreStateAsync(string projectName)
    {
        var path = ResolveDataPath(projectName, "explore-state.json");
        return await ReadJsonAsync<ExploreStateDto>(path);
    }

    // --- Delete ---

    /// <summary>Deletes a specific artifact file for the given project.</summary>
    public Task DeleteArtifactAsync(string projectName, string fileName)
    {
        var path = ResolveDataPath(projectName, fileName);
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to delete '{Path}'", path);
        }
        return Task.CompletedTask;
    }

    // --- Utility ---

    public Task<bool> HasPersistedDataAsync(string projectName)
    {
        try
        {
            var dir = ResolveDataDir(projectName);
            var exists = Directory.Exists(dir) &&
                         Directory.EnumerateFiles(dir).Any();
            return Task.FromResult(exists);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to check persisted data for '{Project}'", projectName);
            return Task.FromResult(false);
        }
    }

    // --- Private Helpers ---

    private string ResolveDataDir(string projectName)
    {
        ValidateProjectName(projectName);
        var dir = Path.GetFullPath(Path.Combine(_projectDir, projectName));
        // Containment check: resolved path must be under _projectDir
        var relative = Path.GetRelativePath(_projectDir, dir);
        if (relative.StartsWith("..", StringComparison.Ordinal))
            throw new ArgumentException("Invalid project name: path traversal detected.", nameof(projectName));
        return dir;
    }

    private string ResolveDataPath(string projectName, string fileName)
    {
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Contains(".."))
            throw new ArgumentException("Invalid artifact file name.", nameof(fileName));

        var dir = ResolveDataDir(projectName);
        return Path.Combine(dir, fileName);
    }

    private string EnsureDataDirectory(string projectName)
    {
        var dir = ResolveDataDir(projectName);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Writes content to a temporary file, then atomically moves it to the target path.
    /// This prevents data corruption if the process crashes mid-write.
    /// </summary>
    private static async Task WriteFileAtomicAsync(string path, string content)
    {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, content);
        File.Move(tmpPath, path, overwrite: true);
    }

    private async Task<T?> ReadJsonAsync<T>(string path) where T : class
    {
        if (!File.Exists(path))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to load '{Path}'", path);
            return null;
        }
    }

    private async Task<string?> ReadTextAsync(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            return await File.ReadAllTextAsync(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to read '{Path}'", path);
            return null;
        }
    }

    private static void ValidateProjectName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException(
                "Invalid project name: contains invalid filename characters.", nameof(name));
        }
    }
}
