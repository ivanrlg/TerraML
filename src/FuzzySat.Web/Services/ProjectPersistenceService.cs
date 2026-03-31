using System.Text;
using FuzzySat.Core.Classification;
using FuzzySat.Core.Configuration;
using FuzzySat.Core.Persistence;
using FuzzySat.Core.Training;
using FuzzySat.Core.Validation;

namespace FuzzySat.Web.Services;

/// <summary>
/// Scoped service that auto-saves project state to disk whenever it changes.
/// Subscribes to <see cref="ProjectStateService.OnStateChanged"/> and persists
/// only the artifacts that actually changed, with a debounce to avoid excessive writes.
/// </summary>
public sealed class ProjectPersistenceService : IDisposable
{
    private readonly ProjectStateService _state;
    private readonly IProjectRepository _repo;
    private readonly TrainingService _training;
    private readonly ILogger<ProjectPersistenceService> _logger;

    private CancellationTokenSource? _debounceCts;
    private readonly object _lock = new();
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

    // Track previous references to detect changes
    private List<TrainingRegion>? _lastRegions;
    private List<LabeledPixelSample>? _lastSamples;
    private TrainingSession? _lastSession;
    private ClassificationResult? _lastClassResult;
    private ConfusionMatrix? _lastConfusionMatrix;
    private ClassificationOptions? _lastClassOptions;
    private ExploreBandSelection? _lastExploreBands;
    private string? _lastExploreViewMode;

    // Thread-safe accumulative dirty flags (0=clean, 1=dirty)
    private int _dirtyRegions, _dirtySamples, _dirtySession;
    private int _dirtyClassResult, _dirtyConfusion, _dirtyOptions;
    private int _dirtyExploreBands, _dirtyExploreView;

    private const int DebounceMs = 500;

    public ProjectPersistenceService(
        ProjectStateService state,
        IProjectRepository repo,
        TrainingService training,
        ILogger<ProjectPersistenceService> logger)
    {
        _state = state;
        _repo = repo;
        _training = training;
        _logger = logger;

        _state.OnStateChanged += OnStateChanged;
    }

    private void OnStateChanged()
    {
        var projectName = _state.Configuration?.ProjectName;
        if (string.IsNullOrWhiteSpace(projectName)) return;

        // Detect what changed and set dirty flags atomically
        if (!ReferenceEquals(_lastRegions, _state.TrainingRegions))
            Interlocked.Exchange(ref _dirtyRegions, 1);
        if (!ReferenceEquals(_lastSamples, _state.TrainingSamples))
            Interlocked.Exchange(ref _dirtySamples, 1);
        if (!ReferenceEquals(_lastSession, _state.TrainingSession))
            Interlocked.Exchange(ref _dirtySession, 1);
        if (!ReferenceEquals(_lastClassResult, _state.ClassificationResult))
            Interlocked.Exchange(ref _dirtyClassResult, 1);
        if (!ReferenceEquals(_lastConfusionMatrix, _state.ConfusionMatrix))
            Interlocked.Exchange(ref _dirtyConfusion, 1);
        if (!ReferenceEquals(_lastClassOptions, _state.ClassificationOptions))
            Interlocked.Exchange(ref _dirtyOptions, 1);
        if (!ReferenceEquals(_lastExploreBands, _state.ExploreBands))
            Interlocked.Exchange(ref _dirtyExploreBands, 1);
        if (_lastExploreViewMode != _state.ExploreViewMode)
            Interlocked.Exchange(ref _dirtyExploreView, 1);

        // Check if anything is dirty
        if (Volatile.Read(ref _dirtyRegions) == 0 && Volatile.Read(ref _dirtySamples) == 0 &&
            Volatile.Read(ref _dirtySession) == 0 && Volatile.Read(ref _dirtyClassResult) == 0 &&
            Volatile.Read(ref _dirtyConfusion) == 0 && Volatile.Read(ref _dirtyOptions) == 0 &&
            Volatile.Read(ref _dirtyExploreBands) == 0 && Volatile.Read(ref _dirtyExploreView) == 0)
            return;

        // Update last references
        _lastRegions = _state.TrainingRegions;
        _lastSamples = _state.TrainingSamples;
        _lastSession = _state.TrainingSession;
        _lastClassResult = _state.ClassificationResult;
        _lastConfusionMatrix = _state.ConfusionMatrix;
        _lastClassOptions = _state.ClassificationOptions;
        _lastExploreBands = _state.ExploreBands;
        _lastExploreViewMode = _state.ExploreViewMode;

        // Snapshot current state for the save task
        var regions = _state.TrainingRegions;
        var samples = _state.TrainingSamples;
        var session = _state.TrainingSession;
        var classResult = _state.ClassificationResult;
        var confusion = _state.ConfusionMatrix;
        var options = _state.ClassificationOptions;
        var config = _state.Configuration;
        var exploreBands = _state.ExploreBands;
        var exploreViewMode = _state.ExploreViewMode;

        // Debounce: cancel previous pending save, schedule new one
        CancellationTokenSource cts;
        lock (_lock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = cts = new CancellationTokenSource();
        }

        var ct = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceMs, ct);

                // Serialize writes to prevent concurrent file operations
                await _writeSemaphore.WaitAsync(ct);
                try
                {
                    await SaveArtifactsAsync(projectName, config,
                        regions, samples, session, classResult,
                        confusion, options, exploreBands, exploreViewMode);
                }
                finally
                {
                    _writeSemaphore.Release();
                }

                _logger.LogDebug("Auto-saved project '{Project}' artifacts", projectName);
            }
            catch (TaskCanceledException)
            {
                // Debounce cancelled — a newer save is pending
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto-save failed for project '{Project}'", projectName);
            }
        }, CancellationToken.None);
    }

    private async Task SaveArtifactsAsync(
        string projectName,
        ClassifierConfiguration? config,
        List<TrainingRegion>? regions,
        List<LabeledPixelSample>? samples,
        TrainingSession? session,
        ClassificationResult? classResult,
        ConfusionMatrix? confusion,
        ClassificationOptions? options,
        ExploreBandSelection? exploreBands,
        string? exploreViewMode)
    {
        // Read and clear dirty flags atomically
        var saveRegions = Interlocked.Exchange(ref _dirtyRegions, 0) == 1;
        var saveSamples = Interlocked.Exchange(ref _dirtySamples, 0) == 1;
        var saveSession = Interlocked.Exchange(ref _dirtySession, 0) == 1;
        var saveClassResult = Interlocked.Exchange(ref _dirtyClassResult, 0) == 1;
        var saveConfusion = Interlocked.Exchange(ref _dirtyConfusion, 0) == 1;
        var saveOptions = Interlocked.Exchange(ref _dirtyOptions, 0) == 1;
        var saveExploreBands = Interlocked.Exchange(ref _dirtyExploreBands, 0) == 1;
        var saveExploreView = Interlocked.Exchange(ref _dirtyExploreView, 0) == 1;

        if (saveRegions)
        {
            if (regions is { Count: > 0 })
                await _repo.SaveTrainingRegionsAsync(projectName, regions);
            else
                await _repo.DeleteArtifactAsync(projectName, "training-regions.json");
        }

        if (saveSamples)
        {
            if (samples is { Count: > 0 } && config?.Bands is not null)
            {
                var bandNames = config.Bands.Select(b => b.Name).ToList();
                var csv = _training.ExportSamplesCsv(samples, bandNames);
                await _repo.SaveTrainingSamplesCsvAsync(projectName, csv);
            }
            else
            {
                await _repo.DeleteArtifactAsync(projectName, "training-samples.csv");
            }
        }

        if (saveSession)
        {
            if (session is not null)
            {
                var dto = TrainingSessionDto.FromSession(session);
                await _repo.SaveTrainingSessionAsync(projectName, dto);
            }
            else
            {
                await _repo.DeleteArtifactAsync(projectName, "training-session.json");
            }
        }

        if (saveOptions)
        {
            if (options is not null)
            {
                var dto = new ClassificationOptionsDto
                {
                    MembershipFunctionType = options.MembershipFunctionType,
                    AndOperator = options.AndOperator,
                    DefuzzifierType = options.DefuzzifierType,
                    ClassificationMethod = options.ClassificationMethod
                };
                await _repo.SaveClassificationOptionsAsync(projectName, dto);
            }
            else
            {
                await _repo.DeleteArtifactAsync(projectName, "classification-options.json");
            }
        }

        if (saveClassResult)
        {
            if (classResult is not null)
            {
                var metadata = new ClassificationResultDto
                {
                    Rows = classResult.Rows,
                    Columns = classResult.Columns,
                    ClassNames = classResult.Classes.Select(c => c.Name).ToList(),
                    ClassCodes = classResult.Classes.Select(c => c.Code).ToList(),
                    ClassColors = classResult.Classes.Select(c => c.Color).ToList()
                };

                var classMap = new string[classResult.Rows, classResult.Columns];
                var confidenceMap = new double[classResult.Rows, classResult.Columns];
                for (var r = 0; r < classResult.Rows; r++)
                    for (var c = 0; c < classResult.Columns; c++)
                    {
                        classMap[r, c] = classResult.GetClass(r, c);
                        confidenceMap[r, c] = classResult.GetConfidence(r, c);
                    }

                await _repo.SaveClassificationResultAsync(projectName, metadata, classMap, confidenceMap);
                _logger.LogInformation("ClassificationResult saved: {Rows}x{Cols}", classResult.Rows, classResult.Columns);
            }
            else
            {
                await _repo.DeleteArtifactAsync(projectName, "classification-meta.json");
                await _repo.DeleteArtifactAsync(projectName, "classification-result.bin.gz");
            }
        }

        if (saveConfusion)
        {
            if (confusion is not null)
            {
                var n = confusion.ClassNames.Count;
                var matrix = new int[n][];
                for (var i = 0; i < n; i++)
                {
                    matrix[i] = new int[n];
                    for (var j = 0; j < n; j++)
                        matrix[i][j] = confusion[confusion.ClassNames[i], confusion.ClassNames[j]];
                }

                var dto = new ValidationResultDto
                {
                    ClassNames = confusion.ClassNames.ToList(),
                    Matrix = matrix,
                    OverallAccuracy = confusion.OverallAccuracy,
                    KappaCoefficient = confusion.KappaCoefficient,
                    TotalSamples = confusion.TotalSamples,
                    CorrectCount = confusion.CorrectCount,
                    PerClassMetrics = confusion.ClassNames.Select(cn => new ClassMetricDto
                    {
                        ClassName = cn,
                        ProducersAccuracy = confusion.ProducersAccuracy(cn),
                        UsersAccuracy = confusion.UsersAccuracy(cn),
                        ActualCount = confusion.RowTotal(cn),
                        PredictedCount = confusion.ColumnTotal(cn)
                    }).ToList()
                };
                await _repo.SaveValidationResultAsync(projectName, dto);
            }
            else
            {
                await _repo.DeleteArtifactAsync(projectName, "validation-result.json");
            }
        }

        if (saveExploreBands || saveExploreView)
        {
            var exploreState = new ExploreStateDto
            {
                ViewMode = exploreViewMode,
                SelectedBandIndex = exploreBands?.SelectedBandIndex,
                RedBandIndex = exploreBands?.RedBandIndex,
                GreenBandIndex = exploreBands?.GreenBandIndex,
                BlueBandIndex = exploreBands?.BlueBandIndex
            };
            await _repo.SaveExploreStateAsync(projectName, exploreState);
        }
    }

    /// <summary>
    /// Restores all persisted artifacts into <see cref="ProjectStateService"/>.
    /// Called when a project is loaded. Resets state first to prevent cross-project contamination.
    /// </summary>
    public async Task RestoreProjectStateAsync(string projectName)
    {
        _state.BeginBatch();
        try
        {
            // Clear all prior project artifacts before restoring
            _state.TrainingRegions = null;
            _state.TrainingSamples = null;
            _state.TrainingSession = null;
            _state.ClassificationResult = null;
            _state.ConfusionMatrix = null;
            _state.ClassificationOptions = null;
            _state.CachedImage = null;
            _state.ExploreBands = null;
            _state.ExploreViewMode = null;

            var hasData = await _repo.HasPersistedDataAsync(projectName);
            if (!hasData) return;

            // Training regions
            var regions = await _repo.LoadTrainingRegionsAsync(projectName);
            if (regions is not null)
                _state.TrainingRegions = regions;

            // Training samples
            var csv = await _repo.LoadTrainingSamplesCsvAsync(projectName);
            if (csv is not null)
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
                var (samples, _, _) = _training.LoadSamplesFromCsv(stream);
                if (samples.Count > 0)
                    _state.TrainingSamples = samples;
            }

            // Training session
            var sessionDto = await _repo.LoadTrainingSessionAsync(projectName);
            if (sessionDto is not null)
                _state.TrainingSession = sessionDto.ToSession();

            // Classification options
            var optionsDto = await _repo.LoadClassificationOptionsAsync(projectName);
            if (optionsDto is not null)
                _state.ClassificationOptions = new ClassificationOptions(
                    optionsDto.ClassificationMethod,
                    optionsDto.MembershipFunctionType,
                    optionsDto.AndOperator,
                    optionsDto.DefuzzifierType);

            // Classification result
            var classData = await _repo.LoadClassificationResultAsync(projectName);
            if (classData is not null)
            {
                var (meta, classMap, confidenceMap) = classData.Value;
                var classes = new List<LandCoverClass>();
                for (var i = 0; i < meta.ClassNames.Count; i++)
                {
                    classes.Add(new LandCoverClass
                    {
                        Name = meta.ClassNames[i],
                        Code = i < meta.ClassCodes.Count ? meta.ClassCodes[i] : i + 1,
                        Color = i < meta.ClassColors.Count ? meta.ClassColors[i] : null
                    });
                }
                _state.ClassificationResult = new ClassificationResult(classMap, confidenceMap, classes);
            }

            // Validation result
            var validation = await _repo.LoadValidationResultAsync(projectName);
            if (validation is not null)
                _state.ConfusionMatrix = ConfusionMatrix.FromPersistedData(
                    validation.ClassNames, validation.ToMatrix());

            // Explore band selection + view mode
            var exploreState = await _repo.LoadExploreStateAsync(projectName);
            if (exploreState is not null)
            {
                _state.ExploreViewMode = exploreState.ViewMode;
                if (exploreState.SelectedBandIndex is not null)
                    _state.ExploreBands = new ExploreBandSelection(
                        exploreState.SelectedBandIndex.Value,
                        exploreState.RedBandIndex ?? 2,
                        exploreState.GreenBandIndex ?? 1,
                        exploreState.BlueBandIndex ?? 0);
            }

            _logger.LogInformation("Restored persisted state for project '{Project}'", projectName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore some artifacts for project '{Project}'", projectName);
        }
        finally
        {
            // Always sync references — even on partial failure — to prevent
            // auto-save from overwriting complete data with partial data
            SyncTrackingReferences();
            _state.EndBatch();
        }
    }

    /// <summary>
    /// Auto-loads the last used project on app startup.
    /// Called from MainLayout to ensure it runs regardless of which page the user lands on.
    /// </summary>
    public async Task AutoLoadLastProjectAsync(ProjectLoaderService loader)
    {
        if (_state.HasProject) return;

        var lastProject = loader.GetLastProject();
        if (lastProject is null) return;

        var config = loader.LoadProject(lastProject);
        if (config is null) return;

        _state.Configuration = config;
        await RestoreProjectStateAsync(lastProject);
        _logger.LogInformation("Auto-loaded last project '{Project}'", lastProject);
    }

    /// <summary>Sync tracking references so auto-save doesn't re-save what was just loaded.</summary>
    private void SyncTrackingReferences()
    {
        _lastRegions = _state.TrainingRegions;
        _lastSamples = _state.TrainingSamples;
        _lastSession = _state.TrainingSession;
        _lastClassResult = _state.ClassificationResult;
        _lastConfusionMatrix = _state.ConfusionMatrix;
        _lastClassOptions = _state.ClassificationOptions;
        _lastExploreBands = _state.ExploreBands;
        _lastExploreViewMode = _state.ExploreViewMode;

        // Clear all dirty flags
        Interlocked.Exchange(ref _dirtyRegions, 0);
        Interlocked.Exchange(ref _dirtySamples, 0);
        Interlocked.Exchange(ref _dirtySession, 0);
        Interlocked.Exchange(ref _dirtyClassResult, 0);
        Interlocked.Exchange(ref _dirtyConfusion, 0);
        Interlocked.Exchange(ref _dirtyOptions, 0);
        Interlocked.Exchange(ref _dirtyExploreBands, 0);
        Interlocked.Exchange(ref _dirtyExploreView, 0);
    }

    public void Dispose()
    {
        _state.OnStateChanged -= OnStateChanged;
        lock (_lock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
        _writeSemaphore.Dispose();
    }
}
