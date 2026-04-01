using System.CommandLine;
using FuzzySat.Core.Raster;
using FuzzySat.Core.Visualization;
using Spectre.Console;

namespace FuzzySat.CLI.Commands;

/// <summary>
/// CLI command: fuzzysat visualize — render a false color composite PNG from a raster image.
/// </summary>
public static class VisualizeCommand
{
    public static Command Create()
    {
        var inputArg = new Argument<string>("input") { Description = "Path to the input raster image" };
        var redOption = new Option<int>("--red", "-r") { Description = "Band index for the red channel (1-based)", Required = true };
        var greenOption = new Option<int>("--green", "-g") { Description = "Band index for the green channel (1-based)", Required = true };
        var blueOption = new Option<int>("--blue", "-b") { Description = "Band index for the blue channel (1-based)", Required = true };
        var outputOption = new Option<string>("--output", "-o") { Description = "Output PNG file path", Required = true };
        var widthOption = new Option<int>("--width") { Description = "Maximum output width in pixels", DefaultValueFactory = _ => 1024 };
        var heightOption = new Option<int>("--height") { Description = "Maximum output height in pixels", DefaultValueFactory = _ => 768 };

        var command = new Command("visualize", "Render a false color composite PNG from a raster image");
        command.Arguments.Add(inputArg);
        command.Options.Add(redOption);
        command.Options.Add(greenOption);
        command.Options.Add(blueOption);
        command.Options.Add(outputOption);
        command.Options.Add(widthOption);
        command.Options.Add(heightOption);

        command.SetAction(parseResult =>
        {
            var inputPath = parseResult.GetValue(inputArg)!;
            var red = parseResult.GetValue(redOption);
            var green = parseResult.GetValue(greenOption);
            var blue = parseResult.GetValue(blueOption);
            var outputPath = parseResult.GetValue(outputOption)!;
            var maxWidth = parseResult.GetValue(widthOption);
            var maxHeight = parseResult.GetValue(heightOption);

            AnsiConsole.MarkupLine("[bold blue]FuzzySat Visualize[/]");
            AnsiConsole.MarkupLine($"  Input:  [green]{Markup.Escape(inputPath)}[/]");
            AnsiConsole.MarkupLine($"  Bands:  R={red}, G={green}, B={blue}");
            AnsiConsole.MarkupLine($"  Output: [green]{Markup.Escape(outputPath)}[/]");
            AnsiConsole.WriteLine();

            try
            {
                if (!File.Exists(inputPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Input raster not found: {Markup.Escape(inputPath)}");
                    return 1;
                }

                if (maxWidth <= 0 || maxHeight <= 0)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] --width and --height must be positive integers greater than 0.");
                    return 1;
                }

                // Load only the 3 requested bands (single dataset open via ReadBands)
                AnsiConsole.MarkupLine("[dim]Reading raster bands...[/]");
                var reader = new GdalRasterReader();
                var bands = reader.ReadBands(inputPath, [red, green, blue]);
                var redBand = bands[0];
                var greenBand = bands[1];
                var blueBand = bands[2];

                // Compute statistics
                AnsiConsole.MarkupLine("[dim]Computing band statistics...[/]");
                var redStats = BandStatisticsCalculator.Compute(redBand);
                var greenStats = BandStatisticsCalculator.Compute(greenBand);
                var blueStats = BandStatisticsCalculator.Compute(blueBand);

                // Display stats table
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("[bold]Channel[/]")
                    .AddColumn("[bold]Band[/]")
                    .AddColumn("[bold]Min[/]")
                    .AddColumn("[bold]Max[/]")
                    .AddColumn("[bold]Mean[/]")
                    .AddColumn("[bold]StdDev[/]");

                table.AddRow("Red", $"Band {red}", $"{redStats.Min:F2}", $"{redStats.Max:F2}",
                    $"{redStats.Mean:F2}", $"{redStats.StdDev:F2}");
                table.AddRow("Green", $"Band {green}", $"{greenStats.Min:F2}", $"{greenStats.Max:F2}",
                    $"{greenStats.Mean:F2}", $"{greenStats.StdDev:F2}");
                table.AddRow("Blue", $"Band {blue}", $"{blueStats.Min:F2}", $"{blueStats.Max:F2}",
                    $"{blueStats.Mean:F2}", $"{blueStats.StdDev:F2}");

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                // Render composite
                AnsiConsole.MarkupLine("[dim]Rendering RGB composite...[/]");
                var png = RgbCompositeRenderer.RenderRgb(
                    redBand, greenBand, blueBand,
                    redStats, greenStats, blueStats,
                    maxWidth, maxHeight);

                // Write PNG
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllBytes(outputPath, png);

                var fileSize = new FileInfo(outputPath).Length;
                AnsiConsole.MarkupLine($"[green]Composite saved![/] ({fileSize:N0} bytes)");
                AnsiConsole.MarkupLine($"  [green]{Markup.Escape(outputPath)}[/]");
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
