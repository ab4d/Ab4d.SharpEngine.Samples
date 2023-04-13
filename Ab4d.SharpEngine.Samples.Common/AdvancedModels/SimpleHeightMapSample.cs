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

    protected override void OnCreateScene(Scene scene)
    {
        // Load height data from image
        var heightImageData = BitmapIO.LoadBitmap("Resources/HeightMaps/europe_height.png");
        var heightData = HeightMapSurfaceNode.CreateHeightDataFromImageData(heightImageData);

        // Use this as texture for HeightMapModel
        var europeTextureMaterial = new StandardMaterial("Resources/HeightMaps/europe.png", BitmapIO);
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
        var heightMapWireframeNode = new HeightMapWireframeNode(heightMapSurfaceNode, name: "HeightMapWireframe")
        {
            VerticalLineFrequency   = 10,
            HorizontalLineFrequency = 10,
            LineColor               = Colors.DimGray,
            LineThickness           = 1
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
        scene.Lights.Add(new AmbientLight(0.5f));

        scene.Lights.Add(new DirectionalLight(new Vector3(1, -0.8f, 0)));

        //base.OnCreateLights(scene);
    }
}