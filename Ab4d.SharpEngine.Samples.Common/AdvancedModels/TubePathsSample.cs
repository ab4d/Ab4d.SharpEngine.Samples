using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class TubePathsSample : CommonSample
{
    public override string Title => "3D Tube paths";
    public override string Subtitle => "TubePathModelNode and TubeLineModelNode can be used to create a 3D tube along a path or line.";

    private StandardMaterial _specularDarkBlueMaterial = StandardMaterials.DarkBlue.SetSpecular(Color3.White, 16);
    private StandardMaterial _specularRedMaterial = StandardMaterials.IndianRed.SetSpecular(Color3.White, 16);
    private StandardMaterial _specularGreenMaterial = StandardMaterials.ForestGreen.SetSpecular(Color3.White, 16);

    public TubePathsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        CreateTubePaths(scene: scene, segments: 3,  zOffset: -150, material: _specularRedMaterial, generateTextureCoordinates: false);
        CreateTubePaths(scene: scene, segments: 30, zOffset: -50,  material: _specularGreenMaterial, generateTextureCoordinates: false);

        var textureMaterial = new StandardMaterial(@"Resources\Textures\uvchecker2.jpg", BitmapIO);
        CreateTubePaths(scene: scene, segments: 3,  zOffset: 50,  material: textureMaterial, generateTextureCoordinates: true);
        CreateTubePaths(scene: scene, segments: 30, zOffset: 150, material: textureMaterial, generateTextureCoordinates: true);


        // Lines
        var node = new TubeLineModelNode(
            startPosition: new Vector3(-150, -50, -300),
            endPosition: new Vector3(-150, -50, 300),
            radius: 2,
            segments: 30,
            generateTextureCoordinates: true,
            isStartPositionClosed: true,
            isEndPositionClosed: true,
            material: _specularRedMaterial,
            name: "3D line, red");

        scene.RootNode.Add(node);


        node = new TubeLineModelNode(
            startPosition: new Vector3(-50, -50, -300),
            endPosition: new Vector3(-50, -50, 300),
            radius: 2,
            segments: 30,
            generateTextureCoordinates: true,
            isStartPositionClosed: true,
            isEndPositionClosed: true,
            material: _specularGreenMaterial,
            name: "3D line, green");

        scene.RootNode.Add(node);


        node = new TubeLineModelNode(
            startPosition: new Vector3(50, -50, -300),
            endPosition: new Vector3(50, -50, 300),
            radius: 2,
            segments: 30,
            generateTextureCoordinates: true,
            isStartPositionClosed: true,
            isEndPositionClosed: true,
            material: _specularDarkBlueMaterial,
            name: "3D line, blue");

        scene.RootNode.Add(node);


        node = new TubeLineModelNode(
            startPosition: new Vector3(150, -50, -300),
            endPosition: new Vector3(150, -50, 300),
            radius: 2,
            segments: 30,
            generateTextureCoordinates: true,
            isStartPositionClosed: true,
            isEndPositionClosed: true,
            material: textureMaterial,
            name: "3D line, textured");

        scene.RootNode.Add(node);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -50;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 900;
            targetPositionCamera.TargetPosition = new Vector3(70, 0, 0);
        }
    }

    void CreateTubePaths(Scene scene, int segments, float zOffset, StandardMaterial material, bool generateTextureCoordinates)
    {
        // Basic cylinder (one-segment path, non-closed)
        var pathPoints = new Vector3[]
        {
                new (0, 0, 0),
                new (0, 100, 0)
        };

        var node = new TubePathModelNode(
            pathPoints,
            20,
            true,
            false,
            segments,
            null,
            generateTextureCoordinates,
            material,
            $"Single-segment path, non-closed ({segments} side segments)")
        {
            Transform = new TranslateTransform(-200, 0, zOffset)
        };

        scene.RootNode.Add(node);


        // Three-segment path, non-closed
        pathPoints = new Vector3[]
        {
                new (0, 0, 0),
                new (30, 50, 0),
                new (30, 100, 0),
                new (0, 150, 0)
        };

        node = new TubePathModelNode(
            pathPoints,
            20,
            true,
            false,
            segments,
            null,
            generateTextureCoordinates,
            material,
            $"Three-segment path, non-closed ({segments} side segments)")
        {
            Transform = new TranslateTransform(-100, 0, zOffset)
        };

        scene.RootNode.Add(node);


        // L-shape, non-closed
        pathPoints = new Vector3[]
        {
                new (0, 0, 0),
                new (0, 100, 0),
                new (50, 100, 0),
        };
        node = new TubePathModelNode(
            pathPoints,
            20,
            true,
            false,
            segments,
            null,
            generateTextureCoordinates,
            material,
            $"L-shape path, non-closed ({segments} side segments)")
        {
            Transform = new TranslateTransform(100, 0, zOffset)
        };

        scene.RootNode.Add(node);

        // L-shape, closed
        pathPoints = new Vector3[]
        {
                new (0, 0, 0),
                new (0, 100, 0),
                new (50, 100, 0),
        };

        node = new TubePathModelNode(
            pathPoints,
            20,
            true,
            true,
            segments,
            null,
            generateTextureCoordinates,
            material,
            $"L-shape path, closed ({segments} side segments)")
        {
            Transform = new TranslateTransform(200, 0, zOffset)
        };

        scene.RootNode.Add(node);


        // Path
        pathPoints = new Vector3[]
        {
                new (250, 0, 45),
                new (-250, 0, 45),
                new (-250, 100, 45),
                new (0, 150, 45),
                new (0, 100, -45),
                new (250, 100, -45),
        };

        node = new TubePathModelNode(
            pathPoints,
            5,
            true,
            false,
            segments,
            null,
            generateTextureCoordinates,
            material,
            $"Longer path ({segments} side segments)")
        {
            Transform = new TranslateTransform(0, 0, zOffset)
        };

        scene.RootNode.Add(node);
    }

    protected override void OnCreateLights(Scene scene)
    {
        scene.Lights.Clear();

        // Add lights
        scene.SetAmbientLight(intensity: 0.3f);

        var directionalLight = new DirectionalLight(new Vector3(-1, -0.3f, 0));
        scene.Lights.Add(directionalLight);

        scene.Lights.Add(new PointLight(new Vector3(500, 200, 100), range: 10000));

        //base.OnCreateLights(scene);
    }
}