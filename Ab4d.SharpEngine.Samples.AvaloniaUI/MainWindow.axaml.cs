using System;
using System.Diagnostics;
using System.Numerics;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TextureLoader = Ab4d.SharpEngine.Utilities.TextureLoader;

// !!! IMPORTANT !!!
// When SharpEngine is used with Avalonia, then Avalonia needs to be initialized
// by setting UseWgl to true in Win32PlatformOptions.
// If this is not done, then shared texture cannot be used and then WritableBitmap will be used.
// This is much slower because in this case the rendered image is copied from GPU to main memory 
// into the WritableBitmap and then it is copied back to GPU to show the rendering.
// See Program.cs file on how to do that.


namespace AvaloniaTest
{
    public partial class MainWindow : Window
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private DirectionalLight? _directionalLight;
        private TargetPositionCamera? _targetPositionCamera;
        private MouseCameraController? _mouseCameraController;
        private SkiaSharpBitmapIO? _skiaSharpBitmapIo;

        private AxisLineNode? _axisLineNode;

        public MainWindow()
        {
            InitializeComponent(); // To generate the source for InitializeComponent include XamlNameReferenceGenerator

            // SharpEngineSceneView is an Avalonia control that can show SharpEngine's SceneView in an Avalonia application.
            // The SharpEngineSceneView creates the VulkanDevice, Scene and SceneView objects.
            //
            // Scene is used to define the 3D objects (children of RootNode) and lights (added to Lights collection).
            // SceneView is a view of the Scene and can render the objects in the scene. It provides a Camera to define the view.
            //
            // The SharpEngineSceneView below will try to use SharedTexture as presentation option.
            // This way the rendered 3D scene will be shared with WPF composition engine so that
            // the rendered image will stay on the graphics card.
            // This allows composition of 3D scene with other WPF objects.
            // If this mode is not possible, then WriteableBitmap presentation type is used.
            // In this mode, the rendered texture is copied to main memory into a WriteableBitmap.
            // This is much slower because of additional memory traffic.
            // The OverlayTexture mode is not supported by the SharpEngineSceneView

            if (System.OperatingSystem.IsWindows() && !SharpEngineSceneView.IsCorrectAvaloniaPlatform(SharpEngineSceneView.PresentationType))
                ErrorTextBlock.Text = "On Windows SharpEngineSceneView requires that Avalonia application is using the native Windows OpenGL platform. Otherwise it is not possible to use SharedTexture as PresentationType. To solve that add the '.With(new Win32PlatformOptions { UseWgl = true })' to the Avalonia's AppBuilder (usually in the Program.cs file).";


            // Set EnableStandardValidation to true, but the Vulkan validation will be enabled only when the Vulkan SDK is installed on the system.
            SharpEngineSceneView.CreateOptions.EnableStandardValidation = true;

            SharpEngineSceneView.SceneViewCreated += delegate(object sender, SceneViewCreatedEventArgs args)
            {
                _scene     = args.Scene;
                _sceneView = args.SceneView;

                SetupMouseCameraController();
                CreateLights();

                CreateTestScene(_scene);
            };

            SharpEngineSceneView.SceneViewInitialized += delegate(object? sender, EventArgs args)
            {
                PresentationTypeTextBlock.Text = SharpEngineSceneView.PresentationType.ToString();
                
                if (_scene != null)
                    GraphicsCardTextBlock.Text = _scene.GpuDevice.GpuName;
            };

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void SetupMouseCameraController()
        {
            if (_sceneView == null)
                return;


            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -25,
                Distance = 1100,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Auto // Automatically add a CameraLight if there are no other lights in the Scene
            };

            _sceneView.Camera = _targetPositionCamera;


            _mouseCameraController = new MouseCameraController(SharpEngineSceneView)
            {
                RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed,                                                   // this is already the default value but is still set up here for clarity
                MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.ControlKey,             // this is already the default value but is still set up here for clarity
                QuickZoomConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.MousePosition,
                RotateAroundMousePosition = true
            };
        }

        private void CreateLights()
        {
            if (_scene == null)
                return;


            _scene.Lights.Clear();

            // Add lights
            _scene.Lights.Add(new AmbientLight(new Color3(0.3f, 0.3f, 0.3f)));

            _directionalLight = new DirectionalLight(new Vector3(-1, -0.3f, 0));
            _scene.Lights.Add(_directionalLight);

            _scene.Lights.Add(new PointLight(new Vector3(-200, 0, 100), 20000) { Attenuation = new Vector3(1, 0, 0) });
            _scene.Lights.Add(new SpotLight(new Vector3(300, 0, 300), new Vector3(0, -0.3f, -1)) { Color = new Color3(0.4f, 0.4f, 0.4f) });
        }

        private void CreateTestScene(Scene scene)
        {
            var sphereModelNode = new SphereModelNode("Blue sphere")
            {
                Radius = 20,
                Material = StandardMaterials.Blue,
                Transform = new TranslateTransform(0, 0, 200)
            };

            scene.RootNode.Add(sphereModelNode);


            var boxModelNode = new BoxModelNode("Red box")
            {
                Position = new Vector3(200, 0, 0),
                PositionType = PositionTypes.Center,
                Size = new Vector3(40, 20, 40),
                Material = StandardMaterials.Red,
            };

            scene.RootNode.Add(boxModelNode);


            var planeModelNode = new PlaneModelNode("Gray plane")
            {
                Position = new Vector3(0, -50, 0),
                PositionType = PositionTypes.Center,
                Normal = new Vector3(0, 1, 0),
                HeightDirection = new Vector3(1, 0, 0),
                Size = new Vector2(800, 1000),
                Material = StandardMaterials.Gray,
                BackMaterial = StandardMaterials.Black
            };

            scene.RootNode.Add(planeModelNode);


            // We use SkiaSharp library to load images
            // This is also internally used by Avalonia so the library should be already loaded
            _skiaSharpBitmapIo ??= new SkiaSharpBitmapIO();

            var treePlaneMaterial = TextureLoader.CreateTextureMaterial(@"Resources\TreeTexture.png", _skiaSharpBitmapIo, scene.GpuDevice);

            for (int i = 0; i < 5; i++)
            {
                StandardMaterial usedMaterial;

                if (i < 2)
                {
                    usedMaterial = treePlaneMaterial;
                }
                else
                {
                    usedMaterial = (StandardMaterial)treePlaneMaterial.Clone($"TreeTexture_{i}");
                    usedMaterial.AlphaClipThreshold = 0.2f * (float)(i - 1);
                }

                var treePlane = new PlaneModelNode($"TreePlane_{i}")
                {
                    Position = new Vector3(-400 + i * 50, 20, -100),
                    Size = new Vector2(100, 150),
                    Normal = new Vector3(1, 0, 0),
                    HeightDirection = new Vector3(0, 1, 0),
                    Material = usedMaterial
                };

                treePlane.BackMaterial = treePlane.Material;

                scene.RootNode.Add(treePlane);
            }


            var readerObj = new ReaderObj();
            var readObjModelNode = readerObj.ReadSceneNodes("Resources/robotarm.obj");

            SceneNodeUtils.PositionAndScaleSceneNode(readObjModelNode, new Vector3(-300, -49, 100), PositionTypes.Bottom, new Vector3(200, 200, 200));

            scene.RootNode.Add(readObjModelNode);


            _axisLineNode = new AxisLineNode();
            scene.RootNode.Add(_axisLineNode);

            UpdateAxisVisibility();
        }

        public void AddSphere()
        {
            if (_scene == null)
                return;

            var sphereModelNode = new SphereModelNode("Green sphere")
            {
                Radius = 20,
                Material = StandardMaterials.Green,
                Transform = new TranslateTransform(150, (_scene.RootNode.Count - 7) * 50 - 150, 0)
            };

            _scene.RootNode.Add(sphereModelNode);
        }

        private void AddSphereButton_OnClick(object? sender, RoutedEventArgs e)
        {
            AddSphere();
        }

        private void StartStopCameraRotateButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_targetPositionCamera == null)
                return;

            if (_targetPositionCamera.IsRotating)
            {
                _targetPositionCamera.StopRotation();
                StartStopCameraRotateButton.Content = "Start camera rotate";
            }
            else
            {
                _targetPositionCamera.StartRotation(50, 0);
                StartStopCameraRotateButton.Content = "Stop camera rotate";

            }
        }

        private void RenderToBitmapButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (SharpEngineSceneView == null)
                return;

            var renderedSceneBitmap = SharpEngineSceneView.RenderToBitmap(renderNewFrame: true);

            if (renderedSceneBitmap != null)
            {
                string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AvaloniaSharpEngineScene.png");
                renderedSceneBitmap.Save(fileName);

                System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
            }
        }

        private void OnUseTransparentBackgroundCheckBoxCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (UseTransparentBackgroundCheckBox == null || SharpEngineSceneView == null)
                return;

            if (UseTransparentBackgroundCheckBox.IsChecked ?? false)
            {
                SharpEngineSceneView.BackgroundColor = Colors.Transparent.ToAvaloniaColor();
            }
            else
            {
                var rnd = new Random();
                SharpEngineSceneView.BackgroundColor = new Avalonia.Media.Color(255, (byte)(rnd.NextDouble() * 255), (byte)(rnd.NextDouble() * 255), (byte)(rnd.NextDouble() * 255));
            }
        }
        
        private void OnShowAxisCheckBoxCheckedChanged(object? sender, RoutedEventArgs e)
        {
            UpdateAxisVisibility();
        }

        private void UpdateAxisVisibility()
        {
            if (_axisLineNode == null || ShowAxisCheckBox == null)
                return;

            _axisLineNode.Visibility = (ShowAxisCheckBox.IsChecked ?? false) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        }
    }
}
