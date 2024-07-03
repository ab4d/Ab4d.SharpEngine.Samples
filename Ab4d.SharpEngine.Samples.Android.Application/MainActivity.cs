using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using System.Numerics;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Android.Application;
using Ab4d.SharpEngine.Samples.TestScenes;
using Ab4d.SharpEngine.Samples.Utilities;
using Ab4d.SharpEngine.SceneNodes;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Activity = Android.App.Activity;

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
        private static readonly bool StartWithFullLogging = false;
        private static readonly bool ShowStartLoggingButton = true;


        private VulkanDevice? _vulkanDevice;
        private Scene? _scene;
        private SceneView? _sceneView;
        private TargetPositionCamera? _targetPositionCamera;

        private AndroidCameraController? _cameraController;

        private bool _isForceResizeWithRenderCalled;

        private Random? _rnd;
        private BoxModelNode? _boxModel;

        private AllObjectsTestScene? _allObjectsTestScene;
        private string[]? _allManifestResourceNames;
        private AndroidBitmapIO? _androidBitmapIO;

        private SharpEngineSceneView? _vulkanView;

        // Saved app state:
        private bool _isComplexScene = false;
        private Color3 _lastUsedColor = Color3.Black;


        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
            // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                                  licenseType: "SamplesLicense",
                                                  platforms: "All",
                                                  license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");


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

            var startLoggingButton = FindViewById<Button>(Resource.Id.button_start_logging);
            if (startLoggingButton != null)
            {
                if (ShowStartLoggingButton && !StartWithFullLogging)
                {
                    startLoggingButton.Click += delegate (object? sender, EventArgs args)
                    {
                        Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Trace;
                        Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;
                    };
                }
                else
                {
                    startLoggingButton.Visibility = ViewStates.Gone;
                }
            }


            // Create VulkanView and add it to layout
            var layout = FindViewById<LinearLayout>(Resource.Id.Layout);

            if (layout != null && ApplicationContext != null)
            {
                _vulkanView = new SharpEngineSceneView(ApplicationContext);
                _vulkanView.SurfaceCreatedAction = OnSurfaceCreated;
                _vulkanView.RenderSceneAction = RenderScene;
                _vulkanView.SurfaceSizeChangedAction = OnSurfaceSizeChanged;
                _vulkanView.SurfaceDestroyedAction = DisposeSceneView;

                layout.AddView(_vulkanView);
            }
        }

        // OnSurfaceCreated method is called when the app get new Android surface
        // This happens on app startup or when the app was resumed from background
        private void OnSurfaceCreated(IntPtr windowPtr)
        {
            // Create VulkanSurface provider that will create the surface pointer when the VulkanInstance will be available
            var vulkanSurface = new AndroidVulkanSurfaceProvider(windowPtr);

            // If app was resumed from background, then the Vulkan device and Scene were preserved
            if (_vulkanDevice == null)
            {
                // Setup logger
                SetupSharpEngineLogger(enableFullLogging: StartWithFullLogging); // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company

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

                // Create a VulkanDevice (and VulkanInstance if it was not created yet)
                // In case when we have _vulkanSurface it is recommended to provide it when creating the VulkanDevice.
                // This way the correct settings for the device can be used (for example SwapChainImagesCount).
                // It is also possible not to specify surface when creating VulkanDevice and do that only when creating SceneView.
                try
                {
                    _vulkanDevice = VulkanDevice.Create(vulkanSurface, engineCreateOptions);
                }
                catch (SharpEngineException ex)
                {
                    ShowErrorMessage("Error creating Vulkan device:\n" + ex.Message);
                    return;
                }
            }

            if (_scene == null)
            {
                // Create the Scene that will contain the 3D objects (as SceneNodes) and Lights
                // We do not initialize Scene with the created VulkanDevice - we postpone that until we initialize SceneView
                _scene = new Scene("MainScene");

                // Add lights
                _scene.Lights.Add(new AmbientLight(intensity: 0.3f));
                _scene.Lights.Add(new PointLight(new Vector3(500, 200, 0), range: 10000));
                //_scene.Lights.Add(new DirectionalLight(new Vector3(-1, -0.3f, 0)));
                //_scene.Lights.Add(new SpotLight(new Vector3(300, 0, 300), new Vector3(-1, -0.3f, 0)) { Color = new Color3(0.4f, 0.4f, 0.4f) });
            }

            // If app was resumed from background, then we need to recreate the SceneView because it was disposed in OnTrimMemory
            if (_sceneView == null)
            {
                // Create SceneView that will show the scene
                _sceneView = new SceneView(_scene, "MainSceneView");

                _sceneView.WaitForVSync = true;                // This is recommended for mobile. This will use FIFO present mode. Read more about the recommendations here: https://developer.samsung.com/sdp/blog/en-us/2019/07/26/vulkan-mobile-best-practice-how-to-configure-your-vulkan-swapchain
                _sceneView.BackgroundColor = Colors.LightGray;

                if (_vulkanView != null)
                    _vulkanView.SceneView = _sceneView;
            }



            // Initialize GPU resources after we have a valid surface
            _sceneView.Initialize(_vulkanDevice, vulkanSurface);


            // Setup camera
            if (_targetPositionCamera == null)
            {
                _targetPositionCamera = new TargetPositionCamera()
                {
                    Heading = -40,
                    Attitude = -25,
                    Distance = 500,
                    ViewWidth = 500,
                    TargetPosition = new Vector3(0, 0, 0)
                };
            }

            _sceneView.Camera = _targetPositionCamera;


            if (this.ApplicationContext != null)
            {
                // AndroidCameraController is implemented in this sample
                _cameraController = new AndroidCameraController(this.ApplicationContext, this, _sceneView)
                {
                    ZoomMode = CameraZoomMode.ViewCenter,
                    //RotateAroundMousePosition = true
                };
            }

            // Recreate the scene if it was not preserved
            if (_scene.RootNode.Count == 0)
            {
                if (_isComplexScene) // _isComplexScene is set in SaveAppState method
                    CreateComplexScene();
                else
                    CreateSimpleScene();

                if (_lastUsedColor != Color3.Black)
                    SetCustomColor(_lastUsedColor);
            }

            // Render the scene
            RenderScene();
        }

        public override void OnTrimMemory(TrimMemory level)
        {
            if (level == TrimMemory.UiHidden)
            {
                // When app is put to background, we can dispose the SceneView resources (Swapchain, images and render passes)
                // We will recreated them when the app is resumed
                DisposeSceneView();
            }
            else
            {
                // In all other cases when the OnTrimMemory is called, we dispose all SharpEngine resources (in real app you many preserve more resources based on level).
                // Here we should save the state of the 3D scene so it could be recreated.
                SaveAppState();

                // We can simulate that by first putting the app in background and then execute:
                // adb shell am send-trim-memory com.ab4d.SharpEngineApp1 MODERATE
                DisposeSharpEngine();
            }

            base.OnTrimMemory(level);
        }

        protected override void OnDestroy()
        {
            // Dispose all SharpEngine resources
            DisposeSharpEngine();

            base.OnDestroy();
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

        private void CreateComplexScene()
        {
            if (_scene == null || _sceneView == null || this.Resources == null)
                return;

            _scene.RootNode.Clear();

            EnsureAndroidBitmapIO();

            _allObjectsTestScene = new AllObjectsTestScene(_scene, _sceneView, _androidBitmapIO, this.Resources);
            _allObjectsTestScene.Drawable1Id = Resource.Drawable.uvchecker;
            _allObjectsTestScene.Drawable2Id = Resource.Drawable.TreeTexture;


            // Add demo objects to _scene
            _allObjectsTestScene.CreateTestScene();

            if (_targetPositionCamera != null && _targetPositionCamera.Distance < 1500)
                _targetPositionCamera.Distance = 1500;
        }

        private void SaveAppState()
        {
            // This method is called when all the SharpEngine resources are disposed.
            // Here we should save the state of the app so it can be recreated when the app is resumed.
            // In this sample we preserve the _targetPositionCamera so the view to the scene is preserved.
            // We also also save the type of the scene: simple or complex.
            // If we changed the color, then it will be also resumed (saved into _lastUsedColor)
            _isComplexScene = _allObjectsTestScene != null;
        }

        // Rotation change (in Android v10+) is handled by forcing rendering until VK_SUBOPTIMAL_KHR is returned from Present call
        // See: https://developer.android.com/games/optimize/vulkan-prerotation
        //
        // Also changes in size (for example when two apps are opened size by side and user resizes the area) may require to 
        // call Present method. Until then the Surface may still report the previous size and orientation.
        public override void OnConfigurationChanged(Configuration newConfig)
        {
            ForceResizeWithRender();
            base.OnConfigurationChanged(newConfig);
        }

        // The following method is called from SharpEngineSceneView.Draw when used Canvas size is different than the size of SceneView
        private void OnSurfaceSizeChanged()
        {
            ForceResizeWithRender();
        }

        private void RenderScene()
        {
            if (_sceneView != null)
            {
                if (_allObjectsTestScene != null)
                    _allObjectsTestScene.AnimateModels(); // update animation

                _sceneView.Render();
                _isForceResizeWithRenderCalled = false;
            }
        }

        private void ForceResizeWithRender()
        {
            // Prevent calling Resize and Render from both events: OnConfigurationChanged and OnSurfaceSizeChanged
            if (_isForceResizeWithRenderCalled)
                return;

            if (_sceneView != null)
            {
                _sceneView.Resize(renderNextFrameAfterResize: false);
                _sceneView.Render(forceRender: true);

                _isForceResizeWithRenderCalled = true;
            }
        }

        private void ChangeBoxColor()
        {
            _rnd ??= new Random();

            var randomColor = new Color3((float)_rnd.NextDouble(), (float)_rnd.NextDouble(), (float)_rnd.NextDouble());

            SetCustomColor(randomColor);
        }

        private void SetCustomColor(Color3 color)
        {
            if (_allObjectsTestScene != null)
                _allObjectsTestScene.ChangeBasePlaneColor(color);
            else if (_boxModel != null)
                _boxModel.Material = new StandardMaterial(color);

            _lastUsedColor = color;
        }

        private void ChangeTestScene()
        {
            if (_scene == null || _sceneView == null || this.Resources == null)
                return;
            
            if (_allObjectsTestScene == null)
            {
                CreateComplexScene();
            }
            else
            {
                _allObjectsTestScene.Dispose();
                _allObjectsTestScene = null;

                CreateSimpleScene();
            }
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

        private void ShowErrorMessage(string message)
        {
            var sutTitleTextView = FindViewById<TextView>(Resource.Id.text_view_subtitle);

            if (sutTitleTextView == null)
                return;

            sutTitleTextView.Text = message;
            sutTitleTextView.SetTextColor(Color.Red);
        }

        public override bool DispatchTouchEvent(MotionEvent? e)
        {
            if (_vulkanDevice == null)
                return false;

            bool isHandled;
            if (_cameraController != null)
                isHandled = _cameraController.ProcessTouchEvent(e);
            else
                isHandled = false;

            return base.DispatchTouchEvent(e) || isHandled;
        }


        // This must be called on the same thread as the SharpEngineSceneView was created
        private bool ProcessTouchEvent(MotionEvent? e)
        {
            if (_cameraController != null)
                return _cameraController.ProcessTouchEvent(e);

            return false;
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

        private void DisposeSceneView()
        {
            if (_sceneView != null)
            {
                _sceneView.Dispose();
                _sceneView = null;
            }
        }

        private void DisposeSharpEngine()
        {
            if (_allObjectsTestScene != null)
            {
                _allObjectsTestScene.Dispose();
                _allObjectsTestScene = null;
            }

            DisposeSceneView();

            if (_scene != null)
            {
                _scene.Dispose();
                _scene = null;
            }

            if (_vulkanDevice != null)
            {
                _vulkanDevice.Dispose();
                _vulkanDevice = null;
            }
        }
    }
}