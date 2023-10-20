using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common;

public interface ICommonSamplesContext
{
    VulkanDevice? GpuDevice { get; }

    IBitmapIO BitmapIO { get; }

    ISharpEngineSceneView? CurrentSharpEngineSceneView { get; }

    // Returns null when CurrentSharpEngineSceneView is null
    TextBlockFactory GetTextBlockFactory();
}