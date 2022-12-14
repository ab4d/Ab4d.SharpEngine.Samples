using System.Diagnostics.CodeAnalysis;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using System.Numerics;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Android.Application;
using Ab4d.SharpEngine.Samples.TestScenes;
using Ab4d.SharpEngine.Samples.Utilities;
using Ab4d.SharpEngine.SceneNodes;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Java.Lang;

// This Android sample is using a .Net 6 Android Application template.
// It uses only standard Android objects like Activity and SurfaceView to show the 3D scene by SharpEngine.

// There is another Android sample that uses third-party Silk.NET library and can use SDL or GLFW to create a Vulkan Surface.
// But that sample currently has some problems (with screen rotation) so it is recommended to use the approach demonstrated here.

namespace AndroidApp1
{
    [Activity(Label = "@string/app_name", 
              MainLauncher = true, 
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)] // handle those changes without recreating the activity
    public class MainActivity : Activity
    {
        private VulkanDevice? _vulkanDevice;
        private Scene? _scene;
        private SceneView? _sceneView;
        private TargetPositionCamera? _targetPositionCamera;

        private AndroidCameraController? _cameraController;

        private bool _isConfigurationChanged;
        private DateTime _configurationChangedTime;
        private readonly TimeSpan _resizeAfterConfigurationChangedTimeout = TimeSpan.FromSeconds(2);

        private Random? _rnd;
        private BoxModelNode? _boxModel;

        private AllObjectsTestScene? _allObjectsTestScene;
        private string[]? _allManifestResourceNames;
        private SkiaSharpBitmapIO? _skiaSharpBitmapIO;


        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var changeColorButton = FindViewById<Button>(Resource.Id.button_change_color);
            if (changeColorButton != null)
            {
                changeColorButton.Click += delegate (object? sender, EventArgs args)
                {
                    ChangeBoxColor();
                };
            }
            
            var changeSceneButton = FindViewById<Button>(Resource.Id.button_change_scene);
            if (changeSceneButton != null)
            {
                changeSceneButton.Click += delegate (object? sender, EventArgs args)
                {
                    ChangeTestScene();
                };
            }
            

            // Create VulkanView and add it to layout
            var layout = FindViewById<LinearLayout>(Resource.Id.Layout);

            if (layout != null && ApplicationContext != null)
            {
                var vulkanView = new SharpEngineSceneView(ApplicationContext);
                vulkanView.SurfaceCreatedAction = OnSurfaceCreated;
                vulkanView.RenderSceneAction = RenderScene;

                layout.AddView(vulkanView);
            }
        }

        private void OnSurfaceCreated(IntPtr windowPtr)
        {
            // Setup logger
            SetupSharpEngineLogger(enableFullLogging: false); // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company

            // EngineCreateOptions can be used to set many engine options.

            // To enable Vulkan validation layers first download the *.so libraries from:
            // https://github.com/KhronosGroup/Vulkan-ValidationLayers/releases
            // 
            // Then copy the *.so files to the subfolders in the Resources/lib folder.
            // Check that the the Build Action is set to "AndroidNativeLibrary"
            //
            // Then also set the following to true:
            bool enableStandardValidation = false;
            
            var engineCreateOptions = new EngineCreateOptions(applicationName: "SharpEngineAndroidDemo", enableStandardValidation: enableStandardValidation);

            // Create VulkanSurface provider that will create the surface pointer when the VulkanInstance will be available
            var vulkanSurface = new AndroidVulkanSurfaceProvider(windowPtr);

            // Create a VulkanDevice (and VulkanInstance if it was not created yet)
            // In case when we have _vulkanSurface it is recommended to provide it when creating the VulkanDevice.
            // This way the correct settings for the device can be used (for example SwapChainImagesCount).
            // It is also possible not to specify surface when creating VulkanDevice and do that only when creating SceneView.
            _vulkanDevice = VulkanDevice.Create(engineCreateOptions);


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

            _sceneView.WaitForVSync = true;                // This is recommended for mobile. This will use FIFO present mode. Read more about the recommendations here: https://developer.samsung.com/sdp/blog/en-us/2019/07/26/vulkan-mobile-best-practice-how-to-configure-your-vulkan-swapchain
            _sceneView.BackgroundColor = Colors.LightGray;

            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -25,
                Distance = 500,
                ViewWidth = 500,
                TargetPosition = new Vector3(0, 0, 0)
            };

            _sceneView.Camera = _targetPositionCamera;

            _sceneView.ViewResized += OnSceneViewResized; // This is needed to handle orientation change - see comments for OnConfigurationChanged method below

            _sceneView.Initialize(vulkanSurface);


            if (this.ApplicationContext != null)
            {
                // AndroidCameraController is implemented in this sample
                _cameraController = new AndroidCameraController(this.ApplicationContext, _sceneView)
                {
                    ZoomMode = CameraZoomMode.ViewCenter,
                    //RotateAroundMousePosition = true
                };
            }


            CreateSimpleScene();

            // Render the scene
            RenderScene();
        }

        private void CreateSimpleScene()
        {
            if (_scene == null)
                return;

            _scene.RootNode.Clear();

            _boxModel = new BoxModelNode(centerPosition: new Vector3(0, 0, 0), size: new Vector3(100, 80, 60), name: "BoxModel")
            {
                Material = StandardMaterials.Green
            };

            _scene.RootNode.Add(_boxModel);
        }

        private void RenderScene()
        {
            if (_sceneView != null)
            {
                if (_allObjectsTestScene != null)
                    _allObjectsTestScene.AnimateModels(); // update animation


                // Usually the scene is rendered only when there are any changes in the scene.
                // But after the configuration was changed (screen rotated or size changed),
                // then we need to force rendering the scene until the surface is resized.
                // See comments for OnConfigurationChanged method below.

                // Just in case have a timeout in which the resize need to be done
                if (_isConfigurationChanged)
                {
                    if ((DateTime.Now - _configurationChangedTime) > _resizeAfterConfigurationChangedTimeout)
                        _isConfigurationChanged = false; // prevent forcing to render each frame
                }

                _sceneView.Render(forceRender: _isConfigurationChanged);
            }
        }

        // Rotation change (in Android v10+) is handled by forcing rendering until VK_SUBOPTIMAL_KHR is returned from Present call
        // See: https://developer.android.com/games/optimize/vulkan-prerotation
        //
        // So when the screen is rotated, we the OnConfigurationChanged is called (because we setup ConfigurationChanges enum in the Activity attribute).
        // There we set _isConfigurationChanged to true.
        // When _isConfigurationChanged is true, each call to Render method will force rendering the scene.
        // When the Present method will return VK_SUBOPTIMAL_KHR, then the SharpEngine will automatically
        // call Resize and in then the ViewResized event will be triggered and in the handler (OnSceneViewResized)
        // the _isConfigurationChanged will be set to false to prevent forcing rendering.

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            _configurationChangedTime = DateTime.Now; // Save time so we can use timeout in which the change should be handled
            _isConfigurationChanged = true;

            base.OnConfigurationChanged(newConfig);
        }
        
        private void OnSceneViewResized(object? sender, EventArgs e)
        {
            _isConfigurationChanged = false;
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

        private void ChangeBoxColor()
        {
            _rnd ??= new Random();

            var randomColor = new Color3((float)_rnd.NextDouble(), (float)_rnd.NextDouble(), (float)_rnd.NextDouble());

            if (_allObjectsTestScene != null)
                _allObjectsTestScene.ChangeBasePlaneColor(randomColor);
            else if (_boxModel != null)
                _boxModel.Material = new StandardMaterial(randomColor);
        }

        private void ChangeTestScene()
        {
            if (_scene == null)
                return;
            
            if (_allObjectsTestScene == null)
            {
                _scene.RootNode.Clear();

                EnsureSkiaSharpBitmapIO();

                _allObjectsTestScene = new AllObjectsTestScene(_scene, _skiaSharpBitmapIO);

                // Add demo objects to _scene
                _allObjectsTestScene.CreateTestScene();

                if (_targetPositionCamera != null && _targetPositionCamera.Distance < 1500)
                    _targetPositionCamera.Distance = 1500;
            }
            else
            {
                _allObjectsTestScene.Dispose();
                _allObjectsTestScene = null;

                CreateSimpleScene();
            }
        }

        [MemberNotNull(nameof(_skiaSharpBitmapIO))]
        private void EnsureSkiaSharpBitmapIO()
        {
            if (_skiaSharpBitmapIO != null)
                return;

            _skiaSharpBitmapIO = new SkiaSharpBitmapIO();

            _allManifestResourceNames = this.GetType().Assembly.GetManifestResourceNames();

            // Setup simple resolver that returns file stream from Android Resources based on the fileName
            // For this to work the build action for image files need to be set to Embedded resources
            _skiaSharpBitmapIO.FileStreamResolver = delegate (string fileName)
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
            bool isHandled;

            if (_cameraController != null)
                isHandled = _cameraController.ProcessTouchEvent(e);
            else
                isHandled = false;

            return base.DispatchTouchEvent(e) || isHandled;
        }
    }
}