namespace FuzzySat.Web.Services;

/// <summary>
/// Options for project file storage location.
/// When BasePath is empty, defaults to ApplicationData/FuzzySat/projects.
/// </summary>
public sealed class ProjectStorageOptions
{
    public string BasePath { get; set; } = "";

    /// <summary>
    /// Resolves the effective storage path, using ApplicationData as default.
    /// </summary>
    public string GetEffectivePath() =>
        string.IsNullOrWhiteSpace(BasePath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FuzzySat", "projects")
            : BasePath;
}
