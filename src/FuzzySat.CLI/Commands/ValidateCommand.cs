using System.CommandLine;
using FuzzySat.Core.Validation;
using Spectre.Console;

namespace FuzzySat.CLI.Commands;

/// <summary>
/// CLI command: fuzzysat validate — validate classification against ground truth CSV.
/// CSV format: actual,predicted (header row required).
/// </summary>
public static class ValidateCommand
{
    public static Command Create()
    {
        var truthOption = new Option<string>("--truth", "-t") { Description = "Path to ground truth CSV (columns: actual,predicted)", Required = true };

        var command = new Command("validate", "Validate classification accuracy against ground truth");
        command.Options.Add(truthOption);

        command.SetAction(parseResult =>
        {
            var truthPath = parseResult.GetValue(truthOption)!;

            AnsiConsole.MarkupLine("[bold blue]FuzzySat Validate[/]");
            AnsiConsole.MarkupLine($"  Truth: [green]{Markup.Escape(truthPath)}[/]");
            AnsiConsole.WriteLine();

            try
            {
                if (!File.Exists(truthPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {Markup.Escape(truthPath)}");
                    return 1;
                }

                // Parse CSV: actual,predicted
                var lines = File.ReadAllLines(truthPath);
                if (lines.Length < 2)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] CSV must have a header row and at least one data row.");
                    return 1;
                }

                var actual = new List<string>();
                var predicted = new List<string>();

                for (var i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var parts = line.Split(',');
                    if (parts.Length < 2)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning:[/] Skipping line {i + 1}: expected 2 columns.");
                        continue;
                    }

                    actual.Add(parts[0].Trim());
                    predicted.Add(parts[1].Trim());
                }

                if (actual.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] No valid samples found in CSV.");
                    return 1;
                }

                // Build confusion matrix
                var matrix = new ConfusionMatrix(actual, predicted);

                // Overall metrics
                var metricsPanel = new Panel(
                    new Markup(
                        $"[bold]Overall Accuracy:[/] [green]{matrix.OverallAccuracy:P2}[/]\n" +
                        $"[bold]Kappa Coefficient:[/] [green]{matrix.KappaCoefficient:F4}[/]\n" +
                        $"[bold]Total Samples:[/] {matrix.TotalSamples}\n" +
                        $"[bold]Correct:[/] {matrix.CorrectCount}"))
                    .Header("[bold]Accuracy Metrics[/]")
                    .Border(BoxBorder.Rounded);

                AnsiConsole.Write(metricsPanel);
                AnsiConsole.WriteLine();

                // Confusion matrix table
                var cmTable = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[bold]Confusion Matrix[/] (rows=actual, cols=predicted)")
                    .AddColumn("[bold]Actual \\ Predicted[/]");

                foreach (var cls in matrix.ClassNames)
                    cmTable.AddColumn($"[bold]{Markup.Escape(cls)}[/]");
                cmTable.AddColumn("[bold]Total[/]");

                foreach (var actualCls in matrix.ClassNames)
                {
                    var row = new List<string> { $"[bold]{Markup.Escape(actualCls)}[/]" };
                    foreach (var predCls in matrix.ClassNames)
                    {
                        var count = matrix[actualCls, predCls];
                        var style = actualCls == predCls ? $"[green]{count}[/]" : (count > 0 ? $"[red]{count}[/]" : "[dim]0[/]");
                        row.Add(style);
                    }
                    row.Add(matrix.RowTotal(actualCls).ToString());
                    cmTable.AddRow(row.ToArray());
                }

                AnsiConsole.Write(cmTable);
                AnsiConsole.WriteLine();

                // Per-class metrics
                var perClassTable = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[bold]Per-Class Metrics[/]")
                    .AddColumn("[bold]Class[/]")
                    .AddColumn("[bold]Producer's Acc (Recall)[/]")
                    .AddColumn("[bold]User's Acc (Precision)[/]")
                    .AddColumn("[bold]Actual Count[/]")
                    .AddColumn("[bold]Predicted Count[/]");

                foreach (var cls in matrix.ClassNames)
                {
                    perClassTable.AddRow(
                        Markup.Escape(cls),
                        matrix.ProducersAccuracy(cls).ToString("P1"),
                        matrix.UsersAccuracy(cls).ToString("P1"),
                        matrix.RowTotal(cls).ToString(),
                        matrix.ColumnTotal(cls).ToString());
                }

                AnsiConsole.Write(perClassTable);
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
