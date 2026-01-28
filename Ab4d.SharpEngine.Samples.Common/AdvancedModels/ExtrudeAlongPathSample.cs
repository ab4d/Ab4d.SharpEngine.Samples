using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class ExtrudeAlongPathSample : CommonSample
{
    public override string Title => "Extrude a 2D shape along a 3D path";

    public ExtrudeAlongPathSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        // First prepare two 2D shapes:
        var letterTShapePositions = new Vector2[]
        {
            new Vector2(5, 0),
            new Vector2(-5, 0),
            new Vector2(-5, 40),
            new Vector2(-20, 40),
            new Vector2(-20, 50),
            new Vector2(20, 50),
            new Vector2(20, 40),
            new Vector2(5, 40),
        };

        var ellipsePositionList = new List<Vector2>();
        for (int i = 0; i < 360; i += 20)
        {
            ellipsePositionList.Add(new Vector2(MathF.Sin(i / 180.0f * MathF.PI) * 20, MathF.Cos(i / 180.0f * MathF.PI) * 10));
        }

        var ellipsePositions = ellipsePositionList.ToArray();


        // Now define a simple 3D path:
        var extrudePath = new Vector3[]
        {
            new Vector3(0,    0,   0),
            new Vector3(0,    90,  0),
            new Vector3(-20,  110,  0),
            new Vector3(-50,  130, 20),
            new Vector3(-50,  130, 100),
        };


        // Create extruded models:

        var extrudedMesh1 = MeshFactory.CreateExtrudedMeshAlongPath(
            shapePositions: ellipsePositions, 
            extrudePathPositions: extrudePath, 
            shapeYVector3D: new Vector3(0, 0, -1),
            isClosed: true,
            isSmooth: true,
            name: "");

        var meshModelNode1 = new MeshModelNode(extrudedMesh1, StandardMaterials.Green)
        {
            Transform = new TranslateTransform(-150, 0, -50)
        };

        scene.RootNode.Add(meshModelNode1);



        var extrudedMesh2 = MeshFactory.CreateExtrudedMeshAlongPath(
            shapePositions: ellipsePositions, 
            extrudePathPositions: extrudePath, 
            shapeYVector3D: new Vector3(0, 0, -1),
            isClosed: false,
            isSmooth: false,
            name: "");

        // Because this mesh is not closed, we are able to see inside - so set the back material to dim gray.
        var meshModelNode2 = new MeshModelNode(extrudedMesh2, StandardMaterials.Green)
        {
            Transform = new TranslateTransform(0, 0, -50),
            BackMaterial = StandardMaterials.DimGray
        };

        scene.RootNode.Add(meshModelNode2);



        // In the previous 2 samples we provided the shape positions to the CreateExtrudedMeshAlongPath.
        // This method then triangulates the shape in case it was closed.
        // Here we manually triangulate the shape and provide the shapeTriangleIndices to CreateExtrudedMeshAlongPath:
        var triangulator = new Ab4d.SharpEngine.Utilities.Triangulator(letterTShapePositions);

        // NOTE: CreateTriangleIndices can throw FormatException when the positions are not correctly defined (for example if the lines intersect each other).
        var triangleIndices = triangulator.CreateTriangleIndices();

        var extrudedMesh3 = MeshFactory.CreateExtrudedMeshAlongPath(
            shapePositions: letterTShapePositions, 
            shapeTriangleIndices: triangleIndices.ToArray(), 
            extrudePathPositions: extrudePath, 
            shapeYVector3D: new Vector3(0, 0, -1),
            isClosed: true,
            isSmooth: false,
            flipNormals: triangulator.IsClockwise, // If true, then normals are flipped - used when positions are defined in a counter-clockwise order
            name: "");

        // Because this mesh is not closed, we are able to see inside - so set the back material to dim gray.
        var meshModelNode3 = new MeshModelNode(extrudedMesh3, StandardMaterials.Green)
        {
            Transform = new TranslateTransform(150, 0, -50),
        };

        scene.RootNode.Add(meshModelNode3);


        var textBlockFactory = await context.GetTextBlockFactoryAsync();
        textBlockFactory.FontSize = 12;
        textBlockFactory.BackgroundColor = Colors.LightYellow;
        textBlockFactory.BorderThickness = 1;
        textBlockFactory.BorderColor = Colors.DimGray;

        var textNode = textBlockFactory.CreateTextBlock("Ellipse shape\nisClosed: true\nisSmooth: true", new Vector3(-170, 10, 30), textAttitude: 30);
        scene.RootNode.Add(textNode);

        textNode = textBlockFactory.CreateTextBlock("Ellipse shape\nisClosed: false\nisSmooth: false", new Vector3(-20, 10, 30), textAttitude: 30);
        scene.RootNode.Add(textNode);

        textNode = textBlockFactory.CreateTextBlock("T shape\nisClosed: true\nisSmooth: false", new Vector3(130, 10, 30), textAttitude: 30);
        scene.RootNode.Add(textNode);


        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -5, -50),
            Size = new Vector2(600, 300),
            WidthCellsCount = 12,
            HeightCellsCount = 6,
            MajorLineColor = Colors.DimGray,
            MajorLineThickness = 2
        };

        scene.RootNode.Add(wireGridNode);
        

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.TargetPosition = new Vector3(-50, 70, 0);
            targetPositionCamera.Distance = 550;
        }

        scene.SetAmbientLight(0.2f);
    }
}