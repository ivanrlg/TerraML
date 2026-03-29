using System.Text.Json;
using FuzzySat.Core.Configuration;
using Microsoft.Extensions.Options;

namespace FuzzySat.Web.Services;

/// <summary>
/// Manages project configuration persistence (save, load, list).
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
        _projectDir = options.Value.GetEffectivePath();
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
    /// </summary>
    public ClassifierConfiguration? LoadProject(string name)
    {
        var filePath = Path.Combine(_projectDir, $"{name}.json");
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
    /// </summary>
    public async Task SaveProjectAsync(string safeName, ClassifierConfiguration config)
    {
        Directory.CreateDirectory(_projectDir);
        var filePath = Path.Combine(_projectDir, $"{safeName}.json");
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }
}
