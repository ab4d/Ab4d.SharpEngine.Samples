using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public class TwoDimensionalCameraSample : CommonSample
{
    public override string Title => "TwoDimensionalCamera";
    public override string Subtitle => "TwoDimensionalCamera can be used to easily show 2D graphics with Ab4d.SharpEngine.\nSee also 'Graphs/2D Graph' for an example on how to show 2D graphs.";

    private TwoDimensionalCamera? _twoDimensionalCamera;
    private ICommonSampleUIElement? _sceneInfoLabel;

    private VectorFontFactory? _vectorFontFactory;

    public TwoDimensionalCameraSample(ICommonSamplesContext context)
        : base(context)
    {

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

        _twoDimensionalCamera.CameraChanged += delegate (object? sender, EventArgs args)
        {
            UpdateSceneInfo();
        };

        UpdateSceneInfo();
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        //
        // Mark coordinate origin (0, 0)
        //
        var centerPositions = new Vector3[]
        {
            new Vector3(-10,  0, 0),
            new Vector3(10, 0, 0),
            new Vector3(0, -10,  0),
            new Vector3(0,  10,  0),
        };

        var multiLineNode = new MultiLineNode()
        {
            Positions = centerPositions,
            LineColor = Colors.Red,
            LineThickness = 1,
        };

        scene.RootNode.Add(multiLineNode);


        // AddText will be called after the font is loaded async (see below)
        //AddText("Manually created 2D lines and shapes:", new Vector3(-400, 250, 0), Colors.Black, fontSize: 20);

        var simpleShapesGroup = new GroupNode("SimpleShapes")
        {
            Transform = new TranslateTransform(-100, 80, 0)
        };

        scene.RootNode.Add(simpleShapesGroup);


        //
        // Create a few lines
        //
        for (int i = 0; i < 5; i++)
        {
            // 2D coordinates of the line
            float x1 = -300;
            float y1 = -70 + i * 20;
            
            float x2 = -250;
            float y2 = -20 + i * 20;

            var lineNode = new LineNode()
            {
                // Because we use 3D engine to show 2D lines, we need to convert 2D coordinates to 3D coordinates.
                // This is done with setting Z to 0 (but we could use that to move some lines or shapes in front of the other lines).
                StartPosition = new Vector3(x1, y1, 0),
                EndPosition   = new Vector3(x2, y2, 0),
                LineColor     = Colors.Black,
                LineThickness = i + 1
            };

            simpleShapesGroup.Add(lineNode);
        }


        //
        // Create a polyline
        //
        var polygonPositions = new Vector3[]
        {
            new Vector3(-70,  -50, 0),
            new Vector3(-170, -50, 0),
            new Vector3(-170, 50,  0),
            new Vector3(-70,  50,  0),
        };

        var polyLineNode = new PolyLineNode()
        {
            Positions     = polygonPositions,
            LineColor     = Colors.Black,
            LineThickness = 1,
            IsClosed      = true
        };

        simpleShapesGroup.Add(polyLineNode);


        //
        // Create 2 lines with pattern (see LinesWithPatternSample for more info)
        //

        var lineWithPatternMaterial = new LineMaterial(Colors.Orange, lineThickness: 2)
        {
            LinePattern = 0x3333 // 0x3333 is 0011001100110011
        };

        var lineWithPattern1 = new LineNode(lineWithPatternMaterial)
        {
            StartPosition = new Vector3(-300, -100, 0),
            EndPosition = new Vector3(300, -100, 0),
        };

        simpleShapesGroup.Add(lineWithPattern1);


        var lineWithPattern2 = new LineNode(lineWithPatternMaterial)
        {
            StartPosition = new Vector3(-300, -110, 0),
            EndPosition = new Vector3(300, -110, 0),
        };

        simpleShapesGroup.Add(lineWithPattern2);


        //
        // Create curve with BezierCurve class
        //
        var curveControlPoints = new Vector3[]
        {
            new Vector3(0,   0,  0),
            new Vector3(30,  0,  0),
            new Vector3(60,  50, 0),
            new Vector3(90,  0,  0),
            new Vector3(120, 0,  0)
        };

        var bezierCurve = Utilities.BezierCurve.CreateBezierCurvePositionsThroughPoints(curveControlPoints, positionsPerSegment: 20);

        var curveLineNode = new PolyLineNode()
        {
            Positions     = bezierCurve,
            LineColor     = Colors.Green,
            LineThickness = 2,
            Transform     = new TranslateTransform(0, -50, 0)
        };

        simpleShapesGroup.Add(curveLineNode);


        var ellipseLineNode = new EllipseLineNode()
        {
            CenterPosition = new Vector3(60, 40, 0),
            Width = 120,
            Height = 50,
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 1, 0),
            LineColor = Colors.Green,
            LineThickness = 2,
        };

        simpleShapesGroup.Add(ellipseLineNode);


        //
        // Show 2D shapes with using triangulator to convert the shape to a set of triangles.
        //
        var shapePoints = new Vector2[]
        {
            new Vector2(0,   0),
            new Vector2(50,  0),
            new Vector2(100, 50),
            new Vector2(100, 100),
            new Vector2(50,  80),
            new Vector2(50,  40),
            new Vector2(0,   40),
            new Vector2(0,   0),
        };

        var triangulator = new Utilities.Triangulator(shapePoints);
        var triangleIndices = triangulator.CreateTriangleIndices();

        // Convert 2D points to 3D points
        var shapePositions3D = new Vector3[shapePoints.Length];
        var shapeNormals = new Vector3[shapePoints.Length];
        for (var i = 0; i < shapePoints.Length; i++)
        {
            shapePositions3D[i] = new Vector3(shapePoints[i].X, shapePoints[i].Y, 0);
            shapeNormals[i] = new Vector3(0, 0, 1);
        }


        var geometryMesh = new GeometryMesh(shapePositions3D, shapeNormals, triangleIndices.ToArray(), "TriangulatedMesh");

        var meshModelNode = new MeshModelNode(geometryMesh, StandardMaterials.Orange, "TriangulatedModel");

        // NOTE:
        // We set Z of the shape to -0.5 !!! 
        // This will move the solid shape slightly behind the 3D line so the line will be always on top of the shape
        meshModelNode.Transform = new TranslateTransform(200, -50, -0.5f);

        simpleShapesGroup.Add(meshModelNode);


        // Also add an outline to the shape
        var shapeOutlineNode = new PolyLineNode()
        {
            Positions     = shapePositions3D,
            LineColor     = Colors.Black,
            LineThickness = 1,
            Transform     = new TranslateTransform(200, -50, 0)
        };

        simpleShapesGroup.Add(shapeOutlineNode);


        // Add many lines that are read from imported file (created from a 2D CAD file)
        var importedLinesGroup = await LoadSampleLinesDataAsync(lineThickness: 0.8f, targetPosition: new Vector2(-400, -300), targetSize: new Vector2(600, 500));

        scene.RootNode.Add(importedLinesGroup);


        await LoadFont();

        AddText("2D lines and shapes:", new Vector3(-400, 180, 0), Colors.Black, fontSize: 20);
        AddText("Imported 2D CAD drawing:", new Vector3(-400, -100, 0), Colors.Black, fontSize: 20);
    }

    private async Task LoadFont()
    {
        if (Scene == null || Scene.GpuDevice == null)
            return;


        string fontName = "Roboto-Black.ttf";

        try
        {
#if VULKAN
            string fontFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/TrueTypeFonts/", fontName);
            await TrueTypeFontLoader.Instance.LoadFontFileAsync(fontFileName, fontName);
#elif WEB_GL
            string fontFileName = Path.Combine("fonts", fontName);
            await TrueTypeFontLoader.Instance.LoadFontFileAsync(fontFileName, fontName, Scene.GpuDevice.CanvasInterop);
#endif
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error loading font:\n" + ex.Message);
            return;
        }

        // After font is loaded, we can create an instance of VectorFontFactory by passing the fontName to constructor
        _vectorFontFactory = new VectorFontFactory(fontName);
    }

    private void AddText(string text, Vector3 position, Color4 textColor, float fontSize = 20, TextPositionTypes positionType = TextPositionTypes.Baseline)
    {
        if (Scene == null || _vectorFontFactory == null)
            return;

        var textMesh = _vectorFontFactory.CreateTextMesh(text,
                                                         position,
                                                         positionType,
                                                         textDirection: new Vector3(1, 0, 0),
                                                         upDirection: new Vector3(0, 1, 0),
                                                         fontSize: fontSize);

        if (textMesh == null)
            return;

        var usedMaterial = new SolidColorMaterial(textColor);
        var textMeshModelNode = new MeshModelNode(textMesh, usedMaterial)
        {
            BackMaterial = usedMaterial // Make text visible from both sides
        };

        Scene.RootNode.Add(textMeshModelNode);
    }

    private async Task<GroupNode> LoadSampleLinesDataAsync(float lineThickness, Vector2 targetPosition, Vector2 targetSize)
    {
        var linesGroup = new GroupNode("ImportedLines");


        // Read many lines from a custom bin file format.
        // The bin file was created from a metafile (wmf) file that was read by Ab2d.ReaderWmf,
        // then the lines were grouped by color and saved to a custom bin file.

        (List<LineData>? lines, Vector2 boundsPosition, Vector2 boundsSize) = await ReadLineDataFromBinAsync();

        if (lines == null)
            return linesGroup;


        var targetCenter = new Vector2(targetPosition.X + targetSize.X * 0.5f, targetPosition.Y + targetSize.Y * 0.5f);

        float xScale = targetSize.X / boundsSize.X;
        float yScale = targetSize.Y / boundsSize.Y;

        float scale = Math.Min(xScale, yScale); // Preserve aspect ratio - so use the minimal scale

        float xOffset = targetCenter.X - boundsSize.X * scale * 0.5f;
        float yOffset = targetCenter.Y - boundsSize.Y * scale * 0.5f; // targetCenter.Y - bounds.Height * scale * 0.5 + bounds.Height * scale // because we flipped y we need to offset by height


        
        var transformMatrix = new Matrix3x2(scale, 0,
                                            0, -scale, // We also need to flip y axis because here y axis is pointing up
                                            xOffset, yOffset);


        for (var i = 0; i < lines.Count; i++)
        {
            var oneLineData = lines[i];
            var positions2D = oneLineData.Positions;

            var positions3D = new Vector3[positions2D.Count];
            for (var j = 0; j < positions2D.Count; j++)
            {
                var pos2D = Vector2.Transform(positions2D[j], transformMatrix);
                positions3D[j] = new Vector3(pos2D.X, pos2D.Y, 0);
            }

            if (oneLineData.IsLineStrip)
            {
                var polyLineVisual3D = new PolyLineNode()
                {
                    Positions     = positions3D,
                    LineColor     = oneLineData.LineColor,
                    LineThickness = lineThickness < 0 ? oneLineData.LineThickness : lineThickness
                };

                linesGroup.Add(polyLineVisual3D);
            }
            else
            {
                var multiLineNode = new MultiLineNode()
                {
                    Positions     = positions3D,
                    LineColor     = oneLineData.LineColor,
                    LineThickness = lineThickness < 0 ? oneLineData.LineThickness : lineThickness
                };

                linesGroup.Add(multiLineNode);
            }
        }

        return linesGroup;
    }

    private async Task<(List<LineData>?, Vector2 boundsPosition, Vector2 boundsSize)> ReadLineDataFromBinAsync()
    {
        List<LineData> lines;
        Vector2 boundsPosition, boundsSize;

        // Get all the names of the embedded resources in the assembly
        //var manifestResourceNames = typeof(TwoDimensionalCameraSample).Assembly.GetManifestResourceNames();


        // Read many lines from palazz_sport.bin file. This file uses a custom binary file format.
        // The bin file was created from a metafile (wmf) file that was read by Ab2d.ReaderWmf,
        // then the lines were grouped by color and saved to a custom bin file.
        string sampleDataFileName = "Resources/palazz_sport.bin";
        
        Stream? stream;

#if WEB_GL
        if (Scene == null || Scene.GpuDevice == null)
            return (null, Vector2.Zero, Vector2.Zero);

        var sampleFileBytes = await Scene.GpuDevice.CanvasInterop.LoadBinaryFileAsync(sampleDataFileName);
        stream = new MemoryStream(sampleFileBytes);

#else
        string fullFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sampleDataFileName);
        fullFileName = FileUtils.FixDirectorySeparator(fullFileName);
        stream = System.IO.File.OpenRead(fullFileName);
#endif

                
        if (stream == null)
            throw new FileNotFoundException("The resource file 'palazz_sport.bin' was not found.");


        using (stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                int linesCount = reader.ReadInt32();

                boundsPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                boundsSize = new Vector2(reader.ReadSingle(), reader.ReadSingle());

                lines = new List<LineData>(linesCount);

                for (int i = 0; i < linesCount; i++)
                {
                    var alpha = reader.ReadByte();
                    var red   = reader.ReadByte();
                    var green = reader.ReadByte();
                    var blue  = reader.ReadByte();

                    var lineColor = Color4.FromByteRgba(red, green, blue, alpha);
                    var lineThickness = reader.ReadSingle();
                    var isLineStrip = reader.ReadBoolean();

                    var pointsCount = reader.ReadInt32();

                    var lineData = new LineData(lineColor, lineThickness, isLineStrip, pointsCount);

                    var positions = lineData.Positions;
                    for (int j = 0; j < pointsCount; j++)
                        positions.Add(new Vector2(reader.ReadSingle(), reader.ReadSingle()));

                    lines.Add(lineData);
                }
            }
        }

        return (lines, boundsPosition, boundsSize);
    }

    private class LineData
    {
        public readonly Color4 LineColor;
        public readonly float LineThickness;
        public readonly bool IsLineStrip;
        public readonly List<Vector2> Positions;

        public LineData(Color4 lineColor, float lineThickness, bool isLineStrip, int initialPointsCount = 0)
        {
            LineColor     = lineColor;
            LineThickness = lineThickness;
            IsLineStrip   = isLineStrip;

            if (initialPointsCount > 0)
                Positions = new List<Vector2>(initialPointsCount);
            else
                Positions = new List<Vector2>();
        }
    }

    private void UpdateSceneInfo()
    {
        if (_sceneInfoLabel == null)
            return;

        string infoText;

        if (_twoDimensionalCamera == null)
        {
            infoText = "";
        }
        else
        {
            var visibleRect = _twoDimensionalCamera.GetVisibleRect();
            infoText = $"Zoom factor: {_twoDimensionalCamera.ZoomFactor:F2}\r\nOffset: {_twoDimensionalCamera.Offset.X:F1} {_twoDimensionalCamera.Offset.Y:F1}\r\nVisible Rect: {visibleRect.position.X:F0} {visibleRect.position.Y:F0} => {(visibleRect.position.X + visibleRect.size.X):F0} {(visibleRect.position.Y + visibleRect.size.Y):F0}\r\nView size: {visibleRect.size.X:F0} x {visibleRect.size.Y:F0}";
        }

        _sceneInfoLabel.SetText(infoText);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(alignment: PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("2D Camera controls:", isHeader: true);
        ui.CreateLabel("PAN - left mouse button\nZOOM - mouse wheel\nQUICK ZOOM - left & right mouse button");

        ui.CreateLabel("View info:", isHeader: true);

        _sceneInfoLabel = ui.CreateLabel("");

        ui.AddSeparator();

        ui.CreateButton("Reset camera", () => _twoDimensionalCamera?.Reset());

        base.OnCreateUI(ui);

        UpdateSceneInfo();
    }
}