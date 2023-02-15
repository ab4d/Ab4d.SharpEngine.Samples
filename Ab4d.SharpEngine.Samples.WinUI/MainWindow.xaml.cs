// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Microsoft.UI.Xaml;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Transformations;
using Microsoft.UI;
using System;
using Windows.ApplicationModel;
using Ab4d.SharpEngine.Samples.WinUI.Common;
using Ab4d.SharpEngine.WinUI;
using Microsoft.UI.Windowing;
using Colors = Microsoft.UI.Colors;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Ab4d.SharpEngine.Samples.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private DirectionalLight? _directionalLight;
        private TargetPositionCamera? _targetPositionCamera;
        private MouseCameraController? _mouseCameraController;
        private WinUIBitmapIO? _winUiBitmapIo;

        private AxisLineNode? _axisLineNode;

        public MainWindow()
        {
            // Setup logger
            // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company
            LogHelper.SetupSharpEngineLogger(enableFullLogging: false);        
        
            InitializeComponent(); // To generate the source for InitializeComponent include XamlNameReferenceGenerator

            SetWindowIcon();

            this.Title = "SharpEngine with WinUI";

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


            _mouseCameraController = new MouseCameraController(MainSceneView)
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


            // Create WinUIBitmapIO that will use objects from WinUI to load bitmaps
            _winUiBitmapIo ??= new WinUIBitmapIO();

            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\TreeTexture.png");
            var treePlaneMaterial = TextureLoader.CreateTextureMaterial(fileName, _winUiBitmapIo, scene.GpuDevice);

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

            string objFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\robotarm.obj");
            var readObjModelNode = readerObj.ReadSceneNodes(objFileName);

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

        private void OnUseTransparentBackgroundCheckBoxCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (UseTransparentBackgroundCheckBox == null || MainSceneView == null)
                return;

            if (UseTransparentBackgroundCheckBox.IsChecked ?? false)
            {
                MainSceneView.BackgroundColor = Colors.Transparent;
            }
            else
            {
                var rnd = new Random();
                MainSceneView.BackgroundColor = Windows.UI.Color.FromArgb(255, (byte)(rnd.NextDouble() * 255), (byte)(rnd.NextDouble() * 255), (byte)(rnd.NextDouble() * 255));
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

        private void SetWindowIcon()
        {
            // From: https://github.com/microsoft/microsoft-ui-xaml/issues/4056
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sharp-engine_logo.ico"));
        }
    }
}
