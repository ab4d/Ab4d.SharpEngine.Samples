using System;
using System.Numerics;
using System.Windows;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.Samples.Wpf.Diagnostics;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.SharpEngine.Wpf;

namespace Ab4d.SharpEngine.Samples.Wpf.Common;

public class WpfSamplesContext : CommonSamplesContext
{
    public static readonly WpfSamplesContext Current = new WpfSamplesContext();

    private WpfSamplesContext()
        : base(applicationName: "SharpEngine WPF Samples", bitmapIO: new WpfBitmapIO())
    {
        PreferredEngineCreateOptions.DesiredDeviceExtensionNames.AddRange(SharpEngineSceneView.RequiredDeviceExtensionNamesForSharedTexture);
    }

    public void RegisterCurrentSharpEngineSceneView(SharpEngineSceneView? sharpEngineSceneView)
    {
        SetCurrentSharpEngineSceneView(sharpEngineSceneView);
    }

    #region GetRandom... methods
    public System.Windows.Media.Color GetRandomWpfColor()
    {
        var randomColor = System.Windows.Media.Color.FromRgb(GetRandomByte(), GetRandomByte(), GetRandomByte());
        return randomColor;
    }

    public System.Windows.Media.Color GetRandomWpfColorWithAlpha()
    {
        var randomColor = System.Windows.Media.Color.FromArgb(GetRandomByte(), GetRandomByte(), GetRandomByte(), GetRandomByte());
        return randomColor;
    }
    #endregion
}