using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common;

public interface ICommonSamplesContext
{
    VulkanDevice? GpuDevice { get; }

    IBitmapIO BitmapIO { get; }

    ISharpEngineSceneView? CurrentSharpEngineSceneView { get; }
}