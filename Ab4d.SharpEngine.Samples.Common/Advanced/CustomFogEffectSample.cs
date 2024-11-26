using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

// This sample shows how to create a custom effect with custom vertex and fragment shaders.
//
// The shaders for the FogEffect are stored in Resources/Shaders folder.
// There are also two scripts that can be used to compile the shaders.
// Vulkan SDK must be installed on the computer to be able to run the compile script.
// The compiled versions of the shaders are stored in the spv filter.
// In txt folder there are some helper files that can be used to check the structure of the required shader resources.
//
// The new effect can be used by creating an instance of FogMaterial and setting it to a ModelNode object.
// The FogMaterial defines the properties for the new material.
// It also sets the Effect property to the FogEffect.
//
// The FogEffect is used to prepare all the resources for rendering.
// Those resources are stored to the RenderingItem in the FogEffect.ApplyRenderingItemMaterial method.
//
// The most important property for rendering in RenderingItem is the EffectTechnique.
// The EffectTechnique defines the Render method that calls the Vulkan methods that
// bind the Vulkan pipeline, resources and the call Draw method (CmdDrawIndexed or CmdDraw).

//
// Notes and tips for users who want to use custom effects:
//
// - Using Vulkan is much more complicated than OpenGL (WebGL) or DirectX.
//   Please try to understand how Vulkan rendering works before start doing any bigger changed to this sample.
//   Please do not ask me to help you with writing Vulkan code. This would take too much of my time.
//
// - Simple changes can be done by changing the fragment shader (Resources/Shaders/FogShader.frag.glsl)
//   You can add additional code to the shader and add new properties to the FogMaterial.
//   Then compile the shader and check the updated buffer structure (FiledOffset value) in Resources/Shaders/txt/FogShader.frag.json.
//   Based on that update the FogMaterialUniformBuffer struct in FogEffect.
//   Then add new properties to FogMaterial and then update the FogEffect.UpdateMaterialData method.
//   Such changes can be done quite easily.
//   Adding textures support or some other bigger pipeline changes are much harder to implement and
//   require good knowledge of Vulkan programming.
//  
// - For simple debugging of the shaders update the fragment shader to write fixed colors values or
//   color values based on some properties (for example normal vector).
//
// - For advanced debugging Use RenderDoc application (free and available from https://renderdoc.org/).
//   You will need to use WPF or Generic SDL / Gltf (Ab4d.SharpEngine.Samples.CrossPlatform.sln) to 
//   debug the Vulkan shaders. Using Avalonia will not work because Avalonia is using OpenGL to render its UI.
//   To start debugging, start your application from RenderDoc.
//   When using WPF's SharpEngineSceneView, then the easiest way to capture a frame is to use 
//   OverlayTexture as PresentationType. This will allow RenderDoc to capture any frame by pressing F12 or PrintScreen.
//   When using SharedTexture or WritableBitmap presentation type, you will need to call the SceneView.CaptureNextFrameInRenderDoc() method
//   to capture the frame. This can be also done by using the Diagnostics window - select the "Capture next frame in RenderDoc" menu item.
//   
// - It is possible to purchase source code of some SharpEngine effects and shaders.
//   This can show you how the existing effects are implemented. Contact support (https://www.ab4d.com/Feedback.aspx) for more info.
//

public class CustomFogEffectSample : CommonSample
{
    public override string Title => "Custom fog effect ";
    public override string Subtitle => "This sample shows how to create a custom effect with custom vertex and fragment shaders.\nFogEffect and FogMaterial source code is in the Advanced folder. Shaders source code is in the Resources/Shaders folder.";
    
    
    private float _fogStart = 260;
    private float _fogDistance = 150;
    private Color3 _fogColor = Color3.White;

    private List<FogMaterial> _allFogMaterials = new List<FogMaterial>();

    public CustomFogEffectSample(ICommonSamplesContext context)
        : base(context)
    {
        // First create an instance of AssemblyShaderBytecodeProvider.
        // This will allow using ShadersManager to cache and get the shaders from the assembly's EmbeddedResources.
        var resourceAssembly = this.GetType().Assembly;
        var assemblyShaderBytecodeProvider = new AssemblyShaderBytecodeProvider(resourceAssembly, resourceRootName: resourceAssembly.GetName().Name + ".Resources.Shaders.spv.");

        Ab4d.SharpEngine.Utilities.ShadersManager.RegisterShaderResourceStatic(assemblyShaderBytecodeProvider);
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Uncomment to test rendering a StandardMaterial:
        //var standardBoxNode = new BoxModelNode(new Vector3(0, 0, 0), new Vector3(60, 80, 40), "StandardBox")
        //{
        //    UseSharedBoxMesh = false
        //};
        //standardBoxNode.Material = new StandardMaterial(Colors.SkyBlue);

        //scene.RootNode.Add(standardBoxNode);



        var bottomFogMaterial = new FogMaterial()
        {
            DiffuseColor      = Colors.Gray,
            Opacity           = 1, // no transparency,
            FogStart          = _fogStart,
            FogFullColorStart = _fogStart + _fogDistance,
            FogColor          = _fogColor
        }; 
        
        var sphereFogMaterial = new FogMaterial()
        {
            DiffuseColor      = Colors.Orange,
            Opacity           = 1, // no transparency,
            FogStart          = _fogStart,
            FogFullColorStart = _fogStart + _fogDistance,
            FogColor          = _fogColor
        };
        
        _allFogMaterials.Add(bottomFogMaterial);
        _allFogMaterials.Add(sphereFogMaterial);

        
        var bottomBoxNode = new BoxModelNode(new Vector3(0, -2, 0), new Vector3(200, 4, 100), "BottomBox")
        {
            UseSharedBoxMesh = false, // this will generate the mesh with position and size defined here (and not use a 1x1x1 box mesh that is than transformed to final size - this does not work when using vertex shader with fixed matrices)
            //Transform        = new TranslateTransform(0, 30, 0) // test using world matrix
            Material = bottomFogMaterial
        };

        scene.RootNode.Add(bottomBoxNode);


        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var position = new Vector3(-80 + 30 * x, 10, -30 + 30 * y);
                var fogSphere = new SphereModelNode(position, radius: 10, sphereFogMaterial, $"FogSphere_{x}_{y}");

                scene.RootNode.Add(fogSphere);
            }
        }


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Distance = 350;
            targetPositionCamera.Heading = 60;
            targetPositionCamera.Attitude = -20;
        }
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.BackgroundColor = _fogColor.ToColor4();

        base.OnSceneViewInitialized(sceneView);
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (SceneView != null)
            SceneView.BackgroundColor = Color4.Transparent;

        base.OnDisposed();
    }

    private void UpdateFogMaterial()
    {
        foreach (var fogMaterial in _allFogMaterials)
        {
            fogMaterial.FogStart          = _fogStart;
            fogMaterial.FogFullColorStart = _fogStart + _fogDistance;
        }
    }
    
    private void ChangeFogColor(string? colorText)
    {
        if (!Color3.TryParse(colorText, out _fogColor))
            _fogColor = Color3.White;

        foreach (var fogMaterial in _allFogMaterials)
            fogMaterial.FogColor = _fogColor;

        if (SceneView != null)
            SceneView.BackgroundColor = _fogColor.ToColor4();
    }

    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateSlider(
            minValue: 0,
            maxValue: 500, 
            getValueFunc: () => _fogStart, 
            setValueAction: newValue =>
            {
                _fogStart = newValue;
                UpdateFogMaterial();
            }, 
            width: 100, 
            keyText: "Fog start:", 
            formatShownValueFunc: sliderValue => $"{sliderValue:F0}", 
            keyTextWidth: 80);
        
        ui.CreateSlider(
            minValue: 0, 
            maxValue: 500, 
            getValueFunc: () => _fogDistance, 
            setValueAction: newValue =>
            {
                _fogDistance = newValue;
                UpdateFogMaterial();
            }, 
            width: 100, 
            keyText: "Fog distance:", 
            formatShownValueFunc: sliderValue => $"{sliderValue:F0}", 
            keyTextWidth: 80);

        ui.AddSeparator();

        ui.CreateComboBox(new string[] { "White", "LightCyan", "Black" }, 
            (itemIndex, itemText) => ChangeFogColor(itemText), 
            selectedItemIndex: 0,
            width: 100,
            keyText: "Fog color:",
            keyTextWidth: 80);
    }
}