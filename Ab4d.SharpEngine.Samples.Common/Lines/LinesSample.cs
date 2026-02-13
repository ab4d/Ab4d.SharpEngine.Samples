using Ab4d.SharpEngine.Cameras;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;

namespace Ab4d.SharpEngine.Samples.Common.Lines;

public class LinesSample : CommonSample
{
    public override string Title => "3D lines";

#if WEB_GL
    public override string Subtitle => "IMPORTANT:\nThe current version of Ab4d.SharpEngine.Web does not support thick lines (LineThickness > 1) and LineCaps (line with arrows and other ending shapes) ";
#endif

    private RectangleNode? _rectanglePositionTypeNode;
    private WireBoxNode? _wireBoxNode;
    private CornerWireBoxNode? _cornerWireBoxNode;

    private int _currentPositionTypeIndex;

    private PositionTypes[] _allPositionTypesInSample = new PositionTypes[]
    {
        PositionTypes.TopLeft,
        PositionTypes.Top,
        PositionTypes.TopRight,

        PositionTypes.Left,
        PositionTypes.Center,
        PositionTypes.Right,

        PositionTypes.BottomLeft,
        PositionTypes.Bottom,
        PositionTypes.BottomRight,

        PositionTypes.Front,
        PositionTypes.Back,
        PositionTypes.Front | PositionTypes.Top | PositionTypes.Left, // All combinations are possible
    };

    public LinesSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        SceneNode sceneNode;
        
        #region LineNode

        // Different LineThickness
        var startPosition = new Vector3(-50, 0, 250);
        var thicknessList = new[] { 0.7f, 1, 1.5f, 2f, 3f, 5f, 10f };
        
        for (var i = 0; i < thicknessList.Length; i++)
        {
            var thickness = thicknessList[i];
            var node = new LineNode(name: $"Line (thickness: {thickness})")
            {
                LineThickness = thickness,
                LineColor = Colors.Black,
                StartPosition = startPosition,
                EndPosition = startPosition + new Vector3(0, 0, 100),
            };

            startPosition += new Vector3(-30, 0, 0);

            scene.RootNode.Add(node);
        }

        #endregion

        #region Lines with arrows
#if VULKAN
        sceneNode = new LineNode(name: $"Line (EndLineCap: ArrowAnchor)")
        {
            LineThickness = 2,
            LineColor = Colors.Black,
            StartPosition = new Vector3(50, 0, 250),
            EndPosition = new Vector3(50, 0, 350),
            StartLineCap = LineCap.Flat,
            EndLineCap = LineCap.ArrowAnchor
        };
        scene.RootNode.Add(sceneNode);
        
        sceneNode = new LineNode(name: $"Line (StartLineCap: ArrowAnchor)")
        {
            LineThickness = 2,
            LineColor = Colors.Black,
            StartPosition = new Vector3(80, 0, 250),
            EndPosition = new Vector3(80, 0, 350),
            StartLineCap = LineCap.ArrowAnchor,
            EndLineCap = LineCap.Flat
        };
        scene.RootNode.Add(sceneNode);
        
        sceneNode = new LineNode(name: $"Line (StartLineCap: ArrowAnchor; EndLineCap: ArrowAnchor)")
        {
            LineThickness = 2,
            LineColor = Colors.Black,
            StartPosition = new Vector3(110, 0, 250),
            EndPosition = new Vector3(110, 0, 350),
            StartLineCap = LineCap.ArrowAnchor,
            EndLineCap = LineCap.ArrowAnchor
        };
        scene.RootNode.Add(sceneNode);
#endif
        #endregion

        #region WireCrossNode

        // WireCrossNode can be used to show a position in 3D space
        sceneNode = new WireCrossNode()
        {
            Position = new Vector3(200, 0, 250),
            LineThickness = 2,
            LineColor = Colors.Black, // this is also a default value
            LinesLength = 50          // this is also a default value
        };
        scene.RootNode.Add(sceneNode);

        #endregion

        #region AxisLineNode

        // Axis arrows show axes direction
        var axesNode = new AxisLineNode()
        {
            Length = 50
        };
        scene.RootNode.Add(axesNode);

        #endregion

        #region PolyLineNode, MultiLineNode

        // Add PolyLineNode
        // It shows connected points (the line segments are connected)
        var currentPosition = new Vector3(-200, 0, -300);
        var positions = new List<Vector3>();
        var dx = 30;
        
        for (int i = 0; i < 6; i++)
        {
            positions.Add(currentPosition);

            currentPosition += new Vector3(30, dx, 0);
            dx = -dx;
        }

        sceneNode = new PolyLineNode(name: "PolyLine")
        {
            Positions = positions.ToArray(),
            LineColor = Colors.Gray,
            LineThickness = 5.0f,
            MiterLimit = 2f // defines at which line thickness the mitered (sharp) line joint is converted into beveled (square) line joint. Default value is 2.
        };
        scene.RootNode.Add(sceneNode);


        // Add MultiLineNode
        // MultiLine can show multiple lines - each line is defined by 2 positions (the line segments are not connected)
        sceneNode = new MultiLineNode(name: "MultiLine")
        {
            Positions = positions.ToArray(),
            IsLineStrip = true, // Positions array defines connected lines
            LineColor = Colors.Gray,
            LineThickness = 5.0f,
            Transform = new TranslateTransform(250, 30, 0)
        };
        scene.RootNode.Add(sceneNode);



        currentPosition = new Vector3(50, 0, -300);
        positions = new List<Vector3>();
        dx = 30;
        
        for (int i = 0; i < 5; i++)
        {
            // When IsLineStrip = false, then each line is defined by 2 positions
            positions.Add(currentPosition);

            currentPosition += new Vector3(30, dx, 0);
            dx = -dx;

            positions.Add(currentPosition);
        }

        // Add some additional lines
        positions.Add(currentPosition + new Vector3(10, 0, 0));
        positions.Add(currentPosition + new Vector3(10, dx, 0));
        
        positions.Add(currentPosition + new Vector3(20, 0, 0));
        positions.Add(currentPosition + new Vector3(20, dx, 0));
        
        positions.Add(currentPosition + new Vector3(30, 0, 0));
        positions.Add(currentPosition + new Vector3(30, dx, 0));

        sceneNode = new MultiLineNode(name: "MultiLine")
        {
            Positions = positions.ToArray(),
            IsLineStrip = false, // Positions array defines dis-connected lines
            LineColor = Colors.Gray,
            LineThickness = 5.0f,
        };
        scene.RootNode.Add(sceneNode);

        #endregion

        #region Circles and Ellipses
        // Basic circle (default upwards normal)
        sceneNode = new CircleLineNode(name: "Circle (lying)")
        {
            CenterPosition = new Vector3(75, 0, 30),
            Radius = 25,
            LineColor = Colors.Green,
            LineThickness = 5,
        };
        scene.RootNode.Add(sceneNode);

        // Upright circle (normal pointing along Z axis)
        sceneNode = new CircleLineNode(name: "Circle (upright)")
        {
            CenterPosition = new Vector3(75, 30, 0),
            Radius = 25,
            LineColor = Colors.Blue,
            LineThickness = 2,
            Normal = new Vector3(0, 0, 1),
        };
        scene.RootNode.Add(sceneNode);

        // Low-segment circle (default upwards normal)
        sceneNode = new CircleLineNode(name: "Circle (low-segment, lying)")
        {
            CenterPosition = new Vector3(135, 0, 30),
            Radius = 25,
            Segments = 4,
            LineColor = Colors.Green,
            LineThickness = 5,
        };
        scene.RootNode.Add(sceneNode);

        // Low-segment upright circle (normal pointing along Z axis)
        sceneNode = new CircleLineNode(name: "Circle (low-segment, upright)")
        {
            CenterPosition = new Vector3(135, 30, 0),
            Radius = 25,
            Segments = 4,
            LineColor = Colors.Blue,
            LineThickness = 2,
            Normal = new Vector3(0, 0, 1),
        };
        scene.RootNode.Add(sceneNode);

        // Ellipse (default upwards normal)
        sceneNode = new EllipseLineNode(name: "Ellipse (lying)")
        {
            CenterPosition = new Vector3(220, 0, 30),
            Width = 100,
            Height = 50,
            LineColor = Colors.Green,
            LineThickness = 5,
        };
        scene.RootNode.Add(sceneNode);

        // Upright ellipse (normal pointing along Z axis)
        sceneNode = new EllipseLineNode(name: "Ellipse (upright)")
        {
            CenterPosition = new Vector3(220, 30, 0),
            Width = 100,
            Height = 50,
            LineColor = Colors.Blue,
            LineThickness = 2,
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 1, 0),
        };
        scene.RootNode.Add(sceneNode);

        #endregion

        #region Arcs
        // Basic circle (default upwards normal)
        sceneNode = new EllipseArcLineNode("Circular arc (lying)")
        {
            CenterPosition = new Vector3(75, 0, -150 + 30),
            Width = 25,
            Height = 25,
            StartAngle = 25,
            EndAngle = 325,
            LineColor = Colors.Green,
            LineThickness = 5,
        };
        scene.RootNode.Add(sceneNode);

        // Upright circle (normal pointing along Z axis)
        sceneNode = new EllipseArcLineNode("Circular arc (upright)")
        {
            CenterPosition = new Vector3(75, 30, -150),
            Width = 50,
            Height = 50,
            StartAngle = 25,
            EndAngle = 325,
            LineColor = Colors.Blue,
            LineThickness = 2,
            HeightDirection = new Vector3(0, 1, 0),
        };
        scene.RootNode.Add(sceneNode);

        // Low-segment circle (default upwards normal)
        sceneNode = new EllipseArcLineNode("Circular arc (low-segment, lying)")
        {
            CenterPosition = new Vector3(135, 0, -150 + 30),
            Width = 50,
            Height = 50,
            StartAngle = 25,
            EndAngle = 325,
            Segments = 4,
            LineColor = Colors.Green,
            LineThickness = 5,
        };
        scene.RootNode.Add(sceneNode);

        // Low-segment upright circle (normal pointing along Z axis)
        sceneNode = new EllipseArcLineNode("Circular arc (low-segment, upright)")
        {
            CenterPosition = new Vector3(135, 30, -150),
            Width = 50,
            Height = 50,
            StartAngle = 25,
            EndAngle = 325,
            Segments = 4,
            LineColor = Colors.Blue,
            LineThickness = 2,
            HeightDirection = new Vector3(0, 1, 0),
        };
        scene.RootNode.Add(sceneNode);

        // Ellipse (default upwards normal)
        sceneNode = new EllipseArcLineNode("Ellipsoid arc (lying)")
        {
            CenterPosition = new Vector3(220, 0, -150 + 30),
            Width = 100,
            Height = 50,
            StartAngle = 25,
            EndAngle = 325,
            LineColor = Colors.Green,
            LineThickness = 5,
        };
        scene.RootNode.Add(sceneNode);

        // Upright ellipse (normal pointing along Z axis)
        sceneNode = new EllipseArcLineNode("Ellipse (upright)")
        {
            CenterPosition = new Vector3(220, 30, -150),
            Width = 100,
            Height = 50,
            StartAngle = 25,
            EndAngle = 325,
            LineColor = Colors.Blue,
            LineThickness = 2,
            HeightDirection = new Vector3(0, 1, 0),
        };
        scene.RootNode.Add(sceneNode);

        #endregion

        #region Rectangles
        // Square (upwards normal)
        sceneNode = new RectangleNode("Square (lying)")
        {
            Position = new Vector3(0, 0, 25),
            PositionType = PositionTypes.Center, // default
            Size = new Vector2(50, 50),
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 0, 1),
            LineColor = Colors.Green,
            LineThickness = 5,
            Transform = new TranslateTransform(75, 0, 150)
        };
        scene.RootNode.Add(sceneNode);

        // Upright square (normal pointing along Z axis)
        sceneNode = new RectangleNode(name: "Square (upright)")
        {
            Position = new Vector3(0, 25, 0),
            PositionType = PositionTypes.Center, // default
            Size = new Vector2(50, 50),
            LineColor = Colors.Blue,
            LineThickness = 2,
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 1, 0),
            Transform = new TranslateTransform(75, 0, 150)
        };
        scene.RootNode.Add(sceneNode);

        // Rectangle (default upwards normal)
        sceneNode = new RectangleNode("Rectangle (lying)")
        {
            Position = new Vector3(0, 0, 25),
            PositionType = PositionTypes.Bottom,
            Size = new Vector2(100, 50),
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 0, 1),
            LineColor = Colors.Green,
            LineThickness = 5,
            Transform = new TranslateTransform(220, 0, 125)
        };
        scene.RootNode.Add(sceneNode);

        // Upright rectangle (normal pointing along Z axis)
        sceneNode = new RectangleNode(name: "Rectangle (upright)")
        {
            Position = new Vector3(0, 0, 25),
            PositionType = PositionTypes.Bottom,
            Size = new Vector2(100, 50),
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 1, 0),
            LineColor = Colors.Blue,
            LineThickness = 2,
            Transform = new TranslateTransform(220, 0, 125)
        };
        scene.RootNode.Add(sceneNode);

        // Test PositionType
        var wireCrossNode = new WireCrossNode("WireCross_for_Rectangle_PositionType")
        {
            Position = new Vector3(350, 0, 100),
            LinesLength = 20,
            LineThickness = 3f,
            LineColor = Colors.Red,
        };
        scene.RootNode.Add(wireCrossNode);

        _rectanglePositionTypeNode = new RectangleNode("Rectangle_test_PositionType")
        {
            Position = wireCrossNode.Position,
            PositionType = _allPositionTypesInSample[0], // TopLeft
            Size = new Vector2(50, 50),
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 1, 0),
            LineColor = Colors.Orange,
            LineThickness = 2,
        };
        scene.RootNode.Add(_rectanglePositionTypeNode);
        #endregion

        #region WireBox & CornerWireBox
        // Box
        sceneNode = new WireBoxNode(name: "Wire box")
        {
            Position = new Vector3(-100, 0, -125),
            PositionType = PositionTypes.Center,
            Size = new Vector3(50, 50, 50),
            LineThickness = 2.5f,
            LineColor = Colors.DarkRed,
        };
        scene.RootNode.Add(sceneNode);

        // Corner box (relative)
        sceneNode = new CornerWireBoxNode(name: "Corner wire box (relative)")
        {
            Position = new Vector3(-100, 0, 0),
            PositionType = PositionTypes.Center,
            Size = new Vector3(50, 50, 50),
            IsLineLengthRelative = true,
            LineLength = 0.20f, // 10%
            LineThickness = 2.5f,
            LineColor = Colors.DarkGreen,
        };
        scene.RootNode.Add(sceneNode);

        // Corner box (absolute)
        sceneNode = new CornerWireBoxNode(name: "Corner wire box (absolute)")
        {
            Position = new Vector3(-100, 0, 150),
            PositionType = PositionTypes.Center,
            Size = new Vector3(50, 50, 50),
            IsLineLengthRelative = false,
            LineLength = 20,
            LineThickness = 2.5f,
            LineColor = Colors.DarkBlue,
        };
        scene.RootNode.Add(sceneNode);

        // Cuboid
        sceneNode = new WireBoxNode(name: "Wire cuboid")
        {
            Position = new Vector3(-250, 0, -125),
            PositionType = PositionTypes.Center,
            Size = new Vector3(100, 75, 50),
            LineThickness = 2.5f,
            LineColor = Colors.DarkRed,
        };
        scene.RootNode.Add(sceneNode);

        // Corner cuboid (relative)
        sceneNode = new CornerWireBoxNode(name: "Corner wire cuboid (relative)")
        {
            Position = new Vector3(-250, 0, 0),
            PositionType = PositionTypes.Center,
            Size = new Vector3(100, 75, 50),
            IsLineLengthRelative = true,
            LineLength = 0.20f,  // 10%
            LineThickness = 2.5f,
            LineColor = Colors.DarkGreen,
        };
        scene.RootNode.Add(sceneNode);

        // Corner cuboid (absolute)
        sceneNode = new CornerWireBoxNode(name: "Corner wire cuboid (absolute)")
        {
            Position = new Vector3(-250, 0, 150),
            PositionType = PositionTypes.Center,
            Size = new Vector3(100, 75, 50),
            IsLineLengthRelative = false,
            LineLength = 20,
            LineThickness = 2.5f,
            LineColor = Colors.DarkBlue,
        };
        scene.RootNode.Add(sceneNode);

        // Test PositionType
        var wireCrossNode2 = new WireCrossNode("WireCross_for_WireBox")
        {
            Position = new Vector3(350, 0, 0),
            LinesLength = 20,
            LineThickness = 3f,
            LineColor = Colors.Red,
        };
        scene.RootNode.Add(wireCrossNode2);

        _wireBoxNode = new WireBoxNode(name: "WireBox_PositionType")
        {
            Position = wireCrossNode2.Position,
            PositionType = _allPositionTypesInSample[0], // TopLeft,
            Size = new Vector3(50, 50, 50),
            LineThickness = 2.5f,
            LineColor = Colors.Orange,
        };
        scene.RootNode.Add(_wireBoxNode);


        var wireCrossNode3 = new WireCrossNode("WireCross_for_CornerWireBox")
        {
            Position = new Vector3(350, 0, -150),
            LinesLength = 20,
            LineThickness = 3f,
            LineColor = Colors.Red,
        };
        scene.RootNode.Add(wireCrossNode3);

        _cornerWireBoxNode = new CornerWireBoxNode(name: "CornerWireBox_PositionType")
        {
            Position = wireCrossNode3.Position,
            PositionType = _allPositionTypesInSample[0], // TopLeft,
            Size = new Vector3(50, 50, 50),
            IsLineLengthRelative = true,
            LineLength = 0.20f, // 10%
            LineThickness = 2.5f,
            LineColor = Colors.Orange,
        };

        scene.RootNode.Add(_cornerWireBoxNode);
        #endregion


#region Different start and end color
#if VULKAN
        var positions2 = new Vector3[]
        {
            new Vector3(-50, 0, 0),
            new Vector3( 50, 0, 0),
            new Vector3(-50, 0, 30),
            new Vector3( 50, 0, 30),
            new Vector3(-50, 0, 60),
            new Vector3( 50, 0, 60),
        };

        var positionColors = new Color4[]
        {
            Colors.Blue,
            Colors.Green,
            Colors.Yellow,
            Colors.Orange,
            Colors.Red,
            Colors.Transparent
        };

        var lineMaterial = new PositionColoredLineMaterial("PositionColoredLineMaterial")
        {
            LineColor      = Color4.White, // When PositionColors are used, then LineColor is used as a mask - each color is multiplied by LineColor - use White to preserve PositionColors
            LineThickness  = 3,
            PositionColors = positionColors,
        };

        var multiLineNode = new MultiLineNode(positions2, isLineStrip: false, lineMaterial, "PerPositionColoredMultiLine")
        {
            Transform = new TranslateTransform(0, 0, 400)
        };

        scene.RootNode.Add(multiLineNode);
#endif
        #endregion

        #region Line with patterns
#if VULKAN
        var stipplePatterns = new ushort[]
        {
            0b0101010101010101,
            0b0011001100110011,
            0b0000111100001111,
            0b0000000000000001,
            0b0000000000001111,
        };

        for (int i = 0; i < stipplePatterns.Length * 3; i++)
        {
            var lineMaterial2 = new LineMaterial(Colors.Green)
            {
                LineThickness     = 3,
                LinePattern       = stipplePatterns[i % stipplePatterns.Length],
                LinePatternScale  = 1 + (i / stipplePatterns.Length),
                LinePatternOffset = 0 // set that to 1/16 to shift the pattern for one bit
            };

            float y = (i + (i / stipplePatterns.Length) * 2) * 10; // add 2 empty lines after each change of scale
            var line = new LineNode(startPosition: new Vector3(-400, y, -100), endPosition: new Vector3(-400, y, 100), lineMaterial2, $"Line3D-Stipple_{i}");

            scene.RootNode.Add(line);
        }
#endif
        #endregion


        _currentPositionTypeIndex = 4; // Center
        SetPositionType(_currentPositionTypeIndex);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -35;
            targetPositionCamera.Attitude = -30;
            targetPositionCamera.Distance = 1200;
        }
    }

    private void SetPositionType(int positionTypeIndex)
    {
        var newPositionType = _allPositionTypesInSample[positionTypeIndex];

        if (_rectanglePositionTypeNode != null)
            _rectanglePositionTypeNode.PositionType = newPositionType;

        if (_wireBoxNode != null)
            _wireBoxNode.PositionType = newPositionType;

        if (_cornerWireBoxNode != null)
            _cornerWireBoxNode.PositionType = newPositionType;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateComboBox(_allPositionTypesInSample.Select(p => p.ToString()).ToArray(), (selectedIndex, selectedText) => SetPositionType(selectedIndex), _currentPositionTypeIndex, 130, "PositionType:", 0);
    }
}