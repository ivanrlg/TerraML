using System.CommandLine;
using Spectre.Console;

namespace FuzzySat.CLI.Commands;

/// <summary>
/// CLI command: fuzzysat info — display raster metadata.
/// </summary>
public static class InfoCommand
{
    public static Command Create()
    {
        var fileArgument = new Argument<string>("file") { Description = "Path to the raster file" };

        var command = new Command("info", "Display raster file metadata (bands, dimensions, projection)");
        command.Arguments.Add(fileArgument);

        command.SetAction(parseResult =>
        {
            var file = parseResult.GetValue(fileArgument)!;

            AnsiConsole.MarkupLine("[bold blue]FuzzySat Info[/]");
            AnsiConsole.MarkupLine($"  File: [green]{Markup.Escape(file)}[/]");

            AnsiConsole.MarkupLine("[yellow]Info display not yet implemented (requires GDAL raster I/O).[/]");
            return 1;
        });

        return command;
    }
}
