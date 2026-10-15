using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class AdvancedBooleanOperationsSample : CommonSample
{
    public override string Title => "Advanced Boolean operations processing options";

    private GroupNode? _wireframeGroup;

    private bool _generateInnerTriangles = true;

    private TextBlockFactory? _textBlockFactory;
    private StandardMesh? _subtractedMesh1;
    private StandardMesh? _subtractedMesh2;

    public AdvancedBooleanOperationsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        CreateTestScene(scene);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 20;
            targetPositionCamera.Attitude = -35;
            targetPositionCamera.Distance = 700;
        }


        _textBlockFactory = await context.GetTextBlockFactoryAsync();
        _textBlockFactory.FontSize = 10;
        _textBlockFactory.BackgroundColor = Colors.LightYellow;
        _textBlockFactory.BorderThickness = 1;
        _textBlockFactory.BorderColor = Colors.DimGray;

        if (this.IsDisposed) // return if the test was already changed while waiting for async tasks
            return;

        AddTextDescriptions(scene);
    }

    private void CreateTestScene(Scene scene)
    { 
        _wireframeGroup = new GroupNode("WireframeGroup");
        scene.RootNode.Add(_wireframeGroup);


        var boxMesh = MeshFactory.CreateBoxMesh(centerPosition: new Vector3(0, 0, 0), size: new Vector3(200, 10, 200), xSegments: 10, ySegments: 1, zSegments: 10);

        // NOTE:
        // When doing the boolean operations it is very good to keep the meshes as simple as possible.
        // Here we use quite complex meshes because segments parameter is set to 30. This better demonstrates the use of processOnlyIntersectingTriangles parameter.
        // For a real world scenarios I would recommend to use lower segment values.
        var cylinder1Mesh = MeshFactory.CreateCylinderMesh(new Vector3(-50, -50, 90), radius: 8, height: 100, segments: 30, isSmooth: true);
        var cylinder2Mesh = MeshFactory.CreateCylinderMesh(new Vector3(-20, -50, 90), radius: 8, height: 100, segments: 30, isSmooth: true);

        // It is recommended to combine the cylinder meshes and then do one subtraction.
        var combinedMesh = Utilities.MeshUtils.CombineMeshes(cylinder1Mesh, cylinder2Mesh);


        // Process mesh subtraction only on triangles that intersect the combinedMesh.
        // Because in our case the combinedMesh is much smaller then boxMesh, this produces significantly less triangles.
        //
        // But when you know that most of the triangles in the meshes would intersect, then
        // it is worth setting processOnlyIntersectingTriangles to false to skip getting intersecting triangles.

        _subtractedMesh1 = Ab4d.SharpEngine.Utilities.MeshBooleanOperations.Subtract(boxMesh, combinedMesh, processOnlyIntersectingTriangles: true, _generateInnerTriangles);
        ShowMesh(scene, _subtractedMesh1, -120);

        _subtractedMesh2 = Ab4d.SharpEngine.Utilities.MeshBooleanOperations.Subtract(boxMesh, combinedMesh, processOnlyIntersectingTriangles: false, _generateInnerTriangles);
        ShowMesh(scene, _subtractedMesh2, 120);


        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -15, 0),
            Size = new Vector2(600, 280),
            WidthCellsCount = 42,
            HeightCellsCount = 24,
            MajorLineColor = Colors.DimGray,
            MajorLineThickness = 1
        };

        scene.RootNode.Add(wireGridNode);


        var planeModelNode = new PlaneModelNode()
        {
            Position = new Vector3(0, -15.1f, 0),
            PositionType = PositionTypes.Center,
            Size = new Vector2(600, 280),
            Material = StandardMaterials.LightGray,
            BackMaterial = StandardMaterials.LightGray,
        };

        scene.RootNode.Add(planeModelNode);


        AddTextDescriptions(scene);
    }

    private void AddTextDescriptions(Scene scene)
    {
        if (_textBlockFactory == null || _subtractedMesh1 == null || _subtractedMesh2 == null) 
            return;

        var textNode = _textBlockFactory.CreateTextBlock($"processOnlyIntersectingTriangles: true\r\nFinal triangles count: {_subtractedMesh1.TriangleIndices!.Length / 3}",
                                                         new Vector3(-120, 0, 150),
                                                         textAttitude: 30);
        scene.RootNode.Add(textNode);

        textNode = _textBlockFactory.CreateTextBlock($"processOnlyIntersectingTriangles: false\r\nFinal triangles count: {_subtractedMesh2.TriangleIndices!.Length / 3}",
                                                     new Vector3(120, 0, 150),
                                                     textAttitude: 30);
        scene.RootNode.Add(textNode);
    }

    private void ShowMesh(Scene scene, StandardMesh? mesh, float xOffset)
    {
        if (mesh == null)
            return;

        var meshModelNode = new MeshModelNode(mesh)
        {
            Material = StandardMaterials.Gold,
            BackMaterial = StandardMaterials.Black,
            Transform = new TranslateTransform(x: xOffset)
        };

        scene.RootNode.Add(meshModelNode);


        if (_wireframeGroup != null)
        {
            var wireframePositions = LineUtils.GetWireframeLinePositions(mesh, removedDuplicateLines: false); // do not remove duplicate lines because this may take long time when the mesh complex

            var lineMaterial = new LineMaterial(Color3.Black, lineThickness: 0.7f)
            {
                DepthBias = 0.002f
            };

            var wireframeLineNode = new MultiLineNode(wireframePositions, isLineStrip: false, lineMaterial, "WireframeLines")
            {
                Transform = meshModelNode.Transform,
            };

            _wireframeGroup.Add(wireframeLineNode);
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("GenerateInnerTriangles", true, isChecked =>
            {
                _generateInnerTriangles = isChecked;

                if (Scene != null)
                {
                    Scene.RootNode.Clear();
                    CreateTestScene(Scene);
                }
            }).SetToolTip("When checked, then subtraction also generates the inner triangles. This closes the hole that is created by subtraction.");
    }
}