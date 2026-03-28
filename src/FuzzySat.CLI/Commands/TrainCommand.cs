using System.CommandLine;
using Spectre.Console;

namespace FuzzySat.CLI.Commands;

/// <summary>
/// CLI command: fuzzysat train — extract training statistics from labeled samples.
/// </summary>
public static class TrainCommand
{
    public static Command Create()
    {
        var inputOption = new Option<string>("--input", "-i") { Description = "Path to the input raster image", Required = true };
        var samplesOption = new Option<string>("--samples", "-s") { Description = "Path to training samples (CSV or shapefile)", Required = true };
        var outputOption = new Option<string>("--output", "-o") { Description = "Path for the output training session JSON", Required = true };
        var configOption = new Option<string?>("--config", "-c") { Description = "Path to project configuration JSON" };

        var command = new Command("train", "Extract training statistics from labeled pixel samples");
        command.Options.Add(inputOption);
        command.Options.Add(samplesOption);
        command.Options.Add(outputOption);
        command.Options.Add(configOption);

        command.SetAction(parseResult =>
        {
            var input = parseResult.GetValue(inputOption)!;
            var samples = parseResult.GetValue(samplesOption)!;
            var output = parseResult.GetValue(outputOption)!;
            var config = parseResult.GetValue(configOption);

            AnsiConsole.MarkupLine("[bold blue]FuzzySat Train[/]");
            AnsiConsole.MarkupLine($"  Input:   [green]{input}[/]");
            AnsiConsole.MarkupLine($"  Samples: [green]{samples}[/]");
            AnsiConsole.MarkupLine($"  Output:  [green]{output}[/]");
            if (config is not null)
                AnsiConsole.MarkupLine($"  Config:  [green]{config}[/]");

            AnsiConsole.MarkupLine("[yellow]Training not yet implemented (requires GDAL raster I/O).[/]");
        });

        return command;
    }
}
