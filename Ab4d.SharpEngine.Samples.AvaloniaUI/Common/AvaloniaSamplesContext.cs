using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Samples.Common;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Common;

public class AvaloniaSamplesContext : CommonSamplesContext
{
    public static readonly AvaloniaSamplesContext Current = new AvaloniaSamplesContext();

    private AvaloniaSamplesContext()
        : base(applicationName: "SharpEngine AvaloniaUI Samples", bitmapIO: new SkiaSharpBitmapIO())
    {
        PreferredEngineCreateOptions.DesiredDeviceExtensionNames.AddRange(SharpEngineSceneView.RequiredDeviceExtensionNamesForSharedTexture);
    }

    public void RegisterCurrentSharpEngineSceneView(SharpEngineSceneView? sharpEngineSceneView)
    {
        SetCurrentSharpEngineSceneView(sharpEngineSceneView);
    }

    #region GetRandom... methods
    public Color GetRandomAvaloniaColor()
    {
        var randomColor = Color.FromRgb(GetRandomByte(), GetRandomByte(), GetRandomByte());
        return randomColor;
    }

    public Color GetRandomAvaloniaColorWithAlpha()
    {
        var randomColor = Color.FromArgb(GetRandomByte(), GetRandomByte(), GetRandomByte(), GetRandomByte());
        return randomColor;
    }
    #endregion
}