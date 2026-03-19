# Ab4d.SharpEngine Performance Tips

Performance is an important aspect of any 3D engine, and Ab4d.SharpEngine is designed to be fast and efficient. Here are some tips to help you get the best performance out of Ab4d.SharpEngine.

### General performance tips

- Try to **lower the number of draw calls** (individual SceneNode objects). Rendering a few complex objects is usually faster than rendering many simple objects because the GPU can be fully utilized without waiting for the API and driver to process many draw calls.
- Use **instancing** (InstancedMeshNode) when rendering many similar objects. This allows the GPU to render multiple instances of the same geometry with a single draw call, which can significantly improve performance.
- Use MultiLineNode instead of many LineNode objects when rendering many lines. This reduces the number of draw calls.
- Check where the time is spent by inspecting the values in `RenderingStatistics`. The easiest way to do that is to use the Diagnostics Window (see "Troubleshooting" / "DiagnosticsWindow" on how to include it in your app). 
You can also get the `RenderingStatistics` from the `SceneView.Statistics` property.
Before the `Statistics` is collected, you need to enable it by setting `SceneView.IsCollectingStatistics` to `true`.
See also [RenderingStatistics online help](https://www.ab4d.com/help/SharpEngine/html/T_Ab4d_SharpEngine_Core_RenderingStatistics.htm).

You can also get the `CompleteRenderTime` and `FrameCopyTime` from the `SceneView.Statistics` property.
Before the `Statistics` is collected, you need to enable it by setting `SceneView.IsCollectingStatistics` to `true`.


### Use a dedicated GPU on a laptop with multiple graphics cards (for Windows)

When a laptop has multiple graphics cards (integrated and dedicated), Windows, by default, chooses to use the integrated graphics card for your application.
This which can lead to poor performance. 

To ensure that your app and Ab4d.SharpEngine use the dedicated graphics card, 
open the Window Graphics Settings, add your application and set it to use the **"High Performance" option** (instead of "Let Windows decide").
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


Another option is to **force using dedicated GPU** for Ab4d.SharpEngine.

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


