using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

// This sample shows how to create an advanced custom effect with custom vertex and fragment shaders.
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

public sealed class VertexColorOverlayEffectSample : CommonSample
{
    public override string Title => "Advanced custom effect";
    public override string Subtitle => "This sample shows how to create an advanced custom effect with custom vertex and fragment shaders.\nVertexColorPlusEffect and VertexColorPlusMaterial source code is in the Advanced folder. Shaders source code is in the Resources/Shaders folder.";

    private List<VertexColorPlusMaterial> _allMaterials = new();

    public VertexColorOverlayEffectSample(ICommonSamplesContext context)
        : base(context)
    {
        // First create an instance of AssemblyShaderBytecodeProvider.
        // This will allow using ShadersManager to cache and get the shaders from the assembly's EmbeddedResources.
        var resourceAssembly = GetType().Assembly;
        var assemblyShaderBytecodeProvider = new AssemblyShaderBytecodeProvider(resourceAssembly, resourceRootName: resourceAssembly.GetName().Name + ".Resources.Shaders.spv.");

        Ab4d.SharpEngine.Utilities.ShadersManager.RegisterShaderResourceStatic(assemblyShaderBytecodeProvider);
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Uncomment to test rendering a StandardMaterial:
        var material = new VertexColorPlusMaterial([
            new Color4(1, 0, 0, .5f),
            new Color4(0, 1, 0, .5f),
            new Color4(0, 0, 1, .5f),
            new Color4(0, 0, 0, .5f),
        ]);
        material.LoadDiffuseTexture("Resources/Textures/sharp-engine-logo.png");
        var planeModelNode = new PlaneModelNode(new Vector3(0, 0, 0), new Vector2(60, 40), Vector3.UnitY, Vector3.UnitZ, "ThePlane")
        {
            Material = material
        };

        scene.RootNode.Add(planeModelNode);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Distance = 350;
            targetPositionCamera.Heading = 60;
            targetPositionCamera.Attitude = -20;
        }
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (SceneView != null)
            SceneView.BackgroundColor = Color4.Transparent;

        base.OnDisposed();
    }
}
