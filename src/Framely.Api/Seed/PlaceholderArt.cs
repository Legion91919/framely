using System.Globalization;

namespace Framely.Api.Seed;

/// <summary>Generated SVG landscapes used as demo photos (no real images anywhere). Mirrors the sketch's art() helper.</summary>
internal static class PlaceholderArt
{
    private static readonly string[][] Palettes =
    [
        ["#F4B88A", "#F7E2C6", "#FFF3DA", "#7A5C61", "#3E3440"],
        ["#8EC5D6", "#DDEFF2", "#FFFFFF", "#5D8C7B", "#2F4F4A"],
        ["#2B2F4E", "#6A5D86", "#F3D9A4", "#3A3552", "#1B1C2E"],
        ["#E9C46A", "#F4E3B5", "#FFFBEA", "#B5835A", "#6B4E3D"],
        ["#C9D6DF", "#EEF2F4", "#FAFAFA", "#8A9BA8", "#4A5A66"],
        ["#F28F79", "#FBD1A2", "#FFF1D6", "#9C4F5C", "#4B2C3A"],
    ];

    public static string DataUri(int seed, int width, int height)
    {
        var p = Palettes[seed % Palettes.Length];
        double R(int n) => ((seed * 9301L + 49297L * n) % 233280) / 233280.0;
        int I(double v) => (int)Math.Round(v);
        int w = width, h = height;
        int sx = I(w * (0.2 + 0.6 * R(1))), sy = I(h * (0.22 + 0.2 * R(2)));
        int y1 = I(h * (0.55 + 0.1 * R(3))), y2 = I(h * (0.72 + 0.1 * R(4)));
        var svg = string.Create(CultureInfo.InvariantCulture,
            $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {w} {h}'><defs><linearGradient id='s' x1='0' y1='0' x2='0' y2='1'>" +
            $"<stop offset='0' stop-color='{p[0]}'/><stop offset='1' stop-color='{p[1]}'/></linearGradient></defs>" +
            $"<rect width='{w}' height='{h}' fill='url(#s)'/><circle cx='{sx}' cy='{sy}' r='{I(w * 0.09)}' fill='{p[2]}'/>" +
            $"<path d='M0 {y1} L{I(w * 0.3)} {I(y1 - h * 0.12)} L{I(w * 0.55)} {I(y1 - h * 0.03)} L{I(w * 0.8)} {I(y1 - h * 0.15)} L{w} {I(y1 - h * 0.05)} V{h} H0Z' fill='{p[3]}'/>" +
            $"<path d='M0 {y2} Q{I(w * 0.4)} {I(y2 - h * 0.1)} {w} {I(y2 + h * 0.02)} V{h} H0Z' fill='{p[4]}'/></svg>");
        return "data:image/svg+xml," + Uri.EscapeDataString(svg);
    }
}
