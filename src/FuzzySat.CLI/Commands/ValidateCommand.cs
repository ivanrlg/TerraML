using System.CommandLine;
using Spectre.Console;

namespace FuzzySat.CLI.Commands;

/// <summary>
/// CLI command: fuzzysat validate — validate classification against ground truth.
/// </summary>
public static class ValidateCommand
{
    public static Command Create()
    {
        var classifiedOption = new Option<string>("--classified", "-r") { Description = "Path to the classified raster", Required = true };
        var truthOption = new Option<string>("--truth", "-t") { Description = "Path to ground truth raster or samples", Required = true };
        var configOption = new Option<string?>("--config", "-c") { Description = "Path to project configuration JSON" };

        var command = new Command("validate", "Validate classification accuracy against ground truth");
        command.Options.Add(classifiedOption);
        command.Options.Add(truthOption);
        command.Options.Add(configOption);

        command.SetAction(parseResult =>
        {
            var classified = parseResult.GetValue(classifiedOption)!;
            var truth = parseResult.GetValue(truthOption)!;
            var config = parseResult.GetValue(configOption);

            AnsiConsole.MarkupLine("[bold blue]FuzzySat Validate[/]");
            AnsiConsole.MarkupLine($"  Classified: [green]{Markup.Escape(classified)}[/]");
            AnsiConsole.MarkupLine($"  Truth:      [green]{Markup.Escape(truth)}[/]");
            if (config is not null)
                AnsiConsole.MarkupLine($"  Config:     [green]{Markup.Escape(config)}[/]");

            AnsiConsole.MarkupLine("[yellow]Validation not yet implemented (requires GDAL raster I/O).[/]");
            return 1;
        });

        return command;
    }
}
