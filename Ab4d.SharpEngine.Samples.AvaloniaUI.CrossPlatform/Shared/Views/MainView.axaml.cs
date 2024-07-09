using System;
using System.Diagnostics;
using System.Numerics;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.CrossPlatform.Views
{
    public partial class MainView : UserControl
    {
        private GroupNode? _groupNode;
        
        private PointerCameraController? _pointerCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private int _newObjectsCounter;

        public MainView()
        {
            // We need to call SetLicense in the entry assembly.
            // Therefore, this call is moved to Android/MainActivity.cs, Desktop/Program.cs and iOS/Main.cs
            //// Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
            //// To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
            //Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
            //                                      licenseType: "SamplesLicense",
            //                                      platforms: "All",
            //                                      license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");


            InitializeComponent();

            // In case when VulkanDevice cannot be created, show an error message
            // If this is not handled by the user, then SharpEngineSceneView will show its own error message
            MainSceneView.GpuDeviceCreationFailed += delegate (object sender, DeviceCreateFailedEventArgs args)
            {
                ShowDeviceCreateFailedError(args.Exception); // Show error message
                args.IsHandled = true;                       // Prevent showing error by SharpEngineSceneView
            };

            MainSceneView.GpuDeviceCreated += (sender, args) =>
            {
                var sharpEngineVersion = MainSceneView.GetType().Assembly.GetName().Version ?? new Version(0, 0, 0);
                InfoText.Text = $"Using Ab4d.SharpEngine v{sharpEngineVersion.Major}.{sharpEngineVersion.Minor}.{sharpEngineVersion.Revision} on {args.GpuDevice.GpuName} GPU";
            };

            CreateTestScene();
            SetupMouseCameraController();

            this.Unloaded += (sender, args) => MainSceneView.Dispose();
        }

        private void SetupMouseCameraController()
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
                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                     // this is already the default value but is still set up here for clarity
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,             // this is already the default value but is still set up here for clarity
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

        private void ShowDeviceCreateFailedError(Exception ex)
        {
            var errorTextBlock = new TextBlock()
            {
                Text = "Error creating VulkanDevice:\r\n" + ex.Message,
                Foreground = Brushes.Red,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
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

            MainSceneView.BackgroundColor = Color.FromRgb((byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256));
        }
        
        private void ChangeMaterial1Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            var index = Random.Shared.Next(_groupNode.Count - 1);

            if (_groupNode[index] is ModelNode modelNode)
            {
                if (modelNode.Material is StandardMaterial standardMaterial)
                    standardMaterial.DiffuseColor = new Color3(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle());
            }
        }
        
        private void ChangeMaterial2Button_OnClick(object sender, RoutedEventArgs e)
        {
            if (_groupNode == null || _groupNode.Count == 0)
                return;

            var index = Random.Shared.Next(_groupNode.Count - 1);

            if (_groupNode[index] is ModelNode modelNode)
            {
                var randomColor = new Color3(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle());
                modelNode.Material = new StandardMaterial(randomColor);
            }
        }
    }
}