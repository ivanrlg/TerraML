using System.CommandLine;
using FluentAssertions;
using FuzzySat.CLI.Commands;

namespace FuzzySat.CLI.Tests.Commands;

public class TrainCommandTests
{
    private readonly Command _command = TrainCommand.Create();

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        _command.Name.Should().Be("train");
    }

    [Fact]
    public void Create_HasRequiredOptions()
    {
        var optionNames = _command.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("--samples");
        optionNames.Should().Contain("--output");

        _command.Options.Should().OnlyContain(o => o.Required);
    }
}
