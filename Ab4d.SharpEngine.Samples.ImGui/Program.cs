using System.Diagnostics;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.ImGui;

internal class Program
{
    // Silk
    private static Silk.NET.Windowing.IWindow? _window;
    private static Silk.NET.Input.IKeyboard? _keyboard;
    private static Silk.NET.Input.IMouse? _mouse;

    // SharpEngine
    private static bool _vulkanValidation;

    private static VulkanDevice? _vulkanDevice;
    private static VulkanSurfaceProvider? _vulkanSurfaceProvider;

    private static Scene? _scene;
    private static SceneView? _sceneView;
    private static ManualPointerCameraController? _pointerCameraController;

    private static ImGuiRenderingStep? _imGuiRenderingStep;

    // ImGui
    private static nint _imGuiCtx;
    private static ImGuiNET.ImGuiIOPtr _imGuiIo;

    private static DateTime _previousFrameTime;

    private static bool _showDemoWindow = true;
    private static bool _showOtherWindow = true;

    private static void Main()
    {
        // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
        // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase
        Licensing.SetLicense(licenseOwner: "AB4D",
                             licenseType: "SamplesLicense",
                             license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");

        // Enable logging of warnings and errors from SharpEngine. In debug builds, also enable Vulkan validation.
        Log.LogLevel = LogLevels.Warn;
#if DEBUG
        Log.IsLoggingToDebugOutput = true;
        _vulkanValidation = true;
#endif

        // Create ImGui context
        _imGuiCtx = ImGuiNET.ImGui.CreateContext();

        _imGuiIo = ImGuiNET.ImGui.GetIO();
        _imGuiIo.BackendFlags |= ImGuiNET.ImGuiBackendFlags.RendererHasVtxOffset;
        _imGuiIo.ConfigFlags |= ImGuiNET.ImGuiConfigFlags.NavEnableKeyboard |
                                ImGuiNET.ImGuiConfigFlags.DockingEnable;
        _imGuiIo.Fonts.Flags |= ImGuiNET.ImFontAtlasFlags.NoBakedLines;

        // Create window using Silk; setup is performed once window is created/loaded.
        var options = Silk.NET.Windowing.WindowOptions.DefaultVulkan;
        options.Size = new Silk.NET.Maths.Vector2D<int>(1440, 856);
        options.Title = "Ab4d.SharpEngine with ImGui";

        // You can decide to use SDL or Glfw
        //Silk.NET.Windowing.Window.PrioritizeSdl();
        //Silk.NET.Windowing.Window.PrioritizeGlfw();

        _window = Silk.NET.Windowing.Window.Create(options);

        _window.Load += () =>
        {
            SetupInput();
            SetupSharpEngine();
            CreateScene();
            SetupImGui();
        };

        // ReSharper disable once AccessToDisposedClosure
        _window.Render += (_) => { _sceneView?.Render(); };

        // ReSharper disable once AccessToDisposedClosure
        _window.FramebufferResize += (size) =>
        {
            _sceneView?.Resize(newWidth: size.X > 0 ? size.X : 800,
                               newHeight: size.Y > 0 ? size.Y : 600,
                               renderNextFrameAfterResize: true);
        };

        _window.Initialize();

        Silk.NET.Windowing.WindowExtensions.Run(_window);

        // Cleanup
        _scene?.GpuDevice?.WaitUntilIdle(); // Finish rendering before we start disposing objects

        _imGuiRenderingStep?.Dispose();

        _sceneView?.Dispose();
        _sceneView = null;

        _scene?.Dispose();

        _vulkanSurfaceProvider?.Dispose();
        _vulkanDevice?.Dispose();

        ImGuiNET.ImGui.DestroyContext(_imGuiCtx);

        _window.Dispose();
    }

    private static void SetupInput()
    {
        // Register input events on fist available keyboard and mouse
        Debug.Assert(_window != null, nameof(_window) + " != null");
        var input = Silk.NET.Input.InputWindowExtensions.CreateInput(_window);
        var keyboards = input.Keyboards;
        if (keyboards.Count > 0)
        {
            _keyboard = keyboards[0];
            _keyboard.KeyDown += OnKeyboardKeyDown;
            _keyboard.KeyUp += OnKeyboardKeyUp;
            _keyboard.KeyChar += OnKeyboardKeyChar;
        }

        var mice = input.Mice;
        if (mice.Count > 0)
        {
            _mouse = mice[0];
            _mouse.MouseDown += OnMouseButtonDown;
            _mouse.MouseUp += OnMouseButtonUp;
            _mouse.MouseMove += OnMouseMove;
            _mouse.Scroll += OnMouseScroll;
        }
    }

    private static unsafe void SetupSharpEngine()
    {
        // Setup SharpEngine to use Silk window as presentation surface
        _vulkanSurfaceProvider = new CustomVulkanSurfaceProvider(instance =>
            {
                Debug.Assert(_window != null, nameof(_window) + " != null");
                Debug.Assert(_window.VkSurface != null, "_window.VkSurface != null");
                var surfaceHandle = _window.VkSurface.Create(new Silk.NET.Core.Native.VkHandle(instance.Handle), (byte*)null);
                return new SurfaceKHR(surfaceHandle.Handle);
            },
            addDefaultSurfaceExtensions: true);

        var engineCreateOptions =
            new EngineCreateOptions(applicationName: "Ab4d.SharpEngine.Samples.ImGui", enableStandardValidation: _vulkanValidation)
            {
                EnableSurfaceSupport = true,
            };

        _vulkanDevice = VulkanDevice.Create(defaultSurfaceProvider: _vulkanSurfaceProvider, engineCreateOptions);
    }

    private static void CreateScene()
    {
        // Set up test scene
        Debug.Assert(_vulkanDevice != null, nameof(_vulkanDevice) + " != null");
        _scene = new Scene(_vulkanDevice);

        // Add lights
        _scene.Lights.Add(new DirectionalLight(new Vector3(-1, -0.3f, 0)));
        _scene.Lights.Add(new PointLight(new Vector3(500, 200, 100), range: 10000));
        _scene.SetAmbientLight(intensity: 0.3f);


        // Create test 3D scene: wire grid and a hash symbol
        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -0.1f, 0),
            Size = new Vector2(200, 200),
        };

        _scene.RootNode.Add(wireGridNode);


        float hashModelSize = 100;
        float hashModelBarThickness = 16;
        float hashModelBarOffset = 20;

        var hashSymbolMesh = MeshFactory.CreateHashSymbolMesh(centerPosition: new Vector3(0, hashModelBarThickness * 0.5f, 0),
                                                              shapeYVector: new Vector3(0, 0, 1),
                                                              extrudeVector: new Vector3(0, hashModelBarThickness, 0),
                                                              size: hashModelSize,
                                                              barThickness: hashModelBarThickness,
                                                              barOffset: hashModelBarOffset,
                                                              name: "HashSymbolMesh");

        var hashModelNode = new Ab4d.SharpEngine.SceneNodes.MeshModelNode(hashSymbolMesh, "HashSymbolModel")
        {
            Material = new StandardMaterial(Color3.FromByteRgb(255, 197, 0)),
            Transform = new StandardTransform()
        };
        
        _scene.RootNode.Add(hashModelNode);

        // See Avalonia, WPF or WinForms samples for more 3D objects and demonstration of other SharpEngine features.

        // Create scene view
        _sceneView = new SceneView(_scene);
        _sceneView.BackgroundColor = Color4.White;
        
        Debug.Assert(_vulkanSurfaceProvider != null, nameof(_vulkanSurfaceProvider) + " != null");

        // TODO: How to read DPI scale?
        float dpiScaleX = 1.0f;
        float dpiScaleY = 1.0f;


        // Initialize the SceneView
        _sceneView.Initialize(_vulkanSurfaceProvider,
                              dpiScaleX: dpiScaleX,
                              dpiScaleY: dpiScaleY,
                              multisampleCount: 4,
                              supersamplingCount: 1,
                              fallbackWidth: (_window!.FramebufferSize.X > 0) ? _window!.FramebufferSize.X : 800,   // fallback width and height is used in case when the surface does not provide a valid size (for example in Wayland)
                              fallbackHeight: (_window!.FramebufferSize.Y > 0) ? _window!.FramebufferSize.Y : 600);
        

        // Camera
        var targetPositionCamera = new TargetPositionCamera()
                                   {
                                       Heading = -40,
                                       Attitude = -25,
                                       Distance = 300,
                                       TargetPosition = new Vector3(0, 0, 0),
                                       ShowCameraLight = ShowCameraLightType.Auto
                                   };

        targetPositionCamera.StartRotation(headingChangeInSecond: 30);

        _sceneView.Camera = targetPositionCamera;

        // Controller
        _pointerCameraController = new ManualPointerCameraController(_sceneView)
        {
            RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,
            MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed |
                                   PointerAndKeyboardConditions.ControlKey,
            QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed |
                                  PointerAndKeyboardConditions.RightPointerButtonPressed,

            RotateAroundPointerPosition = false,
            ZoomMode = CameraZoomMode.PointerPosition,
        };
    }

    private static void SetupImGui()
    {
        Debug.Assert(_sceneView != null, nameof(_sceneView) + " != null");
        _imGuiIo.DisplaySize = new Vector2(_sceneView.RenderWidth, _sceneView.RenderHeight);
        _imGuiIo.DeltaTime = 0;

        _previousFrameTime = DateTime.Now;

        // Create and register custom rendering step
        _imGuiRenderingStep = new ImGuiRenderingStep(_sceneView, _imGuiCtx, "ImGuiRenderingStep");
        Debug.Assert(_sceneView.DefaultRenderObjectsRenderingStep != null, "_sceneView.DefaultRenderObjectsRenderingStep != null");
        _sceneView.RenderingSteps.AddAfter(_sceneView.DefaultRenderObjectsRenderingStep, _imGuiRenderingStep);

        // This allows UI to be animated
        _sceneView.SceneUpdating += (_, _) => { UpdateImgUiInterface(); };
    }

    private static void UpdateImgUiInterface()
    {
        var shouldUpdate = true; // This can be set to false when we know the UI has not changed

        // Update time delta between calls
        var now = DateTime.Now;
        _imGuiIo.DeltaTime = (float)(now - _previousFrameTime).TotalSeconds;
        _previousFrameTime = now;

        // Keep display size up to date
        Debug.Assert(_sceneView != null, nameof(_sceneView) + " != null");
        _imGuiIo.DisplaySize = new Vector2(_sceneView.RenderWidth / _sceneView.DpiScaleX, _sceneView.RenderHeight / _sceneView.DpiScaleY);

        // Begin frame
        ImGuiNET.ImGui.NewFrame();

        if (_showOtherWindow)
        {
            ImGuiNET.ImGui.Begin("Camera controls info", ref _showOtherWindow);
            ImGuiNET.ImGui.Text("ROTATE: left mouse button\nMOVE: CTRL + left mouse button\nQUICK ZOOM: left + right mouse button");
            ImGuiNET.ImGui.End();
        }

        if (_showDemoWindow)
        {
            ImGuiNET.ImGui.ShowDemoWindow(ref _showDemoWindow);
        }

        ImGuiNET.ImGui.EndFrame();
        ImGuiNET.ImGui.Render();

        if (shouldUpdate)
            _sceneView.NotifyChange(SceneViewDirtyFlags.SpritesChanged);

        // If all ImGui windows were closed, exit the application
        if (!_showDemoWindow && !_showOtherWindow)
            _window?.Close();
    }

    #region Input

    private static void OnKeyboardKeyDown(Silk.NET.Input.IKeyboard keyboard, Silk.NET.Input.Key key, int keyCode)
    {
        if (MapKeyImGui(key, out var mappedKey))
            _imGuiIo.AddKeyEvent(mappedKey, true);

        if (_imGuiIo.WantCaptureKeyboard)
            return;

        // Pressing ESC key exits the application
        if (key == Silk.NET.Input.Key.Escape && _window != null)
            _window.Close();
    }

    private static void OnKeyboardKeyUp(Silk.NET.Input.IKeyboard keyboard, Silk.NET.Input.Key key, int keyCode)
    {
        if (MapKeyImGui(key, out var mappedKey))
            _imGuiIo.AddKeyEvent(mappedKey, false);
    }

    private static void OnKeyboardKeyChar(Silk.NET.Input.IKeyboard keyboard, char c)
    {
        _imGuiIo.AddInputCharacter(c);
    }

    private static void OnMouseMove(Silk.NET.Input.IMouse mouse, Vector2 position)
    {
        position = AdjustMousePositionForDpiScale(position);

        _imGuiIo.AddMousePosEvent(position.X, position.Y);

        if (_imGuiIo.WantCaptureMouse)
            return;
        
        _pointerCameraController?.ProcessPointerMoved(position, GetPressedMouseButtons(), GetKeyboardModifiers());
    }

    private static void OnMouseButtonDown(Silk.NET.Input.IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        if (MapMouseButtonImGui(button, out var mappedButton))
            _imGuiIo.AddMouseButtonEvent(mappedButton, true);

        if (_imGuiIo.WantCaptureMouse)
            return;

        if (MapMouseButtonToSharpEngine(button, out var mappedButtonSharpEngine))
        {
            var position = AdjustMousePositionForDpiScale(mouse.Position);
            _pointerCameraController?.ProcessPointerPressed(position, mappedButtonSharpEngine, GetKeyboardModifiers());
        }
    }

    private static void OnMouseButtonUp(Silk.NET.Input.IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        if (MapMouseButtonImGui(button, out var mappedButton))
            _imGuiIo.AddMouseButtonEvent(mappedButton, false);

        if (_imGuiIo.WantCaptureMouse)
            return;

        if (MapMouseButtonToSharpEngine(button, out var mappedButtonSharpEngine))
        {
            var position = AdjustMousePositionForDpiScale(mouse.Position);
            _pointerCameraController?.ProcessPointerPressed(position, mappedButtonSharpEngine, GetKeyboardModifiers());
        }
    }

    private static void OnMouseScroll(Silk.NET.Input.IMouse mouse, Silk.NET.Input.ScrollWheel wheel)
    {
        _imGuiIo.AddMouseWheelEvent(0f, wheel.Y);
        if (_imGuiIo.WantCaptureMouse)
            return;

        var position = AdjustMousePositionForDpiScale(mouse.Position);
        _pointerCameraController?.ProcessPointerWheelChanged(position, wheel.Y);
    }

    private static Vector2 AdjustMousePositionForDpiScale(Vector2 position)
    {
        if (_sceneView == null)
            return position;

        return new Vector2(position.X / _sceneView.DpiScaleX, position.Y / _sceneView.DpiScaleY);
    }


    private static KeyboardModifiers GetKeyboardModifiers()
    {
        if (_keyboard == null)
            return KeyboardModifiers.None;

        return (_keyboard.IsKeyPressed(Silk.NET.Input.Key.ShiftLeft) ? KeyboardModifiers.ShiftKey : 0) |
               (_keyboard.IsKeyPressed(Silk.NET.Input.Key.ShiftRight) ? KeyboardModifiers.ShiftKey : 0) |
               (_keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) ? KeyboardModifiers.ControlKey : 0) |
               (_keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight) ? KeyboardModifiers.ControlKey : 0) |
               (_keyboard.IsKeyPressed(Silk.NET.Input.Key.AltLeft) ? KeyboardModifiers.AltKey : 0) |
               (_keyboard.IsKeyPressed(Silk.NET.Input.Key.AltRight) ? KeyboardModifiers.AltKey : 0) |
               (_keyboard.IsKeyPressed(Silk.NET.Input.Key.SuperLeft) ? KeyboardModifiers.SuperKey : 0) |
               (_keyboard.IsKeyPressed(Silk.NET.Input.Key.SuperRight) ? KeyboardModifiers.SuperKey : 0);
    }

    private static PointerButtons GetPressedMouseButtons()
    {
        if (_mouse == null)
            return PointerButtons.None;

        return (_mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Left) ? PointerButtons.Left : 0) |
               (_mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Middle) ? PointerButtons.Middle : 0) |
               (_mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Right) ? PointerButtons.Right : 0) |
               (_mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Button4) ? PointerButtons.XButton1 : 0) |
               (_mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Button5) ? PointerButtons.XButton2 : 0);
    }

    private static bool MapMouseButtonToSharpEngine(Silk.NET.Input.MouseButton button, out PointerButtons result)
    {
        result = button switch
        {
            Silk.NET.Input.MouseButton.Left => PointerButtons.Left,
            Silk.NET.Input.MouseButton.Right => PointerButtons.Right,
            Silk.NET.Input.MouseButton.Middle => PointerButtons.Middle,
            Silk.NET.Input.MouseButton.Button4 => PointerButtons.XButton1,
            Silk.NET.Input.MouseButton.Button5 => PointerButtons.XButton2,
            _ => PointerButtons.None
        };
        return result != PointerButtons.None;
    }

    private static bool MapMouseButtonImGui(Silk.NET.Input.MouseButton button, out int result)
    {
        result = button switch
        {
            Silk.NET.Input.MouseButton.Left => 0,
            Silk.NET.Input.MouseButton.Right => 1,
            Silk.NET.Input.MouseButton.Middle => 2,
            Silk.NET.Input.MouseButton.Button4 => 3,
            Silk.NET.Input.MouseButton.Button5 => 4,
            _ => -1
        };
        return result != -1;
    }

    private static bool MapKeyImGui(Silk.NET.Input.Key key, out ImGuiNET.ImGuiKey result)
    {
        ImGuiNET.ImGuiKey KeyToImGuiKeyShortcut(Silk.NET.Input.Key keyToConvert, Silk.NET.Input.Key startKey1, ImGuiNET.ImGuiKey startKey2)
        {
            var changeFromStart1 = (int)keyToConvert - (int)startKey1;
            return startKey2 + changeFromStart1;
        }

        result = key switch
        {
            >= Silk.NET.Input.Key.F1 and <= Silk.NET.Input.Key.F24 => KeyToImGuiKeyShortcut(key, Silk.NET.Input.Key.F1, ImGuiNET.ImGuiKey.F1),
            >= Silk.NET.Input.Key.Keypad0 and <= Silk.NET.Input.Key.Keypad9 => KeyToImGuiKeyShortcut(key, Silk.NET.Input.Key.Keypad0, ImGuiNET.ImGuiKey.Keypad0),
            >= Silk.NET.Input.Key.A and <= Silk.NET.Input.Key.Z => KeyToImGuiKeyShortcut(key, Silk.NET.Input.Key.A, ImGuiNET.ImGuiKey.A),
            >= Silk.NET.Input.Key.Number0 and <= Silk.NET.Input.Key.Number9 => KeyToImGuiKeyShortcut(key, Silk.NET.Input.Key.Number0, ImGuiNET.ImGuiKey._0),
            Silk.NET.Input.Key.ShiftLeft or Silk.NET.Input.Key.ShiftRight => ImGuiNET.ImGuiKey.ModShift,
            Silk.NET.Input.Key.ControlLeft or Silk.NET.Input.Key.ControlRight => ImGuiNET.ImGuiKey.ModCtrl,
            Silk.NET.Input.Key.AltLeft or Silk.NET.Input.Key.AltRight => ImGuiNET.ImGuiKey.ModAlt,
            Silk.NET.Input.Key.SuperLeft or Silk.NET.Input.Key.SuperRight => ImGuiNET.ImGuiKey.ModSuper,
            Silk.NET.Input.Key.Menu => ImGuiNET.ImGuiKey.Menu,
            Silk.NET.Input.Key.Up => ImGuiNET.ImGuiKey.UpArrow,
            Silk.NET.Input.Key.Down => ImGuiNET.ImGuiKey.DownArrow,
            Silk.NET.Input.Key.Left => ImGuiNET.ImGuiKey.LeftArrow,
            Silk.NET.Input.Key.Right => ImGuiNET.ImGuiKey.RightArrow,
            Silk.NET.Input.Key.Enter => ImGuiNET.ImGuiKey.Enter,
            Silk.NET.Input.Key.Escape => ImGuiNET.ImGuiKey.Escape,
            Silk.NET.Input.Key.Space => ImGuiNET.ImGuiKey.Space,
            Silk.NET.Input.Key.Tab => ImGuiNET.ImGuiKey.Tab,
            Silk.NET.Input.Key.Backspace => ImGuiNET.ImGuiKey.Backspace,
            Silk.NET.Input.Key.Insert => ImGuiNET.ImGuiKey.Insert,
            Silk.NET.Input.Key.Delete => ImGuiNET.ImGuiKey.Delete,
            Silk.NET.Input.Key.PageUp => ImGuiNET.ImGuiKey.PageUp,
            Silk.NET.Input.Key.PageDown => ImGuiNET.ImGuiKey.PageDown,
            Silk.NET.Input.Key.Home => ImGuiNET.ImGuiKey.Home,
            Silk.NET.Input.Key.End => ImGuiNET.ImGuiKey.End,
            Silk.NET.Input.Key.CapsLock => ImGuiNET.ImGuiKey.CapsLock,
            Silk.NET.Input.Key.ScrollLock => ImGuiNET.ImGuiKey.ScrollLock,
            Silk.NET.Input.Key.PrintScreen => ImGuiNET.ImGuiKey.PrintScreen,
            Silk.NET.Input.Key.Pause => ImGuiNET.ImGuiKey.Pause,
            Silk.NET.Input.Key.NumLock => ImGuiNET.ImGuiKey.NumLock,
            Silk.NET.Input.Key.KeypadDivide => ImGuiNET.ImGuiKey.KeypadDivide,
            Silk.NET.Input.Key.KeypadMultiply => ImGuiNET.ImGuiKey.KeypadMultiply,
            Silk.NET.Input.Key.KeypadSubtract => ImGuiNET.ImGuiKey.KeypadSubtract,
            Silk.NET.Input.Key.KeypadAdd => ImGuiNET.ImGuiKey.KeypadAdd,
            Silk.NET.Input.Key.KeypadDecimal => ImGuiNET.ImGuiKey.KeypadDecimal,
            Silk.NET.Input.Key.KeypadEnter => ImGuiNET.ImGuiKey.KeypadEnter,
            Silk.NET.Input.Key.GraveAccent => ImGuiNET.ImGuiKey.GraveAccent,
            Silk.NET.Input.Key.Minus => ImGuiNET.ImGuiKey.Minus,
            Silk.NET.Input.Key.Equal => ImGuiNET.ImGuiKey.Equal,
            Silk.NET.Input.Key.LeftBracket => ImGuiNET.ImGuiKey.LeftBracket,
            Silk.NET.Input.Key.RightBracket => ImGuiNET.ImGuiKey.RightBracket,
            Silk.NET.Input.Key.Semicolon => ImGuiNET.ImGuiKey.Semicolon,
            Silk.NET.Input.Key.Apostrophe => ImGuiNET.ImGuiKey.Apostrophe,
            Silk.NET.Input.Key.Comma => ImGuiNET.ImGuiKey.Comma,
            Silk.NET.Input.Key.Period => ImGuiNET.ImGuiKey.Period,
            Silk.NET.Input.Key.Slash => ImGuiNET.ImGuiKey.Slash,
            Silk.NET.Input.Key.BackSlash => ImGuiNET.ImGuiKey.Backslash,
            _ => ImGuiNET.ImGuiKey.None
        };

        return result != ImGuiNET.ImGuiKey.None;
    }

    #endregion
}