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
    /// Interaction logic for ExtrudedMeshTestScene.xaml
    /// </summary>
    public partial class ExtrudedMeshTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO? _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private StandardMaterial specularDarkBlueMaterial = StandardMaterials.DarkBlue.SetSpecular(Color3.White, 16);
        private StandardMaterial specularRedMaterial = StandardMaterials.IndianRed.SetSpecular(Color3.White, 16);
        private StandardMaterial specularGreenMaterial = StandardMaterials.ForestGreen.SetSpecular(Color3.White, 16);

        public ExtrudedMeshTestScene()
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
                Distance = 800,
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
            // Cube
            var baseShape = new Vector2[]
            {
                new(25, 25),
                new(-25, 25),
                new(-25, -25),
                new(25, -25)
            };
            var mesh = MeshFactory.CreateExtrudedMesh(
                baseShape,
                false,
                new Vector3(0, 0, 0),
                new Vector3(0, 50, 0),
                true,
                true
            );
            var model = new MeshModelNode(mesh, specularDarkBlueMaterial, "Blue box")
            {
                Transform = new TranslateTransform(-100, 0, 0)
            };
            scene.RootNode.Add(model);


            // 3D parallelogram
            baseShape = new Vector2[]
            {
                new(25, 25),
                new(-25, 25),
                new(-25, -25),
                new(25, -25)
            };
            mesh = MeshFactory.CreateExtrudedMesh(
                baseShape,
                false,
                new Vector3(0, 0, 0),
                new Vector3(0, 25, 25),
                true,
                true
            );
            model = new MeshModelNode(mesh, specularGreenMaterial, "Green 3D parallelogram")
            {
                Transform = new TranslateTransform(0, 0, 0)
            };
            scene.RootNode.Add(model);


            // 3D parallelogram (ground-plane aligned)
            baseShape = new Vector2[]
            {
                new(25, 25),
                new(-25, 25),
                new(-25, -25),
                new(25, -25)
            };
            mesh = MeshFactory.CreateExtrudedMesh(
                baseShape,
                false,
                new Vector3(0, 0, 0),
                new Vector3(0, 50, 50),
                new Vector3(0, 0, 1),  // Force base shape to lie in the "ground" plane.
                MeshFactory.ExtrudeTextureCoordinatesGenerationType.Cylindrical,
                true,
                true
            );
            model = new MeshModelNode(mesh, specularRedMaterial, "Red 3D parallelogram (ground-plane aligned)")
            {
                Transform = new TranslateTransform(100, 0, 0)
            };
            scene.RootNode.Add(model);


            // Triangle base
            baseShape = CreateBaseShape(new Vector2(0, 0), 25, 3);
            mesh = MeshFactory.CreateExtrudedMesh(
                baseShape,
                false,
                new Vector3(0, 0, 0),
                new Vector3(0, 50, 0),
                true,
                true
            );
            model = new MeshModelNode(mesh, specularRedMaterial, "Triangle base")
            {
                Transform = new TranslateTransform(-100, 0, -100)
            };
            scene.RootNode.Add(model);


            // Pentagon base
            baseShape = CreateBaseShape(new Vector2(0, 0), 25, 5);
            mesh = MeshFactory.CreateExtrudedMesh(
                baseShape,
                false,
                new Vector3(0, 0, 0),
                new Vector3(0, 50, 0),
                true,
                true
            );
            model = new MeshModelNode(mesh, specularGreenMaterial, "Pentagon base")
            {
                Transform = new TranslateTransform(0, 0, -100)
            };
            scene.RootNode.Add(model);


            // Hexagon base
            baseShape = CreateBaseShape(new Vector2(0, 0), 25, 6);
            mesh = MeshFactory.CreateExtrudedMesh(
                baseShape,
                false,
                new Vector3(0, 0, 0),
                new Vector3(0, 50, 0),
                true,
                true
            );
            model = new MeshModelNode(mesh, specularDarkBlueMaterial, "Hexagon base")
            {
                Transform = new TranslateTransform(100, 0, -100)
            };
            scene.RootNode.Add(model);


            // Pentagon base with texture
            if (_bitmapIO != null && _bitmapIO.IsFileFormatImportSupported("png"))
            {
                var textureMaterial = TextureLoader.CreateTextureMaterial(@"Resources\uvchecker2.jpg", _bitmapIO, scene.GpuDevice, generateMipMaps: true);

                baseShape = CreateBaseShape(new Vector2(0, 0), 25, 5);
                mesh = MeshFactory.CreateExtrudedMesh(
                    baseShape,
                    false,
                    new Vector3(0, 0, 0),
                    new Vector3(0, 50, 0),
                    true,
                    true
                );
                model = new MeshModelNode(mesh, textureMaterial, "Pentagon base, textured")
                {
                    Transform = new TranslateTransform(200, 0, -100)
                };
                scene.RootNode.Add(model);
            }


            // Approximated cylinder with texture
            if (_bitmapIO != null && _bitmapIO.IsFileFormatImportSupported("png"))
            {
                var textureMaterial = TextureLoader.CreateTextureMaterial(@"Resources\uvchecker2.jpg", _bitmapIO, scene.GpuDevice, generateMipMaps: true);

                baseShape = CreateBaseShape(new Vector2(0, 0), 25, 35);
                mesh = MeshFactory.CreateExtrudedMesh(
                    baseShape,
                    true,
                    new Vector3(0, 0, 0),
                    new Vector3(0, 50, 0),
                    true,
                    true
                );
                model = new MeshModelNode(mesh, textureMaterial, "Almost cylinder, textured")
                {
                    Transform = new TranslateTransform(300, 0, -100)
                };
                scene.RootNode.Add(model);
            }


            // Extrusion along path
            baseShape = CreateBaseShape(new Vector2(0, 0), 5, 5);
            var path = new Vector3[]
            {
                new(-300, 0, 0),
                new(-300, 100, 0),
                new(0, 200, 0),
                new(0, 200, -100),
                new(300, 200, -100)
            };

            mesh = MeshFactory.CreateExtrudedMeshAlongPath(
                baseShape,
                path,
                new Vector3(0, 0, 1)
            );
            model = new MeshModelNode(mesh, specularGreenMaterial, "Extruded path with pentagon base");
            scene.RootNode.Add(model);
        }

        private Vector2[] CreateBaseShape(Vector2 center, float radius, int numCorners)
        {
            var corners = new Vector2[numCorners];
            var angleStep = 2 * MathF.PI / numCorners;

            for (var i = 0; i < numCorners; i++)
            {
                var angle = i * angleStep;
                corners[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            }

            return corners;
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
