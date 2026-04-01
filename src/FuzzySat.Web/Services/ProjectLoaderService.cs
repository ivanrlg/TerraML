using System.Text.Json;
using FuzzySat.Core.Configuration;
using FuzzySat.Core.Persistence;
using Microsoft.Extensions.Options;

namespace FuzzySat.Web.Services;

/// <summary>
/// Manages project configuration persistence (save, load, list).
/// All path operations are guarded against path traversal attacks.
/// </summary>
public sealed class ProjectLoaderService
{
    private readonly string _projectDir;
    private readonly ILogger<ProjectLoaderService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ProjectLoaderService(IOptions<ProjectStorageOptions> options, ILogger<ProjectLoaderService> logger)
    {
        _projectDir = Path.GetFullPath(options.Value.GetEffectivePath());
        _logger = logger;
    }

    /// <summary>
    /// Lists all saved project names (without .json extension).
    /// Returns empty list on I/O or permission errors.
    /// </summary>
    public IReadOnlyList<string> ListProjects()
    {
        try
        {
            if (!Directory.Exists(_projectDir))
                return [];

            return Directory.GetFiles(_projectDir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => !string.IsNullOrEmpty(n))
                .OrderBy(n => n)
                .ToList()!;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to list projects in '{Dir}'", _projectDir);
            return [];
        }
    }

    /// <summary>
    /// Loads a project configuration by name.
    /// Returns null on missing file, corrupt JSON, or I/O errors.
    /// Throws ArgumentException if name attempts path traversal.
    /// </summary>
    public ClassifierConfiguration? LoadProject(string name)
    {
        var filePath = ResolveSafePath(name);

        if (!File.Exists(filePath)) return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ClassifierConfiguration>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to load project '{Name}' from '{Path}'", name, filePath);
            return null;
        }
    }

    /// <summary>
    /// Saves a project configuration with the given safe filename.
    /// Throws ArgumentException if safeName attempts path traversal.
    /// </summary>
    public async Task SaveProjectAsync(string safeName, ClassifierConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var filePath = ResolveSafePath(safeName);

        Directory.CreateDirectory(_projectDir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Saves the name of the last active project for auto-load on next visit.
    /// </summary>
    public async Task SaveLastProjectAsync(string projectName)
    {
        try
        {
            Directory.CreateDirectory(_projectDir);
            await File.WriteAllTextAsync(Path.Combine(_projectDir, ".last-project"), projectName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to save last project marker");
        }
    }

    /// <summary>
    /// Gets the name of the last active project, or null if none.
    /// </summary>
    public string? GetLastProject()
    {
        try
        {
            var path = Path.Combine(_projectDir, ".last-project");
            if (!File.Exists(path)) return null;
            var name = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to read last project marker");
            return null;
        }
    }

    /// <summary>
    /// Returns lightweight summaries for all persisted projects.
    /// Reads each project's config JSON and checks for artifact files to determine status.
    /// Skips corrupt or unreadable projects.
    /// </summary>
    public IReadOnlyList<ProjectSummary> GetProjectSummaries()
    {
        var names = ListProjects();
        var summaries = new List<ProjectSummary>(names.Count);

        foreach (var name in names)
        {
            try
            {
                var config = LoadProject(name);
                if (config is null) continue;

                var dataDir = Path.Combine(_projectDir, name);
                var status = DetermineStatus(dataDir);
                var lastModified = GetLastModified(name, dataDir);

                ClassificationOptionsDto? classOpts = null;
                ValidationResultDto? validation = null;

                if (status >= ProjectStatus.Classified)
                    classOpts = ReadJson<ClassificationOptionsDto>(Path.Combine(dataDir, "classification-options.json"));

                if (status >= ProjectStatus.Validated)
                    validation = ReadJson<ValidationResultDto>(Path.Combine(dataDir, "validation-result.json"));

                summaries.Add(new ProjectSummary
                {
                    Name = config.ProjectName,
                    BandCount = config.Bands.Count,
                    ClassCount = config.Classes.Count,
                    ClassificationMethod = classOpts?.ClassificationMethod,
                    OverallAccuracy = validation?.OverallAccuracy,
                    KappaCoefficient = validation?.KappaCoefficient,
                    LastModified = lastModified,
                    Status = status
                });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.LogWarning(ex, "Skipping project '{Name}' due to read error", name);
            }
        }

        return summaries;
    }

    /// <summary>
    /// Deletes a project configuration file and its data directory.
    /// Throws ArgumentException if name attempts path traversal.
    /// </summary>
    public void DeleteProject(string name)
    {
        var configPath = ResolveSafePath(name);

        if (File.Exists(configPath))
            File.Delete(configPath);

        var dataDir = Path.Combine(_projectDir, name);
        var resolvedDataDir = Path.GetFullPath(dataDir);
        var relative = Path.GetRelativePath(_projectDir, resolvedDataDir);
        if (relative.StartsWith("..", StringComparison.Ordinal))
            throw new ArgumentException("Invalid project name: path traversal detected.", nameof(name));

        if (Directory.Exists(resolvedDataDir))
            Directory.Delete(resolvedDataDir, true);
    }

    private static ProjectStatus DetermineStatus(string dataDir)
    {
        if (!Directory.Exists(dataDir))
            return ProjectStatus.Configured;

        if (File.Exists(Path.Combine(dataDir, "validation-result.json")))
            return ProjectStatus.Validated;

        if (File.Exists(Path.Combine(dataDir, "classification-meta.json")))
            return ProjectStatus.Classified;

        if (File.Exists(Path.Combine(dataDir, "training-session.json")))
            return ProjectStatus.Trained;

        return ProjectStatus.Configured;
    }

    private DateTime GetLastModified(string name, string dataDir)
    {
        var configPath = Path.Combine(_projectDir, $"{name}.json");
        var configTime = File.Exists(configPath) ? File.GetLastWriteTimeUtc(configPath) : DateTime.MinValue;

        if (!Directory.Exists(dataDir))
            return configTime;

        try
        {
            var artifactTime = Directory.EnumerateFiles(dataDir)
                .Select(f => File.GetLastWriteTimeUtc(f))
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            return artifactTime > configTime ? artifactTime : configTime;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return configTime;
        }
    }

    private static T? ReadJson<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a project name to a file path within the project directory.
    /// Throws if the resolved path escapes the project directory (path traversal).
    /// </summary>
    private string ResolveSafePath(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        // Reject directory separators to prevent nested paths
        if (name.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
            (Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar &&
             name.IndexOf(Path.AltDirectorySeparatorChar) >= 0))
        {
            throw new ArgumentException("Invalid project name: directory separators are not allowed.", nameof(name));
        }

        var filePath = Path.GetFullPath(Path.Combine(_projectDir, $"{name}.json"));

        // Use GetRelativePath for OS-appropriate path containment check
        var relativePath = Path.GetRelativePath(_projectDir, filePath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal))
            throw new ArgumentException("Invalid project name: path traversal detected.", nameof(name));

        return filePath;
    }
}
