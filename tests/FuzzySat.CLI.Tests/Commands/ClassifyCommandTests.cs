using System.CommandLine;
using FluentAssertions;
using FuzzySat.CLI.Commands;

namespace FuzzySat.CLI.Tests.Commands;

public class ClassifyCommandTests
{
    private readonly Command _command = ClassifyCommand.Create();

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        _command.Name.Should().Be("classify");
    }

    [Fact]
    public void Create_HasThreeRequiredOptions()
    {
        var optionNames = _command.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("--input");
        optionNames.Should().Contain("--model");
        optionNames.Should().Contain("--output");

        _command.Options.Should().OnlyContain(o => o.Required);
    }
}
