using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.WinForms;

namespace Ab4d.SharpEngine.Samples.WinForms.Common;

public class WinFormsSamplesContext : CommonSamplesContext
{
    public static readonly WinFormsSamplesContext Current = new WinFormsSamplesContext();

    private WinFormsSamplesContext()
        : base(applicationName: "SharpEngine WinForms Samples", bitmapIO: new SystemDrawingBitmapIO())
    {
        PreferredEngineCreateOptions.DesiredDeviceExtensionNames.AddRange(SharpEngineSceneView.RequiredDeviceExtensionNamesForSharedTexture);
    }

    #region GetRandom... methods
    public Color GetRandomWinFormsColor()
    {
        var randomColor = Color.FromArgb(GetRandomByte(), GetRandomByte(), GetRandomByte());
        return randomColor;
    }

    public Color GetRandomWinFormsColorWithAlpha()
    {
        var randomColor = Color.FromArgb(GetRandomByte(), GetRandomByte(), GetRandomByte(), GetRandomByte());
        return randomColor;
    }
    #endregion
}