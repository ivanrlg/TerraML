namespace FuzzySat.Core.Persistence;

/// <summary>DTO for persisting the Explore and Train page band selection and view mode.</summary>
public sealed class ExploreStateDto
{
    public string? ViewMode { get; set; }
    public int? SelectedBandIndex { get; set; }
    public int? RedBandIndex { get; set; }
    public int? GreenBandIndex { get; set; }
    public int? BlueBandIndex { get; set; }
}
