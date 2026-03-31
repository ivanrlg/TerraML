namespace FuzzySat.Core.Training;

/// <summary>
/// A labeled rectangular region in raster pixel coordinates.
/// </summary>
public record TrainingRegion(
    string ClassName,
    string Color,
    int StartRow, int StartCol,
    int EndRow, int EndCol)
{
    /// <summary>Number of pixels in this region.</summary>
    public int PixelCount =>
        (Math.Abs(EndRow - StartRow) + 1) * (Math.Abs(EndCol - StartCol) + 1);
}
