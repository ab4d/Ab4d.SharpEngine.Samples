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
    private static IWindow? _window;
    private static IKeyboard? _keyboard;
    private static IMouse? _mouse;

    private static VulkanDevice? _vulkanDevice;
    private static VulkanSurfaceProvider? _vulkanSurfaceProvider;

    private static Scene? _scene;
    private static SceneView? _sceneView;
    private static ManualPointerCameraController? _pointerCameraController;

    private static ImGuiRenderer? _imGuiRenderer;

    private static bool ShowDemoWindow = true;
    private static bool ShowOtherWindow = true;

    private static void Main(string[] args)
    {
        var options = WindowOptions.DefaultVulkan;
        options.Size = new Vector2D<int>(800, 600);
        options.Title = "Silk.NET + SharpEngine + ImGui";

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Update += OnUpdate;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Closing += OnClose;

        _window.Initialize();

        _window.Run();

        _window.Dispose();
    }
    static unsafe void OnLoad()
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

        // Set up SharpEngine
        _vulkanSurfaceProvider = new CustomVulkanSurfaceProvider(instance =>
            {
                Debug.Assert(_window.VkSurface != null, "_window.VkSurface != null");
                var vkNonDispatchableHandle = _window.VkSurface.Create(new VkHandle(instance.Handle), (byte*)null);
                return new SurfaceKHR(vkNonDispatchableHandle.Handle);
            },
            addDefaultSurfaceExtensions: true);

        var engineCreateOptions = new EngineCreateOptions(applicationName: "SharpEngine.Samples.ImGui", enableStandardValidation: true)
        {
            EnableSurfaceSupport = true,
        };

        _vulkanDevice = VulkanDevice.Create(defaultSurfaceProvider: _vulkanSurfaceProvider, engineCreateOptions);

        // Set up test scene
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

        // Add ImGui renderer to the scene view
        _imGuiRenderer = new ImGuiRenderer(_sceneView, () =>
        {
            if (ShowOtherWindow)
            {
                ImGuiNET.ImGui.Begin("Another window", ref ShowOtherWindow);
                ImGuiNET.ImGui.Text("Some text");
                ImGuiNET.ImGui.End();
            }

            if (ShowDemoWindow)
            {
                ImGuiNET.ImGui.ShowDemoWindow(ref ShowDemoWindow);
            }

            return true; // Always update
        });
    }

    private static void OnRender(double obj)
    {
        if (_scene != null && _sceneView != null)
            _sceneView.Render();
    }

    private static void OnUpdate(double obj)
    {
    }

    private static void OnFramebufferResize(Vector2D<int> newSize)
    {
        _sceneView?.Resize(renderNextFrameAfterResize: true);
    }

    private static void OnClose()
    {
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
