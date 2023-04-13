using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class TubesSample : CommonSample
{
    public override string Title => "3D tubes";

    private StandardMaterial _specularRedMaterial = StandardMaterials.IndianRed.SetSpecular(Color3.White, 16);
    private StandardMaterial _specularGreenMaterial = StandardMaterials.ForestGreen.SetSpecular(Color3.White, 16);

    public TubesSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Low-segment tubes (to show that segments are correctly handled)
        CreateTubeMeshes(scene: scene, segments: 3, startAngle: 0,  endAngle: 360, zOffset: 300, material: _specularRedMaterial);
        CreateTubeMeshes(scene: scene, segments: 3, startAngle: 45, endAngle: 225, zOffset: 150, material: _specularRedMaterial);

        // High-segment tubes
        CreateTubeMeshes(scene: scene, segments: 30, startAngle: 0,  endAngle: 360, zOffset: -150, material: _specularGreenMaterial);
        CreateTubeMeshes(scene: scene, segments: 30, startAngle: 45, endAngle: 225, zOffset: -300, material: _specularGreenMaterial);

        if (BitmapIO.IsFileFormatImportSupported("png"))
        {
            var textureMaterial = new StandardMaterial(@"Resources\Textures\uvchecker2.jpg", BitmapIO);

            // Low-segment tubes (to show that segments are correctly handled)
            CreateTubeMeshes(scene: scene, segments: 3, startAngle: 0,  endAngle: 360, zOffset: 600, material: textureMaterial);
            CreateTubeMeshes(scene: scene, segments: 3, startAngle: 45, endAngle: 225, zOffset: 450, material: textureMaterial);

            // High-segment tubes
            CreateTubeMeshes(scene: scene, segments: 30, startAngle: 0,  endAngle: 360, zOffset: -450, material: textureMaterial);
            CreateTubeMeshes(scene: scene, segments: 30, startAngle: 45, endAngle: 225, zOffset: -600, material: textureMaterial);
        }

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -120;
            targetPositionCamera.Attitude = -40;
            targetPositionCamera.Distance = 1800;
        }
    }

    protected override void OnCreateLights(Scene scene)
    {
        scene.Lights.Clear();

        // Add lights
        scene.Lights.Add(new AmbientLight(new Color3(0.3f, 0.3f, 0.3f)));

        var directionalLight = new DirectionalLight(new Vector3(-1, -0.3f, 0));
        scene.Lights.Add(directionalLight);

        scene.Lights.Add(new PointLight(new Vector3(500, 200, 100), range: 10000));

        //base.OnCreateLights(scene);
    }


    private void CreateTubeMeshes(Scene scene, int segments, float startAngle, float endAngle, float zOffset, StandardMaterial material)
    {
        var circleDescription = startAngle == 0 && endAngle == 360 ? "full circle" : "partial circle";

        // Regular-case tube
        var node = new TubeModelNode(
            bottomCenterPosition: new Vector3(0, 0, 0),
            heightDirection: new Vector3(0, 1, 0),
            bottomOuterRadius: 30,
            bottomInnerRadius: 35,
            topOuterRadius: 45,
            topInnerRadius: 40,
            height: 200,
            segments: segments,
            startAngle: startAngle,
            endAngle: endAngle,
            material: material,
            name: $"{segments}-segment tube, regular, {circleDescription}"
        )
        {
            Transform = new TranslateTransform(x: -150, y: 0, z: zOffset)
        };

        scene.RootNode.Add(node);


        // Case with both inner radii being zero -> closed lathe
        node = new TubeModelNode(
            bottomCenterPosition: new Vector3(0, 0, 0),
            heightDirection: new Vector3(0, 1, 0),
            bottomOuterRadius: 30,
            bottomInnerRadius: 0,
            topOuterRadius: 45,
            topInnerRadius: 0,
            height: 200,
            segments: segments,
            startAngle: startAngle,
            endAngle: endAngle,
            material: material,
            name: $"{segments}-segment tube, closed lathe, {circleDescription}")
        {
            Transform = new TranslateTransform(x: 0, y: 0, z: zOffset)
        };

        scene.RootNode.Add(node);


        // Case with height being zero -> flat shape
        node = new TubeModelNode(
            bottomCenterPosition: new Vector3(0, 0, 0),
            heightDirection: new Vector3(0, 1, 0),
            bottomOuterRadius: 45,
            bottomInnerRadius: 40,
            topOuterRadius: 45,
            topInnerRadius: 40,
            height: 0,
            segments: segments,
            startAngle: startAngle,
            endAngle: endAngle,
            material: material,
            name: $"{segments}-segment tube, zero height, {circleDescription}")
        {
            Transform = new TranslateTransform(x: 150, y: 0, z: zOffset),
            BackMaterial = material
        };

        scene.RootNode.Add(node);
    }
}