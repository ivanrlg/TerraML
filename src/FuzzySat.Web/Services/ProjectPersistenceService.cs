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

        // Detect what changed by comparing references
        var regionsChanged = !ReferenceEquals(_lastRegions, _state.TrainingRegions);
        var samplesChanged = !ReferenceEquals(_lastSamples, _state.TrainingSamples);
        var sessionChanged = !ReferenceEquals(_lastSession, _state.TrainingSession);
        var classResultChanged = !ReferenceEquals(_lastClassResult, _state.ClassificationResult);
        var confusionChanged = !ReferenceEquals(_lastConfusionMatrix, _state.ConfusionMatrix);

        if (!regionsChanged && !samplesChanged && !sessionChanged &&
            !classResultChanged && !confusionChanged)
            return;

        // Snapshot current references
        var regions = _state.TrainingRegions;
        var samples = _state.TrainingSamples;
        var session = _state.TrainingSession;
        var classResult = _state.ClassificationResult;
        var confusion = _state.ConfusionMatrix;
        var config = _state.Configuration;

        // Update last references
        _lastRegions = regions;
        _lastSamples = samples;
        _lastSession = session;
        _lastClassResult = classResult;
        _lastConfusionMatrix = confusion;

        // Debounce: cancel previous pending save, schedule new one
        lock (_lock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
        }

        var ct = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceMs, ct);

                if (regionsChanged && regions is not null)
                    await _repo.SaveTrainingRegionsAsync(projectName, regions);

                if (samplesChanged && samples is { Count: > 0 } && config?.Bands is not null)
                {
                    var bandNames = config.Bands.Select(b => b.Name).ToList();
                    var csv = _training.ExportSamplesCsv(samples, bandNames);
                    await _repo.SaveTrainingSamplesCsvAsync(projectName, csv);
                }

                if (sessionChanged && session is not null)
                {
                    var dto = TrainingSessionDto.FromSession(session);
                    await _repo.SaveTrainingSessionAsync(projectName, dto);
                }

                if (classResultChanged && classResult is not null)
                {
                    var metadata = new ClassificationResultDto
                    {
                        Rows = classResult.Rows,
                        Columns = classResult.Columns,
                        ClassNames = classResult.Classes.Select(c => c.Name).ToList(),
                        ClassCodes = classResult.Classes.Select(c => c.Code).ToList(),
                        ClassColors = classResult.Classes.Select(c => c.Color).ToList()
                    };

                    // Extract maps from ClassificationResult
                    var classMap = new string[classResult.Rows, classResult.Columns];
                    var confidenceMap = new double[classResult.Rows, classResult.Columns];
                    for (var r = 0; r < classResult.Rows; r++)
                        for (var c = 0; c < classResult.Columns; c++)
                        {
                            classMap[r, c] = classResult.GetClass(r, c);
                            confidenceMap[r, c] = classResult.GetConfidence(r, c);
                        }

                    await _repo.SaveClassificationResultAsync(projectName, metadata, classMap, confidenceMap);
                }

                if (confusionChanged && confusion is not null)
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
    /// Called when a project is loaded.
    /// </summary>
    public async Task RestoreProjectStateAsync(string projectName)
    {
        if (!await _repo.HasPersistedDataAsync(projectName))
            return;

        _state.BeginBatch();
        try
        {
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

            // Sync tracking references so auto-save doesn't re-save what we just loaded
            _lastRegions = _state.TrainingRegions;
            _lastSamples = _state.TrainingSamples;
            _lastSession = _state.TrainingSession;
            _lastClassResult = _state.ClassificationResult;
            _lastConfusionMatrix = _state.ConfusionMatrix;

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
