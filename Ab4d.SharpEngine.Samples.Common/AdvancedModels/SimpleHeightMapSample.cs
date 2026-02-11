using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class SimpleHeightMapSample : CommonSample
{
    public override string Title => "Simple HeightMap of Europe";
    public override string? Subtitle => "Height data and texture are read from png files";

    public SimpleHeightMapSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        // Load height data from image
#if VULKAN
        var heightDateFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/HeightMaps/europe_height.png");
        var heightImageData = BitmapIO.LoadBitmap(heightDateFileName);
        
        // Use this as texture for HeightMapModel
        var heightTextureFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/HeightMaps/europe.png");
        var europeTextureMaterial = new StandardMaterial(heightTextureFileName, BitmapIO);
#else
        if (scene.GpuDevice == null)
            return;

        string heightDateFileName = GetCommonTexturePath("Resources/HeightMaps/europe_height.png");
        var heightImageData = await scene.GpuDevice.CanvasInterop.LoadImageBytesAsync(heightDateFileName);

        var heightTextureFileName = GetCommonTexturePath("Resources/HeightMaps/europe.png");
        var europeGpuImage = await base.GetCommonTextureAsync(heightTextureFileName, scene);
        var europeTextureMaterial = new StandardMaterial(europeGpuImage);
#endif

        var heightData = HeightMapSurfaceNode.CreateHeightDataFromImageData(heightImageData);
        var backgroundMaterial    = StandardMaterials.Gray.SetSpecular(Color3.White, 16);

        // Create height map surface
        var heightMapSurfaceNode = new HeightMapSurfaceNode(centerPosition: new Vector3(0, 0, 0),
                                                            size: new Vector3(528, 20, 508),
                                                            heightData: heightData,
                                                            name: "HeightMapSurface")
        {
            Material     = europeTextureMaterial,
            BackMaterial = backgroundMaterial,
        };


        // Create height map wireframe, and tie its properties to the height map surface
        // Set all available parameters in the constructor, because changing those values later will call UpdateMesh on each change.
        var heightMapWireframeNode = new HeightMapWireframeNode(heightMapSurfaceNode,
                                                                verticalLineFrequency: 10,
                                                                horizontalLineFrequency: 10,
                                                                wireframeOffset: 0.05f, // lift the grid slightly on top of the HeightMap
                                                                name: "HeightMapWireframe")
        {
            // Changing LineColor and LineThickness will not call UpdateMesh
            LineColor = Colors.DimGray,
            LineThickness = 1
        };

        scene.RootNode.Add(heightMapSurfaceNode);
        scene.RootNode.Add(heightMapWireframeNode);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading   = 0;
            targetPositionCamera.Attitude  = -30;
            targetPositionCamera.Distance  = 800;
        }
    }

    protected override void OnCreateLights(Scene scene)
    {
        scene.SetAmbientLight(intensity: 0.5f);

        scene.Lights.Add(new DirectionalLight(new Vector3(1, -0.8f, 0)));

        //base.OnCreateLights(scene);
    }
}