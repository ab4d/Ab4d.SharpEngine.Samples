using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.Samples.Android.Generic;
using Ab4d.SharpEngine.Samples.TestScenes;
using Ab4d.SharpEngine.Samples.Utilities;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Gestures;
using Android.OS;
using Android.Views;
using Java.Security;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl.Android;


// This Android sample is using a third-party Silk.Net libraries
// to create SDL or GLFW based Activity and View that is then used to show the 3D scene by SharpEngine.
//
// Currently SDL is used but the rotation of screen is prevented,
// because of a bug in Silk.NET.Sdl that crashes the app when the screen is rotated.
//
// Using GLFW does not crash the application after rotating the screen, but after some time
// surface is not updated anymore (though the rendering is done).
// This will need to be investigated further...
// If you find a solution for this, please report that to support at ab4d.com
//
// It is recommended to use the approach that is demonstrated with Android.Application sample.
// Also there it is possible to show 3D scene only on part of the Activity,
// but here the whole Activity is used for the 3D scene.
//
// An advantage of using SDL or GLFW is that this can be also used on Windows and Linux,
// so you can have almost the same code that runs on all 3 systems
// (Android requires only different startup code - SilkActivity).


namespace AndroidDemo
{
    [Activity(Label = "@string/app_name", 
              MainLauncher = true,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)]
    public class MainActivity : SilkActivity
    {
        // Instead of IWindow, we use IView. 
        // IWindow inherits IView, so you can also use this with your desktop code.
        private IView? _view;

        private VulkanDevice? _vulkanDevice;
        private Scene? _scene;
        private SceneView? _sceneView;

        private Ab4d.Vulkan.SurfaceKHR _vulkanSurface;

        private TargetPositionCamera? _targetPositionCamera;

        private string[]? _allManifestResourceNames;

        private IInputContext? _inputContext;
        private ScaleGestureDetector? _scaleGestureDetector;
        private ManualMouseCameraController? _mouseCameraController;
        private MyScaleListener? _myScaleListener;
        private AndroidBitmapIO? _androidBitmapIO;


        protected override void OnCreate(Bundle? savedInstanceState)
        {
            // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
            // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                                  licenseType: "SamplesLicense",
                                                  platforms: "All",
                                                  license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");


            // Check if the BLUETOOTH_CONNECT permission is granted (this is required when using SilkActivity in Android 12+)
            if (CheckSelfPermission(Android.Manifest.Permission.BluetoothConnect) != Android.Content.PM.Permission.Granted)
            {
                RequestPermissions(new string[] { Android.Manifest.Permission.BluetoothConnect }, 0);
            }

            // See: https://developer.android.com/training/gestures/scale#java
            _myScaleListener = new MyScaleListener();
            _scaleGestureDetector = new ScaleGestureDetector(this.ApplicationContext, _myScaleListener);

            base.OnCreate(savedInstanceState);
        }

        /// <summary>
        /// This is where the application starts.
        /// Note that when using net6-android, you do not need to have a main method.
        /// </summary>
        protected override void OnRun()
        {
            Log.Info?.Write("STARTING: OnRun");


            // We need to use GLFW instead of SDL
            // because currently there is a bug in Silk.NET.Sdl that crashes the app when the screen is rotated:
            // https://github.com/dotnet/Silk.NET/issues/922
            // If you want to use SDK, then call this in OnCreate method above:
            RequestedOrientation = ScreenOrientation.Locked; // Prevent rotating the screen
            
            Silk.NET.Windowing.Window.PrioritizeSdl();
            //Silk.NET.Windowing.Window.PrioritizeGlfw();


            var options = ViewOptions.DefaultVulkan;
            options.API = new GraphicsAPI(ContextAPI.Vulkan, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(30, 0));
            _view = Silk.NET.Windowing.Window.GetView(options);


            _view.Load += OnLoad;
            _view.Render += OnRender;
            _view.Resize += OnResize;
            _view.Closing += OnClose;
            
            _view.Run();
        }

        private void OnLoad()
        {
            Log.Info?.Write("View loaded");

            // We need to overwrite the "Silk.NET Window" that is used when the view is opened
            // There does not seem to be a way to set the name of this view
            RunOnUiThread(() => {
                //this.Title = Resources.GetString(Android.Resource.String.app_name); // app_name not defined !?!?
                this.Title = "Ab4d.SharpEngine Demo";
            });


            if (_view != null)
                _inputContext = _view.CreateInput();

            InitVulkanWithSharpEngine();
            InitializeSharpEngineScene();

            CreateAllObjectsTestScene();
        }

        private void OnRender(double obj)
        {
            if (_sceneView != null)
                _sceneView.Render(forceRender: false);
        }

        private void OnResize(Silk.NET.Maths.Vector2D<int> newSize)
        {
            Log.Info?.Write("Resize to " + newSize);

            if (_sceneView != null)
                _sceneView.Resize();
        }

        private void OnClose()
        {

        }

        private unsafe void InitVulkanWithSharpEngine()
        {
            // Setup logger
            SetupSharpEngineLogger(enableFullLogging: false); // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company


            // Prepare Surface provider that will be called when there a Vulkan Instance is created.
            // It create a Vulkan Surface object that is used to show the rendered image.
            var vulkanSurface = new CustomVulkanSurfaceProvider(instance =>
            {
                if (_view == null || _view.VkSurface == null)
                    return SurfaceKHR.Null;

                // Convert Ab4d.Vulkan.Instance's handle to Silk.Net's VkHandle
                var vkHandle = new VkHandle(instance.Handle);

                // Call Create on VkSurface
                var vkNonDispatchableHandle = _view.VkSurface.Create(vkHandle, (byte*)null);

                // Create vulkan surface that can be used by SharpEngine
                _vulkanSurface = new Ab4d.Vulkan.SurfaceKHR(vkNonDispatchableHandle.Handle);

                return _vulkanSurface;
            },
            addDefaultSurfaceExtensions: true);


            // To enable Vulkan validation layers first download the *.so libraries from:
            // https://github.com/KhronosGroup/Vulkan-ValidationLayers/releases
            // 
            // Then copy the *.so files to the subfolders in the Resources/lib folder.
            // Check that the the Build Action is set to "AndroidNativeLibrary"
            //
            // Then also set the following to true:
            bool enableStandardValidation = false;

            var engineCreateOptions = new EngineCreateOptions(applicationName: "SharpEngineAndroidDemo", enableStandardValidation: enableStandardValidation);

            _vulkanDevice = VulkanDevice.Create(vulkanSurface, engineCreateOptions);

            Log.Info?.Write("Vulkan device created!!!");
        }

        // Setup SharpEngine logging
        // In case of problems and then please send the log text with the description of the problem
        private void SetupSharpEngineLogger(bool enableFullLogging)
        {
            // The alpha and beta version are compiled with release build options but support full logging.
            // This means that it is possible to get Trace level log messages
            // (production version will have only Warning and Error logging compiled into the assembly).

            // When you have some problems, then please enable Trace level logging and writing log messages to a file or debug output.
            // To do this please find the existing code that sets up logging an change it to:

            if (enableFullLogging)
            {
                Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Trace;
                Ab4d.SharpEngine.Utilities.Log.WriteSimplifiedLogMessage = false; // write full log messages timestamp, thread id and other details

                // Use one of the following:

                // Write log messages to output window (for example Visual Studio Debug window):
                Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;

                //// Write log to file:
                //Ab4d.SharpEngine.Utilities.Log.LogFileName = @"c:\SharpEngine.log";

                //// Write to local StringBuilder:
                //// First create a new StringBuilder field:
                //private System.Text.StringBuilder _logStringBuilder;
                //// Then call AddLogListener:
                //Ab4d.SharpEngine.Utilities.Log.AddLogListener((logLevel, message) => _logStringBuilder.AppendLine(message));
            }
            else
            {
                // Setup minimal logging (write warnings and error to output window)
                Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Warn;        // Log Warnings and Errors
                Ab4d.SharpEngine.Utilities.Log.WriteSimplifiedLogMessage = true; // write log messages without timestamp, thread id and other details
                Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;    // write log messages to output window
            }
        }

        private void InitializeSharpEngineScene()
        {
            if (_vulkanDevice == null)
                return;

            // After we have VulkanDevice, we can create Scene.
            // The Scene will contain the 3D objects (as SceneNodes) and Lights
            _scene = new Scene(_vulkanDevice, "MainScene");

            // Add lights
            _scene.Lights.Add(new AmbientLight(intensity: 0.3f));
            _scene.Lights.Add(new PointLight(new Vector3(500, 200, 0), range: 10000));
            //_scene.Lights.Add(new DirectionalLight(new Vector3(-1, -0.3f, 0)));
            //_scene.Lights.Add(new SpotLight(new Vector3(300, 0, 300), new Vector3(-1, -0.3f, 0)) { Color = new Color3(0.4f, 0.4f, 0.4f) });


            // Create SceneView that will show the scene
            _sceneView = new SceneView(_scene, "MainSceneView");

            _sceneView.WaitForVSync = true;                               // This is recommended for mobile. This will use FIFO present mode. Read more about the recommendations here: https://developer.samsung.com/sdp/blog/en-us/2019/07/26/vulkan-mobile-best-practice-how-to-configure-your-vulkan-swapchain
            _sceneView.BackgroundColor = Color4.White; //new Color4(0.9f, 0.3f, 0.3f, 1);


            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -25,
                Distance = 1500,
                ViewWidth = 1500,
                TargetPosition = new Vector3(0, 0, 0)
            };

            _sceneView.Camera = _targetPositionCamera;

            _sceneView.Initialize(_vulkanSurface);


            // Currently RotateCameraConditions and MoveCameraConditions are set by using mouse events - this will be improved in the future and touch events will be used
            _mouseCameraController = new ManualMouseCameraController(_sceneView)
            {
                RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed,
                MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed,
                ZoomMode = CameraZoomMode.ViewCenter,
                //RotateAroundMousePosition = true
            };

            if (_myScaleListener != null)
                _myScaleListener.SetMouseCameraController(_mouseCameraController);
        }

        private void CreateAllObjectsTestScene()
        {
            if (_scene == null || _sceneView == null || this.Resources == null)
                return;

            EnsureAndroidBitmapIO();

            var allObjectsTestScene = new AllObjectsTestScene(_scene, _sceneView, _androidBitmapIO, this.Resources);
            allObjectsTestScene.Drawable1Id = Resource.Drawable.uvchecker;
            allObjectsTestScene.Drawable2Id = Resource.Drawable.TreeTexture;

            // Add demo objects to _scene
            allObjectsTestScene.CreateTestScene();
        }

        [MemberNotNull(nameof(_androidBitmapIO))]
        private void EnsureAndroidBitmapIO()
        {
            if (_androidBitmapIO != null)
                return;

            _androidBitmapIO = new AndroidBitmapIO();

            // Setup simple resolver that returns file stream based on the fileName for files that
            // have build action set to Embedded resources.
            // This is not used in this sample, because images here are compiled with AndroidResource and added to drawable folder.
            _allManifestResourceNames = this.GetType().Assembly.GetManifestResourceNames();
            _androidBitmapIO.FileStreamResolver = delegate (string fileName)
            {
                if (_allManifestResourceNames != null)
                {
                    string onlyFileName = System.IO.Path.GetFileName(fileName);

                    for (int i = 0; i < _allManifestResourceNames.Length; i++)
                    {
                        if (_allManifestResourceNames[i].EndsWith(onlyFileName))
                            return this.GetType().Assembly.GetManifestResourceStream(_allManifestResourceNames[i]);
                    }
                }

                return null; // Not found
            };
        }

        public override bool DispatchTouchEvent(MotionEvent? e)
        {
            if (_vulkanDevice == null)
                return false;

            if (_vulkanDevice.IsOnMainThread())
            {
                ProcessTouchEvent(e);
            }
            else
            {
                this.RunOnUiThread(() =>
                {
                    ProcessTouchEvent(e);
                });
            }

            bool isScaleEvent;
            if (_scaleGestureDetector != null && e != null)
                isScaleEvent = _scaleGestureDetector.OnTouchEvent(e);
            else
                isScaleEvent = false;

            return base.DispatchTouchEvent(e) || isScaleEvent;
        }

        // This must be called on the same thread as the SharpEngineSceneView was created
        private void ProcessTouchEvent(MotionEvent? e)
        {
            if (e == null || _scaleGestureDetector == null) 
                return;

            float xPos = e.GetX();
            float yPos = e.GetY();

            Log.Trace?.Write($"TouchEvent: Action: {e.Action}; pos: {xPos} {yPos}; PointerCount: {e.PointerCount}");

            if (_mouseCameraController != null)
            {
                MouseButtons simulatedMouseButtons;

                if (e.PointerCount == 1)
                    simulatedMouseButtons = MouseButtons.Left;
                else if (e.PointerCount == 2)
                    simulatedMouseButtons = MouseButtons.Left | MouseButtons.Right;
                else
                    simulatedMouseButtons = MouseButtons.None;

                if (e.Action == MotionEventActions.Down ||
                    e.Action == MotionEventActions.Pointer1Down ||
                    e.Action == MotionEventActions.Pointer2Down)
                {
                    // Start rotate
                    _mouseCameraController.ProcessMouseDown(new Vector2(xPos, yPos), simulatedMouseButtons, KeyboardModifiers.None);
                }
                else if (e.Action == MotionEventActions.Up ||
                         e.Action == MotionEventActions.Pointer1Up ||
                         e.Action == MotionEventActions.Pointer2Up)
                {
                    // End rotate
                    // e.PointerCount value shows the value before the finger was lifted so we need to decrease the value by one
                    // This means we have only 2 cases:
                    if (e.PointerCount == 2)
                    {
                        if (e.Action == MotionEventActions.Pointer1Up)
                            simulatedMouseButtons = MouseButtons.Right; // 1st is up so only right remains
                        else if (e.Action == MotionEventActions.Pointer1Up)
                            simulatedMouseButtons = MouseButtons.Left; // 2nd is up so only left remains
                        else
                            simulatedMouseButtons = MouseButtons.None;
                    }
                    else
                    {
                        simulatedMouseButtons = MouseButtons.None;
                    }

                    _mouseCameraController.ProcessMouseUp(simulatedMouseButtons, KeyboardModifiers.None);
                }
                else if (e.Action == MotionEventActions.Move)
                {
                    // Rotate / move

                    _mouseCameraController.ProcessMouseMove(new Vector2(xPos, yPos), simulatedMouseButtons, KeyboardModifiers.None);
                }
            }
        }
    }
}