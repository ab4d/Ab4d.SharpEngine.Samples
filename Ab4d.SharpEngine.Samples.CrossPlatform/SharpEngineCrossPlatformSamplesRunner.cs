using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.TestScenes;
using Ab4d.SharpEngine.Samples.Utilities;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using GlfwUI;
using SilkWindowingUI;

#if WPF
using WpfUI;
#endif

#if WINFORMS
using WinFormsUI;
using System.Windows.Forms;
#endif

// This project can be run on Linux and Windows (MacOS support is coming in the near future)
//
// It uses Silk.Net.Windowing library that can use SDL or GLFW and
// can create a window and VulkanSurface from it.
// 
// The library is using Ab4d.StandardPresentation assembly that provides
// common interfaces and classes for different windowing platforms.
// This also allows simple changing of platform by only
// changing the value of UsedPresentationFramework in the constructor
// Note that to use WPF or WindowsForms you will also need to 
// add WPF or WINFORMS as a DefineConstants to the csproj file.
//
// The sample also supports using different BitmapIO provides.
// On Windows this can be SystemDrawingBitmapIO that uses Bitmap from System.Drawing assembly.
// On Linux and Windows it is also possible to use SkiaSharpBitmapIO or ImageMagickBitmapIO. 
//
//
// NOTE:
// This sample can also work with minimal changes on Android (using SilkWindowing)
// (you would just need to adjust the startup code and creation of Activity).


namespace Ab4d.SharpEngine.Samples.CrossPlatform
{
    public class SharpEngineCrossPlatformSamplesRunner : IDisposable
    {
        private const int InitialWindowWidth = 900;
        private const int InitialWindowHeight = 500;

        private VulkanSurfaceProvider? _vulkanSurfaceProvider;
        private SurfaceKHR _vkSurfaceKHR;

        public PhysicalDevice VulkanPhysicalDevice;
        
        private VulkanDevice? _vulkanDevice;
        private Scene? _scene;
        private SceneView? _sceneView;

        private IPresentationControl _presentationControl;

        private ManualMouseCameraController? _mouseCameraController;

        private TargetPositionCamera? _targetPositionCamera;

        private bool _isMinimized;
        private bool _isShuttingDown;

        private List<SceneNode>? _lightsModels;
        private StandardMaterial? _lightEmissiveMaterial;

        private GroupNode? _lightsGroup;

        private IBitmapIO _bitmapIO;

        private bool _enableStandardValidation;

        public enum PresentationFrameworks
        {
            Wpf,
            WinForms,
            Glfw,
            SilkWindowing,
            SilkWindowingSdl,
            SilkWindowingGlfw,
        }

        public PresentationFrameworks UsedPresentationFramework;
        private AllObjectsTestScene? _allObjectsTestScene;


        public SharpEngineCrossPlatformSamplesRunner()
        {
            //
            // Global settings:
            //
#if WPF
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                UsedPresentationFramework = PresentationFrameworks.WinForms;
            else
#elif OSX
            UsedPresentationFramework = PresentationFrameworks.SilkWindowingGlfw; // On MacOS only Glfw is supported (SDL is not)
#elif LINUX
            UsedPresentationFramework = PresentationFrameworks.SilkWindowing;
#else
            UsedPresentationFramework = PresentationFrameworks.SilkWindowing;
#endif

            _enableStandardValidation = true;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // Setup logger
            SetupSharpEngineLogger(enableFullLogging: true); // Set enableFullLogging to true in case of problems and then please send the log text with the description of the problem to AB4D company


#if WINDOWS
            _bitmapIO = new SystemDrawingBitmapIO();
#elif LINUX || OSX
            _bitmapIO = new SkiaSharpBitmapIO();
            //_bitmapIO = new ImageMagickBitmapIO();
#else
            _bitmapIO = new UnsupportedBitmapIO();
#endif
            
#if OSX
            // On MacOS Vulkan can be used by MoltenVK library - see: https://github.com/KhronosGroup/MoltenVK
            // This requires that instead of the standard vulkan loader library (libvulkan.1.dylib), the 
            // Molten VK library is loaded. Then the wrappers for Vulkan functions can be retrieved from that library.
            // To make sure that GLFW will load the correct library, we need to make sure that MoltenVK library is 
            // in the application execution folder and that it is named as the Vulkan loader library.
            // 
            // In this example we do that by copying the libMoltenVK.dylib into the libvulkan.1.dylib file.
            // When you application is used only on MacOS, then you can also add the libMoltenVK.dylib
            // from the lib/macos folder to this project and rename it to libvulkan.1.dylib file
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string vulkanLibraryName = "libvulkan.1.dylib";
                
                string vulkanLibraryTargetPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, vulkanLibraryName);

                if (!System.IO.File.Exists(vulkanLibraryTargetPath))
                {
                    string vulkanLibrarySourcePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../lib/macos/libMoltenVK.dylib");
                    if (System.IO.File.Exists(vulkanLibrarySourcePath))
                    {
                        try
                        {
                            File.Copy(vulkanLibrarySourcePath, vulkanLibraryTargetPath);
                        }
                        catch
                        {
                            // pass
                        }
                    }
                }
            }
#endif            

            SetupPresentationControl();
        }

        [MemberNotNull(nameof(_presentationControl))]
        private void SetupPresentationControl()
        {
            // Set window properties
            switch (UsedPresentationFramework)
            {
#if WPF
                case PresentationFrameworks.Wpf:
                    _presentationControl = new WpfWindow(InitialWindowWidth, InitialWindowHeight, "WPF D3DHost test");
                    break;
#endif
#if WINFORMS
                case PresentationFrameworks.WinForms:
                    _presentationControl = new WinFormsWindow(InitialWindowWidth, InitialWindowHeight, "WinForms test");
                    break;
#endif

                case PresentationFrameworks.SilkWindowing:
                case PresentationFrameworks.SilkWindowingSdl:
                case PresentationFrameworks.SilkWindowingGlfw:

                    bool isSupported;
                    if (UsedPresentationFramework == PresentationFrameworks.SilkWindowingSdl)
                    {
                        isSupported = SilkWindow.PrioritizeSdl();
                    }
                    else if (UsedPresentationFramework == PresentationFrameworks.SilkWindowingGlfw)
                    {
                        isSupported = SilkWindow.PrioritizeGlfw();
                    }
                    else
                    {
                        isSupported = SilkWindow.IsAnyPlatformSupported;
                    }

                    if (!isSupported)
                    {
                        string message = UsedPresentationFramework.ToString() + "is not supported on this system.";
                        Log.Error?.Write(message);
                        throw new NotSupportedException(message);
                    }

                    if (SilkWindow.IsViewOnlyPlatform)
                    {
                        Log.Info?.Write("Start crating SilkView...");
                        _presentationControl = new SilkView();
                        Log.Info?.Write("   SilkView created");
                    }
                    else
                    {
                        Log.Info?.Write("Start crating SilkWindow...");
                        _presentationControl = new SilkWindow(InitialWindowWidth, InitialWindowHeight, "SilkWindowing test");
                        Log.Info?.Write("   SilkWindow created");
                    }

                    break;

                default:
                case PresentationFrameworks.Glfw:
                    _presentationControl = new GlfwWindow(InitialWindowWidth, InitialWindowHeight, "GLFW test");
                    break;
            }


            _presentationControl.Title = "Ab4d.SharpEngine cross platform sample";
            
            _presentationControl.Loaded += delegate (object? sender, EventArgs args)
            {
                CreateEngineAndScene();
            };

            _presentationControl.Closing += delegate (object? sender, EventArgs args)
            {
                Dispose();
            };

            // We need to update the ViewSize when the size of the view is changed (this is done above in the _presentationControl.SizeChanged)
            _presentationControl.SizeChanged += delegate (object sender, Ab4d.SizeChangeEventArgs e)
            {
                // SizeChanged is also called when _presentationControl.IsMinimized (in this case Size is still the same).
                // We need to handle when IsMinimized is set to false (we need to force rendering)
                if (_presentationControl.IsMinimized && !_isMinimized)
                {
                    // State changed to Minimized
                    _isMinimized = true;
                    return;
                }

                if (_isMinimized && !_presentationControl.IsMinimized)
                {
                    // State changed from Minimized to Normal or Maximized

                    _isMinimized = false; // Update _isMinimized ...
                }

                if (_sceneView != null)
                {
                    // ... then call resize and Render in the lines below ...
                    // Calling Resize (and aquiring the new size of the surface) is required on some platforms (for example Glfw).
                    // For other (WPF) a new Render would be enough, but it does not hurt to also call Resize.

                    _sceneView.Resize(renderNextFrameAfterResize: true);
                }
            };

            
            // We need to call ProcessMouseDown, ProcessMouseUp, ProcessMouseMove, ProcessMouseWheel methods.
            // This is done in the event handler that are subscribed below
            _presentationControl.MouseDown += OnMouseDown;
            _presentationControl.MouseUp += OnMouseUp;
            _presentationControl.MouseMove += OnMouseMove;
            _presentationControl.MouseWheel += OnMouseWheel;

            _presentationControl.Show();
        }

        private void CreateEngineAndScene()
        {
            bool success = InitializeEngine();

            if (!success)
                return;
            
            InitializeSceneView();


            if (_scene != null && _sceneView != null)
            {
                // Add demo objects to _scene
                _allObjectsTestScene = new AllObjectsTestScene(_scene, _bitmapIO);
                _allObjectsTestScene.CreateTestScene();

                UpdateLightModels();
            }
        }

        #region InitializeEngine

        private bool InitializeEngine()
        {
            Log.Info?.Write("Start initializing Ab4d.SharpEngine...");


            if (_presentationControl.WindowHandle != IntPtr.Zero)
            {
                _vulkanSurfaceProvider = new WindowsVulkanSurfaceProvider(_presentationControl.WindowHandle, addDefaultSurfaceExtensions: true);
            }
            else
            {
                _vulkanSurfaceProvider = new CustomVulkanSurfaceProvider(instance =>
                {
                    var surfaceHandle = _presentationControl.CreateVulkanSurface(instance.Handle);
                    return new SurfaceKHR(surfaceHandle);
                },
                    addDefaultSurfaceExtensions: true);
            }


            var engineCreateOptions = new EngineCreateOptions(applicationName: "SharpEngine.Tests", enableStandardValidation: _enableStandardValidation)
            {
                EnableSurfaceSupport = true, // true by default; you can set it to false in case when the SceneView will be used to render only to a bitmap or to a shared texture
            };

            // Create a VulkanDevice (and VulkanInstance if it was not created yet)
            // In case when we have _vulkanSurface it is recommended to provide it when creating the VulkanDevice.
            // This way the correct settings for the device can be used (for example SwapChainImagesCount).
            // It is also possible not to specify surface when creating VulkanDevice and do that only when creating SceneView.
            _vulkanDevice = VulkanDevice.Create(engineCreateOptions, defaultSurfaceProvider: _vulkanSurfaceProvider);

            return true;
        }

        private void RecreateEngine()
        {
            _isShuttingDown = true;

            if (_scene != null)
            {
                _scene.GpuDevice.WaitUntilIdle();

                Dispose();

                System.Threading.Thread.Sleep(200);
            }

            // When changing Vulkan device it is recommended not to reuse the hwnd - it is better to recreate it
            // Usually the problem is that the last image is preserved by the hwnd (it is resized without aspect rate when window is resized)
            // If another VulkanDevice is created on the same device, then the hwnd can be reused.
            // It seems that it works well (at least on Windows)
            // For now this is supported only for WPF window
#if WPF
            if (_presentationControl is WpfWindow wpfWindow)
                wpfWindow.RecreateD3DHost();
#else
            CreateEngineAndScene();
#endif

            _isShuttingDown = false; // Start processing Render event again
        }


        private bool InitializeEngine_Long()
        {
            VulkanInstance vulkanInstance;

            try
            {
                var engineCreateOptions = new EngineCreateOptions(applicationName: "SharpEngine.Tests", enableStandardValidation: true);
                vulkanInstance = new VulkanInstance(engineCreateOptions);
            }
            catch (Exception ex)
            {
                Log.Error?.Write("Error creating Vulkan instance", ex);
                return false;
            }


            try
            {
                _vkSurfaceKHR = CreateVulkanSurface(vulkanInstance.Instance);
            }
            catch (Exception ex)
            {
                Log.Error?.Write("Error creating vulkan surface", ex);
                return false;
            }

            try
            {
                VulkanPhysicalDevice = vulkanInstance.GetBestPhysicalDevice(_vkSurfaceKHR);
            }
            catch (Exception ex)
            {
                Log.Error?.Write("Error getting physical device", ex);
                return false;
            }

            try
            {
                _vulkanDevice = new VulkanDevice(vulkanInstance, VulkanPhysicalDevice, _vkSurfaceKHR);
            }
            catch (Exception ex)
            {
                Log.Error?.Write("Error creating Vulkan device", ex);
                return false;
            }

            return true;
        }

        private unsafe SurfaceKHR CreateVulkanSurface(Instance vulkanInstance)
        {
            // First call CreateVulkanSurface to check if _presentationControl can create the surface by itself (for example GLFW).
            var surfaceHandle = _presentationControl.CreateVulkanSurface(vulkanInstance.Handle);

            // if surface is created then just wrap the pointer with VkSurfaceKHR and return
            if (surfaceHandle != 0)
                return new SurfaceKHR(surfaceHandle);


            // ... else we use WindowHandle to create a surface here

#if WINDOWS
            if (_presentationControl.WindowHandle == IntPtr.Zero)
                throw new NotSupportedException("Cannot create VulkanSurface because CreateVulkanSurface does not create a VulkanSurface and WindowHandle is not set");

            var surfaceCreateInfo = new Win32SurfaceCreateInfoKHR
            {
                SType = StructureType.Win32SurfaceCreateInfoKhr,
                Hinstance = Process.GetCurrentProcess().Handle,
                Hwnd = _presentationControl.WindowHandle
            };

            SurfaceKHR vulkanSurface;
            var result = VulkanInstance.Win32SurfaceExt.CreateWin32Surface(vulkanInstance, &surfaceCreateInfo, null, &vulkanSurface);

            if (result != Result.Success || vulkanSurface.Handle == 0)
                throw new SharpEngineException("Failed to create a Vulkan surface - reason: " + result.ToString());

            return vulkanSurface;
#else
            throw new NotSupportedException("CreateVulkanSurface is supported only on Windows");
#endif
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
        #endregion

        #region InitializeSceneView, UpdateLightModels

        private void InitializeSceneView()
        {
            if (_vulkanDevice == null || _vulkanSurfaceProvider == null)
                return;

            _scene = new Scene(_vulkanDevice, "MainScene");

            // Add lights
            _scene.Lights.Add(new AmbientLight(intensity: 0.3f));
            _scene.Lights.Add(new PointLight(new Vector3(500, 200, 0), range: 10000));
            _scene.Lights.Add(new DirectionalLight(new Vector3(-1, -0.3f, 0)));
            _scene.Lights.Add(new SpotLight(new Vector3(300, 0, 300), new Vector3(-1, -0.3f, 0)) { Color = new Color3(0.4f, 0.4f, 0.4f) });


            // Create SceneView object that will render the _scene to the specified VulkanSurface
            _sceneView = new SceneView(_scene, "MainSceneView");

            // If the VulkanSurface was already specified as defaultSurface when creating VulkanDevice,
            // and if are creating SceneView with the same surface, then we do not need to provide surface here
            // and we can create the SceneView only with _scene and name (optional).
            // We can also set the useDefaultSurface to true (but we can skip it as its default value is also true):
            //_sceneView = new SceneView(_scene, "MainSceneView", useDefaultSurface: true); 

            _targetPositionCamera = new TargetPositionCamera()
            {
                Heading = -40,
                Attitude = -25,
                Distance = 1500,
                TargetPosition = new Vector3(0, 0, 0)
            };

            _sceneView.Camera = _targetPositionCamera;

            _sceneView.BackgroundColor = Color4.White; //new Color4(0.9f, 0.3f, 0.3f, 1);

            if (_vulkanSurfaceProvider == null)
            {
                // When we are not rendering to a surface, then we need to provide the size of the SceneView
                _sceneView.Initialize(width: _presentationControl.Width != 0 ? (int)_presentationControl.Width : 800,
                                      height: _presentationControl.Height != 0 ? (int)_presentationControl.Height : 600,
                                      dpiScaleX: _presentationControl.DpiScaleX,
                                      dpiScaleY: _presentationControl.DpiScaleY);
            }
            else
            {
                // When we are rendering to a surface, then we do not provide the size (width and height) 
                // but only DPI scale because this cannot be read from the surface data.
                // Note that without correct DPI scale the hit testing will not work correctly.
                _sceneView.Initialize(_vulkanSurfaceProvider, dpiScaleX: _presentationControl.DpiScaleX, dpiScaleY: _presentationControl.DpiScaleY);
            }


            _mouseCameraController = new ManualMouseCameraController(_sceneView)
            {
                RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed,
                MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.ControlKey,
                ZoomMode = CameraZoomMode.MousePosition,
                RotateAroundMousePosition = true
            };

            // Setup mouse capture when supported by the platform
            // This enables getting mouse events after pressing mouse button event when the mouse leaves the window area
            if (_presentationControl.IsMouseCaptureSupported)
            {
                _mouseCameraController.CaptureMouseAction = () => _presentationControl.CaptureMouse();
                _mouseCameraController.ReleaseMouseCaptureAction = () => _presentationControl.ReleaseMouseCapture();
            }
        }

        private void UpdateLightModels()
        {
            if (_scene == null)
                return;

            if (_lightsGroup == null)
            {
                _lightsGroup = new GroupNode("LightsGroup");
                _scene.RootNode.Add(_lightsGroup);
            }

            if (_lightsModels == null)
            {
                _lightsModels = new List<SceneNode>();
            }
            else
            {
                foreach (var lightsModel in _lightsModels)
                {
                    if (_lightsGroup != null)
                        _lightsGroup.Remove(lightsModel);

                    lightsModel.Dispose();
                }

                _lightsModels.Clear();
            }

            if (_lightEmissiveMaterial == null)
                _lightEmissiveMaterial = new StandardMaterial("YellowLightEmissiveMaterial") { EmissiveColor = new Color3(1f, 1f, 0f) };


            for (var i = 0; i < _scene.Lights.Count; i++)
            {
                var oneLight = _scene.Lights[i];

                ModelNode? lightModelNode;

                if (oneLight is ISpotLight spotLight)
                {
                    var spotLightDirection = Vector3.Normalize(spotLight.Direction);

                    lightModelNode = new ArrowModelNode(_lightEmissiveMaterial, $"SpotLightModel_{i}")
                    {
                        StartPosition = spotLight.Position,
                        EndPosition = spotLight.Position + spotLightDirection * 20,
                        Radius = 2
                    };
                }
                else if (oneLight is IPointLight pointLight)
                {
                    lightModelNode = new SphereModelNode(_lightEmissiveMaterial, $"PointLightModel_{i}")
                    {
                        CenterPosition = pointLight.Position,
                        Radius = 3
                    };
                }
                else
                {
                    lightModelNode = null;
                }

                if (lightModelNode != null)
                {
                    if (_lightsGroup != null)
                        _lightsGroup.Add(lightModelNode);

                    if (_lightsModels != null)
                        _lightsModels.Add(lightModelNode);
                }
            }
        }

        #endregion

        #region Run, RenderCallback, UpdateFrameStatistics
        public void Run()
        {
            // Start the render loop
            _presentationControl.StartRenderLoop(RenderCallback);
        }

        private void RenderCallback()
        {
            if (_isShuttingDown)
                return;

            _allObjectsTestScene?.AnimateModels();

            if (_scene != null && _sceneView != null)
                _sceneView.Render();
        }

        #endregion

        #region Mouse event handlers

        private void OnMouseMove(object sender, Ab4d.MouseMoveEventArgs e)
        {
            if (_mouseCameraController != null)
            {
                var keyboardModifiers = _presentationControl.GetKeyboardModifiers();
                _presentationControl.GetMouseState(out float x, out float y, out var pressedMouseButtons);

                // Note that we need to convert from MouseButton enum that is defined in Ab4d.StandardPresentation to MouseCameraController.MouseButtons (both enums use the same values so we can convert them directly); the same is done for keyboardModifiers.
                _mouseCameraController.ProcessMouseMove(new Vector2(x, y), pressedMouseButtons, keyboardModifiers);
            }            
        }

        private void OnMouseUp(object sender, Ab4d.MouseButtonEventArgs e)
        {
            if (_mouseCameraController != null)
            {
                var keyboardModifiers = _presentationControl.GetKeyboardModifiers();
                _presentationControl.GetMouseState(out _, out _, out var pressedMouseButtons);

                // Note that we need to convert from MouseButton enum that is defined in Ab4d.StandardPresentation to MouseCameraController.MouseButtons (both enums use the same values so we can convert them directly); the same is done for keyboardModifiers.
                _mouseCameraController.ProcessMouseUp(pressedMouseButtons, keyboardModifiers);
            }
        }

        private void OnMouseDown(object sender, Ab4d.MouseButtonEventArgs e)
        {
            if (_mouseCameraController != null)
            {
                var keyboardModifiers = _presentationControl.GetKeyboardModifiers();
                _presentationControl.GetMouseState(out float x, out float y, out var pressedMouseButtons);

                // Note that we need to convert from MouseButton enum that is defined in Ab4d.StandardPresentation to MouseCameraController.MouseButtons (both enums use the same values so we can convert them directly); the same is done for keyboardModifiers.
                _mouseCameraController.ProcessMouseDown(new Vector2(x, y), pressedMouseButtons, keyboardModifiers);
            }
        }

        private void OnMouseWheel(object sender, Ab4d.MouseWheelEventArgs e)
        {
            if (_mouseCameraController != null)
            {
                _presentationControl.GetMouseState(out float x, out float y, out var pressedMouseButtons);
                _mouseCameraController.ProcessMouseWheel(new Vector2(x, y), e.DeltaY);
            }
        }

        #endregion

        #region Dispose
        public unsafe void Dispose()
        {
            if (_allObjectsTestScene != null)
            {
                _allObjectsTestScene.Dispose();
                _allObjectsTestScene = null;
            }

            _scene?.RootNode.Clear(); // Remove all SceneNodes from the scene.

            // Dispose created resources:
            if (_sceneView != null)
            {
                _sceneView.Dispose();
                _sceneView = null;
            }

            if (_vulkanSurfaceProvider != null)
            {
                _vulkanSurfaceProvider.Dispose();
                _vulkanSurfaceProvider = null;
            }
            

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
#endregion
    }
}