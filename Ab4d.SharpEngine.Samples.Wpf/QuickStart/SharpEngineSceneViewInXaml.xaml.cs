using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Wpf;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.Transformations;
using System.Windows.Media.Imaging;
using System.IO;

namespace Ab4d.SharpEngine.Samples.Wpf.QuickStart
{
    /// <summary>
    /// Interaction logic for SharpEngineSceneViewInXaml.xaml
    /// </summary>
    public partial class SharpEngineSceneViewInXaml : Page
    {
        private Scene? _scene;
        private SceneView? _sceneView;

        private GroupNode? _groupNode;
        
        private MouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private int _newObjectsCounter;


        public SharpEngineSceneViewInXaml()
        {
            // Setup logger (before calling InitializeComponent so log events from SharpEngineSceneView can be also logged)
            // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company
            LogHelper.SetupSharpEngineLogger(enableFullLogging: false);

            InitializeComponent();


            // This sample shows how to create SharpEngineSceneView in XAML.
            // To see how do create SharpEngineSceneView in code, see the SharpEngineSceneViewInCode sample.
            //
            // When SharpEngineSceneView is defined in XAML, then the Initialize method that creates the Scene and SceneView
            // is called when the SharpEngineSceneView is loaded (this way it is possible to set CreateOptions and other properties).
            // To get the Scene and SceneView event when they are created, we can use the SceneViewCreated event.
            //

            //
            // Enable standard validation that provides additional error information when Vulkan SDK is installed on the system.
            MainSceneView.CreateOptions.EnableStandardValidation = true;
            
            MainSceneView.SceneViewCreated += delegate(object sender, SceneViewCreatedEventArgs args)
            {
                // NOTE: args.Scene and args.SceneView are never null in SceneViewCreated event handler
                _scene = args.Scene;
                _sceneView = args.SceneView;

                CreateTestScene(_scene, _sceneView);
                SetupMouseCameraController();
            };


            // Instead of waiting for SharpEngineSceneView to be loaded and getting Scene and SceneView from SceneViewCreated event,
            // it is also possible to manually call Initialize and immediately get the Scene and SceneView objects.
            // This is done in the commented code below:
            //var engineCreateOptions = new EngineCreateOptions(applicationName: "SharpEngine WPF samples", enableStandardValidation: true);

            //// Call Initialize method that creates the Vulkan device, Scene and SceneView
            //(_scene, _sceneView) = MainSceneView.Initialize(engineCreateOptions);
            //
            //CreateTestScene(_scene, _sceneView);
            //SetupMouseCameraController();


            this.Unloaded += delegate (object sender, RoutedEventArgs args)
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
                Attitude = -30,
                Distance = 500,
                ViewWidth = 500,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Always
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

        private void CreateTestScene(Scene scene, SceneView sceneView)
        {
            var planeModelNode = new PlaneModelNode(centerPosition: new Vector3(0, 0, 0), 
                                                    size: new Vector2(400, 300), 
                                                    normal: new Vector3(0, 1, 0), 
                                                    heightDirection: new Vector3(0, 0, -1), 
                                                    name: "BasePlane")
            {
                Material = StandardMaterials.Gray,
                BackMaterial = StandardMaterials.Black
            };

            scene.RootNode.Add(planeModelNode);

            // Create a GroupNode that will group all created objects
            _groupNode = new GroupNode("GroupNode");
            _groupNode.Transform = new StandardTransform(translateX: 50, translateZ: 30);
            scene.RootNode.Add(_groupNode);
            
            for (int i = 1; i <= 8; i++)
            {
                var boxModel3D = new BoxModelNode($"BoxModel3D_{i}")
                {
                    Position = new Vector3(-240 + i * 40, 5, 50),
                    PositionType = PositionTypes.Bottom,
                    Size = new Vector3(30, 20, 50),
                    Material = new StandardMaterial(new Color3(1f, i * 0.0625f + 0.5f, i * 0.125f)), // orange to white
                };

                _groupNode.Add(boxModel3D);


                var sphereModel3D = new SphereModelNode($"SphereModel3D_{i}")
                {
                    CenterPosition = new Vector3(-240 + i * 40, 20, -10),
                    Radius = 15,
                    Material = new StandardMaterial(new Color3(1f, i * 0.0625f + 0.5f, i * 0.125f)), // orange to white
                };

                _groupNode.Add(sphereModel3D);
            }
        }

        private void AddNewButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_scene == null || _groupNode == null)
                return;

            var boxModel3D = new BoxModelNode($"BoxModel3D_{_newObjectsCounter}")
            {
                Position = new Vector3(-140, _newObjectsCounter * 30 + 20, -100),
                Size = new Vector3(50, 20, 50),
                Material = StandardMaterials.Gold,
            };

            _groupNode.Add(boxModel3D);

            _newObjectsCounter++;
        }
        
        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_scene == null || _groupNode == null || _groupNode.Count == 0)
                return;

            _groupNode.RemoveAt(_groupNode.Count -1);

            if (_newObjectsCounter > 0)
                _newObjectsCounter--;
        }

        private void ChangeBackgroundButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (MainSceneView == null)
                return;

            MainSceneView.BackgroundColor = SamplesContext.Current.GetRandomWpfColor();
        }
        
        private void ChangeMaterial1Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (_scene == null || _groupNode == null || _groupNode.Count == 0)
                return;

            var index = SamplesContext.Current.GetRandomInt(_groupNode.Count - 1);

            if (_groupNode[index] is ModelNode modelNode)
            {
                if (modelNode.Material is StandardMaterial standardMaterial)
                    standardMaterial.DiffuseColor = SamplesContext.Current.GetRandomColor3();
            }
        }
        
        private void ChangeMaterial2Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (_scene == null || _groupNode == null || _groupNode.Count == 0)
                return;

            var index = SamplesContext.Current.GetRandomInt(_groupNode.Count - 1);

            if (_groupNode[index] is ModelNode modelNode)
            {
                modelNode.Material = SamplesContext.Current.GetRandomStandardMaterial();
            }
        }

        private void RenderToBitmapButton_OnClick(object sender, RoutedEventArgs e)
        {
            var renderedBitmap = MainSceneView.RenderToBitmap(renderNewFrame: true);

            if (renderedBitmap == null)
                return;


            string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SharpEngine.png");

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                PngBitmapEncoder enc = new PngBitmapEncoder();
                BitmapFrame bitmapImage = BitmapFrame.Create(renderedBitmap, null, null, null);
                enc.Frames.Add(bitmapImage);
                enc.Save(fs);
            }

            System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }
    }
}
