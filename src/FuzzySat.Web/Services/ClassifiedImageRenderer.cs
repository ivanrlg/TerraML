using FuzzySat.Core.Classification;
using SkiaSharp;

namespace FuzzySat.Web.Services;

/// <summary>
/// Renders a classified image as a colored PNG where each pixel is painted
/// with its predicted land cover class color. Uses SkiaSharp for encoding.
/// Supports auto-color assignment via keyword matching and a colorblind-friendly fallback palette.
/// </summary>
public sealed class ClassifiedImageRenderer
{
    /// <summary>
    /// Default color palette for common land cover class keywords (bilingual EN/ES).
    /// </summary>
    private static readonly (string[] Keywords, string HexColor)[] KeywordPalette =
    [
        (["water", "agua", "mar", "ocean", "oceano", "lake", "lago", "river", "rio"], "#3498DB"),
        (["forest", "bosque", "tree", "arbol"], "#27AE60"),
        (["urban", "urbano", "city", "ciudad", "built", "construido"], "#E74C3C"),
        (["agriculture", "cultivo", "crop", "farm", "agri"], "#F39C12"),
        (["vegetation", "vegetacion", "green", "verde", "shrub", "matorral"], "#2ECC71"),
        (["bare", "suelo", "soil", "sand", "arena", "rock", "roca"], "#D4A574"),
        (["cloud", "nube"], "#ECF0F1"),
        (["shadow", "sombra", "dark"], "#34495E"),
        (["pasture", "pasto", "grass", "cesped", "prado"], "#82E0AA"),
        (["wetland", "humedal", "swamp", "pantano"], "#1ABC9C"),
    ];

    /// <summary>
    /// Colorblind-friendly fallback palette for classes that don't match keywords.
    /// </summary>
    private static readonly string[] FallbackPalette =
    [
        "#E6194B", "#3CB44B", "#FFE119", "#4363D8", "#F58231",
        "#911EB4", "#42D4F4", "#F032E6", "#BFEF45", "#FABED4",
        "#469990", "#DCBEFF", "#9A6324", "#800000", "#AAFFC3",
        "#808000", "#FFD8B1", "#000075"
    ];

    /// <summary>
    /// Assigns a hex color to a class name using keyword matching or fallback palette.
    /// </summary>
    public static string AssignColor(string className, int fallbackIndex)
    {
        var lower = className.ToLowerInvariant();
        foreach (var (keywords, hex) in KeywordPalette)
        {
            if (keywords.Any(k => lower.Contains(k)))
                return hex;
        }
        return FallbackPalette[fallbackIndex % FallbackPalette.Length];
    }

    /// <summary>
    /// Builds a complete color map for all classes, respecting any user overrides.
    /// </summary>
    public static Dictionary<string, string> BuildColorMap(
        IReadOnlyList<LandCoverClass> classes,
        IReadOnlyDictionary<string, string>? userOverrides = null)
    {
        var colorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var fallbackIdx = 0;

        foreach (var cls in classes)
        {
            if (userOverrides is not null && userOverrides.TryGetValue(cls.Name, out var userColor))
            {
                colorMap[cls.Name] = userColor;
            }
            else if (cls.Color is not null)
            {
                colorMap[cls.Name] = cls.Color;
            }
            else
            {
                colorMap[cls.Name] = AssignColor(cls.Name, fallbackIdx);
            }
            fallbackIdx++;
        }

        return colorMap;
    }

    /// <summary>
    /// Renders a classification result as a colored PNG byte array.
    /// Each pixel is painted with its class color.
    /// </summary>
    /// <param name="result">The classification result (class per pixel).</param>
    /// <param name="colorMap">Map of class name → hex color string (e.g., "#3498DB").</param>
    /// <param name="maxWidth">Maximum output width in pixels.</param>
    /// <param name="maxHeight">Maximum output height in pixels.</param>
    public byte[] Render(
        ClassificationResult result,
        IReadOnlyDictionary<string, string> colorMap,
        int maxWidth = 800,
        int maxHeight = 600)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(colorMap);

        var rows = result.Rows;
        var cols = result.Columns;

        var scale = Math.Min(1.0, Math.Min((double)maxWidth / cols, (double)maxHeight / rows));
        var outW = Math.Max(1, (int)(cols * scale));
        var outH = Math.Max(1, (int)(rows * scale));

        // Pre-parse colors
        var parsedColors = new Dictionary<string, (byte R, byte G, byte B)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, hex) in colorMap)
        {
            parsedColors[name] = ParseHex(hex);
        }

        using var bitmap = new SKBitmap(outW, outH, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var pixels = bitmap.GetPixelSpan();

        for (var y = 0; y < outH; y++)
        {
            var srcRow = Math.Min((int)(y / scale), rows - 1);
            for (var x = 0; x < outW; x++)
            {
                var srcCol = Math.Min((int)(x / scale), cols - 1);
                var className = result.GetClass(srcRow, srcCol);

                if (!parsedColors.TryGetValue(className, out var color))
                    color = (128, 128, 128); // gray fallback

                var offset = (y * outW + x) * 4;
                pixels[offset] = color.R;
                pixels[offset + 1] = color.G;
                pixels[offset + 2] = color.B;
                pixels[offset + 3] = 255;
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private static (byte R, byte G, byte B) ParseHex(string hex)
    {
        var s = hex.TrimStart('#');
        if (s.Length < 6) return (128, 128, 128);
        return (
            Convert.ToByte(s[..2], 16),
            Convert.ToByte(s[2..4], 16),
            Convert.ToByte(s[4..6], 16)
        );
    }
}
