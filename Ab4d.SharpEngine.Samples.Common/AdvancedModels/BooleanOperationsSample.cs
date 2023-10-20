using System.Drawing;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.StandardModels;
using Ab4d.SharpEngine.SceneNodes;
using System.Numerics;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class BooleanOperationsSample : CommonSample
{
    public override string Title => "Boolean Operations";

    private GroupNode? _wireframeGroup;

    public BooleanOperationsSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var boxMesh = MeshFactory.CreateBoxMesh(centerPosition: new Vector3(0, 0, 0), size: new Vector3(100, 100, 100));
        var sphereMesh = MeshFactory.CreateSphereMesh(centerPosition: new Vector3(0, 0, 0), radius: 65, segments: 16);

        _wireframeGroup = new GroupNode("WireframeGroup");
        scene.RootNode.Add(_wireframeGroup);


        // Subtract
        var subtractedMesh = Ab4d.SharpEngine.Utilities.MeshBooleanOperations.Subtract(boxMesh, sphereMesh, processOnlyIntersectingTriangles: false);
        ShowMesh(scene, subtractedMesh, -150);

        // Intersect
        var intersectedMesh = Ab4d.SharpEngine.Utilities.MeshBooleanOperations.Intersect(boxMesh, sphereMesh, processOnlyIntersectingTriangles: false);
        ShowMesh(scene, intersectedMesh, 0);

        //Union
        var unionMesh = Ab4d.SharpEngine.Utilities.MeshBooleanOperations.Union(boxMesh, sphereMesh, processOnlyIntersectingTriangles: false);
        ShowMesh(scene, unionMesh, 150);


        var textBlockFactory = context.GetTextBlockFactory();

        textBlockFactory.BackgroundColor = Colors.LightYellow;
        textBlockFactory.BorderThickness = 1;
        textBlockFactory.BorderColor = Colors.DimGray;

        var textNode = textBlockFactory.CreateTextBlock("Subtract", new Vector3(-150, -45, 100), textAttitude: 30);
        scene.RootNode.Add(textNode);

        textNode = textBlockFactory.CreateTextBlock("Intersect", new Vector3(0, -45, 100), textAttitude: 30);
        scene.RootNode.Add(textNode);

        textNode = textBlockFactory.CreateTextBlock("Union", new Vector3(150, -45, 100), textAttitude: 30);
        scene.RootNode.Add(textNode);


        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -51, 0),
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
            targetPositionCamera.Distance = 700;
        }
    }

    private void ShowMesh(Scene scene, StandardMesh? mesh, float xOffset)
    {
        if (mesh == null)
            return;

        var meshModelNode = new MeshModelNode(mesh)
        {
            Material = StandardMaterials.Gold,
            BackMaterial = StandardMaterials.Red, // Add BackMaterial to check if the models is cut to see the inside
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

    private void UpdateVisibleLines(int index)
    {
        if (_wireframeGroup != null)
            _wireframeGroup.Visibility = index == 1 ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateRadioButtons(new string[] { "Solid model", "Solid model with wireframe" },
                              (index, selectedText) => UpdateVisibleLines(index),
                              1);
    }
}