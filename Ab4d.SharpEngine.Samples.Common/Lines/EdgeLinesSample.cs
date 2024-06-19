using Ab4d.SharpEngine.Common;
using System.Numerics;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Lines;


// SharpEngine can generate edge lines from meshes.
// This is done by calculating angles between all triangles and when an angle is bigger than
// a specified EdgeStartAngle then a line is generated.
//
// The recommended way to generate the edge lines is to create an instance of EdgeLinesFactory
// and then call:
// - CreateEdgeLines: returns a List<Vector3> with positions for edge lines
// - AddEdgeLines: adds edge lines to an existing List<Vector3> (can be used to reuse an instance of List<Vector3>)
// - CreateEdgeLineIndices: returns a List<int> that defines the position indices for edge lines (each line has two indexes that define 2 positions from the positions collection).
//
// When calculating edge lines for multiple scene nodes, it is recommended to reuse the instance of EdgeLinesFactory.
// This way the internal lists and arrays are reused.
//
// EdgeLinesFactory provides a few properties that can be adjusted to improve the performance of the edge line generated
// when the mesh is nicely defined. For example is it possible to prevent removing duplicate positions or
// handle edge cases (when some edges are partially covered by some other edges).
// Disabling those features may significantly improve the performance of edge generation,
// but in some meshes it can provide invalid results.
// See online help for more info: https://www.ab4d.com/help/SharpEngine/html/T_Ab4d_SharpEngine_Utilities_EdgeLinesFactory.htm
// 
// To calculate edge lines, it is also possible to use static methods on LineUtils: CreateEdgeLinesForEachSceneNode, AddEdgeLinePositions, ClearEdgeLineIndices.
//
// When the edge lines are generated, the edge line indices are stored into the mesh's EdgeLineIndices channel. This can be read by:
// mesh.GetDataChannel<List<int>>(MeshDataChannelTypes.EdgeLineIndices)
//
// To clear the stored edge lines indices call:
// mesh.RemoveDataChannel(MeshDataChannelTypes.EdgeLineIndices)
// or
// LineUtils.ClearEdgeLineIndices(sceneNode)
//
// 
// NOTE:
// When transformation of a SceneNode is changed, then all the generated edge lines need to be regenerated.
// Because in this case the edge lines indices do not need to be changed, the process of regeneration
// is very fast because only the edge positions need to be transformed by new transformation.
// To regenerate the edge lines call CreateEdgeLines or AddEdgeLines.

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



        // Add edge lines to the _edgeLinePositions
        // See comments at the beginning of this file for more info.

        var edgeLinesFactory = new EdgeLinesFactory();
        edgeLinesFactory.AddEdgeLines(_rootSceneNode, _edgeStartAngle, _edgeLinePositions);


        // Create a new MultiLineNode that will show the edge lines
        var multiLineNode = new MultiLineNode(_edgeLinePositions.ToArray(), isLineStrip: false, _lineMaterial);

        _linesRootNode.Add(multiLineNode);
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