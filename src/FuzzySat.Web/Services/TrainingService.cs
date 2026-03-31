using System.Globalization;
using System.Text.Json;
using FuzzySat.Core.Training;

namespace FuzzySat.Web.Services;

/// <summary>
/// Service for training operations: CSV parsing, session creation, JSON export.
/// </summary>
public sealed class TrainingService
{
    /// <summary>
    /// Parses training samples from a CSV stream.
    /// Format: class,band1,band2,...,bandN (header row required, first column must be "class").
    /// </summary>
    public (List<LabeledPixelSample> Samples, List<string> BandNames, List<string> Warnings) LoadSamplesFromCsv(Stream csvStream)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        using var reader = new StreamReader(csvStream);
        var content = reader.ReadToEnd();
        var lines = content.Split('\n', StringSplitOptions.None);

        if (lines.Length < 2)
            throw new InvalidOperationException("CSV must have a header row and at least one data row.");

        var header = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        if (header.Length < 2)
            throw new InvalidOperationException("CSV header must have at least 2 columns (class,band1,...).");

        if (!header[0].Equals("class", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"First CSV column must be 'class', got '{header[0]}'.");

        var bandNames = header[1..].ToList();

        // Validate band names: non-empty and unique (match CLI validation)
        if (bandNames.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("CSV header contains empty band name.");

        var duplicates = bandNames.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException($"Duplicate band name(s) in header: {string.Join(", ", duplicates)}.");

        var samples = new List<LabeledPixelSample>();
        var warnings = new List<string>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split(',');
            if (parts.Length != header.Length)
            {
                warnings.Add($"Line {i + 1}: expected {header.Length} columns, got {parts.Length}. Skipped.");
                continue;
            }

            var className = parts[0].Trim();
            var bandValues = new Dictionary<string, double>();
            var valid = true;

            for (var j = 1; j < parts.Length; j++)
            {
                if (double.TryParse(parts[j].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    bandValues[bandNames[j - 1]] = value;
                }
                else
                {
                    warnings.Add($"Line {i + 1}: invalid number '{parts[j].Trim()}' in column '{bandNames[j - 1]}'. Skipped.");
                    valid = false;
                    break;
                }
            }

            if (valid)
                samples.Add(new LabeledPixelSample { ClassName = className, BandValues = bandValues });
        }

        return (samples, bandNames, warnings);
    }

    /// <summary>
    /// Creates a TrainingSession from labeled samples.
    /// </summary>
    public TrainingSession CreateSession(IEnumerable<LabeledPixelSample> samples)
        => TrainingSession.CreateFromSamples(samples);

    /// <summary>
    /// Exports labeled samples to CSV format (class,band1,band2,...).
    /// Round-trip compatible with LoadSamplesFromCsv.
    /// </summary>
    public string ExportSamplesCsv(IReadOnlyList<LabeledPixelSample> samples, IReadOnlyList<string> bandNames)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(bandNames);

        var sb = new System.Text.StringBuilder();
        sb.Append("class");
        foreach (var band in bandNames)
            sb.Append(',').Append(band);
        sb.AppendLine();

        foreach (var sample in samples)
        {
            sb.Append(ValidationService.CsvEscape(sample.ClassName));
            foreach (var band in bandNames)
            {
                var value = sample.BandValues.GetValueOrDefault(band, 0);
                sb.Append(',').Append(value.ToString(CultureInfo.InvariantCulture));
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports a TrainingSession to JSON string.
    /// </summary>
    public string ExportSessionJson(TrainingSession session)
    {
        var dto = TrainingSessionDto.FromSession(session);
        return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
    }
}
