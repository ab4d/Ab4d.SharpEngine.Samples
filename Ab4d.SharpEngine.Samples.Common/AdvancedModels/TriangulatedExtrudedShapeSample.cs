using System.Drawing;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class TriangulatedExtrudedShapeSample : CommonSample
{
    public override string Title => "Triangulated and extruded 2D shape";
    public override string Subtitle => "Triangulation creates triangle indices (define triangles) from 2D shape.\nExtruded 3D models are created by extruding a 2D shape along a 3D vector.\nIt is also possible to create extruded models from shapes with holes.";

    private enum SampleShapeTypes
    {
        Hexagon,
        Circle,
        CircleSmooth,
        CShape,
        RectangleWithHoles,
        CircleWithHoles
    }

    private bool _showIndividualTriangles = true;
    private bool _showSemiTransparentModel = true;
    private SampleShapeTypes _currentShapeType = SampleShapeTypes.Hexagon;

    private Vector3 _extrudeVector = new Vector3(0, 0, -30);
    private Vector3 _shapeXVector  = new Vector3(1, 0, 0);
    private Vector3 _shapeYVector  = new Vector3(0, 1, 0);

    private GroupNode? _generatedObjectsGroup;
    private GroupNode? _individualTrianglesNode;

    private int _trianglesCount;
    private int _shownTriangles;
    private float _shownTrianglesPercent = 100;

    private ICommonSampleUIElement? _shownTrianglesSlider;
    private ICommonSampleUIElement? _shapeDirectionsLabel;

    private bool IsShapeWithHoles => _currentShapeType >= SampleShapeTypes.RectangleWithHoles;

    public TriangulatedExtrudedShapeSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        _generatedObjectsGroup = new GroupNode("GeneratedObjects");

        scene.RootNode.Add(_generatedObjectsGroup);

        UpdateCurrentShape();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 25;
            targetPositionCamera.Attitude = -10;
            targetPositionCamera.Distance = 700;
            targetPositionCamera.TargetPosition = new Vector3(-50, 20, 0);
        }

        ShowCameraAxisPanel = true;


        var textBlockFactory = await context.GetTextBlockFactoryAsync();

        scene.RootNode.Add(textBlockFactory.CreateTextBlock("2D shape",           new Vector3(-70, 110, 0),  textAttitude: 90, positionType: PositionTypes.Right));
        scene.RootNode.Add(textBlockFactory.CreateTextBlock("Triangulated shape", new Vector3(-70, 0, 0),    textAttitude: 90, positionType: PositionTypes.Right));
        scene.RootNode.Add(textBlockFactory.CreateTextBlock("Extruded shape",     new Vector3(-70, -110, 0), textAttitude: 90, positionType: PositionTypes.Right));
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
        bool isShapeYUp = true;

        // Get 2D shape positions
        var shape2DPositions = GetSimpleShapePositions(currentShapeType, isShapeYUp);


        // Triangulate 2D shape to get triangle indices that define the triangles
        Triangulator triangulator = new Triangulator(shape2DPositions, isShapeYUp);
        var triangleIndices =  triangulator.CreateTriangleIndices().ToArray();

        // We can also use the static Triangulate, but then we cannot get the value of triangulator.IsClockwise that is used below
        //var triangleIndices = Triangulator.Triangulate(shape2DPositions).ToArray();


        // Convert 2D positions to 3D positions on XZ plane (set Y to 0)
        // See also: https://www.ab4d.com/help/SharpEngine/html/M_Ab4d_SharpEngine_Utilities_MeshUtils_Convert2DShapeTo3DPositions.htm
        var coordinateSystem = Scene?.GetCoordinateSystem() ?? CoordinateSystems.YUpRightHanded;
        Base3DPlaneTypes targetPlane = Base3DPlaneTypes.XY; // GetTargetPlaneFromExtrudeVector(_extrudeVector);
        var shape3DPositions = MeshUtils.Convert2DShapeTo3DPositions(shape2DPositions, targetPlane, isShapeYUp, coordinateSystem);


        // Show shape as a poly-line (show arrow at the end of line to show the orientation of the shape)
        // Add first position to last position to close the shape (if we would set IsClosed to true on PolyLineNode, then arrow is not shown)
        var shapePositionsList = shape3DPositions.ToList();
        shapePositionsList.Add(shape3DPositions[0]);
        shape3DPositions = shapePositionsList.ToArray(); 

        var polyLineNode = new PolyLineNode(shape3DPositions, lineColor: Colors.Orange, lineThickness: 3)
        {
#if VULKAN
            EndLineCap = LineCap.ArrowAnchor, // Add arrow to show the orientation of the shape
#endif
            Transform = new TranslateTransform(y: 110)
        };

        parentGroupNode.Add(polyLineNode);


        if (_showIndividualTriangles)
        {
            // Show each triangle with its own color
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


        // When isSmooth (passed to CreateExtrudedMesh) is false, then side positions are duplicated to create sharp edges. 
        // When isSmooth is true, then side positions are not duplicated and the side is smooth.
        bool isSmooth = currentShapeType == SampleShapeTypes.CircleSmooth;

        // Create extruded mesh - extrude the 2D in the extrudeVector direction
        var extrudedMesh = MeshFactory.CreateExtrudedMesh(positions: shape2DPositions,
                                                          isSmooth: isSmooth,
                                                          modelOffset: new Vector3(0, -110, 0),
                                                          extrudeVector: _extrudeVector,
                                                          shapeXVector: _shapeXVector, // if shapeXVector is not specified, then it is calculated by using Vector3.Cross(extrudeVector, shapeYVector)
                                                          shapeYVector: _shapeYVector, // A 3D vector that defines the 3D direction along the 2D shape surface (i.e., the Y axis of the base 2D shape)
                                                          isYAxisUp: isShapeYUp,
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
        bool isShapeYUp = true;

        // Shape with holes is represented by multiple arrays of positions
        // The holes are identified by the orientation of positions (clockwise or anti-clockwise) that is different from the orientation of positions in the outer polygons.
        // The orientation of the outer polygons is defined by the orientation of the first polygon.
        // In this sample the outer polygons are oriented in clockwise direction, holes in anti-clockwise direction
        Vector2[][] multiplePolygons = GetMultiShapePositions(currentShapeType, isShapeYUp);


        // Create Triangulator that will be used to get triangle indices that define the triangles
        var outerPolygon = multiplePolygons[0];
        var triangulator = new Triangulator(outerPolygon, isShapeYUp);

        for (int i = 1; i < multiplePolygons.Length; i++)
        {
            // When calling AddHole the method checks if the specified positions are oriented in the opposite direction as the outerPolygon.
            // If not, then the isCorrectOrientation is set to false and we can fix that by reversing the orientation of positions
            // (this is needed when multiplePolygons is passed to the MeshFactory.CreateExtrudedMesh;
            // if we only need triangle indices, then this is not needed because triangulator will internally reverse the order of positions).
            bool isCorrectOrientation = triangulator.AddHole(multiplePolygons[i]);

            if (!isCorrectOrientation)
                ReversePointsOrder(multiplePolygons[i]);
        }

        // After we added the outer polygon and all the holes, we can call the Triangulate method.
        // This returns a list of triangulated positions (the outer polygons and holes are connected into a single polygon)
        // and a list of triangle indices that defines the triangles that can show the specified polygon and holes.
        triangulator.Triangulate(out List<Vector2> triangulatedPositions, out List<int> triangleIndicesList);


        // The same can be achieved by calling the static Triangulator.Triangulate method.
        // But here we need to make sure that the hole polygons are correctly oriented as we cannot switch their order.
        // Also, in this case we cannot get the value of triangulator.IsClockwise that is used below.
        //Triangulator.Triangulate(multiplePolygons, out List<Vector2> triangulatedPositions, out List<int> triangleIndicesList);


        // Convert 2D positions to 3D positions on XY plane (set Z to 0)
        // See also: https://www.ab4d.com/help/SharpEngine/html/M_Ab4d_SharpEngine_Utilities_MeshUtils_Convert2DShapeTo3DPositions.htm
        var coordinateSystem = Scene?.GetCoordinateSystem() ?? CoordinateSystems.YUpRightHanded;
        Base3DPlaneTypes targetPlane = Base3DPlaneTypes.XY; // GetTargetPlaneFromExtrudeVector(_extrudeVector);
        var shape3DPositions = MeshUtils.Convert2DShapeTo3DPositions(triangulatedPositions.ToArray(), targetPlane, isShapeYUp, coordinateSystem);


        for (var i = 0; i < multiplePolygons.Length; i++)
        {
            var onePolygonPositions3D = MeshUtils.Convert2DShapeTo3DPositions(multiplePolygons[i], targetPlane, isShapeYUp, coordinateSystem);

            // Show shape as a poly-line (show arrow at the end of line to show the orientation of the shape)
            // Add first position to last position to close the shape (if we would set IsClosed to true on PolyLineNode, then arrow is not shown)
            var shapePositionsList = onePolygonPositions3D.ToList();
            shapePositionsList.Add(onePolygonPositions3D[0]);
            onePolygonPositions3D = shapePositionsList.ToArray();

            var polyLineNode = new PolyLineNode(onePolygonPositions3D, lineColor: Colors.Orange, lineThickness: 3)
            {
#if VULKAN
                EndLineCap = LineCap.ArrowAnchor, // Add arrow to show the orientation of the shape
#endif
                Transform = new TranslateTransform(y: 110)
            };

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

        // Generate the extruded mesh

        // When isSmooth (passed to CreateExtrudedMesh) is false, then side positions are duplicated to create sharp edges. 
        // When isSmooth is true, then side positions are not duplicated and the side is smooth.
        bool isSmooth = currentShapeType == SampleShapeTypes.CircleWithHoles;

        var extrudedMesh = MeshFactory.CreateExtrudedMesh(triangulatedPositions: triangulatedPositions.ToArray(),
                                                          triangleIndices: triangleIndices,
                                                          allPolygons: multiplePolygons,
                                                          isSmooth: isSmooth,
                                                          flipNormals: false,
                                                          modelOffset: new Vector3(0, -110, 0),
                                                          extrudeVector: _extrudeVector,
                                                          shapeXVector: _shapeXVector, // if shapeXVector is not specified, then it is calculated by using Vector3.Cross(extrudeVector, shapeYVector)
                                                          shapeYVector: _shapeYVector, // A 3D vector that defines the 3D direction along the 2D shape surface (i.e., the Y axis of the base 2D shape)
                                                          textureCoordinatesGenerationType: MeshFactory.ExtrudeTextureCoordinatesGenerationType.None,
                                                          isYAxisUp: isShapeYUp,
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

    private Vector2[] GetSimpleShapePositions(SampleShapeTypes shapeType, bool isYAxisUp)
    {
        Vector2[] shapePositions;

        switch (shapeType)
        {
            case SampleShapeTypes.Hexagon:
                shapePositions = CreateNGonShape(center: new Vector2(0, 0), 
                                                 radius: 50, 
                                                 numCorners: 6, 
                                                 isClockwiseOrientation: false,
                                                 isYAxisUp);
                break;

            case SampleShapeTypes.Circle:
            case SampleShapeTypes.CircleSmooth:
                shapePositions = CreateNGonShape(center: new Vector2(0, 0), 
                                                 radius: 45, 
                                                 numCorners: 30, 
                                                 isClockwiseOrientation: false,
                                                 isYAxisUp);
                break;
            
            case SampleShapeTypes.CShape:
                // counter-clockwise orientation of C shape (isYAxisUp: true)
                shapePositions = new Vector2[]
                {
                    new Vector2(60, -20),
                    new Vector2(-20, -20), 
                    new Vector2(-20, 20), 
                    new Vector2(40, 20), 
                    new Vector2(40, 40), 
                    new Vector2(-40, 40), 
                    new Vector2(-40, -40), 
                    new Vector2(60, -40),
                };     
                
                if (!isYAxisUp)
                    ReversePointsOrder(shapePositions);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(shapeType), shapeType, null); // Shapes with holes are not supported
        }

        return shapePositions;
    }

    private Vector2[][] GetMultiShapePositions(SampleShapeTypes shapeType, bool isYAxisUp)
    {
        Vector2[][] multiShapePositions;

        switch (shapeType)
        {
            case SampleShapeTypes.RectangleWithHoles:
                // Define an outer polygon and two holes
                // We use Y-up 2D coordinate system.
                // Outer polygons needs to be defined in counter-clockwise direction, holes in clockwise direction
                var outerPositions = new Vector2[]
                {
                    new Vector2(-50,-40),
                    new Vector2(50,-40),
                    new Vector2(50,40),
                    new Vector2(-50,40),
                };

                // 2 holes (clockwise direction):
                var holePositions1 = new Vector2[]
                {
                    new Vector2(-20,20),
                    new Vector2(-20,30),
                    new Vector2(0,30),
                    new Vector2(0,20),
                };

                // NOTE: This hole is organized in counter-clockwise direction (y-up) - the AddHole method will return false and we will need to reverse the direction
                var holePositions2 = new Vector2[]
                {
                    new Vector2(30,0),
                    new Vector2(30,30),
                    new Vector2(10,30),
                    new Vector2(10,0),
                };

                if (!isYAxisUp)
                {
                    ReversePointsOrder(outerPositions);
                    ReversePointsOrder(holePositions1);
                    ReversePointsOrder(holePositions2);
                }

                multiShapePositions = new Vector2[][] { outerPositions, holePositions1, holePositions2 };
                break;

            case SampleShapeTypes.CircleWithHoles:
                // Create shapes with n-edges
                // The orientation of outer shape should be counter-clockwise (y-up),
                // the orientation of holes need to be clockwise
                var rectangleShapePositions = CreateNGonShape(center: new Vector2(0, 0), 
                                                              radius: 45, 
                                                              numCorners: 40, 
                                                              isClockwiseOrientation: false,
                                                              isYAxisUp);

                var hole1 = CreateNGonShape(center: new Vector2(0, 22), 
                                            radius: 6, 
                                            numCorners: 10, 
                                            isClockwiseOrientation: true,
                                            isYAxisUp);

                var hole2 = CreateNGonShape(center: new Vector2(15, 5),
                                            radius: 10, 
                                            numCorners: 20, 
                                            isClockwiseOrientation: true,
                                            isYAxisUp);

                multiShapePositions = new Vector2[3][] { rectangleShapePositions, hole1, hole2 };
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(shapeType), shapeType, null);  // Shapes WITHOUT holes are not supported in GetMultiShapePositions method
        }

        return multiShapePositions;
    }

    // isYAxisUp defines the orientation of Y axis in 2D coordinate system
    private static Vector2[] CreateNGonShape(Vector2 center, float radius, int numCorners, bool isClockwiseOrientation, bool isYAxisUp)
    {
        var corners = new Vector2[numCorners];
        var angleStep = 2 * MathF.PI / numCorners;

        // When the direction of Y axis is changed, then the orientation is also changed
        if (!isYAxisUp)
            isClockwiseOrientation = !isClockwiseOrientation;

        for (var i = 0; i < numCorners; i++)
        {
            var (sin, cos) = MathF.SinCos(i * angleStep);

            if (isClockwiseOrientation)
                cos = -cos; // this inverses the X value which inverses the orientation of the shape

            corners[i] = new Vector2(center.X + cos * radius, center.Y + sin * radius);
        }

        return corners;
    }
        
    private GroupNode CreateIndividualTrianglesNode(Vector3[] shape3DPositions, int[] triangleIndices)
    {
        _individualTrianglesNode = new GroupNode("IndividualTrianglesNode");

        var singleTriangleIndices = new int[] { 0, 1, 2 }; // triangle indices for a single triangle
        var colorHue = 0;

        for (int i = 0; i < triangleIndices.Length; i += 3)
        {
            var positions = new Vector3[3];

            for (int j = 0; j < 3; j++)
            {
                int index = triangleIndices[i + j];
                positions[j] = shape3DPositions[index];
            }

            var triangulatedMesh = new GeometryMesh(positions, singleTriangleIndices);

            var color = Color3.FromHsl(colorHue);
            colorHue += 33;

            var standardMaterial = new StandardMaterial(color);

            var triangulatedModelNode = new MeshModelNode(triangulatedMesh, standardMaterial);
            triangulatedModelNode.BackMaterial = standardMaterial;

            _individualTrianglesNode.Add(triangulatedModelNode);
        }

        _trianglesCount = triangleIndices.Length / 3;
        UpdateShownTriangles(_shownTrianglesPercent);

        if (_shownTrianglesSlider != null)
            _shownTrianglesSlider.UpdateValue();

        return _individualTrianglesNode;
    }

    //private Base3DPlaneTypes GetTargetPlaneFromExtrudeVector(Vector3 extrudeVector)
    //{
    //    var x = MathF.Abs(extrudeVector.X);
    //    var y = MathF.Abs(extrudeVector.Y);
    //    var z = MathF.Abs(extrudeVector.Z);

    //    if (x > y && x > z)
    //        return Base3DPlaneTypes.YZ;

    //    if (y > x && y > z)
    //        return Base3DPlaneTypes.XZ;

    //    return Base3DPlaneTypes.XY;
    //}

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

        ui.CreateRadioButtons(new string[] { "Hexagon", "Circle (isSmooth: false)", "Circle (isSmooth: true)", "C shape", "Rectangle with simple holes", "Circle with circular holes" },
            (selectedIndex, selectedText) =>
            {
                _currentShapeType = (SampleShapeTypes)selectedIndex;
                UpdateCurrentShape();
            }, selectedItemIndex: (int)_currentShapeType);


        ui.AddSeparator();

        var extrudeVectors = new Vector3[] { new Vector3(0, 0, -15), new Vector3(0, 0, -30), new Vector3(0, 0, 30), new Vector3(0, 30, 0), new Vector3(0, -30, 0), new Vector3(30, 0, 0), new Vector3(-30, 0, 0) };
        var shapeXVectors  = new Vector3[] { new Vector3(1, 0, 0),   new Vector3(1, 0, 0),   new Vector3(1, 0, 0),  new Vector3(1, 0, 0),  new Vector3(1, 0, 0),   new Vector3(0, 0, 1),  new Vector3(0, 0, 1) };
        var shapeYVectors  = new Vector3[] { new Vector3(0, 1, 0),   new Vector3(0, 1, 0),   new Vector3(0, 1, 0),  new Vector3(0, 0, -1), new Vector3(0, 0, -1),  new Vector3(0, 1, 0),  new Vector3(0, 1, 0) };
        ui.CreateComboBox(extrudeVectors.Select(v => $"({v.X:F0}, {v.Y:F0}, {v.Z:F0})").ToArray(), (itemIndex, itemText) =>
        {
            _extrudeVector = extrudeVectors[itemIndex];
            _shapeXVector  = shapeXVectors[itemIndex];
            _shapeYVector  = shapeYVectors[itemIndex];
            _shapeDirectionsLabel?.UpdateValue();
            UpdateCurrentShape();
        }, selectedItemIndex: 1, keyText: "Extrude vector:", keyTextWidth: 95, width: 90);

        _shapeDirectionsLabel = ui.CreateKeyValueLabel("", () => $"shapeXVector: ({_shapeXVector.X:F0}, {_shapeXVector.Y:F0}, {_shapeXVector.Z:F0})\nshapeYVector: ({_shapeYVector.X:F0}, {_shapeYVector.Y:F0}, {_shapeYVector.Z:F0})");

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