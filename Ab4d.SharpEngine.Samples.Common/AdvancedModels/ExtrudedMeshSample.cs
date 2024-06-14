using System.Drawing;
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
    public override string Title => "Extruded 3D models";
    public override string Subtitle => "Extruded 3D models are created by extruding a 2D shape along a 3D vector";

    private enum SampleShapeTypes
    {
        Hexagon,
        Circle,
        CShape,
        RectangleWithHoles1,
        RectangleWithHoles2
    }

    private bool _showIndividualTriangles = true;
    private bool _showSemiTransparentModel = true;
    private SampleShapeTypes _currentShapeType = SampleShapeTypes.Hexagon;

    private Vector3 _extrudeVector = new Vector3(0, 30, 0);
    private Vector3 _shapeYVector = new Vector3(0, 0, 1);

    private List<Color3> _randomColors = new List<Color3>();

    private GroupNode? _generatedObjectsGroup;
    private GroupNode? _individualTrianglesNode;

    private int _trianglesCount;
    private int _shownTriangles;
    private float _shownTrianglesPercent = 100;

    private ICommonSampleUIElement? _shownTrianglesSlider;

    private bool IsShapeWithHoles => _currentShapeType >= SampleShapeTypes.RectangleWithHoles1;

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
        scene.RootNode.Add(textBlockFactory.CreateTextBlock("Extruded shape", new Vector3(-70, -100, 0), positionType: PositionTypes.Right));


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 0;
            targetPositionCamera.Attitude = -40;
            targetPositionCamera.Distance = 550;
        }

        ShowCameraAxisPanel = true;
    }

    private void UpdateCurrentShape()
    {
        if (_generatedObjectsGroup == null)
            return;

        _generatedObjectsGroup.Clear();

        if (IsShapeWithHoles)
            AddShapeWithHoles(_generatedObjectsGroup, _currentShapeType);
        else
            AddSimpleShapeWithoutHoles(_generatedObjectsGroup, _currentShapeType);
    }

    private void AddSimpleShapeWithoutHoles(GroupNode parentGroupNode, SampleShapeTypes currentShapeType)
    {
        // Get 2D shape positions
        var shape2DPositions = GetSimpleShapePositions(currentShapeType);

        bool isShapeYUp = false; // The 2D coordinates that defined the shape have Y axis down: (0,0) is at top left corner


        
        // Triangulate 2D shape
        //var triangleIndices = Triangulator.Triangulate(shape2DPositions).ToArray();

        Triangulator triangulator = new Triangulator(shape2DPositions, isYAxisUp: isShapeYUp);
        var triangleIndices =  triangulator.CreateTriangleIndices().ToArray();


        // Convert 2D positions to 3D positions (set y to zero)
        // See comments in Convert2DTo3D method for more info
        var shape3DPositions = Convert2DTo3D(shape2DPositions, Scene, triangulator.IsClockwise);


        // Show shape as a poly-line
        var polyLineNode = new PolyLineNode(shape3DPositions, lineColor: Colors.Orange, lineThickness: 3);
        polyLineNode.IsClosed = true; // Connect last and first position to close the poly-line
        polyLineNode.Transform = new TranslateTransform(y: 80);
        parentGroupNode.Add(polyLineNode);


        if (_showIndividualTriangles)
        {
            // Show each triangle as with its own color
            var individualTrianglesNode = CreateIndividualTrianglesNode(shape3DPositions, triangleIndices);
            parentGroupNode.Add(individualTrianglesNode);
        }
        else
        {
            // Show as a single MeshModelNode
            var triangulatedMesh = new GeometryMesh(shape3DPositions, triangleIndices);
            var triangulatedModelNode = new MeshModelNode(triangulatedMesh, StandardMaterials.Orange);
            triangulatedModelNode.BackMaterial = StandardMaterials.Red; // Back triangles are shown as red

            parentGroupNode.Add(triangulatedModelNode);
        }

        // Extruded mesh
        var extrudedMesh = MeshFactory.CreateExtrudedMesh(positions: shape2DPositions,
                                                          isSmooth: false,
                                                          modelOffset: new Vector3(0, -130, 0),
                                                          extrudeVector: _extrudeVector,
                                                          shapeYVector: _shapeYVector, // A 3D vector that defines the 3D direction along the 2D shape surface (i.e., the Y axis of the base 2D shape)
                                                          closeBottom: true,
                                                          closeTop: true);

        var material = StandardMaterials.Orange;

        var extrudedModelNode = new MeshModelNode(extrudedMesh, material);

        if (_showSemiTransparentModel)
        {
            material.SetOpacity(0.7f);
            extrudedModelNode.BackMaterial = material; // Also show inner side of the model by showing back triangles
        }

        parentGroupNode.Add(extrudedModelNode);
    }

    private void AddShapeWithHoles(GroupNode parentGroupNode, SampleShapeTypes currentShapeType)
    {
        // Shape with holes is represented by multiple arrays of positions
        // The holes are identified by the orientation of positions (clockwise or anti-clockwise) that is different from the orientation of positions in the outer polygons. 
        // In this sample the outer polygons are oriented in clockwise direction, holes in anti-clockwise direction
        Vector2[][] multiplePolygons = GetMultiShapePositions(currentShapeType);

        bool isShapeYUp = false; // The 2D coordinates that defined the shape have Y axis down: (0,0) is at top left corner

        var outerPolygon = multiplePolygons[0];
        var triangulator = new Triangulator(outerPolygon, isYAxisUp: isShapeYUp);

        for (int i = 1; i < multiplePolygons.Length; i++)
        {
            // When calling AddHole the method checks if the specified positions are oriented in the opposite direction as the outerPolygon.
            // If not, then the isCorrectOrientation is set to false and we can fix that by reversing the orientation of positions.
            bool isCorrectOrientation = triangulator.AddHole(multiplePolygons[i]);
            if (!isCorrectOrientation)
                ReversePointsOrder(multiplePolygons[i]);
        }
        
        // After we added the outer polygon and all holes, we can call the Triangulate method.
        triangulator.Triangulate(out List<Vector2> allPolygonPositions, out List<int> triangleIndicesList);


        // The same can be achieved by calling the static Triangulator.Triangulate method.
        // But here we need to make sure that the hole polygons are correctly oriented as we cannot switch their order.
        //Triangulator.Triangulate(multiplePolygons, out List<Vector2> allPolygonPositions, out List<int> triangleIndicesList);


        // Convert 2D positions to 3D positions (set y to zero)
        // See comments in Convert2DTo3D method for more info
        var shape3DPositions = Convert2DTo3D(allPolygonPositions.ToArray(), Scene, triangulator.IsClockwise);


        for (var i = 0; i < multiplePolygons.Length; i++)
        {
            var onePolygonPositions3D = Convert2DTo3D(multiplePolygons[i], Scene, triangulator.IsClockwise);

            // Show shape as a poly-line
            var polyLineNode = new PolyLineNode(onePolygonPositions3D, lineColor: Colors.Orange, lineThickness: 3);
            polyLineNode.IsClosed  = true; // Connect last and first position to close the poly-line
            polyLineNode.Transform = new TranslateTransform(y: 80);
            parentGroupNode.Add(polyLineNode);
        }


        var triangleIndices = triangleIndicesList.ToArray();

        if (_showIndividualTriangles)
        {
            // Show each triangle as with its own color
            var individualTrianglesNode = CreateIndividualTrianglesNode(shape3DPositions, triangleIndices);
            parentGroupNode.Add(individualTrianglesNode);
        }
        else
        {
            // Show as a single MeshModelNode
            var triangulatedMesh = new GeometryMesh(shape3DPositions, triangleIndices);
            var triangulatedModelNode = new MeshModelNode(triangulatedMesh, StandardMaterials.Orange);
            triangulatedModelNode.BackMaterial = StandardMaterials.Red; // Back triangles are shown as red

            parentGroupNode.Add(triangulatedModelNode);
        }

        // Extruded mesh
        //var extrudedMesh = MeshFactory.CreateExtrudedMesh(positions: multiShape2DPositions,
        //                                                  isSmooth: false,
        //                                                  modelOffset: new Vector3(0, -130, 0),
        //                                                  extrudeVector: new Vector3(0, 30, 0),
        //                                                  closeBottom: true,
        //                                                  closeTop: true);

        // If points in polygon are counter-clockwise oriented we need to flip normals (change order of triangle indices)
        // to make the triangles correctly oriented - so the normals are pointing out of the object
        var flipNormals = !triangulator.IsClockwise;

        var extrudedMesh = MeshFactory.CreateExtrudedMesh(
            positions: allPolygonPositions.ToArray(),
            triangleIndices: triangleIndices,
            allPolygons: multiplePolygons,
            isSmooth: false,
            flipNormals: flipNormals,
            modelOffset: new Vector3(0, -130, 0),
            extrudeVector: _extrudeVector,
            shapeYVector: _shapeYVector, // A 3D vector that defines the 3D direction along the 2D shape surface (i.e., the Y axis of the base 2D shape)
            textureCoordinatesGenerationType: MeshFactory.ExtrudeTextureCoordinatesGenerationType.None,
            isYAxisUp: false,
            closeBottom: true,
            closeTop: true);

        var material = StandardMaterials.Orange;

        var extrudedModelNode = new MeshModelNode(extrudedMesh, material);

        if (_showSemiTransparentModel)
        {
            material.SetOpacity(0.7f);
            extrudedModelNode.BackMaterial = material; // Also show inner side of the model by showing back triangles
        }


        parentGroupNode.Add(extrudedModelNode);
    }

    private Vector3[] Convert2DTo3D(Vector2[] shape2DPositions, Scene? scene, bool isShapeClockwise)
    {
        // Convert 2D positions to 3D positions (set y to zero)

        // We are converting 2D XY coordinates to 3D coordinates to XZ plane (by adding y = 0).
        // Because in this sample in 2D coordinates the Y axis is pointing down ((0,0) is at top left corner),
        // we can directly copy that to 3D coordinates when we have YUpRightHanded (default) or ZUpLeftHanded coordinate system.
        // If we have YUpLeftHanded or ZUpRightHanded, then Z axis is oriented in the other direction
        // and in this case we need to negate the y coordinate.
        //
        // This can be tested by unchecking the "Show individual triangles" checkbox.
        // In this case the front triangles are shown with orange material and back triangles are shown with red material.
        
        float yInvertFactor = 1;

        if (scene != null)
        {
            var coordinateSystem = scene.GetCoordinateSystem();
            if (coordinateSystem == CoordinateSystems.YUpLeftHanded || coordinateSystem == CoordinateSystems.ZUpRightHanded)
                yInvertFactor = -1;
        }

        // To show the top of the 2D shape with front material (set to Material property),
        // the positions in the shape need to be oriented in the clock-wise direction.
        // If they are not, we flip y. This also flips the orientation of the triangles.
        // To demonstrate that, set the isAntiClockwise property in GetSimpleShapePositions method to false.

        if (!isShapeClockwise)
            yInvertFactor *= -1; // flip yInvertFactor


        var shape3DPositions = new Vector3[shape2DPositions.Length];

        for (var i = 0; i < shape2DPositions.Length; i++)
            shape3DPositions[i] = new Vector3(shape2DPositions[i].X, 0, shape2DPositions[i].Y * yInvertFactor);

        return shape3DPositions;
    }

    private GroupNode CreateIndividualTrianglesNode(Vector3[] shape3DPositions, int[] triangleIndices)
    {
        _individualTrianglesNode = new GroupNode("IndividualTrianglesNode");

        var singleTriangleIndices = new int[] { 0, 1, 2 }; // triangle indices for a single triangle


        // Save random colors to _randomColors list so the colors are preserved when we regenerate the mesh
        int trianglesCount = triangleIndices.Length / 3;
        int additionalColorsCount = trianglesCount - _randomColors.Count;
        if (additionalColorsCount > 0)
        {
            for (int i = 0; i < additionalColorsCount; i++)
                _randomColors.Add(GetRandomColor3());
        }

        int triangleIndex = 0;
        for (int i = 0; i < triangleIndices.Length; i += 3)
        {
            var positions = new Vector3[3];

            for (int j = 0; j < 3; j++)
            {
                int index = triangleIndices[i + j];
                positions[j] = shape3DPositions[index];
            }

            var triangulatedMesh = new GeometryMesh(positions, singleTriangleIndices);

            var randomColor = _randomColors[triangleIndex];
            var standardMaterial = new StandardMaterial(randomColor);

            var triangulatedModelNode = new MeshModelNode(triangulatedMesh, standardMaterial);
            triangulatedModelNode.BackMaterial = standardMaterial;

            _individualTrianglesNode.Add(triangulatedModelNode);

            triangleIndex++;
        }

        _trianglesCount = triangleIndices.Length / 3;
        UpdateShownTriangles(_shownTrianglesPercent);

        if (_shownTrianglesSlider != null)
            _shownTrianglesSlider.UpdateValue();

        return _individualTrianglesNode;
    }

    private Vector2[] GetSimpleShapePositions(SampleShapeTypes shapeType)
    {
        Vector2[] shapePositions;

        switch (shapeType)
        {
            case SampleShapeTypes.Hexagon:
                shapePositions = CreateNGonShape(center: new Vector2(0, 0), 
                                                 radius: 50, 
                                                 numCorners: 6, 
                                                 isClockwiseOrientation: true,
                                                 isYAxisUp: false);
                break;

            case SampleShapeTypes.Circle:
                shapePositions = CreateNGonShape(center: new Vector2(0, 0), 
                                                 radius: 50, 
                                                 numCorners: 60, 
                                                 isClockwiseOrientation: false,
                                                 isYAxisUp: false);
                break;
            
            case SampleShapeTypes.CShape:
                // Clockwise orientation of C shape
                shapePositions = new Vector2[]
                {
                    new Vector2(40, -20),
                    new Vector2(-20, -20), 
                    new Vector2(-20, 20), 
                    new Vector2(60, 20), 
                    new Vector2(60, 40), 
                    new Vector2(-40, 40), 
                    new Vector2(-40, -40), 
                    new Vector2(40, -40)
                };
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(shapeType), shapeType, null); // Shapes with holes are not supported
        }

        return shapePositions;
    }

    private Vector2[][] GetMultiShapePositions(SampleShapeTypes shapeType)
    {
        Vector2[][] multiShapePositions;

        switch (shapeType)
        {
            case SampleShapeTypes.RectangleWithHoles1:
                // Define an outer polygon and two holes
                // We use Y-down 2D coordinate system.
                // Outer polygons needs to be defined in clockwise direction, holes in anti-clockwise direction
                var outerPositions = new Vector2[]
                {
                    new Vector2(-50,-50),
                    new Vector2(50,-50),
                    new Vector2(50,50),
                    new Vector2(-50,50),
                };

                // 2 holes (anti-clockwise direction):
                var holePositions1 = new Vector2[]
                {
                    new Vector2(-20,30),
                    new Vector2(-20,40),
                    new Vector2(0,40),
                    new Vector2(0,30),
                };

                // NOTE: This hole is organized in clockwise direction - the AddHole method will return false and we will need to reverse the direction
                var holePositions2 = new Vector2[]
                {
                    new Vector2(30,10),
                    new Vector2(30,40),
                    new Vector2(10,40),
                    new Vector2(10,10),
                };

                multiShapePositions = new Vector2[][] { outerPositions, holePositions1, holePositions2 };
                break;

            case SampleShapeTypes.RectangleWithHoles2:
                // Create shapes with n-edges
                // The orientation of outer shape should be anti-clockwise,
                // the orientation of holes need to be clock-wise
                var rectangleShapePositions = CreateNGonShape(center: new Vector2(0, 0), 
                                                              radius: 50, 
                                                              numCorners: 4, 
                                                              isClockwiseOrientation: true,
                                                              isYAxisUp: false);

                var hole1 = CreateNGonShape(center: new Vector2(0, 22), 
                                            radius: 6, 
                                            numCorners: 10, 
                                            isClockwiseOrientation: false,
                                            isYAxisUp: false);

                var hole2 = CreateNGonShape(center: new Vector2(15, 5),
                                            radius: 10, 
                                            numCorners: 20, 
                                            isClockwiseOrientation: false,
                                            isYAxisUp: false);

                multiShapePositions = new Vector2[3][] { rectangleShapePositions, hole1, hole2 };
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(shapeType), shapeType, null);  // Shapes WITHOUT holes are not supported
        }

        return multiShapePositions;
    }

    private static Vector2[] CreateNGonShape(Vector2 center, float radius, int numCorners, bool isClockwiseOrientation = true, bool isYAxisUp = false)
    {
        var corners = new Vector2[numCorners];
        var angleStep = 2 * MathF.PI / numCorners;

        // When the direction of Y axis is changed, then the orientation is also changed
        if (isYAxisUp)
            isClockwiseOrientation = !isClockwiseOrientation;

        for (var i = 0; i < numCorners; i++)
        {
            var (sin, cos) = MathF.SinCos(i * angleStep);

            if (!isClockwiseOrientation)
                cos = -cos; // this inverses the orientation of a triangle

            corners[i] = new Vector2(center.X + cos * radius, center.Y + sin * radius);
        }

        return corners;
    }

    public static void ReversePointsOrder(Vector2[] points)
    {
        int index1 = 0;
        int index2 = points.Length - 1;

        while (index1 < index2)
        {
            (points[index1], points[index2]) = (points[index2], points[index1]);

            index1++;
            index2--;
        }
    }

    private void UpdateShownTriangles(float shownTrianglesPercent)
    {
        _shownTrianglesPercent = shownTrianglesPercent;
        _shownTriangles = (int)(_trianglesCount * shownTrianglesPercent / 100f);

        if (_individualTrianglesNode != null)
        {
            for (int i = 0; i < _trianglesCount; i++)
                _individualTrianglesNode[i].Visibility = i < _shownTriangles ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateRadioButtons(new string[] { "Hexagon", "Circle", "C shape", "Rectangle with simple holes", "Rectangle with circular holes" },
            (selectedIndex, selectedText) =>
            {
                _currentShapeType = (SampleShapeTypes)selectedIndex;
                UpdateCurrentShape();
            }, selectedItemIndex: (int)_currentShapeType);


        ui.AddSeparator();

        var extrudeVectors = new Vector3[] { new Vector3(0, 15, 0), new Vector3(0, 30, 0), new Vector3(30, 0, 0), new Vector3(0, 0, -30), new Vector3(10, 30, 0) };
        var shapeYVectors = new Vector3[]  { new Vector3(0, 0, 1), new Vector3(0, 0, 1), new Vector3(0, -1, 0),  new Vector3(0, -1, 0),  new Vector3(0, 0, 1) }; // note that shape 2D y axis down
        ui.CreateComboBox(extrudeVectors.Select(v => $"({v.X:F0}, {v.Y:F0}, {v.Z:F0})").ToArray(), (itemIndex, itemText) =>
        {
            _extrudeVector = extrudeVectors[itemIndex];
            _shapeYVector  = shapeYVectors[itemIndex];
            UpdateCurrentShape();
        }, selectedItemIndex: 1, keyText: "Extrude vector:", keyTextWidth: 95, width: 90);


        ui.AddSeparator();

        ui.CreateCheckBox("Semi-transparent 3D model", isInitiallyChecked: _showSemiTransparentModel, isChecked =>
        {
            _showSemiTransparentModel = isChecked;
            UpdateCurrentShape();
        });

        
        ui.AddSeparator();

        ui.CreateCheckBox("Show individual triangles:", isInitiallyChecked: _showIndividualTriangles, isChecked =>
        {
            _showIndividualTriangles = isChecked;
            UpdateCurrentShape();
        });

        _shownTrianglesSlider = ui.CreateSlider(0, 100, 
            () => _shownTrianglesPercent, 
            sliderValue => UpdateShownTriangles(sliderValue),
            width: 140, 
            formatShownValueFunc: sliderValue => $"{_shownTriangles} / {_trianglesCount}",
            shownValueWidth: -1); // do not set the Width of the control that shows value text

    }
}