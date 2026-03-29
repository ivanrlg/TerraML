using System.CommandLine;
using FuzzySat.Core.Raster;
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
            AnsiConsole.WriteLine();

            try
            {
                var reader = new GdalRasterReader();
                var info = reader.ReadInfo(file);

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("[bold]Property[/]")
                    .AddColumn("[bold]Value[/]");

                table.AddRow("File", Markup.Escape(info.FilePath));
                table.AddRow("Dimensions", $"{info.Columns} x {info.Rows} pixels");
                table.AddRow("Bands", info.BandCount.ToString());
                table.AddRow("Data Type", Markup.Escape(info.DataType));
                table.AddRow("Driver", Markup.Escape(info.DriverName));
                table.AddRow("Projection", info.Projection is not null
                    ? Markup.Escape(info.Projection.Length > 80
                        ? info.Projection[..80] + "..."
                        : info.Projection)
                    : "[dim]None[/]");

                var totalPixels = (long)info.Rows * info.Columns;
                table.AddRow("Total Pixels", totalPixels.ToString("N0"));
                table.AddRow("Total Values", (totalPixels * info.BandCount).ToString("N0"));

                AnsiConsole.Write(table);
                return 0;
            }
            catch (FileNotFoundException ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(ex.FileName ?? file)}");
                return 1;
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