using System.CommandLine;
using FluentAssertions;
using FuzzySat.CLI.Commands;

namespace FuzzySat.CLI.Tests;

public class ProgramTests
{
    [Fact]
    public void RootCommand_HasAllFiveSubcommands()
    {
        var root = new RootCommand("FuzzySat");
        root.Subcommands.Add(ClassifyCommand.Create());
        root.Subcommands.Add(TrainCommand.Create());
        root.Subcommands.Add(ValidateCommand.Create());
        root.Subcommands.Add(InfoCommand.Create());
        root.Subcommands.Add(VisualizeCommand.Create());

        root.Subcommands.Should().HaveCount(5);
        root.Subcommands.Select(c => c.Name).Should()
            .BeEquivalentTo(["classify", "train", "validate", "info", "visualize"]);
    }
}
