using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.UnoPlatform;

internal static class ColorExtensions
{
    public static Color4 ToColor4(this SolidColorBrush? brush)
    {
        if (brush == null)
            return Color4.Transparent;

        return new Color4(
            brush.Color.R / 255f,
            brush.Color.G / 255f,
            brush.Color.B / 255f,
            brush.Color.A / 255f
        );
    }
}
