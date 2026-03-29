using System.Text.Json;
using FuzzySat.Core.Configuration;
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
        var filePath = ResolveSafePath(safeName);

        Directory.CreateDirectory(_projectDir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Resolves a project name to a file path within the project directory.
    /// Throws if the resolved path escapes the project directory (path traversal).
    /// </summary>
    private string ResolveSafePath(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        var filePath = Path.GetFullPath(Path.Combine(_projectDir, $"{name}.json"));
        var normalizedDir = _projectDir.EndsWith(Path.DirectorySeparatorChar)
            ? _projectDir
            : _projectDir + Path.DirectorySeparatorChar;

        if (!filePath.StartsWith(normalizedDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid project name: path traversal detected.", nameof(name));

        return filePath;
    }
}
