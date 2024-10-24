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

        _window.Render += (double obj) =>
        {
            _sceneView?.Render();
        };

        _window.FramebufferResize += (Vector2D<int> size) =>
        {
            _sceneView?.Resize(renderNextFrameAfterResize: true);
        };

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

        var engineCreateOptions = new EngineCreateOptions(applicationName: "SharpEngine.Samples.ImGui", enableStandardValidation: true)
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
            MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,
            QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed,

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

        // We need to call GetTexDataAsRGBA32 before first call to ImGui.NewFrame.
        // The data is also retrieved by ImGuiRenderingStep implementation; here, we only need to initialize it.
        _imGuiIo.Fonts.GetTexDataAsRGBA32(out IntPtr _, out _, out _, out _);
        _imGuiIo.Fonts.SetTexID(1); // NOTE: font texture ID is also explicitly set by ImGuiRenderingStep implementation.

        // Create and register custom rendering step
        _imGuiRenderingStep = new ImGuiRenderingStep(_sceneView, _imGuiIo, "ImGuiRenderingStep");
        Debug.Assert(_sceneView.DefaultRenderObjectsRenderingStep != null, "_sceneView.DefaultRenderObjectsRenderingStep != null");
        _sceneView.RenderingSteps.AddAfter(_sceneView.DefaultRenderObjectsRenderingStep, _imGuiRenderingStep);

        // This allows UI to be animated
        _sceneView.SceneUpdating += (object? sender, EventArgs args) =>
        {
            UpdateImgUiInterface();
        };
    }

    private static void UpdateImgUiInterface ()
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

    private static void OnKeyboardKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
    }

    private static void OnKeyboardKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        // Pressing ESC key exits the application
        if (key == Key.Escape && _window != null)
            _window.Close();
    }

    private static void OnMouseMove(IMouse mouse, Vector2 position)
    {
        _pointerCameraController?.ProcessPointerMoved(position, GetPressedMouseButtons(), KeyboardModifiers.None);
    }

    private static void OnMouseButtonDown(IMouse mouse, MouseButton button)
    {
        _pointerCameraController?.ProcessPointerPressed(mouse.Position, MapMouseButton(button), GetKeyboardModifiers());
    }

    private static void OnMouseButtonUp(IMouse mouse, MouseButton button)
    {
        _pointerCameraController?.ProcessPointerPressed(mouse.Position, MapMouseButton(button), GetKeyboardModifiers());
    }

    private static void OnMouseScroll(IMouse mouse, ScrollWheel wheel)
    {
        _pointerCameraController?.ProcessPointerWheelChanged(mouse.Position, wheel.Y);
    }


    private static KeyboardModifiers GetKeyboardModifiers()
    {
        if (_keyboard == null)
            return KeyboardModifiers.None;

        return ((_keyboard.IsKeyPressed(Key.ShiftLeft) || _keyboard.IsKeyPressed(Key.ShiftRight))  ? KeyboardModifiers.ShiftKey : 0) |
               ((_keyboard.IsKeyPressed(Key.ControlLeft) || _keyboard.IsKeyPressed(Key.ControlRight))  ? KeyboardModifiers.ControlKey : 0) |
               ((_keyboard.IsKeyPressed(Key.AltLeft) || _keyboard.IsKeyPressed(Key.AltRight))  ? KeyboardModifiers.AltKey : 0) |
               ((_keyboard.IsKeyPressed(Key.SuperLeft) || _keyboard.IsKeyPressed(Key.ShiftRight))  ? KeyboardModifiers.SuperKey : 0);
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

    private static PointerButtons MapMouseButton(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => PointerButtons.Left,
            MouseButton.Right => PointerButtons.Right,
            MouseButton.Middle => PointerButtons.Middle,
            MouseButton.Button4 => PointerButtons.XButton1,
            MouseButton.Button5 => PointerButtons.XButton2,
            _ => PointerButtons.None
        };
    }

    #endregion
}
