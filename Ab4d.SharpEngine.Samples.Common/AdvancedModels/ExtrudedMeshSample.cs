using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class ExtrudedMeshSample : CommonSample
{
    public override string Title => "Triangulated and Extruded 2D shapes";
    public override string Subtitle => "Triangulator defines the triangles that connect the positions in a 2D shape.\nExtruded 3D models are created by extruding a 2D shape along a 3D vector.";

    private enum SampleShapeTypes
    {
        Hexagon,
        Circle,
        CShape
    }

    
    private SampleShapeTypes _currentShapeType = SampleShapeTypes.Hexagon;
    private bool _showIndividualTriangles = true;
    private Vector3 _extrudeVector = new Vector3(0, 30, 0);
    private Vector3 _shapeYVector = new Vector3(0, 0, -1);

    private Color3[]? _randomColors;

    private GroupNode? _generatedObjectsGroup;

    public ExtrudedMeshSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _generatedObjectsGroup = new GroupNode("GeneratedObjects");

        scene.RootNode.Add(_generatedObjectsGroup);


        UpdateCurrentShape();


        var textBlockFactory = context.GetTextBlockFactory();

        scene.RootNode.Add(textBlockFactory.CreateTextBlock("2D shape", new Vector3(-70, 80, 0), positionType: PositionTypes.Right));
        scene.RootNode.Add(textBlockFactory.CreateTextBlock("Triangulated shape", new Vector3(-70, 0, 0), positionType: PositionTypes.Right));
        scene.RootNode.Add(textBlockFactory.CreateTextBlock("Extruded shape", new Vector3(-70, -115, 0), positionType: PositionTypes.Right));


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 0;
            targetPositionCamera.Attitude = -40;
            targetPositionCamera.Distance = 550;
        }
    }

    private void UpdateCurrentShape()
    {
        if (_generatedObjectsGroup == null)
            return;

        _generatedObjectsGroup.Clear();

        // Get 2D shape positions
        var shape2DPositions = GetShapePositions(_currentShapeType);

        // Convert 2D shape to 3D positions
        var shape3DPositions = new Vector3[shape2DPositions.Length];
        for (var i = 0; i < shape2DPositions.Length; i++)
            shape3DPositions[i] = new Vector3(shape2DPositions[i].X, 0, shape2DPositions[i].Y); // Set y to zero


        // Show shape as a poly-line
        var polyLineNode = new PolyLineNode(shape3DPositions, lineColor: Colors.Orange, lineThickness: 3);
        polyLineNode.IsClosed = true; // Connect last and first position to close the poly-line
        polyLineNode.Transform = new TranslateTransform(y: 80);
        _generatedObjectsGroup.Add(polyLineNode);


        // Triangulate 2D shape
        var triangleIndices = Triangulator.Triangulate(shape2DPositions).ToArray();

        if (_showIndividualTriangles)
        {
            // Show each triangle as with its own color
            _generatedObjectsGroup.Add(CreateIndividualTrianglesNode(shape3DPositions, triangleIndices));
        }
        else
        {
            // Show as a single MeshModelNode
            var triangulatedMesh = new GeometryMesh(shape3DPositions, triangleIndices);
            var triangulatedModelNode = new MeshModelNode(triangulatedMesh, StandardMaterials.Orange);
            triangulatedModelNode.BackMaterial = StandardMaterials.Red; // Back triangles are shown as red

            _generatedObjectsGroup.Add(triangulatedModelNode);
        }

        // Extruded mesh
        var extrudedMesh = MeshFactory.CreateExtrudedMesh(positions: shape2DPositions,
                                                          isSmooth: false,
                                                          modelOffset: new Vector3(0, -130, 0),
                                                          extrudeVector: _extrudeVector,
                                                          shapeYVector: _shapeYVector, // A 3D vector that defines the 3D direction along the 2D shape surface (i.e., the Y axis of the base 2D shape)
                                                          closeBottom: true,
                                                          closeTop: true);

        var extrudedModelNode = new MeshModelNode(extrudedMesh, StandardMaterials.Orange.SetOpacity(0.7f));
        extrudedModelNode.BackMaterial = extrudedModelNode.Material; // Also show inner side of the model by showing back triangles

        _generatedObjectsGroup.Add(extrudedModelNode);
    }

    private GroupNode CreateIndividualTrianglesNode(Vector3[] shape3DPositions, int[] triangleIndices)
    {
        var groupNode = new GroupNode("IndividualTrianglesNode");

        var singleTriangleIndices = new int[] { 0, 1, 2 }; // triangle indices for a single triangle

        int trianglesCount = (int)(triangleIndices.Length / 3);
        if (_randomColors == null || _randomColors.Length != trianglesCount)
        {
            _randomColors = new Color3[trianglesCount];
            for (int i = 0; i < trianglesCount; i++)
                _randomColors[i] = GetRandomColor3();
        }

        for (int i = 0; i < trianglesCount; i ++)
        {
            var positions = new Vector3[3];

            for (int j = 0; j < 3; j++)
            {
                int index = triangleIndices[(i * 3) + j];
                positions[j] = shape3DPositions[index];
            }

            var triangulatedMesh = new GeometryMesh(positions, singleTriangleIndices);

            var randomColor = _randomColors[i];
            var standardMaterial = new StandardMaterial(randomColor);

            var triangulatedModelNode = new MeshModelNode(triangulatedMesh, standardMaterial);
            triangulatedModelNode.BackMaterial = standardMaterial;

            groupNode.Add(triangulatedModelNode);
        }

        return groupNode;
    }

    private Vector2[] GetShapePositions(SampleShapeTypes shapeType)
    {
        Vector2[] shapePositions;

        switch (shapeType)
        {
            case SampleShapeTypes.Hexagon:
                shapePositions = CreateNGonShape(new Vector2(0, 0), 50, 6);
                break;

            case SampleShapeTypes.Circle:
                shapePositions = CreateNGonShape(new Vector2(0, 0), 50, 60);
                break;

            case SampleShapeTypes.CShape:
                shapePositions = new Vector2[]
                                 {
                                     new Vector2(40, -40), 
                                     new Vector2(-40, -40), 
                                     new Vector2(-40, 40), 
                                     new Vector2(40, 40), 
                                     new Vector2(40, 20), 
                                     new Vector2(-20, 20), 
                                     new Vector2(-20, -20), 
                                     new Vector2(40, -20)
                                 };
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(shapeType), shapeType, null);
        }

        return shapePositions;
    }

    private static Vector2[] CreateNGonShape(Vector2 center, float radius, int numCorners, bool isAntiClockwise = true)
    {
        var corners = new Vector2[numCorners];
        var angleStep = 2 * MathF.PI / numCorners;

        for (var i = 0; i < numCorners; i++)
        {
            var (sin, cos) = MathF.SinCos(i * angleStep);

            if (!isAntiClockwise)
                cos = -cos; // this inverses the orientation of a triangle

            corners[i] = new Vector2(center.X + cos * radius, center.Y + sin * radius);
        }

        return corners;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateRadioButtons(new string[] { "Hexagon", "Circle", "C shape" },
            (selectedIndex, selectedText) =>
            {
                _currentShapeType = (SampleShapeTypes)selectedIndex;
                UpdateCurrentShape();
            }, selectedItemIndex: (int)_currentShapeType);


        ui.AddSeparator();

        var extrudeVectors = new Vector3[] { new Vector3(0, 15, 0), new Vector3(0, 30, 0), new Vector3(30, 0, 0), new Vector3(0, 0, 30), new Vector3(10, 30, 0) };
        var shapeYVectors = new Vector3[]  { new Vector3(0, 0, -1), new Vector3(0, 0, -1), new Vector3(0, 1, 0),  new Vector3(0, 1, 0),  new Vector3(0, 0, -1) };
        ui.CreateComboBox(extrudeVectors.Select(v => $"({v.X:F0}, {v.Y:F0}, {v.Z:F0})").ToArray(), (itemIndex, itemText) =>
        {
            _extrudeVector = extrudeVectors[itemIndex];
            _shapeYVector = shapeYVectors[itemIndex];
            UpdateCurrentShape();
        }, selectedItemIndex: 1, keyText: "Extrude vector:", keyTextWidth: 95, width: 90);

        ui.CreateCheckBox("Show individual triangles", isInitiallyChecked: _showIndividualTriangles, isChecked =>
        {
            _showIndividualTriangles = isChecked;
            UpdateCurrentShape();
        });
    }
}