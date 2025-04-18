using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.IO;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Samples.AvaloniaUI.Common;
using Ab4d.SharpEngine.Vulkan;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.QuickStart
{
    /// <summary>
    /// Interaction logic for SharpEngineSceneViewInXaml.xaml
    /// </summary>
    public partial class SharpEngineSceneViewInXaml : UserControl
    {
        private GroupNode? _groupNode;
        
        private PointerCameraController? _pointerCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private int _newObjectsCounter;


        public SharpEngineSceneViewInXaml()
        {
            InitializeComponent();

            // This sample shows how to create SharpEngineSceneView in XAML.
            // To see how do create SharpEngineSceneView in code, see the SharpEngineSceneViewInCode sample.
            //
            // When SharpEngineSceneView is defined in XAML, then the Initialize method that creates the Scene and SceneView
            // is called when the SharpEngineSceneView is loaded (this way it is possible to set CreateOptions and other properties).
            // To get the Scene and SceneView event when they are created, we can use the SceneViewCreated event.
            //


            // When custom MultiSampleCount or SuperSamplingCount is set, use that values.
            // Otherwise, the default values will be used:
            // MSAA: 4x for fast desktop device; 1x otherwise
            // SSAA: 4x for dedicated desktop devices; 2x for integrated devices; 1x otherwise
            if (GlobalSharpEngineSettings.MultisampleCount > 0)
                MainSceneView.MultisampleCount = GlobalSharpEngineSettings.MultisampleCount;
            
            if (GlobalSharpEngineSettings.SupersamplingCount > 0)
                MainSceneView.SupersamplingCount = GlobalSharpEngineSettings.SupersamplingCount;


            // To enable Vulkan's standard validation, set EnableStandardValidation and install Vulkan SDK (this may slightly reduce performance)
            //_sharpEngineSceneView.CreateOptions.EnableStandardValidation = true;

            // Logging was already enabled in SamplesWindow constructor
            //Utilities.Log.LogLevel = LogLevels.Warn;
            //Utilities.Log.IsLoggingToDebugOutput = true;

            // In case when VulkanDevice cannot be created, show an error message
            // If this is not handled by the user, then SharpEngineSceneView will show its own error message
            MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
            {
                ShowDeviceCreateFailedError(args.Exception); // Show error message
                args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
            };

            // We can also manually initialize the SharpEngineSceneView ba calling Initialize method - see commented code below.
            // This would immediately create the VulkanDevice.
            // If this is not done, then Initialize is automatically called when the SharpEngineSceneView is loaded.

            //// Call Initialize method that creates the Vulkan device, Scene and SceneView
            //try
            //{
            //    var gpuDevice = _sharpEngineSceneView.Initialize();
            //}
            //catch (SharpEngineException ex)
            //{
            //    ShowDeviceCreateFailedError(ex);
            //    return;
            //}


            CreateTestScene();
            SetupPointerCameraController();
            
            this.Unloaded += (sender, args) => MainSceneView.Dispose();
        }

        private void SetupPointerCameraController()
        {
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


            _pointerCameraController = new PointerCameraController(MainSceneView)
            {
                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                       // this is already the default value but is still set up here for clarity
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,               // this is already the default value but is still set up here for clarity
                QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default
                ZoomMode = CameraZoomMode.PointerPosition,
                RotateAroundPointerPosition = true
            };
        }

        private void CreateTestScene()
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

            MainSceneView.Scene.RootNode.Add(planeModelNode);

            // Create a GroupNode that will group all created objects
            _groupNode = new GroupNode("GroupNode");
            _groupNode.Transform = new StandardTransform(translateX: 50, translateZ: 30);
            MainSceneView.Scene.RootNode.Add(_groupNode);
            
            for (int i = 1; i <= 8; i++)
            {
                var boxModelNode = new BoxModelNode($"BoxModelNode_{i}")
                {
                    Position = new Vector3(-240 + i * 40, 5, 50),
                    PositionType = PositionTypes.Bottom,
                    Size = new Vector3(30, 20, 50),
                    Material = new StandardMaterial(new Color3(1f, i * 0.0625f + 0.5f, i * 0.125f)), // orange to white
                    UseSharedBoxMesh = false
                };

                _groupNode.Add(boxModelNode);


                var sphereModelNode = new SphereModelNode($"SphereModelNode_{i}")
                {
                    CenterPosition = new Vector3(-240 + i * 40, 20, -10),
                    Radius = 15,
                    Material = new StandardMaterial(new Color3(1f, i * 0.0625f + 0.5f, i * 0.125f)), // orange to white
                };

                _groupNode.Add(sphereModelNode);
            }
        }

        private void ShowDeviceCreateFailedError(Exception ex)
        {
            var errorTextBlock = new TextBlock()
            {
                Text = "Error creating VulkanDevice:\r\n" + ex.Message,
                Foreground = Brushes.Red,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            RootGrid.Children.Add(errorTextBlock);
        }

        private void AddNewButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_groupNode == null)
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
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            _groupNode.RemoveAt(_groupNode.Count -1);

            if (_newObjectsCounter > 0)
                _newObjectsCounter--;
        }

        private void ChangeBackgroundButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (MainSceneView == null)
                return;

            MainSceneView.BackgroundColor = AvaloniaSamplesContext.Current.GetRandomAvaloniaColor();
        }
        
        private void ChangeMaterial1Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            var index = AvaloniaSamplesContext.Current.GetRandomInt(_groupNode.Count - 1);

            if (_groupNode[index] is ModelNode modelNode)
            {
                if (modelNode.Material is StandardMaterial standardMaterial)
                    standardMaterial.DiffuseColor = AvaloniaSamplesContext.Current.GetRandomColor3();
            }
        }
        
        private void ChangeMaterial2Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            var index = AvaloniaSamplesContext.Current.GetRandomInt(_groupNode.Count - 1);

            if (_groupNode[index] is ModelNode modelNode)
            {
                modelNode.Material = AvaloniaSamplesContext.Current.GetRandomStandardMaterial();
            }
        }

        private void RenderToBitmapButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Call SharpEngineSceneView.RenderToBitmap to the get Avalonia's WritableBitmap.
            // This will create a new WritableBitmap on each call. To reuse the WritableBitmap,
            // call the RenderToBitmap and pass the WritableBitmap by ref as the first parameter.
            // It is also possible to call SceneView.RenderToXXXX methods - this give more low level bitmap objects.
            var renderedSceneBitmap = MainSceneView.RenderToBitmap(renderNewFrame: true);

            if (renderedSceneBitmap != null)
            {
                string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AvaloniaSharpEngineScene.png");
                renderedSceneBitmap.Save(fileName);

                System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
            }
        }
    }
}
