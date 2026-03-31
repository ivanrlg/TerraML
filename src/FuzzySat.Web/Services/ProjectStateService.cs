using FuzzySat.Core.Classification;
using FuzzySat.Core.Configuration;
using FuzzySat.Core.Raster;
using FuzzySat.Core.Training;
using FuzzySat.Core.Validation;

namespace FuzzySat.Web.Services;

/// <summary>
/// Scoped service holding the current project's state across pages.
/// Each Blazor Server circuit (browser tab) gets its own instance.
///
/// IMPORTANT: All property setters must be called from the Blazor
/// synchronization context (i.e., from component lifecycle methods
/// or event handlers, NOT from background threads). If you need to
/// update state from a background task, await the task first, then
/// assign the result on the calling context.
///
/// Consumers should implement IDisposable and unsubscribe from
/// OnStateChanged in Dispose() to avoid event leaks.
/// </summary>
public sealed class ProjectStateService
{
    private ClassifierConfiguration? _configuration;
    private RasterInfo? _rasterInfo;
    private TrainingSession? _trainingSession;
    private ClassificationResult? _classificationResult;
    private ConfusionMatrix? _confusionMatrix;
    private string? _importedRasterPath;

    /// <summary>
    /// Fired when any state property changes. Subscribers must call
    /// InvokeAsync(StateHasChanged) from Blazor components.
    /// Must only be invoked from the Blazor synchronization context.
    /// </summary>
    public event Action? OnStateChanged;

    public ClassifierConfiguration? Configuration
    {
        get => _configuration;
        set { _configuration = value; NotifyChanged(); }
    }

    public RasterInfo? RasterInfo
    {
        get => _rasterInfo;
        set { _rasterInfo = value; NotifyChanged(); }
    }

    public TrainingSession? TrainingSession
    {
        get => _trainingSession;
        set { _trainingSession = value; NotifyChanged(); }
    }

    public ClassificationResult? ClassificationResult
    {
        get => _classificationResult;
        set { _classificationResult = value; NotifyChanged(); }
    }

    public ConfusionMatrix? ConfusionMatrix
    {
        get => _confusionMatrix;
        set { _confusionMatrix = value; NotifyChanged(); }
    }

    /// <summary>
    /// Path to a raster imported via the Sentinel-2 Import tool.
    /// Consumed by ProjectSetup to pre-fill the input raster path.
    /// </summary>
    public string? ImportedRasterPath
    {
        get => _importedRasterPath;
        set { _importedRasterPath = value; NotifyChanged(); }
    }

    /// <summary>Whether a project is loaded with a valid configuration.</summary>
    public bool HasProject => _configuration is not null;

    /// <summary>Whether raster info has been loaded for the current project.</summary>
    public bool HasRasterInfo => _rasterInfo is not null;

    /// <summary>Whether a training session exists.</summary>
    public bool HasTrainingSession => _trainingSession is not null;

    /// <summary>Whether a classification has been run.</summary>
    public bool HasClassificationResult => _classificationResult is not null;

    /// <summary>Resets all state (new project).</summary>
    public void Reset()
    {
        _configuration = null;
        _rasterInfo = null;
        _trainingSession = null;
        _classificationResult = null;
        _confusionMatrix = null;
        _importedRasterPath = null;
        NotifyChanged();
    }

    private void NotifyChanged() => OnStateChanged?.Invoke();
}
