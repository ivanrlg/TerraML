using FluentAssertions;
using FuzzySat.Core.Classification;
using FuzzySat.Web.Services;

namespace FuzzySat.Web.Tests.Services;

public class ClassifiedImageRendererTests
{
    private readonly ClassifiedImageRenderer _renderer = new();

    private static ClassificationResult CreateTestResult()
    {
        var classMap = new string[3, 3]
        {
            { "Water", "Urban", "Forest" },
            { "Water", "Forest", "Urban" },
            { "Forest", "Water", "Urban" }
        };
        var confidenceMap = new double[3, 3]
        {
            { 0.9, 0.8, 0.7 },
            { 0.85, 0.75, 0.65 },
            { 0.95, 0.6, 0.55 }
        };
        var classes = new List<LandCoverClass>
        {
            new() { Name = "Water", Code = 1, Color = "#3498DB" },
            new() { Name = "Urban", Code = 2, Color = "#E74C3C" },
            new() { Name = "Forest", Code = 3, Color = "#27AE60" }
        };
        return new ClassificationResult(classMap, confidenceMap, classes);
    }

    [Fact]
    public void Render_ProducesValidPng()
    {
        var result = CreateTestResult();
        var colorMap = ClassifiedImageRenderer.BuildColorMap(result.Classes);

        var png = _renderer.Render(result, colorMap);

        // PNG magic bytes: 137 80 78 71
        png.Should().NotBeEmpty();
        png.Length.Should().BeGreaterThan(8);
        png[0].Should().Be(137);
        png[1].Should().Be(80); // 'P'
        png[2].Should().Be(78); // 'N'
        png[3].Should().Be(71); // 'G'
    }

    [Fact]
    public void BuildColorMap_UsesExplicitColors()
    {
        var classes = new List<LandCoverClass>
        {
            new() { Name = "Water", Code = 1, Color = "#112233" },
            new() { Name = "Forest", Code = 2, Color = "#445566" }
        };

        var map = ClassifiedImageRenderer.BuildColorMap(classes);

        map["Water"].Should().Be("#112233");
        map["Forest"].Should().Be("#445566");
    }

    [Fact]
    public void BuildColorMap_AutoAssignsFromKeywords()
    {
        var classes = new List<LandCoverClass>
        {
            new() { Name = "Water Body", Code = 1 },
            new() { Name = "Dense Forest", Code = 2 },
            new() { Name = "Urban Area", Code = 3 }
        };

        var map = ClassifiedImageRenderer.BuildColorMap(classes);

        // Water keyword should match blue
        map["Water Body"].Should().Be("#3498DB");
        // Forest keyword should match green
        map["Dense Forest"].Should().Be("#27AE60");
        // Urban keyword should match red
        map["Urban Area"].Should().Be("#E74C3C");
    }

    [Fact]
    public void BuildColorMap_RespectsUserOverrides()
    {
        var classes = new List<LandCoverClass>
        {
            new() { Name = "Water", Code = 1, Color = "#3498DB" },
            new() { Name = "Forest", Code = 2, Color = "#27AE60" }
        };
        var overrides = new Dictionary<string, string> { ["Water"] = "#FF0000" };

        var map = ClassifiedImageRenderer.BuildColorMap(classes, overrides);

        map["Water"].Should().Be("#FF0000"); // override
        map["Forest"].Should().Be("#27AE60"); // original
    }

    [Fact]
    public void BuildColorMap_UnknownClass_UsesFallbackPalette()
    {
        var classes = new List<LandCoverClass>
        {
            new() { Name = "XyzUnknown", Code = 1 }
        };

        var map = ClassifiedImageRenderer.BuildColorMap(classes);

        // Should get some color from fallback palette, not crash
        map["XyzUnknown"].Should().StartWith("#");
        map["XyzUnknown"].Should().HaveLength(7);
    }

    [Fact]
    public void AssignColor_SpanishKeywords_Match()
    {
        ClassifiedImageRenderer.AssignColor("Bosque Denso", 0).Should().Be("#27AE60");
        ClassifiedImageRenderer.AssignColor("Zona Urbana", 0).Should().Be("#E74C3C");
        ClassifiedImageRenderer.AssignColor("Cuerpo de Agua", 0).Should().Be("#3498DB");
        ClassifiedImageRenderer.AssignColor("Cultivo de Maiz", 0).Should().Be("#F39C12");
    }

    [Fact]
    public void Render_RespectsMaxDimensions()
    {
        // Create a larger result
        var classMap = new string[100, 200];
        var confidenceMap = new double[100, 200];
        for (var r = 0; r < 100; r++)
            for (var c = 0; c < 200; c++)
            {
                classMap[r, c] = "A";
                confidenceMap[r, c] = 1.0;
            }
        var classes = new List<LandCoverClass> { new() { Name = "A", Code = 1 } };
        var result = new ClassificationResult(classMap, confidenceMap, classes);
        var colorMap = new Dictionary<string, string> { ["A"] = "#FF0000" };

        // Render with small max dimensions
        var png = _renderer.Render(result, colorMap, maxWidth: 50, maxHeight: 50);
        png.Should().NotBeEmpty();
    }
}
