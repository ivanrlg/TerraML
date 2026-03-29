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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ProjectLoaderService(IOptions<ProjectStorageOptions> options)
    {
        _projectDir = options.Value.GetEffectivePath();
    }

    /// <summary>
    /// Lists all saved project names (without .json extension).
    /// </summary>
    public IReadOnlyList<string> ListProjects()
    {
        if (!Directory.Exists(_projectDir))
            return [];

        return Directory.GetFiles(_projectDir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => n)
            .ToList()!;
    }

    /// <summary>
    /// Loads a project configuration by name.
    /// </summary>
    public ClassifierConfiguration? LoadProject(string name)
    {
        var filePath = Path.Combine(_projectDir, $"{name}.json");
        if (!File.Exists(filePath)) return null;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ClassifierConfiguration>(json, JsonOptions);
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
