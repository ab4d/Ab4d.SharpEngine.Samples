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
    /// Interaction logic for CurveLineTestScene.xaml
    /// </summary>
    public partial class CurveLineTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO? _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private CurveLineNode? _curveNode;
        private WireCrossNode[]? _controlPointMarkers;

        private Random _rnd = new Random();

        public CurveLineTestScene()
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
                Distance = 700,
                ViewWidth = 700,
                TargetPosition = new Vector3(0, 0, 0),
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
            // Axes for direction visualization
            var axesNode = new AxisLineNode()
            {
                Length = 50
            };
            scene.RootNode.Add(axesNode);

            // The curve
            _curveNode = new CurveLineNode("Curve")
            {
                CurveType = CurveLineNode.CurveTypes.CurveThroughPoints,
                LineColor = new Color4(Colors.DarkRed),
                LineThickness = 2,
            };
            scene.RootNode.Add(_curveNode);

            _controlPointMarkers = Array.Empty<WireCrossNode>();

            // Initial update
            GenerateRandomControlPoints(20, out var initialControlPoints, out var initialWeights);
            UpdateControlPoints(initialControlPoints, initialWeights);

            UpdateCurveTypeTextBlock();
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


        private void GenerateRandomControlPoints(int pointsCount, out Vector3[] controlPoints, out float[] weights)
        {
            controlPoints = new Vector3[pointsCount];
            weights = new float[pointsCount];

            var point = new Vector3(-(20 * pointsCount / 2), 0, 0);

            for (var i = 0; i < pointsCount; i++)
            {
                var vector = new Vector3(20, 40 * _rnd.NextSingle() - 20, 40 * _rnd.NextSingle() - 20);
                point += vector;

                controlPoints[i] = point;
                weights[i] = _rnd.NextSingle() + 0.5f; // from 0.5 to 1.5
            }
        }

        private void UpdateControlPoints(Vector3[] controlPoints, float[] weights)
        {
            if (_scene == null || _curveNode == null || _controlPointMarkers == null)
                return;

            // Update controls points on the curve
            _curveNode.ControlPoints = controlPoints;
            _curveNode.Weights = weights;

            // *** Update markers for control points ***
            // Remove redundant markers
            for (var i = controlPoints.Length; i < _controlPointMarkers.Length; i++)
            {
                var marker = _controlPointMarkers[i];
                _scene.RootNode.Remove(marker);
            }

            // Allocate new array and fill it with existing/additional markers
            var newMarkers = new WireCrossNode[controlPoints.Length];
            for (var i = 0; i < controlPoints.Length; i++)
            {
                var controlPoint = controlPoints[i];
                WireCrossNode marker;

                if (i < _controlPointMarkers.Length)
                {
                    // Update existing marker
                    marker = _controlPointMarkers[i];
                    marker.Transform = new TranslateTransform(controlPoint.X, controlPoint.Y, controlPoint.Z);
                }
                else
                {
                    // Create new marker
                    marker = new WireCrossNode(name: $"Control point marker #{i}")
                    {
                        LinesLength = 10,
                        LineThickness = 1.5f,
                        LineColor = new Color4(Colors.Red),
                        Transform = new TranslateTransform(controlPoint.X, controlPoint.Y, controlPoint.Z)
                    };
                    _scene.RootNode.Add(marker);
                }

                newMarkers[i] = marker;
            }

            // Replace the array
            _controlPointMarkers = newMarkers;
        }

        private void UpdateCurveTypeTextBlock()
        {
            if (_curveNode == null)
                return;

            CurveTypeTextBlock.Text = $"Curve type: {_curveNode.CurveType}";
        }

        private void GenerateNewCurveButton_OnClick(object sender, RoutedEventArgs e)
        {
            int numPositions = 20;
            GenerateRandomControlPoints(numPositions, out var controlPoints, out var weights);
            UpdateControlPoints(controlPoints, weights);
        }

        private void ChangeCurveTypeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_curveNode == null)
                return;

            int maxTypeIndex = Enum.GetValues<CurveLineNode.CurveTypes>().Length;
            int newTypeIndex = ((int)_curveNode.CurveType + 1) % maxTypeIndex;

            _curveNode.CurveType = (CurveLineNode.CurveTypes)newTypeIndex;

            UpdateCurveTypeTextBlock();
        }

        private void GenerateNewWeightsButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_curveNode == null || _curveNode.ControlPoints == null)
                return;

            float minimumValue = 0.5f;
            float maximumValue = 1.5f;

            var numPoints = _curveNode.ControlPoints.Length;
            var weights = new float[numPoints];
            for (var i = 0; i < numPoints; i++)
            {
                weights[i] = minimumValue + (maximumValue - minimumValue) * _rnd.NextSingle();
            }
            _curveNode.Weights = weights;
        }

        private void ResetWeightsButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_curveNode == null || _curveNode.ControlPoints == null)
                return;

            var numPoints = _curveNode.ControlPoints.Length;
            var weights = new float[numPoints];

            for (var i = 0; i < numPoints; i++)
                weights[i] = 1;

            _curveNode.Weights = weights;
        }
    }
}
