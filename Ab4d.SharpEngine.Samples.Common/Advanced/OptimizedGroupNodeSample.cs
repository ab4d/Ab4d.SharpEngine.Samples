using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

// OptimizedGroupNode combines child meshes into shared vertex and index buffers and uses
// DrawIndexedIndirect to render them in one draw call (two for mixed opaque and transparent models).
// Unlike instancing a single mesh (using InstancedMeshNode), the child models can have different geometry.
//
// SUPPORTED OptimizedGroupNode child nodes:
// - Only StandardMaterial without a texture and specular color is supported
// - Only the Material property is used (not BackMaterial)
// - It is possible to enable two-sided materials for all child models by setting IsTwoSided property on OptimizedGroupNode to true
// - Transparent materials (Opacity < 1) are rendered by its own draw call. But in this case the transparency sorting of child objects is not supported - the transparent models are rendered in the same order as they are added to the OptimizedGroupNode.
// - Only meshes of type StandardMesh and GeometryMesh are supported (PositionsMesh and TriangleMesh with other vertex layout are not supported; also SolidColorMaterial is not supported).
//
// FAST child node  operations (require change of the material or matrix buffer and no rebuilding the combined vertex and index buffer and recording new combined buffers):
// - Change material's color (change the DiffuseColor property) or change material object (assign a new StandardMaterial to ModelNode.Material property)
// - Change visibility of child ModelNode or GroupNode
// - Change transform of child node
//
// SLOW child node operations (require rebuilding the group's combined vertex and index buffer and recording new combined buffers):
// - Changed transparency of a material from opaque to transparent or vice versa.
// - Add or remove child nodes (including GroupNode objects)
//
// NOT SUPPORTED child node operations:
// - Change mesh or individual positions or triangle indices of child MeshModelNode (this also includes changing properties on some ModelNodes, for example, changing Size on BoxModelNode or PyramidModelNode).
//
// 
// Additional notes:
//
// Set OptimizedGroupNode.IsTwoSided to render both sides of all child models.
// Transparent children are drawn in insertion order, without transparency sorting.
// Unsupported children produce a log warning (controlled by WriteLogWarningForUnsupportedChildNodes).
//
// For large groups, InitializeAsync can build the data and GPU buffers on a background thread after
// adding the children and attaching the group to an initialized Scene. Do not modify children until
// initialization completes (IsInitializingInBackground becomes false).

public class OptimizedGroupNodeSample : CommonSample
{
    public override string Title => "OptimizedGroupNode";
    public override string Subtitle => "OptimizedGroupNode combines child meshes and renders them in one draw call (two for opaque and transparent models).\n" +
                                       "This sample demonstrates that how to change child materials, visibility and transforms, or add and remove objects.\n" +
                                       "See comments in the code for more info.";

    private const int RowSize = 8;
    private const float Spacing = 25;
    private const float FirstPosition = -(RowSize - 1) * Spacing / 2;

    private OptimizedGroupNode? _optimizedGroupNode;
    
    // Each row is a horizontal 8 x 8 layer. Keep its identity when its models are translated.
    private readonly List<List<BoxModelNode>> _rows = new();
    private ICommonSampleUIElement? _infoLabel;

    public OptimizedGroupNodeSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _rows.Clear();
        
        _optimizedGroupNode = new OptimizedGroupNode();
        
        for (int y = 0; y < RowSize; y++)
            AddRow();

        // For large groups, InitializeAsync can build the data and GPU buffers on a background thread
        // Do not modify children until initialization completes (IsInitializingInBackground becomes false).
        // await _optimizedGroupNode.InitializeAsync(scene);
        
        scene.RootNode.Add(_optimizedGroupNode);
        
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -25;
            targetPositionCamera.Distance = 700;
        }

        UpdateInfoLabel();
    }

    private void AddRow()
    {
        if (_optimizedGroupNode == null)
            return;

        float y = FirstPosition;
        if (_rows.Count > 0)
        {
            // Place the new layer above the previous one, including any translation applied by the UI.
            var topBox = _rows[^1][0];
            y = topBox.Position.Y + ((topBox.Transform as TranslateTransform)?.Y ?? 0) + Spacing;
        }

        var row = new List<BoxModelNode>(RowSize * RowSize);
        for (int x = 0; x < RowSize; x++)
        {
            for (int z = 0; z < RowSize; z++)
            {
                var position = new Vector3(FirstPosition + x * Spacing, y, FirstPosition + z * Spacing);
                
                // Normalize coordinates to RGB. Clamp Y because users can add layers above the original cube.
                var color = new Color3((position.X - FirstPosition) / ((RowSize - 1) * Spacing),
                                       Math.Clamp((position.Y - FirstPosition) / ((RowSize - 1) * Spacing), 0, 1),
                                       (position.Z - FirstPosition) / ((RowSize - 1) * Spacing));
                
                var size = new Vector3(GetRandomFloat(minValue: 5, maxValue: 18), GetRandomFloat(minValue: 5, maxValue: 18), GetRandomFloat(minValue: 5, maxValue: 18));

                var box = new BoxModelNode(position, size, material: new StandardMaterial(color), name: $"Box_{x}_{_rows.Count}_{z}");
                
                box.Update(); // Generate the mesh now so GetMesh can be used before the first rendered frame.
                
                _optimizedGroupNode.Add(box);
                
                row.Add(box);
            }
        }

        _rows.Add(row);
        UpdateInfoLabel();
    }

    private void ChangeColor()
    {
        if (_rows.Count == 0)
            return;

        foreach (var box in _rows[^1])
        {
            if (box.Material is StandardMaterial material)
                material.DiffuseColor = new Color3(GetRandomFloat(), GetRandomFloat(), GetRandomFloat());
        }
    }

    private void ChangeVisibility()
    {
        // Toggle a vertical column: the box at the first X and Z coordinate in every layer.
        foreach (var row in _rows)
        {
            for (int i = 0; i < RowSize; i++)
            {
                var box = row[i];
                box.Visibility = box.Visibility == SceneNodeVisibility.Visible
                    ? SceneNodeVisibility.Hidden
                    : SceneNodeVisibility.Visible;
            }

        }
    }

    private void ChangeTransformation()
    {
        if (_rows.Count == 0)
            return;

        foreach (var box in _rows[^1])
        {
            if (box.Transform is TranslateTransform translateTransform)
                translateTransform.Y += 10;
            else
                box.Transform = new TranslateTransform(y: 10);
        }
    }

    private void ChangeTransparency()
    {
        if (_rows.Count == 0)
            return;

        float opacity = _rows[^1].Any(box => box.Material is StandardMaterial { Opacity: < 1 }) ? 1.0f : 0.3f;
        
        foreach (var box in _rows[^1])
        {
            if (box.Material is StandardMaterial material)
                material.Opacity = opacity;
        }

        UpdateInfoLabel();
    }

    private void RemoveRow()
    {
        if (_optimizedGroupNode == null || _rows.Count == 0)
            return;

        foreach (var box in _rows[^1])
        {
            _optimizedGroupNode.Remove(box);
            box.Dispose();
        }

        _rows.RemoveAt(_rows.Count - 1);
        UpdateInfoLabel();
    }

    private void UpdateInfoLabel()
    {
        if (_infoLabel == null || _optimizedGroupNode == null)
            return;

        int nodesCount = 0;
        long trianglesCount = 0;
        bool hasOpaqueObjects = false;
        bool hasTransparentObjects = false;
        
        //foreach (var child in GetChildNodes(_optimizedGroupNode))
        foreach (var child in _optimizedGroupNode)
        {
            nodesCount++;
            
            if (child is not ModelNode model)
                continue;

            var mesh = model.GetMesh();
            if (mesh != null)
                trianglesCount += mesh.IndexCount / 3;

            if (model.Material is StandardMaterial material)
            {
                if (material.Opacity < 1)
                    hasTransparentObjects = true;
                else
                    hasOpaqueObjects = true;
            }
        }

        int drawCallsCount = (hasOpaqueObjects ? 1 : 0) + (hasTransparentObjects ? 1 : 0);
        _infoLabel.SetText($"Models count: {nodesCount:N0}\nTriangles count: {trianglesCount:N0}\nDraw calls count: {drawCallsCount}");
    }

    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        _infoLabel = ui.CreateLabel("Models count: 0\nTriangles count: 0\nDraw calls count: 0", isHeader: true);
        ui.AddSeparator();

        ui.CreateLabel("Fast child operations: (?):The following operations are fast because they only require change of the material or matrix buffer and\ndo not require rebuilding the group's combined buffers.");
        ui.CreateButton("Change visibility", ChangeVisibility);
        ui.CreateButton("Change color", ChangeColor);
        ui.CreateButton("Change transformation", ChangeTransformation);
        ui.AddSeparator();
        
        ui.CreateLabel("Slow child operations: (?):The following operations are slow because they require rebuilding the group's combined vertex and index buffers\nand recording the command buffers again.");
        ui.CreateButton("Change transparency", ChangeTransparency);
        ui.CreateButton("Add new models", AddRow);
        ui.CreateButton("Remove models", RemoveRow);
        UpdateInfoLabel();
    }
}

