using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
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
    /// Interaction logic for WireGridTestScene.xaml
    /// </summary>
    public partial class WireGridTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO? _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private WireGridNode? _wireGridNode;

        public WireGridTestScene()
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
            var axesNode = new AxisLineNode(length: 50);
            scene.RootNode.Add(axesNode);

            // Grid
            _wireGridNode = new WireGridNode("Wire grid")
            {
                CenterPosition = new Vector3(0, 0, 0),
                Size = new Vector2(300, 300),

                WidthDirection = new Vector3(1, 0, 0),  // this is also the default value
                HeightDirection = new Vector3(0, 0, -1), // this is also the default value

                WidthCellsCount = 30,
                HeightCellsCount = 30,

                MajorLineColor = Colors.Black,
                MajorLineThickness = 2,

                MinorLineColor = Colors.DimGray,
                MinorLineThickness = 1,

                MajorLinesFrequency = 5,

                IsClosed = true,
            };

            if (IsLoaded)
                UpdateWireGridSetting(); // Set setting from controls

            scene.RootNode.Add(_wireGridNode);
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


        private void OnWireGridSettingChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            UpdateWireGridSetting();
        }

        private void UpdateWireGridSetting()
        {
            if (_wireGridNode == null)
                return;

            // Size
            var selectedSizeText = (string)((ComboBoxItem)SizeComboBox.SelectedItem).Content;
            var sizeParts = selectedSizeText.Split(' ');

            float width = float.Parse(sizeParts[0]);
            float height = float.Parse(sizeParts[1]);

            _wireGridNode.Size = new Vector2(width, height);

            // WidthCellsCount, HeightCellsCount
            _wireGridNode.WidthCellsCount = (int)WidthCellsCountSlider.Value;
            _wireGridNode.HeightCellsCount = (int)HeightCellsCountSlider.Value;

            // MinorLines
            var colorText = (string)((ComboBoxItem)MinorLineColorComboBox.SelectedItem).Content;
            _wireGridNode.MinorLineColor = Color4.Parse(colorText);
            _wireGridNode.MinorLineThickness = (float)MinorLinesThicknessSlider.Value;

            // MajorLines
            colorText = (string)((ComboBoxItem)MajorLineColorComboBox.SelectedItem).Content;
            _wireGridNode.MajorLineColor = Color4.Parse(colorText);
            _wireGridNode.MajorLineThickness = (float)MajorLinesThicknessSlider.Value;

            // MajorLinesFrequency
            _wireGridNode.MajorLinesFrequency = (int)MajorLinesFrequencySlider.Value;

            // IsClosed
            _wireGridNode.IsClosed = IsClosedCheckBox.IsChecked ?? false;
        }
    }
}
