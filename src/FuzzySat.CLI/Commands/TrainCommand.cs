using System.CommandLine;
using System.Text.Json;
using FuzzySat.Core.Training;
using Spectre.Console;

namespace FuzzySat.CLI.Commands;

/// <summary>
/// CLI command: fuzzysat train — extract training statistics from labeled samples CSV.
/// CSV format: class,band1,band2,...,bandN (header row required).
/// </summary>
public static class TrainCommand
{
    public static Command Create()
    {
        var samplesOption = new Option<string>("--samples", "-s") { Description = "Path to training samples CSV (header: class,band1,band2,...)", Required = true };
        var outputOption = new Option<string>("--output", "-o") { Description = "Path for the output training session JSON", Required = true };

        var command = new Command("train", "Extract training statistics from labeled pixel samples");
        command.Options.Add(samplesOption);
        command.Options.Add(outputOption);

        command.SetAction(parseResult =>
        {
            var samplesPath = parseResult.GetValue(samplesOption)!;
            var outputPath = parseResult.GetValue(outputOption)!;

            AnsiConsole.MarkupLine("[bold blue]FuzzySat Train[/]");
            AnsiConsole.MarkupLine($"  Samples: [green]{Markup.Escape(samplesPath)}[/]");
            AnsiConsole.MarkupLine($"  Output:  [green]{Markup.Escape(outputPath)}[/]");
            AnsiConsole.WriteLine();

            try
            {
                if (!File.Exists(samplesPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Samples file not found: {Markup.Escape(samplesPath)}");
                    return 1;
                }

                // Parse CSV
                var lines = File.ReadAllLines(samplesPath);
                if (lines.Length < 2)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] CSV must have a header row and at least one data row.");
                    return 1;
                }

                var header = lines[0].Split(',').Select(h => h.Trim()).ToArray();
                if (header.Length < 2)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] CSV header must have at least 2 columns (class,band1,...).");
                    return 1;
                }

                if (!header[0].Equals("class", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] First CSV column must be 'class', got '{Markup.Escape(header[0])}'.");
                    return 1;
                }

                var bandNames = header[1..];

                // Validate band names are non-empty and unique
                var bandNameSet = new HashSet<string>(StringComparer.Ordinal);
                foreach (var band in bandNames)
                {
                    if (string.IsNullOrWhiteSpace(band))
                    {
                        AnsiConsole.MarkupLine("[red]Error:[/] CSV header contains empty band name.");
                        return 1;
                    }
                    if (!bandNameSet.Add(band))
                    {
                        AnsiConsole.MarkupLine($"[red]Error:[/] Duplicate band name in header: '{Markup.Escape(band)}'.");
                        return 1;
                    }
                }

                var samples = new List<LabeledPixelSample>();

                for (var i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var parts = line.Split(',');
                    if (parts.Length != header.Length)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning:[/] Skipping line {i + 1}: expected {header.Length} columns, got {parts.Length}.");
                        continue;
                    }

                    var className = parts[0].Trim();
                    var bandValues = new Dictionary<string, double>();
                    var valid = true;

                    for (var j = 1; j < parts.Length; j++)
                    {
                        if (double.TryParse(parts[j].Trim(), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var value))
                        {
                            bandValues[bandNames[j - 1].Trim()] = value;
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Skipping line {i + 1}: invalid number '{Markup.Escape(parts[j].Trim())}' in column '{Markup.Escape(bandNames[j - 1].Trim())}'.");
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                        samples.Add(new LabeledPixelSample { ClassName = className, BandValues = bandValues });
                }

                if (samples.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] No valid training samples found in CSV.");
                    return 1;
                }

                // Create training session
                var session = TrainingSession.CreateFromSamples(samples);

                // Serialize to JSON via shared DTO
                var dto = TrainingSessionDto.FromSession(session);
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });

                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir))
                    Directory.CreateDirectory(outputDir);
                File.WriteAllText(outputPath, json);

                // Display results
                AnsiConsole.MarkupLine($"[green]Training complete![/] {samples.Count} samples → {session.ClassNames.Count} classes");
                AnsiConsole.WriteLine();

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("[bold]Class[/]")
                    .AddColumn("[bold]Samples[/]");

                foreach (var band in session.BandNames)
                {
                    table.AddColumn($"[bold]Mean {Markup.Escape(band)}[/]");
                    table.AddColumn($"[bold]StdDev {Markup.Escape(band)}[/]");
                }

                foreach (var className in session.ClassNames)
                {
                    var stats = session.Statistics[className];
                    var sampleCount = samples.Count(s => s.ClassName == className);

                    var row = new List<string> { Markup.Escape(className), sampleCount.ToString() };
                    foreach (var band in session.BandNames)
                    {
                        row.Add(stats.MeanPerBand[band].ToString("F2"));
                        row.Add(stats.StdDevPerBand[band].ToString("F2"));
                    }

                    table.AddRow(row.ToArray());
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n  Saved to: [green]{Markup.Escape(outputPath)}[/]");
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
                return 1;
            }
        });

        return command;
    }
}
