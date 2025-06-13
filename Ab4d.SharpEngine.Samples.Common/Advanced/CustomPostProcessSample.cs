using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Effects;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class CustomPostProcessSample : CommonSample
{
    public override string Title => "Custom post process: HSV color modifier";
    public override string Subtitle => "This sample shows how easy is to create a custom post process by defining its fragment shader and then pass the parameters by using push constants.";
    
    private HsvColorPostProcess _hsvColorPostProcess;

    private ICommonSampleUIElement? _modeButton;
    private ICommonSampleUIElement? _hueSlider;

    public CustomPostProcessSample(ICommonSamplesContext context)
        : base(context)
    {
        // First create an instance of AssemblyShaderBytecodeProvider.
        // This will allow using ShadersManager to cache and get the shaders from the assembly's EmbeddedResources.
        // Instead of AssemblyShaderBytecodeProvider, it is also possible to use:
        // - DictionaryShaderBytecodeProvider
        // - DirectoryShaderBytecodeProvider
        // - FileShaderBytecodeProvider
        // - SimpleShaderBytecodeProvider
        
        var resourceAssembly = this.GetType().Assembly;
        var assemblyShaderBytecodeProvider = new AssemblyShaderBytecodeProvider(resourceAssembly, resourceRootName: resourceAssembly.GetName().Name + ".Resources.Shaders.spv.");

        Ab4d.SharpEngine.Utilities.ShadersManager.RegisterShaderResourceStatic(assemblyShaderBytecodeProvider);
        
        
        _hsvColorPostProcess = new HsvColorPostProcess();
    }

    protected override void OnCreateScene(Scene scene)
    {
        var testScene = TestScenes.GetTestScene(TestScenes.StandardTestScenes.HouseWithTrees, new Vector3(0, -10, 0), PositionTypes.Bottom | PositionTypes.Center, finalSize: new Vector3(400, 400, 400));
        scene.RootNode.Add(testScene);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -20;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 600;
        }
    }

    /// <inheritdoc />
    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.PostProcesses.Add(_hsvColorPostProcess);
        
        // Usually the custom animation is done in the SceneUpdating event handler, that is subscribed by the following code:
        //sceneView.SceneUpdating += OnSceneViewOnSceneUpdating;
        //
        // But in this samples project we use call to CommonSample.SubscribeSceneUpdating method to subscribe to the SceneUpdating event.
        // This allows automatic unsubscribing when the sample is unloaded and automatic UI testing
        // (prevented starting animation and using CallSceneUpdating with providing custom elapsedSeconds value).
        base.SubscribeSceneUpdating(UpdateHsvColor);

        base.OnSceneViewInitialized(sceneView);
    }
    
    /// <inheritdoc />
    protected override void OnDisposed()
    {
        // Most of the post processes do not create any resources, but still it is a good practice to dispose them (maybe in the future they will require some resources).
        _hsvColorPostProcess.Dispose();

        base.OnDisposed();
    }

    private void UpdateHsvColor(float elapsedSeconds)
    {
        _hsvColorPostProcess.HueOffset = ((float)elapsedSeconds * 120f) % 360; // Hue value is in range from 0 to 360, so we need 3 seconds to go from 0 to 360 degrees

        _hueSlider?.UpdateValue();
    }

    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        _hueSlider = ui.CreateSlider(0, 360, () => _hsvColorPostProcess.HueOffset,
            newValue => _hsvColorPostProcess.HueOffset = newValue,
            width: 100,
            keyText: "Hue:",
            keyTextWidth: 70,
            formatShownValueFunc: sliderValue => $"{sliderValue:F0}");

        ui.CreateSlider(0, 2, () => _hsvColorPostProcess.SaturationFactor,
            newValue => _hsvColorPostProcess.SaturationFactor = newValue,
            width: 100,
            keyText: "Saturation:",
            keyTextWidth: 70,
            formatShownValueFunc: sliderValue => $"{sliderValue:F2}");

        ui.CreateSlider(0, 2, () => _hsvColorPostProcess.BrightnessFactor,
            newValue => _hsvColorPostProcess.BrightnessFactor = newValue,
            width: 100,
            keyText: "Brightness:",
            keyTextWidth: 70,
            formatShownValueFunc: sliderValue => $"{sliderValue:F2}");
        
        ui.AddSeparator();

        _modeButton = ui.CreateButton("Stop animation", () =>
        {
            if (base.IsSceneUpdatingSubscribed)
            {
                base.UnsubscribeSceneUpdating();
                _modeButton?.SetText("Start animation");
            }
            else
            {
                base.SubscribeSceneUpdating(UpdateHsvColor);
                _modeButton?.SetText("Stop animation");
            }
        });
    }
}