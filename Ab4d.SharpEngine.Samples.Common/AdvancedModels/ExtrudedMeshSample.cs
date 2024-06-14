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

        // Convert 2D positions to 3D positions (set y to zero)

        // IMPORTANT:
        // In this sample the shape2DPositions are oriented in anti-clockwise orientation.
        // If we map: x2 = x1, y2 = 0, z2 = y1 this changes the order of 3D position to clockwise.
        // The reason for this is that by default the Z axis is pointing to the viewer
        // (but if 2D coordinate system is put to the XY plane, then Y is pointing into the screen).
        // To prevent flipping the orientation, we need to negate the 2D y value when it is set to 3D z position.
        //
        // The code below shows how to correctly convert 2D positions to 3D position for all 4 possible coordinate systems.
        //
        // This can be tested by unchecking the "Show individual triangles" checkbox.
        // In this case the front triangles are shown with orange material and back triangles are shown with red material.

        float yInvertFactor = 1;

        if (Scene != null)
        {
            var coordinateSystem = Scene.GetCoordinateSystem();
            if (coordinateSystem == CoordinateSystems.YUpLeftHanded || coordinateSystem == CoordinateSystems.ZUpRightHanded)
                yInvertFactor = -1;
        }

        var shape3DPositions = new Vector3[shape2DPositions.Length];
        for (var i = 0; i < shape2DPositions.Length; i++)
            shape3DPositions[i] = new Vector3(shape2DPositions[i].X, 0, shape2DPositions[i].Y * yInvertFactor);


        // Show shape as a poly-line
        var polyLineNode = new PolyLineNode(shape3DPositions, lineColor: Colors.Orange, lineThickness: 3);
        polyLineNode.IsClosed = true; // Connect last and first position to close the poly-line
        polyLineNode.Transform = new TranslateTransform(y: 80);
        parentGroupNode.Add(polyLineNode);


        // Triangulate 2D shape
        var triangleIndices = Triangulator.Triangulate(shape2DPositions).ToArray();

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
        Vector2[][] multiplePolygons = GetMultiShapePositions(currentShapeType);

        bool isShapeYUp = false; // The 2D coordinates that defined the shape have Y axis down (0,0) is at top left corner

        var outerPolygon = multiplePolygons[0];
        var triangulator = new Triangulator(outerPolygon, isYAxisUp: isShapeYUp);

        for (int i = 1; i < multiplePolygons.Length; i++)
        {
            bool isCorrectOrientation = triangulator.AddHole(multiplePolygons[i]);
            if (!isCorrectOrientation)
                ReversePointsOrder(multiplePolygons[i]);
        }
        
        triangulator.Triangulate(out List<Vector2> allPolygonPositions, out List<int> triangleIndicesList);

        // Triangulate 2D shapes
        // Triangulator also generates a list of all polygon positions
        //Triangulator.Triangulate(multiplePolygons, out List<Vector2> allPolygonPositions, out List<int> triangleIndicesList);


        // Convert 2D positions to 3D positions (set y to zero)

        // IMPORTANT:
        // In this sample the shape2DPositions are oriented in anti-clockwise orientation.
        // If we map: x2 = x1, y2 = 0, z2 = y1 this changes the order of 3D positions to clockwise.
        // The reason for this is that by default the Z axis is pointing to the viewer
        // (but if 2D coordinate system is put to the XY plane, then Z is pointing into the screen).
        // To prevent flipping the orientation, we need to negate the 2D y value when it is set to 3D z position.
        //
        // The code below shows how to correctly convert 2D positions to 3D position for all 4 possible coordinate systems.
        //
        // This can be tested by unchecking the "Show individual triangles" checkbox.
        // In this case the front triangles are shown with orange material and back triangles are shown with red material.

        float yInvertFactor = 1;

        if (Scene != null)
        {
            var coordinateSystem = Scene.GetCoordinateSystem();
            if (coordinateSystem == CoordinateSystems.YUpLeftHanded || coordinateSystem == CoordinateSystems.ZUpRightHanded)
                yInvertFactor = -1;
        }

        if (isShapeYUp)
            yInvertFactor *= -1; // flip yInvertFactor


        var shape3DPositions = new Vector3[allPolygonPositions.Count];
        for (var i = 0; i < allPolygonPositions.Count; i++)
            shape3DPositions[i] = new Vector3(allPolygonPositions[i].X, 0, allPolygonPositions[i].Y * yInvertFactor);


        //// Convert 2D shape to 3D positions
        //var shape3DPositions = new Vector3[allPolygonPositions.Count];
        //for (var i = 0; i < allPolygonPositions.Count; i++)
        //    shape3DPositions[i] = new Vector3(allPolygonPositions[i].X, 0, allPolygonPositions[i].Y); // Set y to zero


        // Show shape as a poly-line
        //var polyLineNode = new PolyLineNode(shape3DPositions, lineColor: Colors.Orange, lineThickness: 3);
        //polyLineNode.IsClosed = true; // Connect last and first position to close the poly-line
        //polyLineNode.Transform = new TranslateTransform(y: 80);
        //parentGroupNode.Add(polyLineNode);

        for (var i = 0; i < multiplePolygons.Length; i++)
        {
            var onePolygon = multiplePolygons[i];
            var onePolygonPositions3D = new Vector3[onePolygon.Length];
            for (var j = 0; j < onePolygon.Length; j++)
                onePolygonPositions3D[j] = new Vector3(onePolygon[j].X, 0, onePolygon[j].Y * yInvertFactor);

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
            isYAxisUp: triangulator.IsYAxisUp,
            flipNormals: flipNormals,
            //modelOffset: new Vector3(0, 0, 0),
            modelOffset: new Vector3(0, -130, 0),
            extrudeVector: _extrudeVector,
            shapeYVector: _shapeYVector, // A 3D vector that defines the 3D direction along the 2D shape surface (i.e., the Y axis of the base 2D shape)
            //extrudeVector: new Vector3(0, 0, 30),
            //shapeYVector: new Vector3(0, 1, 0), 
            textureCoordinatesGenerationType: MeshFactory.ExtrudeTextureCoordinatesGenerationType.None,
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
                                                 isAntiClockwise: true);
                break;

            case SampleShapeTypes.Circle:
                shapePositions = CreateNGonShape(center: new Vector2(0, 0), 
                                                 radius: 50, 
                                                 numCorners: 60, 
                                                 isAntiClockwise: true);
                break;
            
            case SampleShapeTypes.CShape:
                // anti-clockwise orientation of C shape
                shapePositions = new Vector2[]
                {
                    new Vector2(60, -40), 
                    new Vector2(-40, -40), 
                    new Vector2(-40, 40), 
                    new Vector2(40, 40), 
                    new Vector2(40, 20), 
                    new Vector2(-20, 20), 
                    new Vector2(-20, -20), 
                    new Vector2(60, -20)
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
                                                              isAntiClockwise: true);

                var hole1 = CreateNGonShape(center: new Vector2(5, 12), 
                                            radius: 6, 
                                            numCorners: 10, 
                                            isAntiClockwise: false);

                var hole2 = CreateNGonShape(center: new Vector2(20, -5),
                                            radius: 10, 
                                            numCorners: 20, 
                                            isAntiClockwise: false);

                multiShapePositions = new Vector2[3][] { rectangleShapePositions, hole1, hole2 };
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(shapeType), shapeType, null);  // Shapes WITHOUT holes are not supported
        }

        return multiShapePositions;
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
        var shapeYVectors = new Vector3[]  { new Vector3(0, 0, -1), new Vector3(0, 0, -1), new Vector3(0, 1, 0),  new Vector3(0, 1, 0),  new Vector3(0, 0, -1) };
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