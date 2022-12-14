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
    /// Interaction logic for HeightMapTestScene.xaml
    /// </summary>
    public partial class HeightMapTestScene : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private WpfBitmapIO? _bitmapIO;

        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        public enum GradientType
        {
            Technical,
            GeographicalSmooth,
            GeographicalHard,
        }

        private GradientType _gradientType = GradientType.GeographicalSmooth;
        
        private float[,]? _heightData;

        private HeightMapSurfaceNode? _heightMapSurfaceNode;
        private HeightMapContoursNode? _heightMapContoursNode;
        private StandardMaterial? _heightMapTextureMaterial;

        public HeightMapTestScene()
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
                Heading = -30,
                Attitude = -30,
                Distance = 200,
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
            if (_bitmapIO == null)
                return;

            // Load height data from image
            var heightImageData = _bitmapIO.LoadBitmap("Resources/HeightMaps/simpleHeightMap.png");
            _heightData = HeightMapSurfaceNode.CreateHeightDataFromImageData(heightImageData);


            // Create height map surface
            _heightMapSurfaceNode = new HeightMapSurfaceNode(
                centerPosition: new Vector3(0, 0, 0),
                size: new Vector3(100, 20, 100),
                heightData: _heightData,
                useHeightValuesAsTextureCoordinates: true,
                name: "Surface")
            {
                BackMaterial = StandardMaterials.Gray.SetSpecular(Color3.White, 16),
            };


            // Create gradient
            var gradientData = CreateSampleGradient(_gradientType, addTransparentColor: false);

            var gradientTexture1D = TextureFactory.CreateGradientTexture(scene.GpuDevice, gradientData);

            _heightMapTextureMaterial = new StandardMaterial(gradientTexture1D, CommonSamplerTypes.Clamp);
            _heightMapSurfaceNode.Material = _heightMapTextureMaterial;

            scene.RootNode.Add(_heightMapSurfaceNode);


            // Create height map contours, and tie its properties to the height map surface
            _heightMapContoursNode = new HeightMapContoursNode(parentSurfaceNode: _heightMapSurfaceNode, name: "Contours")
            {
                NumContourLines = 20,
                MajorLinesFrequency = 5,
                MinorLineThickness = 1f,
                MajorLineThickness = 2f,
                VerticalOffset = 0.1f,
            };

            scene.RootNode.Add(_heightMapContoursNode);


            // Add wire box around height map
            var wireBoxNode = new WireBoxNode(lineColor: Color4.Black, lineThickness: 2)
            {
                Position = _heightMapSurfaceNode.CenterPosition,
                Size = _heightMapSurfaceNode.Size
            };

            scene.RootNode.Add(wireBoxNode);


            // and grid below the height map
            var wireGridNode = new WireGridNode()
            {
                CenterPosition = new Vector3(_heightMapSurfaceNode.CenterPosition.X, _heightMapSurfaceNode.CenterPosition.Y - _heightMapSurfaceNode.Size.Y * 0.5f, _heightMapSurfaceNode.CenterPosition.Z),
                Size = new Vector2(_heightMapSurfaceNode.Size.X, _heightMapSurfaceNode.Size.Z),
                WidthCellsCount = 10,
                HeightCellsCount = 10,
                MinorLineColor = Colors.DimGray,
                MinorLineThickness = 1
            };

            scene.RootNode.Add(wireGridNode);
        }

        public static GradientStop[] CreateSampleGradient(GradientType type, bool addTransparentColor)
        {
            GradientStop[] stops;
            var idx = 0;

            switch (type)
            {
                case GradientType.Technical:
                    {
                        stops = new GradientStop[addTransparentColor ? 7 : 5];

                        stops[idx++] = new GradientStop(Colors.Red, 1.0f);
                        stops[idx++] = new GradientStop(Colors.Yellow, 0.75f);
                        stops[idx++] = new GradientStop(Colors.LightGreen, 0.5f);
                        stops[idx++] = new GradientStop(Colors.Aqua, 0.25f);

                        if (addTransparentColor)
                        {
                            // All values below 0.01 will be transparent
                            stops[idx++] = new GradientStop(Colors.Blue, 0.01f);
                            stops[idx++] = new GradientStop(Colors.Blue, 0.009f);
                            stops[idx] = new GradientStop(Color4.Transparent, 0.0f);
                        }
                        else
                        {
                            stops[idx] = new GradientStop(Colors.Blue, 0.0f);
                        }
                        break;
                    }
                case GradientType.GeographicalSmooth:
                    {
                        stops = new GradientStop[addTransparentColor ? 8 : 6];

                        stops[idx++] = new GradientStop(Colors.White, 1.0f);
                        stops[idx++] = new GradientStop(Colors.Gray, 0.8f);
                        stops[idx++] = new GradientStop(Colors.SandyBrown, 0.6f);
                        stops[idx++] = new GradientStop(Colors.LightGreen, 0.4f);
                        stops[idx++] = new GradientStop(Colors.Aqua, 0.2f);

                        if (addTransparentColor)
                        {
                            // All values below 0.01 will be transparent
                            stops[idx++] = new GradientStop(Colors.Blue, 0.01f);
                            stops[idx++] = new GradientStop(Color4.Transparent, 0.009f);
                            stops[idx] = new GradientStop(Color4.Transparent, 0.0f);
                        }
                        else
                        {
                            stops[idx] = new GradientStop(Colors.Blue, 0.0f);
                        }
                        break;
                    }
                case GradientType.GeographicalHard:
                    {
                        stops = new GradientStop[addTransparentColor ? 12 : 10];

                        // The gradient with hard transition is defined by making the transition from one color to another very small (for example from 0.799 to 0.8)
                        stops[idx++] = new GradientStop(Colors.White, 1.0f);
                        stops[idx++] = new GradientStop(Colors.White, 0.8f);
                        stops[idx++] = new GradientStop(Colors.SandyBrown, 0.799f);
                        stops[idx++] = new GradientStop(Colors.SandyBrown, 0.6f);
                        stops[idx++] = new GradientStop(Colors.LightGreen, 0.599f);
                        stops[idx++] = new GradientStop(Colors.LightGreen, 0.400f);
                        stops[idx++] = new GradientStop(Colors.Aqua, 0.399f);
                        stops[idx++] = new GradientStop(Colors.Aqua, 0.2f);
                        stops[idx++] = new GradientStop(Colors.Blue, 0.199f);

                        if (addTransparentColor)
                        {
                            // All values below 0.01 will be transparent
                            stops[idx++] = new GradientStop(Colors.Blue, 0.01f);
                            stops[idx++] = new GradientStop(Color4.Transparent, 0.009f);
                            stops[idx] = new GradientStop(Color4.Transparent, 0.0f);
                        }
                        else
                        {
                            stops[idx] = new GradientStop(Colors.Blue, 0.0f);
                        }
                        break;
                    }
                default:
                    {
                        throw new ArgumentException($"Invalid gradient type: {type}!", nameof(type));
                    }
            }

            return stops;
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
