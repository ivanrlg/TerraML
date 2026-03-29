using FluentAssertions;
using FuzzySat.Core.Classification;
using FuzzySat.Core.Configuration;
using FuzzySat.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FuzzySat.Web.Tests.Services;

public class ProjectLoaderServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ProjectLoaderService _service;

    public ProjectLoaderServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FuzzySat_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var options = Options.Create(new ProjectStorageOptions { BasePath = _tempDir });
        _service = new ProjectLoaderService(options, NullLogger<ProjectLoaderService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("..\\..\\etc\\passwd")]
    [InlineData("valid/../../../escape")]
    [InlineData("foo/../../bar")]
    public void LoadProject_PathTraversal_Throws(string name)
    {
        var act = () => _service.LoadProject(name);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("..\\..\\etc\\passwd")]
    [InlineData("valid/../../../escape")]
    public void SaveProject_PathTraversal_Throws(string name)
    {
        var config = CreateTestConfig("test");
        var act = async () => await _service.SaveProjectAsync(name, config);
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_Works()
    {
        var config = CreateTestConfig("TestProject");
        await _service.SaveProjectAsync("TestProject", config);

        var loaded = _service.LoadProject("TestProject");

        loaded.Should().NotBeNull();
        loaded!.ProjectName.Should().Be("TestProject");
    }

    [Fact]
    public void LoadProject_NonExistent_ReturnsNull()
    {
        var result = _service.LoadProject("DoesNotExist");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ListProjects_ReturnsNames()
    {
        await _service.SaveProjectAsync("Alpha", CreateTestConfig("Alpha"));
        await _service.SaveProjectAsync("Beta", CreateTestConfig("Beta"));

        var projects = _service.ListProjects();

        projects.Should().Contain("Alpha");
        projects.Should().Contain("Beta");
    }

    [Fact]
    public void LoadProject_CorruptJson_ReturnsNull()
    {
        File.WriteAllText(Path.Combine(_tempDir, "corrupt.json"), "{ this is not valid json }}}");

        var result = _service.LoadProject("corrupt");
        result.Should().BeNull();
    }

    private static ClassifierConfiguration CreateTestConfig(string name) => new()
    {
        ProjectName = name,
        Bands = [new BandConfiguration { Name = "B1", SourceIndex = 0 }],
        Classes = [new LandCoverClass { Name = "Urban", Code = 1 }]
    };
}
