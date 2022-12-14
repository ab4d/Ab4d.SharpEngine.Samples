using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Wpf.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Wpf;

namespace Ab4d.SharpEngine.Samples.Wpf.QuickStart
{
    /// <summary>
    /// Interaction logic for SharpEngineSceneViewInCode.xaml
    /// </summary>
    public partial class SharpEngineSceneViewInCode : Page
    {
        private SharpEngineSceneView _sharpEngineSceneView;
        private Scene _scene;
        private SceneView _sceneView;
        private MouseCameraController? _mouseCameraController;

        public SharpEngineSceneViewInCode()
        {
            InitializeComponent();

            // Setup logger
            // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company
            LogHelper.SetupSharpEngineLogger(enableFullLogging: false);


            // Create SharpEngineSceneView:
            //
            // SharpEngineSceneView is a WPF control that can show SharpEngine's SceneView in a WPF application.
            // The SharpEngineSceneView creates the VulkanDevice, Scene and SceneView objects.
            //
            // Scene is used to define the 3D objects (added to Scene.RootNode) and lights (added to Scene.Lights collection).
            // SceneView is a view of the Scene and can render the objects in the Scene. It provides a Camera and size of the view.
            //
            // SharpEngineSceneView for WPF supports the following presentation types:
            // SharedTexture:
            // The SharpEngineSceneView below will try to use SharedTexture as presentation option.
            // This way the rendered 3D scene will be shared with WPF composition engine so that
            // the rendered image will stay on the graphics card.
            // This allows composition of 3D scene with other WPF objects.
            //
            // WriteableBitmap:
            // If this mode is not possible, then WriteableBitmap presentation type is used.
            // In this mode, the rendered texture is copied to main memory into a WPF's WriteableBitmap.
            // This is much slower because of additional memory traffic.
            //
            // OverlayTexture:
            // This mode is the fastest because the engine owns part of the screen and can show the rendered scene
            // independent of the main UI thread (no need to wait for the rendering to be completed).
            // A disadvantage of this mode is that the 3D scene cannot be composed with other WPF objects
            // (WPF objects cannot be rendered on top of 3D scene).
            //
            //
            // To see how to create SharpEngineSceneView in XAML, see the SharpEngineSceneViewInXaml sample.

            _sharpEngineSceneView = new SharpEngineSceneView(PresentationTypes.SharedTexture);

            // Before creating the sharp engine, we define the EngineCreateOptions.
            // This provides many options, but here we only set the application name
            // and enable standard validation that provides additional error information when Vulkan SDK is installed on the system.
            var engineCreateOptions = new EngineCreateOptions(applicationName: "SharpEngine WPF samples", enableStandardValidation: true);

            // Call Initialize method that creates the Vulkan device, Scene and SceneView
            (_scene, _sceneView) = _sharpEngineSceneView.Initialize(engineCreateOptions);


            // Use the following code to create SharpEngine from setting for this sample project and to show Diagnostics window:
            //
            //_sharpEngineSceneView = new SharpEngineSceneView(SamplesContext.Current.PreferredPresentationType)
            //{
            //    PreferedMultisampleCount = SamplesContext.Current.PreferredSuperSamplingCount,
            //    WaitForVSync             = SamplesContext.Current.WaitForVSync
            //};
            //
            //(_scene, _sceneView) = _sharpEngineSceneView.Initialize(SamplesContext.Current.PreferredEngineCreateOptions);
            //
            //// Because the SharpEngineSceneView is manually created in Loaded event, we need to register it to SamplesContext (so we can open the Diagnostics window)
            //SamplesContext.Current.RegisterCurrentSharpEngineSceneView(_sharpEngineSceneView);


            CreateTestScene();
            CreateLights();
            SetupMouseCameraController();

            // Add SharpEngineSceneView to the WPF controls tree
            SceneViewBorder.Child = _sharpEngineSceneView;

            this.Unloaded += delegate (object sender, RoutedEventArgs args)
            {
                _sharpEngineSceneView.Dispose();
            };
        }

        private void SetupMouseCameraController()
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

            _sceneView.Camera = camera;


            // MouseCameraController use mouse to control the camera

            //_mouseCameraController = new MouseCameraController(_sharpEngineSceneView.SceneView, SceneViewBorder) // We could also create MouseCameraController by SceneView and custom EventSourceElement
            _mouseCameraController = new MouseCameraController(_sharpEngineSceneView)
            {
                RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed,                                                   // this is already the default value but is still set up here for clarity
                MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.ControlKey,             // this is already the default value but is still set up here for clarity
                QuickZoomConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed, // quick zoom is disabled by default

                RotateAroundMousePosition = false,
                ZoomMode = CameraZoomMode.ViewCenter,
            };
        }

        private void CreateLights()
        {
            _scene.Lights.Clear();

            // Add lights
            _scene.Lights.Add(new AmbientLight(new Color3(0.3f, 0.3f, 0.3f)));

            var directionalLight = new DirectionalLight(new Vector3(-1, -0.3f, 0));
            _scene.Lights.Add(directionalLight);

            _scene.Lights.Add(new PointLight(new Vector3(500, 200, 100), range: 10000));
        }

        private void CreateTestScene()
        {
            // The 3D objects in SharpEngine are defined in a hierarchical collection of SceneNode objects
            // that are added to the Scene.RootNode object.
            // The SceneNode object are defined in the Ab4d.SharpEngine.SceneNodes namespace.

            var boxModel = new BoxModelNode(centerPosition: new Vector3(0, 0, 0), 
                                            size: new Vector3(80, 40, 60), 
                                            name: "Silver BoxModel")
            {
                Material = StandardMaterials.Gold,
                //Material = new StandardMaterial(Colors.Gold),
                //Material = new StandardMaterial(diffuseColor: new Color3(1f, 0.84313726f, 0f))
            };

            _scene.RootNode.Add(boxModel);
        }
    }
}
