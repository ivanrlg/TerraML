using System.Text;
using FuzzySat.Core.Classification;
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
    private readonly FileProjectRepository _fileRepo;
    private readonly TrainingService _training;
    private readonly ILogger<ProjectPersistenceService> _logger;

    private CancellationTokenSource? _debounceCts;
    private readonly object _lock = new();

    // Track previous references to detect changes
    private List<TrainingRegion>? _lastRegions;
    private List<LabeledPixelSample>? _lastSamples;
    private TrainingSession? _lastSession;
    private ClassificationResult? _lastClassResult;
    private ConfusionMatrix? _lastConfusionMatrix;
    private ClassificationOptions? _lastClassOptions;
    private ExploreBandSelection? _lastExploreBands;
    private string? _lastExploreViewMode;

    // Accumulative dirty flags — survive across debounce resets
    private bool _dirtyRegions, _dirtySamples, _dirtySession;
    private bool _dirtyClassResult, _dirtyConfusion, _dirtyOptions;
    private bool _dirtyExploreBands, _dirtyExploreView;

    private const int DebounceMs = 500;

    public ProjectPersistenceService(
        ProjectStateService state,
        IProjectRepository repo,
        TrainingService training,
        ILogger<ProjectPersistenceService> logger)
    {
        _state = state;
        _repo = repo;
        _fileRepo = repo as FileProjectRepository ?? throw new InvalidOperationException(
            "ProjectPersistenceService requires FileProjectRepository for artifact deletion.");
        _training = training;
        _logger = logger;

        _state.OnStateChanged += OnStateChanged;
    }

    private void OnStateChanged()
    {
        var projectName = _state.Configuration?.ProjectName;
        if (string.IsNullOrWhiteSpace(projectName)) return;

        // Detect what changed and accumulate dirty flags
        if (!ReferenceEquals(_lastRegions, _state.TrainingRegions)) _dirtyRegions = true;
        if (!ReferenceEquals(_lastSamples, _state.TrainingSamples)) _dirtySamples = true;
        if (!ReferenceEquals(_lastSession, _state.TrainingSession)) _dirtySession = true;
        if (!ReferenceEquals(_lastClassResult, _state.ClassificationResult)) _dirtyClassResult = true;
        if (!ReferenceEquals(_lastConfusionMatrix, _state.ConfusionMatrix)) _dirtyConfusion = true;
        if (!ReferenceEquals(_lastClassOptions, _state.ClassificationOptions)) _dirtyOptions = true;
        if (!ReferenceEquals(_lastExploreBands, _state.ExploreBands)) _dirtyExploreBands = true;
        if (_lastExploreViewMode != _state.ExploreViewMode) _dirtyExploreView = true;

        if (!_dirtyRegions && !_dirtySamples && !_dirtySession &&
            !_dirtyClassResult && !_dirtyConfusion && !_dirtyOptions &&
            !_dirtyExploreBands && !_dirtyExploreView)
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

        // Capture dirty flags for this save round
        var saveRegions = _dirtyRegions;
        var saveSamples = _dirtySamples;
        var saveSession = _dirtySession;
        var saveClassResult = _dirtyClassResult;
        var saveConfusion = _dirtyConfusion;
        var saveOptions = _dirtyOptions;
        var saveExploreBands = _dirtyExploreBands;
        var saveExploreView = _dirtyExploreView;

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

                // Training regions
                if (saveRegions)
                {
                    if (regions is { Count: > 0 })
                        await _repo.SaveTrainingRegionsAsync(projectName, regions);
                    else
                        _fileRepo.DeleteArtifact(projectName, "training-regions.json");
                }

                // Training samples
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
                        _fileRepo.DeleteArtifact(projectName, "training-samples.csv");
                    }
                }

                // Training session
                if (saveSession)
                {
                    if (session is not null)
                    {
                        var dto = TrainingSessionDto.FromSession(session);
                        await _repo.SaveTrainingSessionAsync(projectName, dto);
                    }
                    else
                    {
                        _fileRepo.DeleteArtifact(projectName, "training-session.json");
                    }
                }

                // Classification options
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
                        _fileRepo.DeleteArtifact(projectName, "classification-options.json");
                    }
                }

                // Classification result
                if (saveClassResult)
                {
                    _logger.LogInformation("Saving ClassificationResult: {IsNull}, Rows={Rows}",
                        classResult is null, classResult?.Rows ?? 0);
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
                        _fileRepo.DeleteArtifact(projectName, "classification-meta.json");
                        _fileRepo.DeleteArtifact(projectName, "classification-result.bin.gz");
                    }
                }

                // Validation (confusion matrix)
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
                        _fileRepo.DeleteArtifact(projectName, "validation-result.json");
                    }
                }

                // Explore band selection + view mode
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
                    await _fileRepo.SaveExploreStateAsync(projectName, exploreState);
                }

                // Clear dirty flags after successful save
                _dirtyRegions = _dirtySamples = _dirtySession = false;
                _dirtyClassResult = _dirtyConfusion = _dirtyOptions = false;
                _dirtyExploreBands = _dirtyExploreView = false;

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

    /// <summary>
    /// Restores all persisted artifacts into <see cref="ProjectStateService"/>.
    /// Called when a project is loaded. Resets state first to prevent cross-project contamination.
    /// </summary>
    public async Task RestoreProjectStateAsync(string projectName)
    {
        _state.BeginBatch();
        try
        {
            // BLOQ-1 fix: Clear all prior project artifacts before restoring
            _state.TrainingRegions = null;
            _state.TrainingSamples = null;
            _state.TrainingSession = null;
            _state.ClassificationResult = null;
            _state.ConfusionMatrix = null;
            _state.ClassificationOptions = null;
            _state.CachedImage = null;

            var hasData = await _repo.HasPersistedDataAsync(projectName);
            _logger.LogInformation("Restore '{Project}': hasPersistedData={HasData}", projectName, hasData);
            if (!hasData)
            {
                SyncTrackingReferences();
                return;
            }

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
            _logger.LogInformation("Restore classification for '{Project}': hasData={HasData}",
                projectName, classData is not null);
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
            var exploreState = await _fileRepo.LoadExploreStateAsync(projectName);
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

            SyncTrackingReferences();
            _logger.LogInformation("Restored persisted state for project '{Project}'", projectName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore some artifacts for project '{Project}'", projectName);
        }
        finally
        {
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
    }
}
