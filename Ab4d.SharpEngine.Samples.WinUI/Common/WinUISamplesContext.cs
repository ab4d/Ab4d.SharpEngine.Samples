using Windows.UI;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.WinUI;

namespace Ab4d.SharpEngine.Samples.WinUI.Common;

public class WinUISamplesContext : CommonSamplesContext
{
    public static readonly WinUISamplesContext Current = new WinUISamplesContext();

    private WinUISamplesContext()
        : base(applicationName: "SharpEngine WinUI Samples", bitmapIO: new WinUIBitmapIO())
    {
        PreferredEngineCreateOptions.DesiredDeviceExtensionNames.AddRange(SharpEngineSceneView.RequiredDeviceExtensionNamesForSharedTexture);
    }

    public void RegisterCurrentSharpEngineSceneView(SharpEngineSceneView? sharpEngineSceneView)
    {
        SetCurrentSharpEngineSceneView(sharpEngineSceneView);
    }

    #region GetRandom... methods
    public Color GetRandomWinUIColor()
    {
        var randomColor = Color.FromArgb(255, GetRandomByte(), GetRandomByte(), GetRandomByte());
        return randomColor;
    }

    public Color GetRandomWinUIColorWithAlpha()
    {
        var randomColor = Color.FromArgb(GetRandomByte(), GetRandomByte(), GetRandomByte(), GetRandomByte());
        return randomColor;
    }
    #endregion
}