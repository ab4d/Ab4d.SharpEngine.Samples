using System.Diagnostics;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Ab4d.SharpEngine.Samples.ImGui;

internal class Program
{
    // Silk
    private static IWindow? _window;
    private static IKeyboard? _keyboard;
    private static IMouse? _mouse;

    // SharpEngine
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

    private static void Main(string[] args)
    {
        // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
        // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase
        Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
            licenseType: "SamplesLicense",
            platforms: "All",
            license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");

        // Create ImGui context
        _imGuiCtx = ImGuiNET.ImGui.CreateContext();

        _imGuiIo = ImGuiNET.ImGui.GetIO();
        _imGuiIo.BackendFlags |= ImGuiNET.ImGuiBackendFlags.RendererHasVtxOffset;
        _imGuiIo.ConfigFlags |= ImGuiNET.ImGuiConfigFlags.NavEnableKeyboard |
                                ImGuiNET.ImGuiConfigFlags.DockingEnable;
        _imGuiIo.Fonts.Flags |= ImGuiNET.ImFontAtlasFlags.NoBakedLines;

        // Create window using Silk; setup is performed once window is created/loaded.
        var options = WindowOptions.DefaultVulkan;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "Silk.NET + SharpEngine + ImGui";

        _window = Window.Create(options);

        _window.Load += () =>
        {
            SetupInput();
            SetupSharpEngine();
            CreateScene();
            SetupImGui();
        };

        _window.Render += (double obj) => { _sceneView?.Render(); };

        _window.FramebufferResize += (Vector2D<int> size) => { _sceneView?.Resize(renderNextFrameAfterResize: true); };

        _window.Initialize();

        _window.Run();

        // TODO: clean up everything
        _window.Dispose();

        ImGuiNET.ImGui.DestroyContext(_imGuiCtx);
    }

    private static void SetupInput()
    {
        // Register input events on fist available keyboard and mouse
        Debug.Assert(_window != null, nameof(_window) + " != null");
        var input = _window.CreateInput();
        var keyboards = input.Keyboards;
        if (keyboards.Count > 0)
        {
            _keyboard = keyboards[0];
            _keyboard.KeyDown += OnKeyboardKeyDown;
            _keyboard.KeyUp += OnKeyboardKeyUp;
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
                var surfaceHandle = _window.VkSurface.Create(new VkHandle(instance.Handle), (byte*)null);
                return new SurfaceKHR(surfaceHandle.Handle);
            },
            addDefaultSurfaceExtensions: true);

        var engineCreateOptions =
            new EngineCreateOptions(applicationName: "SharpEngine.Samples.ImGui", enableStandardValidation: true)
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

        // Create test mesh
        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -0.1f, 0),
            Size = new Vector2(200, 200),
        };

        _scene.RootNode.Add(wireGridNode);

        // Create scene view
        _sceneView = new SceneView(_scene);
        _sceneView.BackgroundColor = Color4.White;
        Debug.Assert(_vulkanSurfaceProvider != null, nameof(_vulkanSurfaceProvider) + " != null");
        _sceneView.Initialize(_vulkanSurfaceProvider, dpiScaleX: 1.0f, dpiScaleY: 1.0f);

        // Camera
        _sceneView.Camera = new TargetPositionCamera()
        {
            Heading = -40,
            Attitude = -25,
            Distance = 300,
            TargetPosition = new Vector3(0, 0, 0),
            ShowCameraLight = ShowCameraLightType.Auto
        };

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
        _imGuiIo.DisplaySize = new Vector2(_sceneView.Width, _sceneView.Height);
        _imGuiIo.DeltaTime = 0;

        _previousFrameTime = DateTime.Now;

        // Create and register custom rendering step
        _imGuiRenderingStep = new ImGuiRenderingStep(_sceneView, _imGuiCtx, "ImGuiRenderingStep");
        Debug.Assert(_sceneView.DefaultRenderObjectsRenderingStep != null,
            "_sceneView.DefaultRenderObjectsRenderingStep != null");
        _sceneView.RenderingSteps.AddAfter(_sceneView.DefaultRenderObjectsRenderingStep, _imGuiRenderingStep);

        // This allows UI to be animated
        _sceneView.SceneUpdating += (object? sender, EventArgs args) => { UpdateImgUiInterface(); };
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
        _imGuiIo.DisplaySize = new Vector2(_sceneView.Width, _sceneView.Height);

        // Begin frame
        ImGuiNET.ImGui.NewFrame();

        if (_showOtherWindow)
        {
            ImGuiNET.ImGui.Begin("Another window", ref _showOtherWindow);
            ImGuiNET.ImGui.Text("Some text");
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
    }

    #region Input

    private static void OnKeyboardKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (MapKeyImGui(key, out var mappedKey))
            _imGuiIo.AddKeyEvent(mappedKey, true);

        if (_imGuiIo.WantCaptureKeyboard)
            return;

        // Pressing ESC key exits the application
        if (key == Key.Escape && _window != null)
            _window.Close();
    }

    private static void OnKeyboardKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        if (MapKeyImGui(key, out var mappedKey))
            _imGuiIo.AddKeyEvent(mappedKey, false);
    }

    private static void OnMouseMove(IMouse mouse, Vector2 position)
    {
        _imGuiIo.AddMousePosEvent(mouse.Position.X, mouse.Position.Y);

        if (_imGuiIo.WantCaptureMouse)
            return;

        _pointerCameraController?.ProcessPointerMoved(position, GetPressedMouseButtons(), KeyboardModifiers.None);
    }

    private static void OnMouseButtonDown(IMouse mouse, MouseButton button)
    {
        if (MapMouseButtonImGui(button, out var mappedButton))
            _imGuiIo.AddMouseButtonEvent(mappedButton, true);

        if (_imGuiIo.WantCaptureMouse)
            return;

        if (MapMouseButtonToSharpEngine(button, out var mappedButtonSharpEngine))
            _pointerCameraController?.ProcessPointerPressed(mouse.Position, mappedButtonSharpEngine, GetKeyboardModifiers());
    }

    private static void OnMouseButtonUp(IMouse mouse, MouseButton button)
    {
        if (MapMouseButtonImGui(button, out var mappedButton))
            _imGuiIo.AddMouseButtonEvent(mappedButton, false);

        if (_imGuiIo.WantCaptureMouse)
            return;

        if (MapMouseButtonToSharpEngine(button, out var mappedButtonSharpEngine))
            _pointerCameraController?.ProcessPointerPressed(mouse.Position, mappedButtonSharpEngine, GetKeyboardModifiers());
    }

    private static void OnMouseScroll(IMouse mouse, ScrollWheel wheel)
    {
        _imGuiIo.AddMouseWheelEvent(0f, wheel.Y);
        if (_imGuiIo.WantCaptureMouse)
            return;

        _pointerCameraController?.ProcessPointerWheelChanged(mouse.Position, wheel.Y);
    }


    private static KeyboardModifiers GetKeyboardModifiers()
    {
        if (_keyboard == null)
            return KeyboardModifiers.None;

        return (_keyboard.IsKeyPressed(Key.ShiftLeft) ? KeyboardModifiers.ShiftKey : 0) |
               (_keyboard.IsKeyPressed(Key.ShiftRight) ? KeyboardModifiers.ShiftKey : 0) |
               (_keyboard.IsKeyPressed(Key.ControlLeft) ? KeyboardModifiers.ControlKey : 0) |
               (_keyboard.IsKeyPressed(Key.ControlRight) ? KeyboardModifiers.ControlKey : 0) |
               (_keyboard.IsKeyPressed(Key.AltLeft) ? KeyboardModifiers.AltKey : 0) |
               (_keyboard.IsKeyPressed(Key.AltRight) ? KeyboardModifiers.AltKey : 0) |
               (_keyboard.IsKeyPressed(Key.SuperLeft) ? KeyboardModifiers.SuperKey : 0) |
               (_keyboard.IsKeyPressed(Key.SuperRight) ? KeyboardModifiers.SuperKey : 0);
    }

    private static PointerButtons GetPressedMouseButtons()
    {
        if (_mouse == null)
            return PointerButtons.None;
        return (_mouse.IsButtonPressed(MouseButton.Left) ? PointerButtons.Left : 0) |
               (_mouse.IsButtonPressed(MouseButton.Middle) ? PointerButtons.Middle : 0) |
               (_mouse.IsButtonPressed(MouseButton.Right) ? PointerButtons.Right : 0) |
               (_mouse.IsButtonPressed(MouseButton.Button4) ? PointerButtons.XButton1 : 0) |
               (_mouse.IsButtonPressed(MouseButton.Button5) ? PointerButtons.XButton2 : 0);
    }

    private static bool MapMouseButtonToSharpEngine(MouseButton button, out PointerButtons result)
    {
        result = button switch
        {
            MouseButton.Left => PointerButtons.Left,
            MouseButton.Right => PointerButtons.Right,
            MouseButton.Middle => PointerButtons.Middle,
            MouseButton.Button4 => PointerButtons.XButton1,
            MouseButton.Button5 => PointerButtons.XButton2,
            _ => PointerButtons.None
        };
        return result != PointerButtons.None;
    }

    private static bool MapMouseButtonImGui(MouseButton button, out int result)
    {
        result = button switch
        {
            MouseButton.Left => 0,
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            MouseButton.Button4 => 3,
            MouseButton.Button5 => 4,
            _ => -1
        };
        return result != -1;
    }

    private static bool MapKeyImGui(Key key, out ImGuiNET.ImGuiKey result)
    {
        ImGuiNET.ImGuiKey KeyToImGuiKeyShortcut(Key keyToConvert, Key startKey1, ImGuiNET.ImGuiKey startKey2)
        {
            var changeFromStart1 = (int)keyToConvert - (int)startKey1;
            return startKey2 + changeFromStart1;
        }

        result = key switch
        {
            >= Key.F1 and <= Key.F24 => KeyToImGuiKeyShortcut(key, Key.F1, ImGuiNET.ImGuiKey.F1),
            >= Key.Keypad0 and <= Key.Keypad9 => KeyToImGuiKeyShortcut(key, Key.Keypad0, ImGuiNET.ImGuiKey.Keypad0),
            >= Key.A and <= Key.Z => KeyToImGuiKeyShortcut(key, Key.A, ImGuiNET.ImGuiKey.A),
            >= Key.Number0 and <= Key.Number9 => KeyToImGuiKeyShortcut(key, Key.Number0, ImGuiNET.ImGuiKey._0),
            Key.ShiftLeft or Key.ShiftRight => ImGuiNET.ImGuiKey.ModShift,
            Key.ControlLeft or Key.ControlRight => ImGuiNET.ImGuiKey.ModCtrl,
            Key.AltLeft or Key.AltRight => ImGuiNET.ImGuiKey.ModAlt,
            Key.SuperLeft or Key.SuperRight => ImGuiNET.ImGuiKey.ModSuper,
            Key.Menu => ImGuiNET.ImGuiKey.Menu,
            Key.Up => ImGuiNET.ImGuiKey.UpArrow,
            Key.Down => ImGuiNET.ImGuiKey.DownArrow,
            Key.Left => ImGuiNET.ImGuiKey.LeftArrow,
            Key.Right => ImGuiNET.ImGuiKey.RightArrow,
            Key.Enter => ImGuiNET.ImGuiKey.Enter,
            Key.Escape => ImGuiNET.ImGuiKey.Escape,
            Key.Space => ImGuiNET.ImGuiKey.Space,
            Key.Tab => ImGuiNET.ImGuiKey.Tab,
            Key.Backspace => ImGuiNET.ImGuiKey.Backspace,
            Key.Insert => ImGuiNET.ImGuiKey.Insert,
            Key.Delete => ImGuiNET.ImGuiKey.Delete,
            Key.PageUp => ImGuiNET.ImGuiKey.PageUp,
            Key.PageDown => ImGuiNET.ImGuiKey.PageDown,
            Key.Home => ImGuiNET.ImGuiKey.Home,
            Key.End => ImGuiNET.ImGuiKey.End,
            Key.CapsLock => ImGuiNET.ImGuiKey.CapsLock,
            Key.ScrollLock => ImGuiNET.ImGuiKey.ScrollLock,
            Key.PrintScreen => ImGuiNET.ImGuiKey.PrintScreen,
            Key.Pause => ImGuiNET.ImGuiKey.Pause,
            Key.NumLock => ImGuiNET.ImGuiKey.NumLock,
            Key.KeypadDivide => ImGuiNET.ImGuiKey.KeypadDivide,
            Key.KeypadMultiply => ImGuiNET.ImGuiKey.KeypadMultiply,
            Key.KeypadSubtract => ImGuiNET.ImGuiKey.KeypadSubtract,
            Key.KeypadAdd => ImGuiNET.ImGuiKey.KeypadAdd,
            Key.KeypadDecimal => ImGuiNET.ImGuiKey.KeypadDecimal,
            Key.KeypadEnter => ImGuiNET.ImGuiKey.KeypadEnter,
            Key.GraveAccent => ImGuiNET.ImGuiKey.GraveAccent,
            Key.Minus => ImGuiNET.ImGuiKey.Minus,
            Key.Equal => ImGuiNET.ImGuiKey.Equal,
            Key.LeftBracket => ImGuiNET.ImGuiKey.LeftBracket,
            Key.RightBracket => ImGuiNET.ImGuiKey.RightBracket,
            Key.Semicolon => ImGuiNET.ImGuiKey.Semicolon,
            Key.Apostrophe => ImGuiNET.ImGuiKey.Apostrophe,
            Key.Comma => ImGuiNET.ImGuiKey.Comma,
            Key.Period => ImGuiNET.ImGuiKey.Period,
            Key.Slash => ImGuiNET.ImGuiKey.Slash,
            Key.BackSlash => ImGuiNET.ImGuiKey.Backslash,
            _ => ImGuiNET.ImGuiKey.None
        };

        return result != ImGuiNET.ImGuiKey.None;
    }

    #endregion
}