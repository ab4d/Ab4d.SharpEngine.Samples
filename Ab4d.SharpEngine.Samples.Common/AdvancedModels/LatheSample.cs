using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class LatheSample : CommonSample
{
    public override string Title => "Lathe 3D objects";
    public override string Subtitle => "Lathe 3D objects are objects where a 2D shape is rotated around a vertical axis";

    private StandardMaterial _specularDarkBlueMaterial = StandardMaterials.DarkBlue.SetSpecular(Color3.White, 16);
    private StandardMaterial _specularRedMaterial = StandardMaterials.IndianRed.SetSpecular(Color3.White, 16);
    private StandardMaterial _specularGreenMaterial = StandardMaterials.ForestGreen.SetSpecular(Color3.White, 16);

    private const int SegmentsCount = 35;

    public LatheSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        CreateTestMeshes(scene, zOffset: 0,    circlePortion: 100);
        CreateTestMeshes(scene, zOffset: -150, circlePortion: 75);
        CreateTestMeshes(scene, zOffset: -300, circlePortion: 50);
        CreateTestMeshes(scene, zOffset: -450, circlePortion: 25);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -140;
            targetPositionCamera.Attitude = -25;
            targetPositionCamera.Distance = 1350;
            targetPositionCamera.TargetPosition = new Vector3(0, 50, -130);
        }
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

    private void CreateTestMeshes(Scene scene, float zOffset = 0, float circlePortion = 100)
    {
        var startAngle = 0;
        var endAngle = 360 * circlePortion / 100.0f;

        // Simple diamond
        var sections = new[]
        {
                new MeshFactory.LatheSection(0.5f, 50, true),
            };

        var mesh = MeshFactory.CreateLatheMesh(
            startPosition: new Vector3(0, 0, 0),
            endPosition: new Vector3(0, 150, 0),
            sections: sections,
            segments: SegmentsCount,
            isStartPositionClosed: true,
            isEndPositionClosed: true,
            generateTextureCoordinates: true,
            startAngle: startAngle,
            endAngle: endAngle,
            isMeshClosed: true
        );
        var model = new MeshModelNode(mesh, _specularDarkBlueMaterial, $"Blue diamond ({circlePortion}%)")
        {
            Transform = new TranslateTransform(-375, 0, zOffset)
        };
        scene.RootNode.Add(model);


        // Diamond with flat side
        sections = new[]
        {
                new MeshFactory.LatheSection(0.25f, 50, true),
                new MeshFactory.LatheSection(0.75f, 50, true),
            };

        mesh = MeshFactory.CreateLatheMesh(
            startPosition: new Vector3(0, 0, 0),
            endPosition: new Vector3(0, 150, 0),
            sections: sections,
            segments: SegmentsCount,
            isStartPositionClosed: true,
            isEndPositionClosed: true,
            generateTextureCoordinates: true,
            startAngle: startAngle,
            endAngle: endAngle,
            isMeshClosed: true
        );
        model = new MeshModelNode(mesh, _specularRedMaterial, $"Red diamond with flat side ({circlePortion}%)")
        {
            Transform = new TranslateTransform(-225, 0, zOffset)
        };
        scene.RootNode.Add(model);

        // Pine tree
        sections = new[]
        {
                new MeshFactory.LatheSection(0.00f, 25, true),
                new MeshFactory.LatheSection(0.25f, 25, true),
                new MeshFactory.LatheSection(0.25f, 55, true),
                new MeshFactory.LatheSection(0.25f, 65, true),
                new MeshFactory.LatheSection(0.50f, 35, true),
                new MeshFactory.LatheSection(0.50f, 45, true),
                new MeshFactory.LatheSection(0.75f, 15, true),
                new MeshFactory.LatheSection(0.75f, 25, true),
                new MeshFactory.LatheSection(1.0f, 0, true), // Redundant due to end being closed
            };

        mesh = MeshFactory.CreateLatheMesh(
            startPosition: new Vector3(0, 0, 0),
            endPosition: new Vector3(0, 300, 0),
            sections: sections,
            segments: SegmentsCount,
            isStartPositionClosed: true,
            isEndPositionClosed: true,
            generateTextureCoordinates: true,
            startAngle: startAngle,
            endAngle: endAngle,
            isMeshClosed: true
        );
        model = new MeshModelNode(mesh, _specularGreenMaterial, $"Green pine tree ({circlePortion}%)")
        {
            Transform = new TranslateTransform(-75, 0, zOffset)
        };
        scene.RootNode.Add(model);

        // These meshes cannot be rendered in partial way without looking weird...

        // Glass
        sections = new[]
        {
                new MeshFactory.LatheSection(0.00f, 50, true),
                new MeshFactory.LatheSection(1.00f, 50, true),
                new MeshFactory.LatheSection(1.00f, 40, true),
                new MeshFactory.LatheSection(0.10f, 40, true),
                new MeshFactory.LatheSection(0.10f, 0, true), // We need to manually close the bottom - FIXME: creates an artifact!
            };

        mesh = MeshFactory.CreateLatheMesh(
            startPosition: new Vector3(0, 0, 0),
            endPosition: new Vector3(0, 150, 0),
            sections: sections,
            segments: SegmentsCount,
            isStartPositionClosed: true,
            isEndPositionClosed: false,
            generateTextureCoordinates: true,
            startAngle: startAngle,
            endAngle: endAngle,
            isMeshClosed: false
        );
        model = new MeshModelNode(mesh, _specularGreenMaterial, $"Green glass ({circlePortion}%)")
        {
            Transform = new TranslateTransform(75, 0, zOffset),
            BackMaterial = StandardMaterials.Red
        };
        scene.RootNode.Add(model);

        // Cylinder
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (circlePortion == 100)
        {
            sections = new[]
            {
                    new MeshFactory.LatheSection(0.00f, 48, true),
                    new MeshFactory.LatheSection(0.00f, 50, true),
                    new MeshFactory.LatheSection(1.00f, 50, true),
                    new MeshFactory.LatheSection(1.00f, 48, true),
                    new MeshFactory.LatheSection(0.00f, 48, true),
                };

            mesh = MeshFactory.CreateLatheMesh(
                startPosition: new Vector3(0, 0, 0),
                endPosition: new Vector3(0, 150, 0),
                sections: sections,
                segments: SegmentsCount,
                isStartPositionClosed: false,
                isEndPositionClosed: false,
                generateTextureCoordinates: true,
                startAngle: startAngle,
                endAngle: endAngle,
                isMeshClosed: true
            );
            model = new MeshModelNode(mesh, _specularDarkBlueMaterial, $"Blue empty cylinder ({circlePortion}%)")
            {
                Transform = new TranslateTransform(225, 0, zOffset)
            };
            scene.RootNode.Add(model);
        }

        // Chalice
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (circlePortion == 100)
        {
            sections = new[]
            {
                    new MeshFactory.LatheSection(0.00f, 40, true),
                    new MeshFactory.LatheSection(0.05f, 40, true),
                    new MeshFactory.LatheSection(0.20f, 8, true),
                    new MeshFactory.LatheSection(0.25f, 5, true),
                    new MeshFactory.LatheSection(0.38f, 5, true),
                    new MeshFactory.LatheSection(0.40f, 10, true),
                    new MeshFactory.LatheSection(0.42f, 5, true),
                    new MeshFactory.LatheSection(0.50f, 5, true),
                    new MeshFactory.LatheSection(0.75f, 35, true),
                    new MeshFactory.LatheSection(1.00f, 50, true),
                    new MeshFactory.LatheSection(1.00f, 48, true),
                    new MeshFactory.LatheSection(0.75f, 33, true),
                    new MeshFactory.LatheSection(0.50f, 0, true),
                };

            mesh = MeshFactory.CreateLatheMesh(
                startPosition: new Vector3(0, 0, 0),
                endPosition: new Vector3(0, 150, 0),
                sections: sections,
                segments: SegmentsCount,
                isStartPositionClosed: true,
                isEndPositionClosed: true,
                generateTextureCoordinates: true,
                startAngle: startAngle,
                endAngle: endAngle,
                isMeshClosed: true
            );
            model = new MeshModelNode(mesh, _specularRedMaterial, $"Red chalice ({circlePortion}%)")
            {
                Transform = new TranslateTransform(375, 0, zOffset)
            };
            scene.RootNode.Add(model);
        }
    }
}