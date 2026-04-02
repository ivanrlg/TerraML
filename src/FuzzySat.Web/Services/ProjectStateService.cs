using FuzzySat.Core.Classification;
using FuzzySat.Core.Configuration;
using FuzzySat.Core.Raster;
using FuzzySat.Core.Training;
using FuzzySat.Core.Validation;

namespace FuzzySat.Web.Services;

/// <summary>
/// Persisted band selection state for the Explore &amp; Train page.
/// </summary>
public sealed record ExploreBandSelection(
    int SelectedBandIndex,
    int RedBandIndex,
    int GreenBandIndex,
    int BlueBandIndex);

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
    private string? _exploreViewMode;
    private ExploreBandSelection? _exploreBands;
    private List<TrainingRegion>? _trainingRegions;
    private List<LabeledPixelSample>? _trainingSamples;
    private MultispectralImage? _cachedImage;
    private Dictionary<string, string>? _classColors;
    private ClassificationOptions? _classificationOptions;

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

    /// <summary>View mode for the Explore &amp; Train page ("Single" or "RGB").</summary>
    public string? ExploreViewMode
    {
        get => _exploreViewMode;
        set { _exploreViewMode = value; NotifyChanged(); }
    }

    /// <summary>Selected band indices for the Explore &amp; Train page.</summary>
    public ExploreBandSelection? ExploreBands
    {
        get => _exploreBands;
        set { _exploreBands = value; NotifyChanged(); }
    }

    /// <summary>Drawn training regions preserved across navigation.</summary>
    public List<TrainingRegion>? TrainingRegions
    {
        get => _trainingRegions;
        set { _trainingRegions = value; NotifyChanged(); }
    }

    /// <summary>Extracted pixel samples preserved across navigation.</summary>
    public List<LabeledPixelSample>? TrainingSamples
    {
        get => _trainingSamples;
        set { _trainingSamples = value; NotifyChanged(); }
    }

    /// <summary>User-customized class colors (class name → hex string). Null = auto-assign.</summary>
    public Dictionary<string, string>? ClassColors
    {
        get => _classColors;
        set { _classColors = value; NotifyChanged(); }
    }

    /// <summary>Classification options used in the last run (MF type, AND op, defuzzifier, method).</summary>
    public ClassificationOptions? ClassificationOptions
    {
        get => _classificationOptions;
        set { _classificationOptions = value; NotifyChanged(); }
    }

    /// <summary>Cached in-memory raster to avoid re-reading from disk on navigation.</summary>
    public MultispectralImage? CachedImage
    {
        get => _cachedImage;
        set { _cachedImage = value; NotifyChanged(); }
    }

    /// <summary>Whether a project is loaded with a valid configuration.</summary>
    public bool HasProject => _configuration is not null;

    /// <summary>Whether raster info has been loaded for the current project.</summary>
    public bool HasRasterInfo => _rasterInfo is not null;

    /// <summary>Whether a training session exists.</summary>
    public bool HasTrainingSession => _trainingSession is not null;

    /// <summary>Whether a classification has been run.</summary>
    public bool HasClassificationResult => _classificationResult is not null;

    /// <summary>
    /// Renames a land cover class across all state: configuration, training samples,
    /// training regions, training session statistics, and class colors.
    /// Throws if the old name doesn't exist or the new name is invalid/duplicate.
    /// </summary>
    public void RenameClass(string oldName, string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldName);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        if (oldName == newName)
            return;

        var config = _configuration;
        if (config?.Classes is null)
            throw new InvalidOperationException("No configuration loaded.");

        var classList = config.Classes.ToList();
        var classIndex = classList.FindIndex(c => c.Name == oldName);
        if (classIndex < 0)
            throw new ArgumentException($"Class '{oldName}' not found.", nameof(oldName));

        if (config.Classes.Any(c => c.Name == newName))
            throw new ArgumentException($"Class '{newName}' already exists.", nameof(newName));

        BeginBatch();
        try
        {
            // 1. Update configuration classes
            var old = classList[classIndex];
            classList[classIndex] = new LandCoverClass { Name = newName, Code = old.Code, Color = old.Color };
            _configuration = new ClassifierConfiguration
            {
                ProjectName = config.ProjectName,
                Bands = config.Bands,
                Classes = classList,
                TrainingDataPath = config.TrainingDataPath,
                InputRasterPath = config.InputRasterPath,
                OutputRasterPath = config.OutputRasterPath,
                ImportFolderPath = config.ImportFolderPath,
                InputMode = config.InputMode
            };

            // 2. Update training samples
            if (_trainingSamples is { Count: > 0 })
            {
                _trainingSamples = _trainingSamples
                    .Select(s => s.ClassName == oldName ? s with { ClassName = newName } : s)
                    .ToList();
            }

            // 3. Update training regions
            if (_trainingRegions is { Count: > 0 })
            {
                _trainingRegions = _trainingRegions
                    .Select(r => r.ClassName == oldName ? r with { ClassName = newName } : r)
                    .ToList();
            }

            // 4. Update training session (immutable — must rebuild)
            if (_trainingSession is not null && _trainingSession.Statistics.ContainsKey(oldName))
            {
                var newStats = new Dictionary<string, SpectralStatistics>();
                foreach (var (key, value) in _trainingSession.Statistics)
                {
                    if (key == oldName)
                    {
                        newStats[newName] = new SpectralStatistics(newName,
                            value.MeanPerBand.ToDictionary(b => b.Key, b => b.Value),
                            value.StdDevPerBand.ToDictionary(b => b.Key, b => b.Value));
                    }
                    else
                    {
                        newStats[key] = value;
                    }
                }

                var newClassNames = _trainingSession.ClassNames
                    .Select(n => n == oldName ? newName : n).ToList();

                _trainingSession = TrainingSession.CreateFromStatistics(
                    newStats, newClassNames, _trainingSession.BandNames,
                    _trainingSession.Id, _trainingSession.CreatedAt);
            }

            // 5. Update class colors
            if (_classColors is not null && _classColors.Remove(oldName, out var color))
            {
                _classColors[newName] = color;
                _classColors = new Dictionary<string, string>(_classColors);
            }

            // 6. Invalidate stale classification/validation results
            // These contain class names in _classMap arrays and _classIndex dictionaries
            // that would become inconsistent. Safer to invalidate than re-map in-place.
            _classificationResult = null;
            _confusionMatrix = null;
        }
        finally
        {
            EndBatch();
        }
    }

    /// <summary>Resets all state (new project).</summary>
    public void Reset()
    {
        _configuration = null;
        _rasterInfo = null;
        _trainingSession = null;
        _classificationResult = null;
        _confusionMatrix = null;
        _importedRasterPath = null;
        _exploreViewMode = null;
        _exploreBands = null;
        _trainingRegions = null;
        _trainingSamples = null;
        _cachedImage = null;
        _classColors = null;
        _classificationOptions = null;
        NotifyChanged();
    }

    private int _batchCount;

    /// <summary>
    /// Suppresses OnStateChanged notifications until EndBatch is called.
    /// Use for bulk updates that would otherwise fire multiple events.
    /// </summary>
    public void BeginBatch() => _batchCount++;

    /// <summary>
    /// Ends a batch update and fires a single OnStateChanged notification.
    /// </summary>
    public void EndBatch()
    {
        if (_batchCount > 0)
            _batchCount--;
        if (_batchCount == 0)
            OnStateChanged?.Invoke();
    }

    private void NotifyChanged()
    {
        if (_batchCount == 0)
            OnStateChanged?.Invoke();
    }
}
