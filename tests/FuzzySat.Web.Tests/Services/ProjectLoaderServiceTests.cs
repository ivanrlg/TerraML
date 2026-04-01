using System.Text.Json;
using FluentAssertions;
using FuzzySat.Core.Classification;
using FuzzySat.Core.Configuration;
using FuzzySat.Core.Persistence;
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

    // --- GetProjectSummaries tests ---

    [Fact]
    public async Task GetProjectSummaries_Empty_ReturnsEmpty()
    {
        var summaries = _service.GetProjectSummaries();
        summaries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectSummaries_ConfiguredOnly_ReturnsConfiguredStatus()
    {
        await _service.SaveProjectAsync("Demo", CreateTestConfig("Demo"));

        var summaries = _service.GetProjectSummaries();

        summaries.Should().HaveCount(1);
        summaries[0].Name.Should().Be("Demo");
        summaries[0].BandCount.Should().Be(1);
        summaries[0].ClassCount.Should().Be(1);
        summaries[0].Status.Should().Be(ProjectStatus.Configured);
        summaries[0].ClassificationMethod.Should().BeNull();
        summaries[0].OverallAccuracy.Should().BeNull();
    }

    [Fact]
    public async Task GetProjectSummaries_Trained_ReturnsTrainedStatus()
    {
        await _service.SaveProjectAsync("Trained", CreateTestConfig("Trained"));
        CreateArtifact("Trained", "training-session.json", """{"id":"t1","createdAt":"2026-01-01","classNames":[],"bandNames":[],"statistics":{}}""");

        var summaries = _service.GetProjectSummaries();

        summaries.Should().HaveCount(1);
        summaries[0].Status.Should().Be(ProjectStatus.Trained);
    }

    [Fact]
    public async Task GetProjectSummaries_Classified_ReturnsClassifiedWithMethod()
    {
        await _service.SaveProjectAsync("Cls", CreateTestConfig("Cls"));
        CreateArtifact("Cls", "training-session.json", "{}");
        CreateArtifact("Cls", "classification-meta.json", """{"rows":1,"columns":1,"classNames":["A"],"classCodes":[1]}""");
        CreateArtifact("Cls", "classification-options.json", """{"classificationMethod":"Pure Fuzzy","membershipFunctionType":"Gaussian","andOperator":"Minimum","defuzzifierType":"Max Weight"}""");

        var summaries = _service.GetProjectSummaries();

        summaries.Should().HaveCount(1);
        summaries[0].Status.Should().Be(ProjectStatus.Classified);
        summaries[0].ClassificationMethod.Should().Be("Pure Fuzzy");
    }

    [Fact]
    public async Task GetProjectSummaries_Validated_ReturnsMetrics()
    {
        await _service.SaveProjectAsync("Val", CreateTestConfig("Val"));
        CreateArtifact("Val", "training-session.json", "{}");
        CreateArtifact("Val", "classification-meta.json", "{}");
        CreateArtifact("Val", "validation-result.json",
            """{"classNames":["A"],"matrix":[[10]],"overallAccuracy":0.85,"kappaCoefficient":0.72,"totalSamples":10,"correctCount":8,"perClassMetrics":[]}""");

        var summaries = _service.GetProjectSummaries();

        summaries.Should().HaveCount(1);
        summaries[0].Status.Should().Be(ProjectStatus.Validated);
        summaries[0].OverallAccuracy.Should().BeApproximately(0.85, 0.001);
        summaries[0].KappaCoefficient.Should().BeApproximately(0.72, 0.001);
    }

    [Fact]
    public async Task GetProjectSummaries_SkipsCorruptProjects()
    {
        await _service.SaveProjectAsync("Good", CreateTestConfig("Good"));
        File.WriteAllText(Path.Combine(_tempDir, "Corrupt.json"), "not valid json{{{");

        var summaries = _service.GetProjectSummaries();

        summaries.Should().HaveCount(1);
        summaries[0].Name.Should().Be("Good");
    }

    [Fact]
    public async Task GetProjectSummaries_MultipleProjects_ReturnsAll()
    {
        await _service.SaveProjectAsync("Alpha", CreateTestConfig("Alpha"));
        await _service.SaveProjectAsync("Beta", CreateTestConfig("Beta"));
        await _service.SaveProjectAsync("Gamma", CreateTestConfig("Gamma"));

        var summaries = _service.GetProjectSummaries();

        summaries.Should().HaveCount(3);
        summaries.Select(s => s.Name).Should().BeEquivalentTo(["Alpha", "Beta", "Gamma"]);
    }

    // --- DeleteProject tests ---

    [Fact]
    public async Task DeleteProject_RemovesConfigAndData()
    {
        await _service.SaveProjectAsync("ToDelete", CreateTestConfig("ToDelete"));
        CreateArtifact("ToDelete", "training-session.json", "{}");

        _service.DeleteProject("ToDelete");

        File.Exists(Path.Combine(_tempDir, "ToDelete.json")).Should().BeFalse();
        Directory.Exists(Path.Combine(_tempDir, "ToDelete")).Should().BeFalse();
    }

    [Fact]
    public void DeleteProject_NonExistent_DoesNotThrow()
    {
        var act = () => _service.DeleteProject("Ghost");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("..\\..\\etc\\passwd")]
    public void DeleteProject_PathTraversal_Throws(string name)
    {
        var act = () => _service.DeleteProject(name);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task DeleteProject_ConfigOnlyNoDataDir_Works()
    {
        await _service.SaveProjectAsync("ConfigOnly", CreateTestConfig("ConfigOnly"));

        _service.DeleteProject("ConfigOnly");

        _service.ListProjects().Should().NotContain("ConfigOnly");
    }

    // --- Helpers ---

    private void CreateArtifact(string projectName, string fileName, string content)
    {
        var dir = Path.Combine(_tempDir, projectName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, fileName), content);
    }

    private static ClassifierConfiguration CreateTestConfig(string name) => new()
    {
        ProjectName = name,
        Bands = [new BandConfiguration { Name = "B1", SourceIndex = 0 }],
        Classes = [new LandCoverClass { Name = "Urban", Code = 1 }]
    };
}
