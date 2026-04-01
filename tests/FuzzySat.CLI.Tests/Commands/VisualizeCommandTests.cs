using System.CommandLine;
using FluentAssertions;
using FuzzySat.CLI.Commands;

namespace FuzzySat.CLI.Tests.Commands;

public class VisualizeCommandTests
{
    private readonly Command _command = VisualizeCommand.Create();

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        _command.Name.Should().Be("visualize");
    }

    [Fact]
    public void Create_HasInputArgument()
    {
        _command.Arguments.Should().ContainSingle(a => a.Name == "input");
    }

    [Fact]
    public void Create_HasRequiredOptions()
    {
        var optionNames = _command.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("--red");
        optionNames.Should().Contain("--green");
        optionNames.Should().Contain("--blue");
        optionNames.Should().Contain("--output");
    }

    [Fact]
    public void Create_HasOptionalDimensionOptions()
    {
        var optionNames = _command.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("--width");
        optionNames.Should().Contain("--height");
    }

    [Fact]
    public void Create_RedGreenBlueOutputAreRequired()
    {
        _command.Options.First(o => o.Name == "--red").Required.Should().BeTrue();
        _command.Options.First(o => o.Name == "--green").Required.Should().BeTrue();
        _command.Options.First(o => o.Name == "--blue").Required.Should().BeTrue();
        _command.Options.First(o => o.Name == "--output").Required.Should().BeTrue();
    }

    [Fact]
    public void Create_WidthHeightAreOptional()
    {
        _command.Options.First(o => o.Name == "--width").Required.Should().BeFalse();
        _command.Options.First(o => o.Name == "--height").Required.Should().BeFalse();
    }
}
