using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class DepthBiasSample : CommonSample
{
    public override string Title => "Line depth-bias ";
    public override string Subtitle => "When rendering 3D lines on top of solid models, then z-fighting artifacts may appear because some parts of the solid models may hide parts of the lines.\nThis can be prevented by setting the line's DepthBias that offset the line so that it is closer to the camera.";

    private float _depthBias = 0.0005f;
    private float _sceneDepth = 1000;
    private float _lineThickness = 1;
    private int _cameraIndex = 0;

    public DepthBiasSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        CreateTestBoxesWithWireframe();
        ChangeCamera(_cameraIndex);
    }
    
    private void CreateTestBoxesWithWireframe()
    {
        if (Scene == null)
            return;

        Scene.RootNode.Clear();

        int xCount = 5;
        int yCount = 5;

        float zValueIncreaseFactor = _sceneDepth / ((xCount + 1) * (yCount + 1));


        for (int x = 0; x <= xCount; x++)
        {
            for (int y = 0; y <= yCount; y++)
            {
                var boxModelNode = new BoxModelNode($"Box_{x}_{y}")
                {
                    Position = new Vector3((x - 2.5f) * 50, (y - 2.5f) * 50, -zValueIncreaseFactor * ((x + 1) * (y + 1) - 1)),
                    Size = new Vector3(30, 30, 30),
                    XSegmentsCount = 2,
                    YSegmentsCount = 2,
                    ZSegmentsCount = 2,
                    UseSharedBoxMesh = false, // create mesh for each box
                    Material = StandardMaterials.Silver
                };

                Scene.RootNode.Add(boxModelNode);

                boxModelNode.Update(); // generate mesh
                var boxMesh = boxModelNode.GetMesh();

                if (boxMesh != null)
                {
                    // Show wireframe positions
                    var wireframePositions = LineUtils.GetWireframeLinePositions(boxMesh, removedDuplicateLines: true); // remove duplicate lines at the edges of triangles

                    var lineMaterial = new LineMaterial(Color3.Black, _lineThickness)
                    {
                        DepthBias = this._depthBias
                    };

                    var wireframeLineNode = new MultiLineNode(wireframePositions, isLineStrip: false, lineMaterial, $"Wireframe_{x}_{y}");
                    Scene.RootNode.Add(wireframeLineNode);
                }
            }
        }
    }

    private void ChangeCamera(int predefinedPositionIndex)
    {
        if (targetPositionCamera == null)
            return;

        if (predefinedPositionIndex == 0)
        {
            targetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
            targetPositionCamera.Heading = 5;
            targetPositionCamera.Attitude = -5;
            targetPositionCamera.Distance = 700;
        }
        else if (predefinedPositionIndex == 1)
        {
            targetPositionCamera.TargetPosition = new Vector3(123, 32, -114);
            targetPositionCamera.Heading = -40;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 700;
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Depth bias:");

        var depthBiasOptions = new float[] { 0, 0.00001f, 0.00005f, 0.0001f, 0.0005f, 0.001f, 0.005f, 0.01f, 0.05f, 0.1f };
        ui.CreateComboBox(depthBiasOptions.Select(o => $"{o:0.#######}").ToArray(), (selectedIndex, selectedText) =>
        {
            _depthBias = depthBiasOptions[selectedIndex];
            CreateTestBoxesWithWireframe();
        }, selectedItemIndex: Array.IndexOf(depthBiasOptions, _depthBias));


        ui.AddSeparator();

        ui.CreateLabel("Scene depth:");

        var sceneDepthOptions = new float[] { 0, 500, 1000, 2000, 5000, 10000 };
        ui.CreateComboBox(sceneDepthOptions.Select(o => o.ToString()).ToArray(), (selectedIndex, selectedText) =>
        {
            _sceneDepth = sceneDepthOptions[selectedIndex];
            CreateTestBoxesWithWireframe();
        }, selectedItemIndex: Array.IndexOf(sceneDepthOptions, _sceneDepth));


        ui.AddSeparator();
        
        ui.CreateLabel("LineThickness:");

        var lineThicknessOptions = new float[] { 0.2f, 0.5f, 1, 2, 3 };
        ui.CreateComboBox(lineThicknessOptions.Select(o => o.ToString()).ToArray(), (selectedIndex, selectedText) =>
        {
            _lineThickness = lineThicknessOptions[selectedIndex];
            CreateTestBoxesWithWireframe();
        }, selectedItemIndex: Array.IndexOf(lineThicknessOptions, _lineThickness));


        ui.AddSeparator();

        ui.CreateButton("  Change camera  ", () =>
        {
            _cameraIndex = (_cameraIndex + 1) % 2;
            ChangeCamera(_cameraIndex);
        });
    }
}