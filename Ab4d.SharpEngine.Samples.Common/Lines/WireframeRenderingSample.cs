using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

// This sample demonstrates the following techniques of rendering wireframe lines:
//
// 1) Creating an array of wireframe positions by calling LineUtils.GetWireframeLinePositions method
//    + best performance because all lines with the same color are rendered with one draw call
//    + it is possible to remove duplicate lines (duplicate lines would be rendered over sharp edges that have duplicate positions; note that removing duplicate lines can take long time for complex models)
//    + easy to control which objects and line positions are rendered
//    - requires additional memory for line positions and MultiLineNode object
//    - longer initialize time to collect line positions and remove duplicate lines
//
// 2) Create new MeshModelNode with the same mesh but with LineMaterial. This will render the object with ThickLineEffect and will render wireframe instead of solid objects.
//    + no additional line positions arrays and mesh buffers are required
//    + easy to control which objects are rendered (but cannot to control individual line positions)
//    - worse performance because of more draw calls - each object is rendered with its own draw call even if line color is the same; no duplicate lines removal

// 3) Creating a new RenderObjectsRenderingStep that will use WireframeRenderingEffectTechnique (set to OverrideEffectTechnique) to render all objects
//    + no additional line positions arrays and MeshModelNodes are required
//    - worse performance because of more draw calls - each object is rendered with its own draw call even if line color is the same; no duplicate lines removal
//    - harder to filter which objects are rendered (this required to use FilterObjectsFunction)

public class WireframeRenderingSample : CommonSample
{
    public override string Title => "Wireframe rendering";
    public override string Subtitle => "This sample shows different techniques to render 3D objects are wireframe.\nSee comments in the code to get additional info with pros and cons of each technique.";


    private enum WireframeRenderingTechniques
    {
        CreateWireframePositions = 0, 
        UseLineMaterial = 1,
        WireframeRenderingStep = 2
    }
    
    private WireframeRenderingTechniques _wireframeRenderingTechnique = WireframeRenderingTechniques.CreateWireframePositions;

    private bool _useSingleColorLines = false;
    private bool _removeDuplicateLines = true;
    
    private float _lineThickness = 1f;

    private HashSet<ulong>? _distinctLinesHashSet;
    private GroupNode? _testSceneNode;

    public WireframeRenderingSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Nothing to do because the scene is defined in OnSceneViewInitialized (we need to have a SceneView object for WireframeRenderingStep)
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        _testSceneNode = TestScenes.GetTestScene(TestScenes.StandardTestScenes.HouseWithTrees, new Vector3(0, 0, 0), PositionTypes.Bottom | PositionTypes.Center, finalSize: new Vector3(800, 800, 800));

        _testSceneNode.Update(); // This will update the WoldMatrix property

        RecreateWireframe();
        
        //if (targetPositionCamera != null)
        //    targetPositionCamera.StartRotation(20, 0);

        base.OnSceneViewInitialized(sceneView);
    }

    protected override void OnDisposed()
    {
        RemoveWireframeRenderingStep();

        base.OnDisposed();
    }

    private void RecreateWireframe()
    {
        switch (_wireframeRenderingTechnique)
        {
            case WireframeRenderingTechniques.CreateWireframePositions:
                if (_useSingleColorLines)
                    CreateSolidColorWireframeLinePositions();
                else
                    CreatePerObjectColoredWireframeLinePositions();
                break;
            
            case WireframeRenderingTechniques.UseLineMaterial:
                CreateSceneNodesWithLineMaterial();
                break;

            case WireframeRenderingTechniques.WireframeRenderingStep:
                CreateWireframeRenderingStep();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void CreateSolidColorWireframeLinePositions()
    {
        if (_testSceneNode == null || Scene == null)
            return;

        Scene.RootNode.Clear();

        // GetWireframeLinePositions returns an array of Vector3 positions that define wireframe lines for the specified SceneNode (and its child SceneNodes in case a GroupNode is set as parameter).
        // When removedDuplicateLines is true, then only one line will be created for each edge between two triangles.
        // This requires additional processing but is faster to render because less wireframe lines are returned.
        var sphereWireframePositions = LineUtils.GetWireframeLinePositions(_testSceneNode, _removeDuplicateLines);

        // After we have all the line positions, we can render the lines by using MultiLineNode
        var wireframeLineNode = new MultiLineNode(sphereWireframePositions, isLineStrip: false, Color3.Black, _lineThickness, "WireframeLine");

        Scene.RootNode.Add(wireframeLineNode);
    }
    
    private void CreatePerObjectColoredWireframeLinePositions()
    {
        if (_testSceneNode == null || Scene == null)
            return;

        Scene.RootNode.Clear();

        // Create a dictionary with line color as a key and list of line positions as a value
        var allColoredLinePositions = new Dictionary<Color3, List<Vector3>>();

        // When we remove duplicate lines, the code requires a HashSet object to store already processed lines.
        // To prevent creating many instances of HashSet object, we can provide an instance to AddWireframeLinePositions
        // and this will reuse an existing HashSet.
        if (_removeDuplicateLines)
            _distinctLinesHashSet = new HashSet<ulong>();

        // Go through each child SceneNode and add its wireframe positions to allColoredLinePositions
        _testSceneNode.ForEachChild(parentTransformMatrix: _testSceneNode.WorldMatrix,
            (sceneNode, transformMatrix) =>
            {
                if (sceneNode is ModelNode modelNode &&
                    modelNode.Material is StandardMaterial standardMaterial)
                {
                    var mesh = modelNode.GetMesh();

                    if (mesh != null)
                    {
                        var lineColor = standardMaterial.DiffuseColor;

                        // Use allColoredLinePositions dictionary to use one List<Vector3> for each line color
                        if (!allColoredLinePositions.TryGetValue(lineColor, out List<Vector3>? linePositions)) // new color?
                        {
                            linePositions = new List<Vector3>();
                            allColoredLinePositions.Add(lineColor, linePositions);
                        }

                        // Some ModelNodes use mesh transform to transform a mesh without changing the Transform property.
                        // For example this is used for BoxModelNode where shared 1x1x1 box mesh is used and this is then transformed to its final position and size by mesh transform.
                        // In case this modelNode us mesh transform, add it to the current transformation.
                        var meshTransform = modelNode.GetMeshTransform();
                        if (meshTransform != null && !meshTransform.IsIdentity)
                            transformMatrix *= meshTransform.Value;


                        // Add wireframe positions for the current SceneNode's Mesh to the linePositions

                        // NOTE:
                        // In SharpEngine v1.1 it will be possible to call AddWireframeLinePositions and pass WorldMatrix as parameter without the need to convert that to MatrixTransform.
                        var matrixTransform = new MatrixTransform(transformMatrix);
                        LineUtils.AddWireframeLinePositions(linePositions, mesh, _removeDuplicateLines, matrixTransform, _distinctLinesHashSet);
                    }
                }
            });

        // Now create a MultiLineNode for each line color
        foreach (var coloredLinePositions in allColoredLinePositions)
        {
            var lineColor = coloredLinePositions.Key;
            var wireframePositions = coloredLinePositions.Value.ToArray();
            var wireframeLineNode = new MultiLineNode(wireframePositions, isLineStrip: false, lineColor, _lineThickness, "WireframeLine-" + lineColor.ToHexString());

            Scene.RootNode.Add(wireframeLineNode);
        }
    }

    private void CreateSceneNodesWithLineMaterial()
    {
        if (_testSceneNode == null || Scene == null)
            return;

        Scene.RootNode.Clear();

        // Go through each child SceneNode and add its wireframe positions to allColoredLinePositions
        _testSceneNode.ForEachChild(parentTransformMatrix: _testSceneNode.WorldMatrix,
            (sceneNode, transformMatrix) =>
            {
                if (sceneNode is ModelNode modelNode &&
                    modelNode.Material is StandardMaterial standardMaterial)
                {
                    var mesh = modelNode.GetMesh();

                    if (mesh != null)
                    {
                        Color3 lineColor;

                        if (_useSingleColorLines)
                            lineColor = Color3.Black;
                        else
                            lineColor = standardMaterial.DiffuseColor;

                        var lineMaterial = new LineMaterial(lineColor, _lineThickness);

                        // Create a new MeshModelNode from each ModelNode but use LineMaterial instead of StandardMaterial
                        var wireframeModelNode = new MeshModelNode(mesh, lineMaterial);

                        // Some ModelNodes use mesh transform to transform a mesh without changing the Transform property.
                        // For example this is used for BoxModelNode where shared 1x1x1 box mesh is used and this is then transformed to its final position and size by mesh transform.
                        // In case this modelNode us mesh transform, add it to the current transformation.
                        var meshTransform = modelNode.GetMeshTransform();
                        if (meshTransform != null && !meshTransform.IsIdentity)
                            transformMatrix *= meshTransform.Value;

                        if (!transformMatrix.IsIdentity)
                            wireframeModelNode.Transform = new MatrixTransform(transformMatrix);

                        Scene.RootNode.Add(wireframeModelNode);
                    }
                }
            });
    }

    private void CreateWireframeRenderingStep()
    {
        if (Scene == null || SceneView == null || _testSceneNode == null || SceneView.DefaultRenderObjectsRenderingStep == null)
            return;

        var wireframeRenderingEffectTechnique = new WireframeRenderingEffectTechnique(Scene, "CustomWireframeRenderingEffectTechnique")
        {
            UseLineColorFromDiffuseColor = !_useSingleColorLines,

            LineColor = Color4.Black,
            LineThickness = _lineThickness,

            // Use default values:
            DepthBias = 0,
            LinePattern = 0,
            LinePatternScale = 1,
            LinePatternOffset = 0,
        };

        SceneView.DefaultRenderObjectsRenderingStep.OverrideEffectTechnique = wireframeRenderingEffectTechnique;
    }

    private void RemoveWireframeRenderingStep()
    {
        if (SceneView == null || SceneView.DefaultRenderObjectsRenderingStep == null)
            return;

        SceneView.DefaultRenderObjectsRenderingStep.OverrideEffectTechnique = null;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Wireframe rendering technique:", isHeader: true);

        ui.CreateRadioButtons(new string[] { "Create wireframe positions", "Use LineMaterial", "WireframeRenderingStep"}, 
            (selectedIndex, selectedText) =>
            {
                if (_wireframeRenderingTechnique == WireframeRenderingTechniques.WireframeRenderingStep)
                    RemoveWireframeRenderingStep();

                _wireframeRenderingTechnique = (WireframeRenderingTechniques)selectedIndex;
                RecreateWireframe();
            },
            selectedItemIndex: 0);
        
                
        ui.AddSeparator();
        ui.AddSeparator();
        
        var lineThicknessOptions = new float[] { 0.2f, 0.5f, 1, 2, 3 };
        ui.CreateComboBox(lineThicknessOptions.Select(f => f.ToString()).ToArray(), (selectedIndex, selectedText) =>
        {
            _lineThickness = lineThicknessOptions[selectedIndex];
            RecreateWireframe();
        }, selectedItemIndex: 2, keyText: "LineThickness: ");
        
        
        ui.AddSeparator();
        
        ui.CreateCheckBox("Use single color lines (black)", false, isChecked =>
        {
            _useSingleColorLines = isChecked;
            RecreateWireframe();
        });
    }
}