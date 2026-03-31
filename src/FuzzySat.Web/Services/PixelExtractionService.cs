using FuzzySat.Core.Raster;
using FuzzySat.Core.Training;

namespace FuzzySat.Web.Services;

/// <summary>
/// Extracts labeled pixel samples from rectangular regions of a MultispectralImage.
/// Used by the interactive training tool to convert drawn rectangles into training data.
/// </summary>
public sealed class PixelExtractionService
{
    /// <summary>
    /// Extracts all pixels within a rectangular region and labels them with the given class name.
    /// Coordinates are in raster pixel space [row, col], inclusive on both ends.
    /// </summary>
    public List<LabeledPixelSample> ExtractRegion(
        MultispectralImage image,
        string className,
        int startRow, int startCol,
        int endRow, int endCol)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(className);

        // Normalize: ensure start <= end
        if (startRow > endRow) (startRow, endRow) = (endRow, startRow);
        if (startCol > endCol) (startCol, endCol) = (endCol, startCol);

        // Clamp to image bounds
        startRow = Math.Clamp(startRow, 0, image.Rows - 1);
        endRow = Math.Clamp(endRow, 0, image.Rows - 1);
        startCol = Math.Clamp(startCol, 0, image.Columns - 1);
        endCol = Math.Clamp(endCol, 0, image.Columns - 1);

        var rowCount = endRow - startRow + 1;
        var colCount = endCol - startCol + 1;
        var samples = new List<LabeledPixelSample>(rowCount * colCount);

        for (var row = startRow; row <= endRow; row++)
        {
            for (var col = startCol; col <= endCol; col++)
            {
                var pixel = image.GetPixelVector(row, col);
                samples.Add(new LabeledPixelSample
                {
                    ClassName = className,
                    BandValues = pixel.BandValues
                });
            }
        }

        return samples;
    }

    /// <summary>
    /// Extracts pixels from multiple labeled regions and combines them into a single sample list.
    /// </summary>
    public List<LabeledPixelSample> ExtractAllRegions(
        MultispectralImage image,
        IReadOnlyList<TrainingRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(regions);

        var allSamples = new List<LabeledPixelSample>();
        foreach (var region in regions)
        {
            allSamples.AddRange(ExtractRegion(
                image, region.ClassName,
                region.StartRow, region.StartCol,
                region.EndRow, region.EndCol));
        }
        return allSamples;
    }
}
