using System.CommandLine;
using FluentAssertions;
using FuzzySat.CLI.Commands;

namespace FuzzySat.CLI.Tests.Commands;

public class ValidateCommandTests
{
    private readonly Command _command = ValidateCommand.Create();

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        _command.Name.Should().Be("validate");
    }

    [Fact]
    public void Create_HasRequiredTruthOption()
    {
        _command.Options.Should().ContainSingle(o => o.Name == "--truth");
        _command.Options.First(o => o.Name == "--truth").Required.Should().BeTrue();
    }
}
