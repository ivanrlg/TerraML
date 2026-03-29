using System.CommandLine;
using System.Text.Json;
using FuzzySat.Core.Classification;
using FuzzySat.Core.FuzzyLogic.Defuzzification;
using FuzzySat.Core.FuzzyLogic.Inference;
using FuzzySat.Core.Raster;
using FuzzySat.Core.Training;
using Spectre.Console;

namespace FuzzySat.CLI.Commands;

/// <summary>
/// CLI command: fuzzysat classify — classify a raster image using a trained fuzzy model.
/// </summary>
public static class ClassifyCommand
{
    public static Command Create()
    {
        var inputOption = new Option<string>("--input", "-i") { Description = "Path to the input raster image", Required = true };
        var modelOption = new Option<string>("--model", "-m") { Description = "Path to the training session JSON", Required = true };
        var outputOption = new Option<string>("--output", "-o") { Description = "Path for the output classified raster", Required = true };

        var command = new Command("classify", "Classify a raster image using a trained fuzzy model");
        command.Options.Add(inputOption);
        command.Options.Add(modelOption);
        command.Options.Add(outputOption);

        command.SetAction(parseResult =>
        {
            var inputPath = parseResult.GetValue(inputOption)!;
            var modelPath = parseResult.GetValue(modelOption)!;
            var outputPath = parseResult.GetValue(outputOption)!;

            AnsiConsole.MarkupLine("[bold blue]FuzzySat Classify[/]");
            AnsiConsole.MarkupLine($"  Input:  [green]{Markup.Escape(inputPath)}[/]");
            AnsiConsole.MarkupLine($"  Model:  [green]{Markup.Escape(modelPath)}[/]");
            AnsiConsole.MarkupLine($"  Output: [green]{Markup.Escape(outputPath)}[/]");
            AnsiConsole.WriteLine();

            try
            {
                // Validate files exist
                if (!File.Exists(inputPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Input raster not found: {Markup.Escape(inputPath)}");
                    return 1;
                }
                if (!File.Exists(modelPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Training session not found: {Markup.Escape(modelPath)}");
                    return 1;
                }

                // Load training session via shared DTO
                var sessionJson = File.ReadAllText(modelPath);
                var dto = JsonSerializer.Deserialize<TrainingSessionDto>(sessionJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("Failed to deserialize training session.");
                var session = dto.ToSession();

                var ruleSet = session.BuildRuleSet();

                // Build land cover classes with sequential codes
                var classes = session.ClassNames
                    .Select((name, idx) => new LandCoverClass { Name = name, Code = idx + 1 })
                    .ToList();

                // Read raster
                AnsiConsole.MarkupLine("[dim]Reading raster...[/]");
                var reader = new GdalRasterReader();
                var sourceInfo = reader.ReadInfo(inputPath);
                var image = reader.Read(inputPath, session.BandNames.ToList());

                // Classify with progress
                var engine = new FuzzyInferenceEngine(ruleSet);
                var defuzzifier = new MaxWeightDefuzzifier();

                var totalPixels = (long)image.Rows * image.Columns;
                AnsiConsole.MarkupLine($"  Classifying {totalPixels:N0} pixels ({image.Rows}x{image.Columns}, {session.BandNames.Count} bands)...");

                var result = AnsiConsole.Progress()
                    .AutoClear(true)
                    .Columns(
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn())
                    .Start(ctx =>
                    {
                        var task = ctx.AddTask("Classifying", maxValue: image.Rows);
                        var classMap = new string[image.Rows, image.Columns];
                        var confidenceMap = new double[image.Rows, image.Columns];

                        for (var row = 0; row < image.Rows; row++)
                        {
                            for (var col = 0; col < image.Columns; col++)
                            {
                                var pixel = image.GetPixelVector(row, col);
                                var inferenceResult = engine.Infer((IDictionary<string, double>)pixel.BandValues);
                                classMap[row, col] = defuzzifier.Defuzzify(inferenceResult);
                                confidenceMap[row, col] = inferenceResult.WinnerStrength;
                            }
                            task.Increment(1);
                        }

                        return new ClassificationResult(classMap, confidenceMap, classes);
                    });

                // Write output
                AnsiConsole.MarkupLine("[dim]Writing classified raster...[/]");
                var writer = new GdalRasterWriter();
                writer.Write(outputPath, result, sourceInfo);

                // Summary
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]Classification complete![/]");

                var classCounts = new Dictionary<string, int>();
                for (var row = 0; row < result.Rows; row++)
                    for (var col = 0; col < result.Columns; col++)
                    {
                        var cls = result.GetClass(row, col);
                        classCounts[cls] = classCounts.GetValueOrDefault(cls) + 1;
                    }

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("[bold]Class[/]")
                    .AddColumn("[bold]Pixels[/]")
                    .AddColumn("[bold]%[/]");

                foreach (var (cls, count) in classCounts.OrderByDescending(x => x.Value))
                {
                    var pct = 100.0 * count / totalPixels;
                    table.AddRow(Markup.Escape(cls), count.ToString("N0"), pct.ToString("F1") + "%");
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
