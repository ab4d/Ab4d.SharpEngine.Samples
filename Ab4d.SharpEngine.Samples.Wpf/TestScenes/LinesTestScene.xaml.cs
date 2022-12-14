using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Wpf;
using Ab4d.Vulkan;
using Cyotek.Drawing.BitmapFont;
using Page = System.Windows.Controls.Page;

namespace Ab4d.SharpEngine.Samples.Wpf.TestScenes
{
    /// <summary>
    /// Interaction logic for LinesTestScene.xaml
    /// </summary>
    public partial class LinesTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO? _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        public LinesTestScene()
        {
            InitializeComponent();

            _bitmapIO = new WpfBitmapIO(); // _bitmapIO provides a cross-platform way to read bitmaps (it uses WPF as backend)

            // Setup logger
            LogHelper.SetupSharpEngineLogger(enableFullLogging: false); // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company


            // MainSceneView is defined in XAML

            // Set EnableStandardValidation to true, but the Vulkan validation will be enabled only when the Vulkan SDK is installed on the system.
            MainSceneView.CreateOptions.EnableStandardValidation = true;

            MainSceneView.SceneViewInitialized += delegate (object? o, EventArgs args)
            {
                _scene = MainSceneView.Scene;
                _sceneView = MainSceneView.SceneView;

                if (_scene == null || _sceneView == null)
                    return; // This should not happen in SceneViewInitialized

                SetupMouseCameraController();
                CreateLights();

                CreateTestScene(_scene);
            };

            //MainSceneView.SceneUpdating += delegate(object? sender, EventArgs args)
            //{
            //    // Do animations
            //};

            Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                MainSceneView.Dispose();
            };
        }

        private void SetupMouseCameraController()
        {
            if (MainSceneView == null || MainSceneView.SceneView == null)
                return;


            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -25,
                Distance = 1200,
                TargetPosition = new Vector3(0, 0, -150),
                ShowCameraLight = ShowCameraLightType.Auto
            };

            MainSceneView.SceneView.Camera = _targetPositionCamera;


            _mouseCameraController = new MouseCameraController(MainSceneView)
            {
                RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed,                                                   // this is already the default value but is still set up here for clarity
                MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.ControlKey,             // this is already the default value but is still set up here for clarity
                QuickZoomConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.MousePosition,
                RotateAroundMousePosition = true
            };
        }

        private void CreateTestScene(Scene scene)
        {
            // Axis arrows for direction visualization
            if (true)
            {
                // Axes for direction visualization
                var axesNode = new AxisLineNode()
                {
                    Length = 50
                };
                scene.RootNode.Add(axesNode);
            }

            #region LineNode

            GenerateLines(
                scene,
                new Vector3(0, -50, -200),
                new Vector3(0, 100, 0),
                new Vector3(40, 0, 0),
                new[] { 2.5f, 5.0f, 7.5f, 10f, 20f, 30f },
                LineCap.Flat,
                new Color4(Colors.Red),
                "Line (flat cap)");

            GenerateLines(
                scene,
                new Vector3(0, -50, -300),
                new Vector3(0, 100, 0),
                new Vector3(40, 0, 0),
                new[] { 2.5f, 5.0f, 7.5f, 10f, 20f, 30f },
                LineCap.ArrowAnchor,
                new Color4(Colors.Green),
                "Line (arrow cap)");

            #endregion

            #region MultiLineNode

            if (true)
            {
                // Create points for 8 sided shape
                int edgesCount = 8;

                var pentagramPositions = new Vector3[edgesCount + 1];
                EllipseArcLineNode.FillArc3DPoints(
                    centerPosition: new Vector3(0, 0, 0),
                    normalDirection: new Vector3(0, 0, 1),
                    zeroAngleDirection: new Vector3(0, 1, 0),
                    xRadius: 100,
                    yRadius: 100,
                    startAngle: 0,
                    endAngle: 360,
                    segments: edgesCount,
                    positions: pentagramPositions);

                var pentagramLines = new Vector3[edgesCount * 2]; // 2 positions per lines in non-strip mode
                for (var i = 0; i < edgesCount; i++)
                {
                    var idx1 = i;
                    var idx2 = (idx1 + 2) % edgesCount;

                    pentagramLines[i * 2 + 0] = pentagramPositions[idx1];
                    pentagramLines[i * 2 + 1] = pentagramPositions[idx2];
                }

                // Outer shape & star (two separate nodes)
                var node = new MultiLineNode(
                    name: "Hexagon outer (multi-line node)")
                {
                    Positions = pentagramPositions,
                    IsLineStrip = true,
                    LineColor = new Color4(Colors.DarkRed),
                    LineThickness = 5.0f,
                    Transform = new TranslateTransform(-200, 100, -250)
                };
                scene.RootNode.Add(node);

                node = new MultiLineNode(
                    name: "Hexagon star (multi-line node)")
                {
                    Positions = pentagramLines,
                    IsLineStrip = false,
                    LineColor = new Color4(Colors.Red),
                    LineThickness = 2.5f,
                    Transform = new TranslateTransform(-200, 100, -250)
                };
                scene.RootNode.Add(node);
            }

            #endregion

            #region WireCross

            // WireCross: small and thin
            if (true)
            {
                var node = new WireCrossNode(
                    name: "WireCross (small, thin)"
                )
                {
                    LinesLength = 50,
                    LineThickness = 2.5f,
                    LineColor = new Color4(Colors.Red),
                    Transform = new TranslateTransform(-100, 0, -200)
                };
                scene.RootNode.Add(node);
            }

            if (true)
            {
                var node = new WireCrossNode(
                    name: "WireCross (large, thick)"
                )
                {
                    LinesLength = 100,
                    LineThickness = 5.0f,
                    LineColor = new Color4(Colors.Green),
                    Transform = new TranslateTransform(-200, 0, -200)
                };
                scene.RootNode.Add(node);
            }

            if (true)
            {
                var node = new WireCrossNode(
                    name: "WireCross (medium, thick)"
                )
                {
                    LinesLength = 75,
                    LineThickness = 5.0f,
                    LineColor = new Color4(Colors.Blue),
                    Transform = new TranslateTransform(-300, 0, -200)
                };
                scene.RootNode.Add(node);
            }

            #endregion

            #region Circles and Ellipses
            // Basic circle (default upwards normal)
            if (true)
            {
                var node = new CircleLineNode(
                    name: "Circle (lying)"
                )
                {
                    Radius = 25,
                    LineColor = new Color4(Colors.Green),
                    LineThickness = 5,
                    Transform = new TranslateTransform(75, 0, 30),
                };
                scene.RootNode.Add(node);
            }

            // Upright circle (normal pointing along Z axis)
            if (true)
            {
                var node = new CircleLineNode(
                    name: "Circle (upright)"
                )
                {
                    Radius = 25,
                    LineColor = new Color4(Colors.Blue),
                    LineThickness = 2,
                    Normal = new Vector3(0, 0, 1),
                    Transform = new TranslateTransform(75, 30, 0)
                };
                scene.RootNode.Add(node);
            }

            // Low-segment circle (default upwards normal)
            if (true)
            {
                var node = new CircleLineNode(
                    name: "Circle (low-segment, lying)"
                )
                {
                    Radius = 25,
                    Segments = 4,
                    LineColor = new Color4(Colors.Green),
                    LineThickness = 5,
                    Transform = new TranslateTransform(135, 0, 30),
                };
                scene.RootNode.Add(node);
            }

            // Low-segment upright circle (normal pointing along Z axis)
            if (true)
            {
                var node = new CircleLineNode(
                    name: "Circle (low-segment, upright)"
                )
                {
                    Radius = 25,
                    Segments = 4,
                    LineColor = new Color4(Colors.Blue),
                    LineThickness = 2,
                    Normal = new Vector3(0, 0, 1),
                    Transform = new TranslateTransform(135, 30, 0)
                };
                scene.RootNode.Add(node);
            }

            // Ellipse (default upwards normal)
            if (true)
            {
                var node = new EllipseLineNode(
                    name: "Ellipse (lying)"
                )
                {
                    Width = 100,
                    Height = 50,
                    LineColor = new Color4(Colors.Green),
                    LineThickness = 5,
                    Transform = new TranslateTransform(220, 0, 30)
                };
                scene.RootNode.Add(node);
            }

            // Upright ellipse (normal pointing along Z axis)
            if (true)
            {
                var node = new EllipseLineNode(
                    name: "Ellipse (upright)"
                )
                {
                    Width = 100,
                    Height = 50,
                    LineColor = new Color4(Colors.Blue),
                    LineThickness = 2,
                    WidthDirection = new Vector3(1, 0, 0),
                    HeightDirection = new Vector3(0, 1, 0),
                    Transform = new TranslateTransform(220, 30, 0)
                };
                scene.RootNode.Add(node);
            }

            // Low-segment ellipse (default upwards normal)
            if (true)
            {
                var node = new EllipseLineNode(
                    name: "Ellipse (low-segment, lying)"
                )
                {
                    Width = 100,
                    Height = 50,
                    Segments = 4,
                    LineColor = new Color4(Colors.Green),
                    LineThickness = 5,
                    Transform = new TranslateTransform(330, 0, 30)
                };
                scene.RootNode.Add(node);
            }

            // Low-segment upright ellipse (normal pointing along Z axis)
            if (true)
            {
                var node = new EllipseLineNode(
                    name: "Ellipse (low-segment, upright)"
                )
                {
                    Width = 100,
                    Height = 50,
                    Segments = 4,
                    LineColor = new Color4(Colors.Blue),
                    LineThickness = 2,
                    WidthDirection = new Vector3(1, 0, 0),
                    HeightDirection = new Vector3(0, 1, 0),
                    Transform = new TranslateTransform(330, 30, 0)
                };
                scene.RootNode.Add(node);
            }
            #endregion

            #region Arcs
            // Basic circle (default upwards normal)
            if (true)
            {
                var node = new EllipseArcLineNode("Circular arc (lying)")
                {
                    Width = 25,
                    Height = 25,
                    StartAngle = 25,
                    EndAngle = 325,
                    LineColor = new Color4(Colors.Green),
                    LineThickness = 5,
                    Transform = new TranslateTransform(75, 0, -100 + 30),
                };
                scene.RootNode.Add(node);
            }

            // Upright circle (normal pointing along Z axis)
            if (true)
            {
                var node = new EllipseArcLineNode("Circular arc (upright)")
                {
                    Width = 50,
                    Height = 50,
                    StartAngle = 25,
                    EndAngle = 325,
                    LineColor = new Color4(Colors.Blue),
                    LineThickness = 2,
                    HeightDirection = new Vector3(0, 1, 0),
                    Transform = new TranslateTransform(75, 30, -100)
                };
                scene.RootNode.Add(node);
            }

            // Low-segment circle (default upwards normal)
            if (true)
            {
                var node = new EllipseArcLineNode("Circular arc (low-segment, lying)")
                {
                    Width = 50,
                    Height = 50,
                    StartAngle = 25,
                    EndAngle = 325,
                    Segments = 4,
                    LineColor = new Color4(Colors.Green),
                    LineThickness = 5,
                    Transform = new TranslateTransform(135, 0, -100 + 30),
                };
                scene.RootNode.Add(node);
            }

            // Low-segment upright circle (normal pointing along Z axis)
            if (true)
            {
                var node = new EllipseArcLineNode("Circular arc (low-segment, upright)")
                {
                    Width = 50,
                    Height = 50,
                    StartAngle = 25,
                    EndAngle = 325,
                    Segments = 4,
                    LineColor = new Color4(Colors.Blue),
                    LineThickness = 2,
                    HeightDirection = new Vector3(0, 1, 0),
                    Transform = new TranslateTransform(135, 30, -100)
                };
                scene.RootNode.Add(node);
            }

            // Ellipse (default upwards normal)
            if (true)
            {
                var node = new EllipseArcLineNode("Ellipsoid arc (lying)")
                {
                    Width = 100,
                    Height = 50,
                    StartAngle = 25,
                    EndAngle = 325,
                    LineColor = new Color4(Colors.Green),
                    LineThickness = 5,
                    Transform = new TranslateTransform(220, 0, -100 + 30)
                };
                scene.RootNode.Add(node);
            }

            // Upright ellipse (normal pointing along Z axis)
            if (true)
            {
                var node = new EllipseArcLineNode("Ellipse (upright)")
                {
                    Width = 100,
                    Height = 50,
                    StartAngle = 25,
                    EndAngle = 325,
                    LineColor = new Color4(Colors.Blue),
                    LineThickness = 2,
                    HeightDirection = new Vector3(0, 1, 0),
                    Transform = new TranslateTransform(220, 30, -100)
                };
                scene.RootNode.Add(node);
            }

            // Low-segment ellipse (default upwards normal)
            if (true)
            {
                var node = new EllipseArcLineNode("Ellipsoid arc (low-segment, lying)")
                {
                    Width = 100,
                    Height = 50,
                    StartAngle = 25,
                    EndAngle = 325,
                    Segments = 4,
                    LineColor = new Color4(Colors.Green),
                    LineThickness = 5,
                    Transform = new TranslateTransform(330, 0, -100 + 30)
                };
                scene.RootNode.Add(node);
            }

            // Low-segment upright ellipse (normal pointing along Z axis)
            if (true)
            {
                var node = new EllipseArcLineNode("Ellipsoid arc (low-segment, upright)")
                {
                    Width = 100,
                    Height = 50,
                    StartAngle = 25,
                    EndAngle = 325,
                    Segments = 4,
                    LineColor = new Color4(Colors.Blue),
                    LineThickness = 2,
                    HeightDirection = new Vector3(0, 1, 0),
                    Transform = new TranslateTransform(330, 30, -100)
                };
                scene.RootNode.Add(node);
            }
            #endregion

            #region Rectangles
            // Square (upwards normal)
            if (true)
            {
                var node = new RectangleNode("Square (lying)")
                {
                    Position = new Vector3(0, 0, 25),
                    PositionType = PositionTypes.Center, // default
                    Size = new Vector2(50, 50),
                    WidthDirection = new Vector3(1, 0, 0),
                    HeightDirection = new Vector3(0, 0, 1),
                    LineColor = new Color4(Colors.Green),
                    LineThickness = 5,
                    Transform = new TranslateTransform(75, 0, 100)
                };
                scene.RootNode.Add(node);
            }

            // Upright square (normal pointing along Z axis)
            if (true)
            {
                var node = new RectangleNode(
                    name: "Square (upright)"
                )
                {
                    Position = new Vector3(0, 25, 0),
                    PositionType = PositionTypes.Center, // default
                    Size = new Vector2(50, 50),
                    LineColor = new Color4(Colors.Blue),
                    LineThickness = 2,
                    WidthDirection = new Vector3(1, 0, 0),
                    HeightDirection = new Vector3(0, 1, 0),
                    Transform = new TranslateTransform(75, 0, 100)
                };
                scene.RootNode.Add(node);
            }


            // Rectangle (default upwards normal)
            if (true)
            {
                var node = new RectangleNode("Rectangle (lying)")
                {
                    Position = new Vector3(0, 0, 25),
                    PositionType = PositionTypes.Bottom,
                    Size = new Vector2(100, 50),
                    WidthDirection = new Vector3(1, 0, 0),
                    HeightDirection = new Vector3(0, 0, 1),
                    LineColor = new Color4(Colors.Green),
                    LineThickness = 5,
                    Transform = new TranslateTransform(220, 0, 100)
                };
                scene.RootNode.Add(node);
            }

            // Upright rectangle (normal pointing along Z axis)
            if (true)
            {
                var node = new RectangleNode(
                    name: "Rectangle (upright)"
                )
                {
                    Position = new Vector3(0, 0, 25),
                    PositionType = PositionTypes.Bottom,
                    Size = new Vector2(100, 50),
                    WidthDirection = new Vector3(1, 0, 0),
                    HeightDirection = new Vector3(0, 1, 0),
                    LineColor = new Color4(Colors.Blue),
                    LineThickness = 2,
                    Transform = new TranslateTransform(220, 0, 100)
                };
                scene.RootNode.Add(node);
            }


            // Test PositionType
            if (true)
            {
                var wireCrossNode = new WireCrossNode("WireCross_for_Rectangle_PositionType")
                {
                    Position = new Vector3(0, 0, 0),
                    LinesLength = 20,
                    LineThickness = 3f,
                    LineColor = Colors.Red,
                    Transform = new TranslateTransform(350, 0, 100)
                };
                scene.RootNode.Add(wireCrossNode);

                var rectanglePositionTypeNode = new RectangleNode("Rectangle_test_PositionType")
                {
                    Position = wireCrossNode.Position,
                    PositionType = PositionTypes.Top | PositionTypes.Right,
                    Size = new Vector2(50, 50),
                    WidthDirection = new Vector3(1, 0, 0),
                    HeightDirection = new Vector3(0, 1, 0),
                    LineColor = new Color4(Colors.Orange),
                    LineThickness = 2,
                    Transform = new TranslateTransform(350, 0, 100)
                };
                scene.RootNode.Add(rectanglePositionTypeNode);
            }
            #endregion

            #region WireBox & CornerWireBox
            // Box
            if (true)
            {
                var node = new WireBoxNode(
                    name: "Wire box"
                )
                {
                    Size = new Vector3(50, 50, 50),
                    LineThickness = 2.5f,
                    LineColor = new Color4(Colors.DarkRed),
                    Transform = new TranslateTransform(-100, 0, -75)
                };
                scene.RootNode.Add(node);
            }

            // Corner box (relative)
            if (true)
            {
                var node = new CornerWireBoxNode(
                    name: "Corner wire box (relative)"
                )
                {
                    Size = new Vector3(50, 50, 50),
                    IsLineLengthRelative = true,
                    LineLength = 0.20f,  // 10%
                    LineThickness = 2.5f,
                    LineColor = new Color4(Colors.DarkGreen),
                    Transform = new TranslateTransform(-100, 0, 0)
                };
                scene.RootNode.Add(node);
            }

            // Corner box (absolute)
            if (true)
            {
                var node = new CornerWireBoxNode(
                    name: "Corner wire box (absolute)"
                )
                {
                    Size = new Vector3(50, 50, 50),
                    IsLineLengthRelative = false,
                    LineLength = 20,
                    LineThickness = 2.5f,
                    LineColor = new Color4(Colors.DarkBlue),
                    Transform = new TranslateTransform(-100, 0, 75)
                };
                scene.RootNode.Add(node);
            }

            // Cuboid
            if (true)
            {
                var node = new WireBoxNode(
                    name: "Wire cuboid"
                )
                {
                    Size = new Vector3(100, 75, 50),
                    LineThickness = 2.5f,
                    LineColor = new Color4(Colors.DarkRed),
                    Transform = new TranslateTransform(-250, 0, -75)
                };
                scene.RootNode.Add(node);
            }

            // Corner cuboid (relative)
            if (true)
            {
                var node = new CornerWireBoxNode(
                    name: "Corner wire cuboid (relative)"
                )
                {
                    Size = new Vector3(100, 75, 50),
                    IsLineLengthRelative = true,
                    LineLength = 0.20f,  // 10%
                    LineThickness = 2.5f,
                    LineColor = new Color4(Colors.DarkGreen),
                    Transform = new TranslateTransform(-250, 0, 0)
                };
                scene.RootNode.Add(node);
            }

            // Corner cuboid (absolute)
            if (true)
            {
                var node = new CornerWireBoxNode(
                    name: "Corner wire cuboid (absolute)"
                )
                {
                    Size = new Vector3(100, 75, 50),
                    IsLineLengthRelative = false,
                    LineLength = 20,
                    LineThickness = 2.5f,
                    LineColor = new Color4(Colors.DarkBlue),
                    Transform = new TranslateTransform(-250, 0, 75)
                };
                scene.RootNode.Add(node);
            }
            #endregion
        }


        private void GenerateLines(Scene scene, Vector3 startPosition, Vector3 lineDirection, Vector3 step, float[] thicknessList, LineCap capType, Color4 color, string namePrefix)
        {
            for (var i = 0; i < thicknessList.Length; i++)
            {
                var thickness = thicknessList[i];
                Vector3 move = startPosition + i * step;

                var node = new LineNode(
                    name: $"{namePrefix} (thickness: {thickness})"
                )
                {
                    LineThickness = thickness,
                    LineColor = color,
                    StartPosition = new Vector3(0, 0, 0),
                    EndPosition = lineDirection,
                    StartLineCap = capType,
                    EndLineCap = capType,
                    Transform = new TranslateTransform(move.X, move.Y, move.Z)
                };
                scene.RootNode.Add(node);
            }
        }

        private void CreateLights()
        {
            if (_scene == null)
                return;


            _scene.Lights.Clear();

            // Add lights
            _scene.Lights.Add(new AmbientLight(new Color3(0.3f, 0.3f, 0.3f)));

            var directionalLight = new DirectionalLight(new Vector3(-1, -0.3f, 0));
            _scene.Lights.Add(directionalLight);

            _scene.Lights.Add(new PointLight(new Vector3(500, 200, 100), range: 10000));
        }

    }
}
