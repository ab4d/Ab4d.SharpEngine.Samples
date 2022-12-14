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
    /// Interaction logic for LatheTestScene.xaml
    /// </summary>
    public partial class LatheTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO? _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private StandardMaterial specularDarkBlueMaterial = StandardMaterials.DarkBlue.SetSpecular(Color3.White, 16);
        private StandardMaterial specularRedMaterial = StandardMaterials.IndianRed.SetSpecular(Color3.White, 16);
        private StandardMaterial specularGreenMaterial = StandardMaterials.ForestGreen.SetSpecular(Color3.White, 16);

        private const int segments = 35;

        public LatheTestScene()
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
                Heading = -140,
                Attitude = -40,
                Distance = 1300,
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
            CreateTestMeshes(scene, 0, 100);
            CreateTestMeshes(scene, -150, 75);
            CreateTestMeshes(scene, -300, 50);
            CreateTestMeshes(scene, -450, 25);
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


        private void CreateTestMeshes(Scene scene, float zOffset = 0, float circlePortion = 100)
        {
            var startAngle = 0;
            var endAngle = 360 * circlePortion / 100.0f;

            // Simple diamond
            if (true)
            {
                var sections = new[]
                {
                    new MeshFactory.LatheSection(0.5f, 50, true),
                };

                var mesh = MeshFactory.CreateLatheMesh(
                    new Vector3(0, 0, 0),
                    new Vector3(0, 150, 0),
                    sections,
                    segments,
                    isStartPositionClosed: true,
                    isEndPositionClosed: true,
                    generateTextureCoordinates: true,
                    startAngle: startAngle,
                    endAngle: endAngle,
                    isMeshClosed: true
                );
                var model = new MeshModelNode(mesh, specularDarkBlueMaterial, $"Blue diamond ({circlePortion}%)")
                {
                    Transform = new TranslateTransform(-375, 0, zOffset)
                };
                scene.RootNode.Add(model);
            }

            // Diamond with flat side
            if (true)
            {
                var sections = new[]
                {
                    new MeshFactory.LatheSection(0.25f, 50, true),
                    new MeshFactory.LatheSection(0.75f, 50, true),
                };

                var mesh = MeshFactory.CreateLatheMesh(
                    new Vector3(0, 0, 0),
                    new Vector3(0, 150, 0),
                    sections,
                    segments,
                    isStartPositionClosed: true,
                    isEndPositionClosed: true,
                    generateTextureCoordinates: true,
                    startAngle: startAngle,
                    endAngle: endAngle,
                    isMeshClosed: true
                );
                var model = new MeshModelNode(mesh, specularRedMaterial, $"Red diamond with flat side ({circlePortion}%)")
                {
                    Transform = new TranslateTransform(-225, 0, zOffset)
                };
                scene.RootNode.Add(model);
            }

            // Pine tree
            if (true)
            {
                var sections = new[]
                {
                    new MeshFactory.LatheSection(0.00f, 25, true),
                    new MeshFactory.LatheSection(0.25f, 25, true),
                    new MeshFactory.LatheSection(0.25f, 55, true),
                    new MeshFactory.LatheSection(0.25f, 65, true),
                    new MeshFactory.LatheSection(0.50f, 35, true),
                    new MeshFactory.LatheSection(0.50f, 45, true),
                    new MeshFactory.LatheSection(0.75f, 15, true),
                    new MeshFactory.LatheSection(0.75f, 25, true),
                    new MeshFactory.LatheSection(1.0f, 0, true), // Redundant due to end being closed
                };

                var mesh = MeshFactory.CreateLatheMesh(
                    new Vector3(0, 0, 0),
                    new Vector3(0, 300, 0),
                    sections,
                    segments,
                    isStartPositionClosed: true,
                    isEndPositionClosed: true,
                    generateTextureCoordinates: true,
                    startAngle: startAngle,
                    endAngle: endAngle,
                    isMeshClosed: true
                );
                var model = new MeshModelNode(mesh, specularGreenMaterial, $"Green pine tree ({circlePortion}%)")
                {
                    Transform = new TranslateTransform(-75, 0, zOffset)
                };
                scene.RootNode.Add(model);
            }

            // These meshes cannot be rendered in partial way without looking weird...

            // Glass
            //if (circlePortion == 100)
            {
                var sections = new[]
                {
                    new MeshFactory.LatheSection(0.00f, 50, true),
                    new MeshFactory.LatheSection(1.00f, 50, true),
                    new MeshFactory.LatheSection(1.00f, 40, true),
                    new MeshFactory.LatheSection(0.10f, 40, true),
                    new MeshFactory.LatheSection(0.10f, 0, true), // We need to manually close the bottom - FIXME: creates an artifact!
                };

                var mesh = MeshFactory.CreateLatheMesh(
                    new Vector3(0, 0, 0),
                    new Vector3(0, 150, 0),
                    sections,
                    segments,
                    isStartPositionClosed: true,
                    isEndPositionClosed: false,
                    generateTextureCoordinates: true,
                    startAngle: startAngle,
                    endAngle: endAngle,
                    isMeshClosed: false
                );
                var model = new MeshModelNode(mesh, specularGreenMaterial, $"Green glass ({circlePortion}%)")
                {
                    Transform = new TranslateTransform(75, 0, zOffset),
                    BackMaterial = StandardMaterials.Red
                };
                scene.RootNode.Add(model);
            }

            // Cylinder
            if (circlePortion == 100)
            {
                var sections = new[]
                {
                    new MeshFactory.LatheSection(0.00f, 48, true),
                    new MeshFactory.LatheSection(0.00f, 50, true),
                    new MeshFactory.LatheSection(1.00f, 50, true),
                    new MeshFactory.LatheSection(1.00f, 48, true),
                    new MeshFactory.LatheSection(0.00f, 48, true),
                };

                var mesh = MeshFactory.CreateLatheMesh(
                    new Vector3(0, 0, 0),
                    new Vector3(0, 150, 0),
                    sections,
                    segments,
                    isStartPositionClosed: false,
                    isEndPositionClosed: false,
                    generateTextureCoordinates: true,
                    startAngle: startAngle,
                    endAngle: endAngle,
                    isMeshClosed: true
                );
                var model = new MeshModelNode(mesh, specularDarkBlueMaterial, $"Blue empty cylinder ({circlePortion}%)")
                {
                    Transform = new TranslateTransform(225, 0, zOffset)
                };
                scene.RootNode.Add(model);
            }

            // Chalice
            if (circlePortion == 100)
            {
                var sections = new[]
                {
                    new MeshFactory.LatheSection(0.00f, 40, true),
                    new MeshFactory.LatheSection(0.05f, 40, true),
                    new MeshFactory.LatheSection(0.20f, 8, true),
                    new MeshFactory.LatheSection(0.25f, 5, true),
                    new MeshFactory.LatheSection(0.38f, 5, true),
                    new MeshFactory.LatheSection(0.40f, 10, true),
                    new MeshFactory.LatheSection(0.42f, 5, true),
                    new MeshFactory.LatheSection(0.50f, 5, true),
                    new MeshFactory.LatheSection(0.75f, 35, true),
                    new MeshFactory.LatheSection(1.00f, 50, true),
                    new MeshFactory.LatheSection(1.00f, 48, true),
                    new MeshFactory.LatheSection(0.75f, 33, true),
                    new MeshFactory.LatheSection(0.50f, 0, true),
                };

                var mesh = MeshFactory.CreateLatheMesh(
                    new Vector3(0, 0, 0),
                    new Vector3(0, 150, 0),
                    sections,
                    segments,
                    isStartPositionClosed: true,
                    isEndPositionClosed: true,
                    generateTextureCoordinates: true,
                    startAngle: startAngle,
                    endAngle: endAngle,
                    isMeshClosed: true
                );
                var model = new MeshModelNode(mesh, specularRedMaterial, $"Red chalice ({circlePortion}%)")
                {
                    Transform = new TranslateTransform(375, 0, zOffset)
                };
                scene.RootNode.Add(model);
            }
        }
    }
}
