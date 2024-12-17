using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Security.Policy;
using System.Windows;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.WinUI.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Colors = Microsoft.UI.Colors;

namespace Ab4d.SharpEngine.Samples.WinUI.QuickStart
{
    /// <summary>
    /// Interaction logic for SharpEngineSceneViewInCode.xaml
    /// </summary>
    public partial class SharpEngineSceneViewInCode : UserControl
    {
        private readonly SharpEngineSceneView _sharpEngineSceneView;
        private PointerCameraController? _pointerCameraController;

        public SharpEngineSceneViewInCode()
        {
            InitializeComponent();

            // Create SharpEngineSceneView:
            //
            // SharpEngineSceneView is a WinUI control that can show SharpEngine's SceneView in a WinUI application.
            // The SharpEngineSceneView creates the VulkanDevice, Scene and SceneView objects.
            //
            // Scene is used to define the 3D objects (added to Scene.RootNode) and lights (added to Scene.Lights collection).
            // SceneView is a view of the Scene and can render the objects in the Scene. It provides a Camera and size of the view.
            //
            // SharpEngineSceneView for WinUI supports the following presentation types:
            // SharedTexture:
            // The SharpEngineSceneView below will try to use SharedTexture as presentation option.
            // This way the rendered 3D scene will be shared with WinUI composition engine so that
            // the rendered image will stay on the graphics card.
            // This allows composition of 3D scene with other WinUI objects.
            //
            // WriteableBitmap:
            // If this mode is not possible, then WriteableBitmap presentation type is used.
            // In this mode, the rendered texture is copied to main memory into a WinUI's WriteableBitmap.
            // This is much slower because of additional memory traffic.
            //
            // OverlayTexture:
            // Not supported
            //
            //
            // To see how to create SharpEngineSceneView in XAML, see the SharpEngineSceneViewInXaml sample.

            _sharpEngineSceneView = new SharpEngineSceneView(PresentationTypes.SharedTexture); // SharedTexture is also the default presentation type so we could also create the SharpEngineSceneView without that parameter

            // To enable Vulkan's standard validation, set EnableStandardValidation and install Vulkan SDK (this may slightly reduce performance)
            //_sharpEngineSceneView.CreateOptions.EnableStandardValidation = true;

            // Logging was already enabled in SamplesWindow constructor
            //Utilities.Log.LogLevel = LogLevels.Warn;
            //Utilities.Log.IsLoggingToDebugOutput = true;

            // In case when VulkanDevice cannot be created, show an error message
            // If this is not handled by the user, then SharpEngineSceneView will show its own error message
            _sharpEngineSceneView.GpuDeviceCreationFailed += delegate(object sender, DeviceCreateFailedEventArgs args)
            {
                ShowDeviceCreateFailedError(args.Exception); // Show error message
                args.IsHandled = true; // Prevent showing error by SharpEngineSceneView
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



            // Use the following code to create SharpEngine from setting for this sample project and to show Diagnostics window:
            //
            //_sharpEngineSceneView = new SharpEngineSceneView(SamplesContext.Current.PreferredPresentationType)
            //{
            //    PreferedMultisampleCount = SamplesContext.Current.PreferredSuperSamplingCount,
            //    WaitForVSync             = SamplesContext.Current.WaitForVSync
            //};
            //
            //_sharpEngineSceneView.Initialize(SamplesContext.Current.PreferredEngineCreateOptions);
            //
            //// Because the SharpEngineSceneView is manually created in Loaded event, we need to register it to SamplesContext (so we can open the Diagnostics window)
            //SamplesContext.Current.RegisterCurrentSharpEngineSceneView(_sharpEngineSceneView);


            CreateTestScene();
            CreateLights();
            SetupPointerCameraController();

            // Add SharpEngineSceneView to the WPF controls tree
            SceneViewBorder.Child = _sharpEngineSceneView;

            this.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                _sharpEngineSceneView.Dispose();
            };
        }

        private void SetupPointerCameraController()
        {
            // Define the camera
            var camera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -25,
                Distance = 300,
                TargetPosition = new Vector3(0, 0, 0),
                ShowCameraLight = ShowCameraLightType.Auto // If there are no other light in the Scene, then add a camera light that illuminates the scene from the camera's position
            };

            _sharpEngineSceneView.SceneView.Camera = camera;


            // PointerCameraController use mouse to control the camera

            //_pointerCameraController = new PointerCameraController(_sharpEngineSceneView.SceneView, SceneViewBorder) // We could also create PointerCameraController by SceneView and custom EventSourceElement
            _pointerCameraController = new PointerCameraController(_sharpEngineSceneView)
            {
                RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                       // this is already the default value but is still set up here for clarity
                MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,               // this is already the default value but is still set up here for clarity
                QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default

                RotateAroundPointerPosition = false,
                ZoomMode = CameraZoomMode.ViewCenter,
            };
        }

        private void CreateLights()
        {
            var scene = _sharpEngineSceneView.Scene;

            scene.Lights.Clear();

            // Add lights
            scene.Lights.Add(new AmbientLight(new Color3(0.3f, 0.3f, 0.3f)));

            var directionalLight = new DirectionalLight(new Vector3(-1, -0.3f, 0));
            scene.Lights.Add(directionalLight);

            scene.Lights.Add(new PointLight(new Vector3(500, 200, 100), range: 10000));
        }

        private void CreateTestScene()
        {
            // The 3D objects in SharpEngine are defined in a hierarchical collection of SceneNode objects
            // that are added to the Scene.RootNode object.
            // The SceneNode object are defined in the Ab4d.SharpEngine.SceneNodes namespace.

            var boxModel = new BoxModelNode(centerPosition: new Vector3(0, 0, 0), 
                                            size: new Vector3(80, 40, 60), 
                                            name: "Gold BoxModel")
            {
                Material = StandardMaterials.Gold,
                //Material = new StandardMaterial(Colors.Gold),
                //Material = new StandardMaterial(diffuseColor: new Color3(1f, 0.84313726f, 0f))
            };

            _sharpEngineSceneView.Scene.RootNode.Add(boxModel);
        }

        private void ShowDeviceCreateFailedError(Exception ex)
        {
            var errorTextBlock = new TextBlock()
            {
                Text = "Error creating VulkanDevice:\r\n" + ex.Message,
                Foreground = new SolidColorBrush(Colors.Red),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            RootGrid.Children.Add(errorTextBlock);
        }

    }
}
