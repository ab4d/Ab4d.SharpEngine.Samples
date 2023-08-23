using Ab4d.SharpEngine.Common;
using System.Numerics;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class EdgeLinesSample : CommonSample
{
    public override string Title => "Generating edge lines based on the angle between triangles";

    private float _edgeStartAngle = 25;
    private float _lineThickness = 1;

    private LineMaterial? _lineMaterial;
    private GroupNode? _linesRootNode;

    private GroupNode? _rootSceneNode;

    private List<Vector3>? _edgeLinePositions;
    private TranslateTransform? _manTranslateTransform;

    public EdgeLinesSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _rootSceneNode = TestScenes.GetTestScene(TestScenes.StandardTestScenes.HouseWithTrees, new Vector3(0, 0, 0), PositionTypes.Bottom | PositionTypes.Center, finalSize: new Vector3(800, 800, 800));

        // Obj files do not support hierarchically organized objects
        // So we need to manually extract the Man01, Man02 and Man03 objects and put them under a new group that can be moved around
        var manNodes = _rootSceneNode.GetAllChildren("Man*");

        foreach (var manNode in manNodes)
            _rootSceneNode.Remove(manNode);

        var manGroupNode = new GroupNode("ManGroup");

        foreach (var manNode in manNodes)
            manGroupNode.Add(manNode);

        _manTranslateTransform = new TranslateTransform();
        manGroupNode.Transform = _manTranslateTransform;

        _rootSceneNode.Add(manGroupNode);

        
        scene.RootNode.Add(_rootSceneNode);

        _linesRootNode = new GroupNode("LinesRootNode");
        scene.RootNode.Add(_linesRootNode);

        UpdateEdgeLines();

        scene.SetAmbientLight(0.4f);
    }

    private void UpdateEdgeLines()
    {
        if (_linesRootNode == null || _rootSceneNode == null)
            return;

        _linesRootNode.Clear();

        _lineMaterial ??= new LineMaterial(Colors.Black)
        {
            LineThickness = _lineThickness,
            DepthBias = 0.005f // rise the lines above the 3D object
        };

        // Ensure that was have the list for edge line positions
        // If we are updating the positions, then we can reuse the list (just clear it before reusing the backing array)
        if (_edgeLinePositions == null)
            _edgeLinePositions = new List<Vector3>();
        else
            _edgeLinePositions.Clear();

        // Add edge lines positions to the _edgeLinePositions list.
        //
        // We will add edge lines from all child scene nodes to the same list.
        // If we know that some parts may be changed, it is good to have their edge lines
        // in a separate list so we will only need to update that when the object is changed (this is not done here).
        //
        // Behind the scenes, the AddEdgeLinePositions will ensure that the edge lines indices
        // are generated and stored to the mesh's EdgeLineIndices channel.
        // Generation of edge line indices is done by calling the ClearEdgeLineIndices method (this can be also done manually).
        // This can be quite slow because the mesh needs to be analyzed to find sibling triangles.
        // After the EdgeLineIndices are generated, then the AddEdgeLinePositions just adds the positions
        // (that are transformed by the SceneNode's transformation) to the _edgeLinePositions list.
        // 
        // So after changing the transformation of any object in the _rootSceneNode, the AddEdgeLinePositions
        // needs to be called again. This is quite fast operation because it only reads the positions,
        // transforms them and adds them to list.
        LineUtils.AddEdgeLinePositions(_rootSceneNode, _edgeStartAngle, _edgeLinePositions);

        // Create a new MultiLineNode that will show the edge lines
        var multiLineNode = new MultiLineNode(_edgeLinePositions.ToArray(), isLineStrip: false, _lineMaterial);

        _linesRootNode.Add(multiLineNode);


        // We could also call CreateEdgeLinesForEachSceneNode
        // This would add MultiLineNode for each SceneNode with edges
        //EdgeLinesFactory.CreateEdgeLinesForEachSceneNode(_rootSceneNode, _edgeStartAngle, _lineMaterial, _linesRootNode);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateSlider(0, 90, () => _edgeStartAngle, delegate (float newValue)
        {
            _edgeStartAngle = newValue;
            if (_rootSceneNode != null)
            {
                // After changing the angle, we will need to calculate the edge lines again.
                LineUtils.ClearEdgeLineIndices(_rootSceneNode);
                UpdateEdgeLines();
            }
        }, 100, false, "EdgeStartAngle", 100, sliderValue => sliderValue.ToString("F0"));
        
        ui.CreateSlider(0.1f, 2f, () => _lineThickness, delegate (float newValue)
        {
            _lineThickness = newValue;
            if (_lineMaterial != null)
                _lineMaterial.LineThickness = newValue;
        }, 100, false, "LineThickness", 100, sliderValue => sliderValue.ToString("F2"));

        ui.CreateButton("Change transform", () =>
        {
            if (_manTranslateTransform != null)
            {
                _manTranslateTransform.Z -= 2;
                
                // After changing the transformation, we need to update the positions of the edge lines
                // In this case we do not need to calculate the edge lines again (this is quite expensive)
                // and we only need to get the newly transformed positions.
                UpdateEdgeLines(); 
            }
        });
    }
}