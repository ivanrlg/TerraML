using System.CommandLine;
using Spectre.Console;

namespace FuzzySat.CLI.Commands;

/// <summary>
/// CLI command: fuzzysat classify — classify a raster image using a trained model.
/// </summary>
public static class ClassifyCommand
{
    public static Command Create()
    {
        var inputOption = new Option<string>("--input", "-i") { Description = "Path to the input raster image", Required = true };
        var modelOption = new Option<string>("--model", "-m") { Description = "Path to the training session JSON", Required = true };
        var outputOption = new Option<string>("--output", "-o") { Description = "Path for the output classified raster", Required = true };
        var configOption = new Option<string?>("--config", "-c") { Description = "Path to project configuration JSON" };

        var command = new Command("classify", "Classify a raster image using a trained fuzzy model");
        command.Options.Add(inputOption);
        command.Options.Add(modelOption);
        command.Options.Add(outputOption);
        command.Options.Add(configOption);

        command.SetAction(parseResult =>
        {
            var input = parseResult.GetValue(inputOption)!;
            var model = parseResult.GetValue(modelOption)!;
            var output = parseResult.GetValue(outputOption)!;
            var config = parseResult.GetValue(configOption);

            AnsiConsole.MarkupLine("[bold blue]FuzzySat Classify[/]");
            AnsiConsole.MarkupLine($"  Input:  [green]{input}[/]");
            AnsiConsole.MarkupLine($"  Model:  [green]{model}[/]");
            AnsiConsole.MarkupLine($"  Output: [green]{output}[/]");
            if (config is not null)
                AnsiConsole.MarkupLine($"  Config: [green]{config}[/]");

            AnsiConsole.MarkupLine("[yellow]Classification not yet implemented (requires GDAL raster I/O).[/]");
        });

        return command;
    }
}
