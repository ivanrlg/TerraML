using System.CommandLine;
using FluentAssertions;
using FuzzySat.CLI.Commands;

namespace FuzzySat.CLI.Tests.Commands;

public class InfoCommandTests
{
    private readonly Command _command = InfoCommand.Create();

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        _command.Name.Should().Be("info");
    }

    [Fact]
    public void Create_HasFileArgument()
    {
        _command.Arguments.Should().ContainSingle(a => a.Name == "file");
    }

    [Fact]
    public void Create_HasNoRequiredOptions()
    {
        _command.Options.Should().BeEmpty();
    }
}
