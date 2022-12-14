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
    /// Interaction logic for TubesTestScene.xaml
    /// </summary>
    public partial class TubesTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO? _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private StandardMaterial specularRedMaterial = StandardMaterials.IndianRed.SetSpecular(Color3.White, 16);
        private StandardMaterial specularGreenMaterial = StandardMaterials.ForestGreen.SetSpecular(Color3.White, 16);

        public TubesTestScene()
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
                Heading = -120,
                Attitude = -40,
                Distance = 1800,
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
            // Low-segment tubes (to show that segments are correctly handled)
            CreateTubeMeshes(scene, 3, 0, 360, 300, specularRedMaterial);
            CreateTubeMeshes(scene, 3, 45, 225, 150, specularRedMaterial);

            // High-segment tubes
            CreateTubeMeshes(scene, 30, 0, 360, -150, specularGreenMaterial);
            CreateTubeMeshes(scene, 30, 45, 225, -300, specularGreenMaterial);

            if (_bitmapIO != null && _bitmapIO.IsFileFormatImportSupported("png"))
            {
                var textureMaterial = TextureLoader.CreateTextureMaterial(@"Resources\uvchecker2.jpg", _bitmapIO, scene.GpuDevice, generateMipMaps: true);

                // Low-segment tubes (to show that segments are correctly handled)
                CreateTubeMeshes(scene, 3, 0, 360, 600, textureMaterial);
                CreateTubeMeshes(scene, 3, 45, 225, 450, textureMaterial);

                // High-segment tubes
                CreateTubeMeshes(scene, 30, 0, 360, -450, textureMaterial);
                CreateTubeMeshes(scene, 30, 45, 225, -600, textureMaterial);
            }
        }


        private void CreateTubeMeshes(Scene scene, int segments, float startAngle, float endAngle, float zOffset, StandardMaterial material)
        {
            var circleDescription = startAngle == 0 && endAngle == 360 ? "full circle" : "partial circle";

            // Regular-case tube
            if (true)
            {
                var node = new TubeModelNode(
                    new Vector3(0, 0, 0),
                    new Vector3(0, 1, 0),
                    30,
                    35,
                    45,
                    40,
                    200,
                    segments,
                    startAngle,
                    endAngle,
                    material,
                    $"{segments}-segment tube, regular, {circleDescription}"
                )
                {
                    Transform = new TranslateTransform(-150, 0, zOffset)
                };
                scene.RootNode.Add(node);
            }

            // Case with both inner radii being zero -> closed lathe
            if (true)
            {
                var node = new TubeModelNode(
                    new Vector3(0, 0, 0),
                    new Vector3(0, 1, 0),
                    30,
                    0,
                    45,
                    0,
                    200,
                    segments,
                    startAngle,
                    endAngle,
                    material,
                    $"{segments}-segment tube, closed lathe, {circleDescription}")
                {
                    Transform = new TranslateTransform(0, 0, zOffset)
                };
                scene.RootNode.Add(node);
            }

            // Case with height being zero -> flat shape
            if (true)
            {
                var node = new TubeModelNode(
                    new Vector3(0, 0, 0),
                    new Vector3(0, 1, 0),
                    45,
                    40,
                    45,
                    40,
                    0,
                    segments,
                    startAngle,
                    endAngle,
                    material,
                    $"{segments}-segment tube, zero height, {circleDescription}")
                {
                    Transform = new TranslateTransform(150, 0, zOffset),
                    BackMaterial = material
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
