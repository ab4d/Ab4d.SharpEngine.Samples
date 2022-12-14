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
    /// Interaction logic for TubePathsTestScene.xaml
    /// </summary>
    public partial class TubePathsTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO? _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private StandardMaterial specularDarkBlueMaterial = StandardMaterials.DarkBlue.SetSpecular(Color3.White, 16);
        private StandardMaterial specularRedMaterial = StandardMaterials.IndianRed.SetSpecular(Color3.White, 16);
        private StandardMaterial specularGreenMaterial = StandardMaterials.ForestGreen.SetSpecular(Color3.White, 16);

        public TubePathsTestScene()
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
                Heading = -50,
                Attitude = -20,
                Distance = 900,
                TargetPosition = new Vector3(70, 0, 0),
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
            CreateTubePaths(scene, 3, -150, specularRedMaterial, false);
            CreateTubePaths(scene, 30, -50, specularGreenMaterial, false);

            if (_bitmapIO != null && _bitmapIO.IsFileFormatImportSupported("png"))
            {
                var textureMaterial = TextureLoader.CreateTextureMaterial(@"Resources\uvchecker2.jpg", _bitmapIO, scene.GpuDevice, generateMipMaps: true);
                CreateTubePaths(scene, 3, 50, textureMaterial, true);
                CreateTubePaths(scene, 30, 150, textureMaterial, true);
            }

            // Lines
            if (true)
            {
                var node = new TubeLineModelNode(
                    new Vector3(-150, -50, -300),
                    new Vector3(-150, -50, 300),
                    2,
                    30,
                    true,
                    true,
                    true,
                    specularRedMaterial,
                    "3D line, red");
                scene.RootNode.Add(node);
            }

            if (true)
            {
                var node = new TubeLineModelNode(
                    new Vector3(-50, -50, -300),
                    new Vector3(-50, -50, 300),
                    2,
                    30,
                    true,
                    true,
                    true,
                    specularGreenMaterial,
                    "3D line, green");
                scene.RootNode.Add(node);
            }

            if (true)
            {
                var node = new TubeLineModelNode(
                    new Vector3(50, -50, -300),
                    new Vector3(50, -50, 300),
                    2,
                    30,
                    true,
                    true,
                    true,
                    specularDarkBlueMaterial,
                    "3D line, blue");
                scene.RootNode.Add(node);
            }

            if (_bitmapIO != null && _bitmapIO.IsFileFormatImportSupported("png"))
            {
                var textureMaterial = TextureLoader.CreateTextureMaterial(@"Resources\uvchecker2.jpg", _bitmapIO, scene.GpuDevice, generateMipMaps: true);
                var node = new TubeLineModelNode(
                    new Vector3(150, -50, -300),
                    new Vector3(150, -50, 300),
                    2,
                    30,
                    true,
                    true,
                    true,
                    textureMaterial,
                    "3D line, textured");
                scene.RootNode.Add(node);
            }
        }

        void CreateTubePaths(Scene scene, int segments, float zOffset, StandardMaterial material, bool generateTextureCoordinates)
        {
            // Basic cylinder (one-segment path, non-closed)
            if (true)
            {
                var pathPoints = new Vector3[]
                {
                    new (0, 0, 0),
                    new (0, 100, 0)
                };
                var node = new TubePathModelNode(
                    pathPoints,
                    20,
                    true,
                    false,
                    segments,
                    null,
                    generateTextureCoordinates,
                    material,
                    $"Single-segment path, non-closed ({segments} side segments)")
                {
                    Transform = new TranslateTransform(-200, 0, zOffset)
                };
                scene.RootNode.Add(node);
            }

            // Three-segment path, non-closed
            if (true)
            {
                var pathPoints = new Vector3[]
                {
                    new (0, 0, 0),
                    new (30, 50, 0),
                    new (30, 100, 0),
                    new (0, 150, 0)
                };
                var node = new TubePathModelNode(
                    pathPoints,
                    20,
                    true,
                    false,
                    segments,
                    null,
                    generateTextureCoordinates,
                    material,
                    $"Three-segment path, non-closed ({segments} side segments)")
                {
                    Transform = new TranslateTransform(-100, 0, zOffset)
                };
                scene.RootNode.Add(node);
            }

            // L-shape, non-closed
            if (true)
            {
                var pathPoints = new Vector3[]
                {
                    new (0, 0, 0),
                    new (0, 100, 0),
                    new (50, 100, 0),
                };
                var node = new TubePathModelNode(
                    pathPoints,
                    20,
                    true,
                    false,
                    segments,
                    null,
                    generateTextureCoordinates,
                    material,
                    $"L-shape path, non-closed ({segments} side segments)")
                {
                    Transform = new TranslateTransform(100, 0, zOffset)
                };
                scene.RootNode.Add(node);
            }

            // L-shape, closed
            if (true)
            {
                var pathPoints = new Vector3[]
                {
                    new (0, 0, 0),
                    new (0, 100, 0),
                    new (50, 100, 0),
                };
                var node = new TubePathModelNode(
                    pathPoints,
                    20,
                    true,
                    true,
                    segments,
                    null,
                    generateTextureCoordinates,
                    material,
                    $"L-shape path, closed ({segments} side segments)")
                {
                    Transform = new TranslateTransform(200, 0, zOffset)
                };
                scene.RootNode.Add(node);
            }

            // Path
            if (true)
            {
                var pathPoints = new Vector3[]
                {
                    new (250, 0, 45),
                    new (-250, 0, 45),
                    new (-250, 100, 45),
                    new (0, 150, 45),
                    new (0, 100, -45),
                    new (250, 100, -45),
                };
                var node = new TubePathModelNode(
                    pathPoints,
                    5,
                    true,
                    false,
                    segments,
                    null,
                    generateTextureCoordinates,
                    material,
                    $"Longer path ({segments} side segments)")
                {
                    Transform = new TranslateTransform(0, 0, zOffset)
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
