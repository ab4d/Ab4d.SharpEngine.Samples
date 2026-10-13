using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Graphs;

public class Graph2DSample : CommonSample
{
    public override string Title => "2D Graph";
    public override string Subtitle => "This sample shows how to show a simple 2D graph.\nBecause of very fast rendering it would be possible to show graphs with millions of line segments.\nSee also Cameras/TwoDimensionalCamera sample for more examples on how to show 2D lines and shapes.";

    private const float GraphWidth = 360;
    private const float GraphHeight = 100;

    private TwoDimensionalCamera? _twoDimensionalCamera;
    private bool _isFitToViewSubscribed;

    public Graph2DSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnDisposed()
    {
        UnsubscribeFitToView();
        base.OnDisposed();
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        var textBlockFactory = await context.GetTextBlockFactoryAsync();

        if (this.IsDisposed) // return if the test was already changed while waiting for async tasks
            return;


        //
        // Show x and y graph axes
        //

        var xAxis = new AxisWithLabelsNode(textBlockFactory.BitmapTextCreator, axisTitle: "time [s]")
        {
            AxisStartPosition = new Vector3(0, 0, 0),
            AxisEndPosition = new Vector3(GraphWidth, 0, 0),
            RightDirectionVector = new Vector3(0, 1, 0), // RightDirectionVector3 is the direction in which the text is drawn. By default, RightDirectionVector3 points to the right (1, 0, 0). We need to change that because this is also this axis direction.
            
            // Manually define the direction and PositionType of value labels to align the text so it is correctly shown with a TwoDimensionalCamera
            CustomValueLabelsDirection = new Vector3(1, 0, 0), 
            CustomValueLabelsUpDirection = new Vector3(0, 1, 0),
            CustomValueLabelsPositionType = PositionTypes.Top,
            
            MinimumValue = 0,
            MaximumValue = 120,
            MajorTicksStep = 10,
            MinorTicksStep = 2,
        };

        scene.RootNode.Add(xAxis);


        var yAxis = new AxisWithLabelsNode(textBlockFactory.BitmapTextCreator)
        {
            AxisStartPosition = new Vector3(0, 0, 0),
            AxisEndPosition = new Vector3(0, GraphHeight, 0),
            MinimumValue = 0,
            MaximumValue = 1,
            MajorTicksStep = 0.2f,
            MinorTicksStep = 0.1f,
            ValueDisplayFormatString = "0.0"
        };

        scene.RootNode.Add(yAxis);


        //
        // Show graph with 
        //

        int valueCount = 120;

        float xScale = GraphWidth / (valueCount - 1);
        float yScale = GraphHeight;


        // Show graph line with shape under the line
        var graphValues1 = CreateGraphValues(valueCount, offset: 2, factor: 3, scale: 1);
        ShowGraph(graphValues1, xScale, yScale, zIndex: 0, lineColor: Colors.Green, shapeColor: Colors.LightGreen, scene: scene);

        // Show two additional lines without shapes
        var graphValues2 = CreateGraphValues(valueCount, offset: 0, factor: 2, scale: 1f);
        ShowGraph(graphValues2, xScale, yScale, zIndex: 1, lineColor: Colors.Orange, shapeColor: Colors.Transparent, scene: scene); // Show only line

        var graphValues3 = CreateGraphValues(valueCount, offset: 3, factor: 4, scale: 0.8f);
        ShowGraph(graphValues3, xScale, yScale, zIndex: 2, lineColor: Colors.Brown, shapeColor: Colors.Transparent, scene: scene); // Show only line
    }

    private void ShowGraph(float[] graphValues, float xScale, float yScale, float zIndex, Color3 lineColor, Color3 shapeColor, Scene scene)
    {
        // Convert 2D graphValues to 3D positions ...
        var graphPositions = new Vector3[graphValues.Length];
        for (var i = 0; i < graphValues.Length; i++)
            graphPositions[i] = new Vector3(i * xScale, graphValues[i] * yScale, zIndex);

        // ... that can be used to show a PolyLineNode.
        // PolyLineNode shows connected straight lines between the positions.
        // If you want to create a smooth curve from positions, see the Lines/CurveLineSample.
        var graphLine = new PolyLineNode(graphPositions, lineColor: lineColor, lineThickness: 2, "GraphLine");
        scene.RootNode.Add(graphLine);


        // Create 2D shape under the graph line.
        // This is done by first generating the triangles (Triangulate) that will be shown by the MeshModelNode

        if (shapeColor != Colors.Transparent)
        {
            // Use special GraphTriangulator that can very efficiently triangulate the graph values to create a shape under the graph line.
            // It would be also possible to use the Triangulator class, but it is much slower because it is not optimized for this special case of triangulating a graph shape.
            var (shapePositions2D, shapeTriangleIndices) = GraphTriangulator.Triangulate(graphValues, xStart: 0, xScale, yBaseline: 0);

            var shapePositions3D = new Vector3[shapePositions2D.Length];
            var shapeNormals = new Vector3[shapePositions2D.Length];

            for (var i = 0; i < shapePositions2D.Length; i++)
            {
                shapePositions3D[i] = new Vector3(shapePositions2D[i].X, shapePositions2D[i].Y * yScale, zIndex);
                shapeNormals[i] = new Vector3(0, 0, 1); // all normals point towards 2D camera
            }

            var geometryMesh = new GeometryMesh(shapePositions3D, shapeNormals, shapeTriangleIndices, "GraphShapeMesh");

            var material = new SolidColorMaterial(shapeColor);
            var meshModelNode = new MeshModelNode(geometryMesh, material, "GraphShapeModel");
         
            scene.RootNode.Add(meshModelNode);
        }
    }

    /// <inheritdoc />
    public override void InitializePointerCameraController(ManualPointerCameraController pointerCameraController)
    {
        // First call base.InitializePointerCameraController to set default pointerCameraController properties for common sample
        base.InitializePointerCameraController(pointerCameraController);


        // Then create the TwoDimensionalCamera.
        // TwoDimensionalCamera will internally create a TargetPositionCamera and update the settings to pointerCameraController.
        // They will be used to show the 2D scene.

        // NOTE: TwoDimensionalCamera class is available with full source in this samples project in the Common folder.

        _twoDimensionalCamera = new TwoDimensionalCamera(
                pointerCameraController,
                useScreenPixelUnits: true, // when false, then the size in device independent units is used
                coordinateSystemType: TwoDimensionalCamera.TwoDimensionalCoordinateSystems.CenterOfViewOrigin) // This is also the default option and currently the only available option
            {
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,
                QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed,
                IsWheelZoomEnabled = true,
                WheelDistanceChangeFactor = 1.2f, // Increase this value to zoom faster; decrease to zoom slower (but the value must not be lower than 1)
            };

        FitGraphToView();
    }

    public static float[] CreateGraphValues(int valueCount, float offset, float factor, float scale)
    {
        var graphValues = new float[valueCount];

        for (var i = 0; i < valueCount; i++)
        {
            var progress = i / (float)(valueCount - 1);
            var angle = progress * MathF.Tau;

            graphValues[i] = (0.5f
                             + 0.3f * MathF.Sin(angle + offset)
                             + 0.15f * MathF.Sin(angle * factor)) * scale;
        }

        return graphValues;
    }
    
    private void SubscribeFitToView()
    {
        if (this.SceneView != null && !_isFitToViewSubscribed)
        {
            this.SceneView.ViewResized += OnSceneViewOnViewResized;
            _isFitToViewSubscribed = true;
        }
    }

    private void UnsubscribeFitToView()
    {
        if (this.SceneView != null && _isFitToViewSubscribed)
        {
            this.SceneView.ViewResized -= OnSceneViewOnViewResized;
            _isFitToViewSubscribed = false;
        }
    }
    
    private void OnSceneViewOnViewResized(object sender, ViewSizeChangedEventArgs args)
    {
        FitGraphToView(margin: 140);
    }

    private void FitGraphToView(float margin = 140)
    {
        if (_twoDimensionalCamera != null && _twoDimensionalCamera.ViewSize.X > 0)
        {
            _twoDimensionalCamera.Offset = new Vector2(GraphWidth * 0.5f, GraphHeight * 0.5f); // Set to center of the graph that goes from (0, 0) to (360, 100)
            _twoDimensionalCamera.ZoomFactor = _twoDimensionalCamera.ViewSize.X / (GraphWidth + margin); // Zoom so that the graph is visible
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(alignment: PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Fit graph to view (?):When checked and when the size of the Window is changed, then the graph is resized to fit the view", false, isChecked =>
        {
            if (isChecked)
            {
                FitGraphToView();
                SubscribeFitToView();
            }
            else
            {
                UnsubscribeFitToView();
            }
        });
    }
}