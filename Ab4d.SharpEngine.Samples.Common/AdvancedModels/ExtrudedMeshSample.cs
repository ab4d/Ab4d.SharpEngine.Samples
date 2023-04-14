using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class ExtrudedMeshSample : CommonSample
{
    public override string Title => "Extruded 3D models";
    public override string Subtitle => "Extruded 3D models are created by extruding a 2D shape along a 3D vector";

    private StandardMaterial _specularDarkBlueMaterial = StandardMaterials.DarkBlue.SetSpecular(Color3.White, 16);
    private StandardMaterial _specularRedMaterial = StandardMaterials.IndianRed.SetSpecular(Color3.White, 16);
    private StandardMaterial _specularGreenMaterial = StandardMaterials.ForestGreen.SetSpecular(Color3.White, 16);

    public ExtrudedMeshSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Cube
        var baseShape = new Vector2[]
        {
            new(25, 25),
            new(-25, 25),
            new(-25, -25),
            new(25, -25)
        };

        var mesh = MeshFactory.CreateExtrudedMesh(
            positions: baseShape,
            isSmooth: false,
            modelOffset: new Vector3(0, 0, 0),
            extrudeVector: new Vector3(0, 50, 0),
            closeBottom: true,
            closeTop: true
        );

        var model = new MeshModelNode(mesh, _specularDarkBlueMaterial, "Blue box")
        {
            Transform = new TranslateTransform(-100, 0, 0)
        };

        scene.RootNode.Add(model);


        // 3D parallelogram
        baseShape = new Vector2[]
        {
            new(25, 25),
            new(-25, 25),
            new(-25, -25),
            new(25, -25)
        };

        mesh = MeshFactory.CreateExtrudedMesh(
            positions: baseShape,
            isSmooth: false,
            modelOffset: new Vector3(0, 0, 0),
            extrudeVector: new Vector3(0, 25, 25),
            closeBottom: true,
            closeTop: true
        );

        model = new MeshModelNode(mesh, _specularGreenMaterial, "Green 3D parallelogram")
        {
            Transform = new TranslateTransform(0, 0, 0)
        };

        scene.RootNode.Add(model);


        // 3D parallelogram (ground-plane aligned)
        baseShape = new Vector2[]
        {
            new(25, 25),
            new(-25, 25),
            new(-25, -25),
            new(25, -25)
        };

        mesh = MeshFactory.CreateExtrudedMesh(
            positions: baseShape,
            isSmooth: false,
            modelOffset: new Vector3(0, 0, 0),
            extrudeVector: new Vector3(0, 50, 50),
            shapeYVector: new Vector3(0, 0, 1),  // Force base shape to lie in the "ground" plane.
            textureCoordinatesGenerationType: MeshFactory.ExtrudeTextureCoordinatesGenerationType.Cylindrical,
            closeBottom: true,
            closeTop: true
        );

        model = new MeshModelNode(mesh, _specularRedMaterial, "Red 3D parallelogram (ground-plane aligned)")
        {
            Transform = new TranslateTransform(100, 0, 0)
        };

        scene.RootNode.Add(model);


        // Triangle base
        baseShape = CreateBaseShape(new Vector2(0, 0), 25, 3);

        mesh = MeshFactory.CreateExtrudedMesh(
            positions: baseShape,
            isSmooth: false,
            modelOffset: new Vector3(0, 0, 0),
            extrudeVector: new Vector3(0, 50, 0),
            closeBottom: true,
            closeTop: true
        );

        model = new MeshModelNode(mesh, _specularRedMaterial, "Triangle base")
        {
            Transform = new TranslateTransform(-100, 0, -100)
        };

        scene.RootNode.Add(model);


        // Pentagon base
        baseShape = CreateBaseShape(new Vector2(0, 0), 25, 5);

        mesh = MeshFactory.CreateExtrudedMesh(
            positions: baseShape,
            isSmooth: false,
            modelOffset: new Vector3(0, 0, 0),
            extrudeVector: new Vector3(0, 50, 0),
            closeBottom: true,
            closeTop: true
        );

        model = new MeshModelNode(mesh, _specularGreenMaterial, "Pentagon base")
        {
            Transform = new TranslateTransform(0, 0, -100)
        };

        scene.RootNode.Add(model);


        // Hexagon base
        baseShape = CreateBaseShape(new Vector2(0, 0), 25, 6);

        mesh = MeshFactory.CreateExtrudedMesh(
            positions: baseShape,
            isSmooth: false,
            modelOffset: new Vector3(0, 0, 0),
            extrudeVector: new Vector3(0, 50, 0),
            closeBottom: true,
            closeTop: true
        );

        model = new MeshModelNode(mesh, _specularDarkBlueMaterial, "Hexagon base")
        {
            Transform = new TranslateTransform(100, 0, -100)
        };

        scene.RootNode.Add(model);


        // Pentagon base with texture
        var textureMaterial = new StandardMaterial(@"Resources\Textures\uvchecker2.jpg", BitmapIO);

        baseShape = CreateBaseShape(new Vector2(0, 0), 25, 5);

        mesh = MeshFactory.CreateExtrudedMesh(
            positions: baseShape,
            isSmooth: false,
            modelOffset: new Vector3(0, 0, 0),
            extrudeVector: new Vector3(0, 50, 0),
            closeBottom: true,
            closeTop: true
        );

        model = new MeshModelNode(mesh, textureMaterial, "Pentagon base, textured")
        {
            Transform = new TranslateTransform(200, 0, -100)
        };

        scene.RootNode.Add(model);


        // Approximated cylinder with texture
        baseShape = CreateBaseShape(new Vector2(0, 0), 25, 35);

        mesh = MeshFactory.CreateExtrudedMesh(
            positions: baseShape,
            isSmooth: true,
            modelOffset: new Vector3(0, 0, 0),
            extrudeVector: new Vector3(0, 50, 0),
            closeBottom: true,
            closeTop: true
        );

        model = new MeshModelNode(mesh, textureMaterial, "Almost cylinder, textured")
        {
            Transform = new TranslateTransform(300, 0, -100)
        };

        scene.RootNode.Add(model);


        // Extrusion along path
        baseShape = CreateBaseShape(new Vector2(0, 0), 5, 5);

        var path = new Vector3[]
        {
            new(-300, 0, 0),
            new(-300, 100, 0),
            new(0, 200, 0),
            new(0, 200, -100),
            new(300, 200, -100)
        };

        mesh = MeshFactory.CreateExtrudedMeshAlongPath(
            shapePositions: baseShape,
            extrudePathPositions: path,
            shapeYVector3D: new Vector3(0, 0, 1)
        );

        model = new MeshModelNode(mesh, _specularGreenMaterial, "Extruded path with pentagon base");

        scene.RootNode.Add(model);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -40;
            targetPositionCamera.Attitude = -25;
            targetPositionCamera.Distance = 1000;
            targetPositionCamera.TargetPosition = new Vector3(0, 0, -150);
        }
    }

    private Vector2[] CreateBaseShape(Vector2 center, float radius, int numCorners)
    {
        var corners = new Vector2[numCorners];
        var angleStep = 2 * MathF.PI / numCorners;

        for (var i = 0; i < numCorners; i++)
        {
            var angle = i * angleStep;
            corners[i] = new Vector2(center.X + MathF.Cos(angle) * radius, center.Y + MathF.Sin(angle) * radius);
        }

        return corners;
    }
}