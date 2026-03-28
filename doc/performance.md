# Ab4d.SharpEngine Performance Tips

Performance is an important aspect of any 3D engine, and Ab4d.SharpEngine is designed to be fast and efficient. Here are some tips to help you get the best performance out of Ab4d.SharpEngine.

## General performance tips

- Try to **lower the number of draw calls** (individual SceneNode objects). Rendering a few complex objects is usually faster than rendering many simple objects because the GPU can be fully utilized without waiting for the API and driver to process many draw calls.
- Use **instancing** (InstancedMeshNode) when rendering many similar objects. This allows the GPU to render multiple instances of the same geometry with a single draw call, which can significantly improve performance.
- Use **MultiLineNode** instead of many LineNode objects when rendering many lines. This reduces the number of draw calls.
- Use **Diagnostics Window** to check the rendering statistics. See [Install DiagnosticsWindow in your app](../README.md#how-to-install-diagnosticswindow-to-your-app) on how to include it in your app. 
You can also get the `RenderingStatistics` from the `SceneView.Statistics` property.
Before the `Statistics` is collected, you need to enable it by setting `SceneView.IsCollectingStatistics` to `true`.
See also [RenderingStatistics online help](https://www.ab4d.com/help/SharpEngine/html/T_Ab4d_SharpEngine_Core_RenderingStatistics.htm).


## Use a dedicated GPU on a laptop with multiple graphics cards (for Windows)

When a laptop has multiple graphics cards (integrated and dedicated), Windows, by default, chooses to use the integrated graphics card for your application.
This which can lead to poor performance. 

### Dedicated GPU for Avalonia with Vulkan backend

**Avalonia** apps can be created with Vulkan backend. In this case both the UI (rendered by Avalonia) and the 3D graphics (rendered by Ab4d.SharpEngine) are rendered by Vulkan. In this case, it is possible to set `PreferDiscreteGpu` to true and this forces to use dedicated GPU by both Avalonia and Ab4d.SharpEngine. 

The following code in the Program.cs configures the Avalonia app to use Vulkan backend and tries to use a dedicated GPU:
```csharp
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new Win32PlatformOptions
        {
            RenderingMode = new[]
            {
                Win32RenderingMode.Vulkan
            }
        })
        .With(new X11PlatformOptions
        {
            RenderingMode = new[]
            {
                X11RenderingMode.Vulkan
            }
        })
        .With(new Avalonia.Vulkan.VulkanOptions()
        {
            VulkanDeviceCreationOptions = new VulkanDeviceCreationOptions()
            {
                // When the following option is set, then on a laptop with multiple GPUs
                // Avalonia and SharpEngine will use a discrete GPU even if the "High performance"
                // is not selected for this app in the Windows Graphics Settings.
                //
                // It is still recommended to use "High performance" to prevent potential
                // copying of the window's content to the primary graphics card.
                //
                // Comment this setting if you want to use integrated GPU and improve battery life.
                PreferDiscreteGpu = true
            },
            //VulkanInstanceCreationOptions = new Avalonia.Vulkan.VulkanInstanceCreationOptions()
            //{
            //    UseDebug = true // Use Vulkan debug layers for Avalonia UI operations
            //}
        });
```


Note that behind the scenes Windows may still need to copy the rendered Window content to the primary GPU.


### Configure Windows Graphics Settings

On a computer with multiple graphics card, the most reliable and the best option is that the users configure the app to
use the dedicated graphics card.

On Windows this can be done by the following steps:
- open the Window Graphics Settings
- add your application and set it to use the **"High Performance" option** (instead of "Let Windows decide").

This will force Windows to use the dedicated GPU for your application, which can significantly improve performance when rendering complex 3D scenes.

This must be done by the end users of your application.

But you can detect that situation by using the following code (`MainSceneView` is of type `SharpEngineSceneView`):

```csharp
MainSceneView.GpuDeviceCreated += (sender, args) =>
{
    if (args.GpuDevice.IsIntegratedGpu && 
        args.GpuDevice.VulkanInstance.AllPhysicalDeviceDetails.Any(p => p.DeviceProperties.DeviceType == PhysicalDeviceType.DiscreteGpu))
    {
        // Show message box that user should open "Graphics Settings" and set "High performance" for this application to use the discrete GPU instead of integrated GPU.
    }
};
```


### Force using dedicated GPU for Ab4d.SharpEngine

Even when the UI of the application is rendered by the integrated GPU, it is possible to **force using dedicated GPU** for Ab4d.SharpEngine.

This is done by setting the `EngineCreateOptions.DeviceSelectionType` to `BestPhysicalDevice` (from `DefaultDevice`), for example:

```csharp
MainSceneView.CreateOptions.DeviceSelectionType = EngineCreateOptions.DeviceSelectionTypes.BestPhysicalDevice;
```

See description of possible DeviceSelectionTypes values: [DeviceSelectionTypes online help](https://www.ab4d.com/help/SharpEngine/html/T_Ab4d_SharpEngine_Common_EngineCreateOptions_DeviceSelectionTypes.htm).

The problem of using `BestPhysicalDevice` is that Windows can still start your application with the integrated GPU (the UI will be rendered by the integrated GPU).
To show the rendered 3D scene in such case, the rendered 3D scene will need to be copied from the dedicated GPU to the integrated GPU, which can cause a delay and reduce performance.

Therefore, you need to consider how much you will gain by using a dedicated GPU and how much performance will be lost by copying the rendered scene from the dedicated GPU to the integrated GPU.
Usually, when you have a very complex 3D scene, you will gain more by using the dedicated GPU, even with the copying overhead.

You can check that by using the Diagnostics Window (see "Troubleshooting" / "DiagnosticsWindow" on how to include it in your app) 
and compare "CompleteRenderTime" with "FrameCopyTime". The CompleteRenderTime includes waiting for the GPU to finish rendering.

You can also get the `CompleteRenderTime` and `FrameCopyTime` from the `SceneView.Statistics` property.
Before the `Statistics` is collected, you need to enable it by setting `SceneView.IsCollectingStatistics` to `true`.
See also [RenderingStatistics online help](https://www.ab4d.com/help/SharpEngine/html/T_Ab4d_SharpEngine_Core_RenderingStatistics.htm).


