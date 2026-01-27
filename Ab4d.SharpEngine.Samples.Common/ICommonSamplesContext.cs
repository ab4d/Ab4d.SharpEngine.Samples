using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;

#if VULKAN
using Ab4d.SharpEngine.Vulkan;
using Ab4d.SharpEngine.OverlayPanels;
using GpuDevice = Ab4d.SharpEngine.Vulkan.VulkanDevice;
#elif WEB_GL
using Ab4d.SharpEngine.WebGL;
using GpuDevice = Ab4d.SharpEngine.WebGL.WebGLDevice;
#endif

namespace Ab4d.SharpEngine.Samples.Common;

public interface ICommonSamplesContext
{
    GpuDevice? GpuDevice { get; }

    IBitmapIO? BitmapIO { get; }

    ISharpEngineSceneView? CurrentSharpEngineSceneView { get; }

#if VULKAN
    // Returns null when CurrentSharpEngineSceneView is null
    TextBlockFactory GetTextBlockFactory();
#endif
    Task<TextBlockFactory> GetTextBlockFactoryAsync();
}