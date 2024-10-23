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

    private static VulkanDevice? _vulkanDevice;
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
        // Create renderer
        Console.WriteLine("OnLoad!");
        
        // Set-up input context on all available keyboards.
        var input = _window.CreateInput();
        foreach (var keyboard in input.Keyboards)
        {
            keyboard.KeyDown += OnKeyboardKeyDown;
        }

        // Set up SharpEngine and its scene
        var _vulkanSurfaceProvider = new CustomVulkanSurfaceProvider(instance =>
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

    static void OnRender(double obj)
    {
        Console.WriteLine("OnRender!");
        if (_scene != null && _sceneView != null)
            _sceneView.Render();
    }

    static void OnUpdate(double obj)
    {
        Console.WriteLine("OnUpdate!");
    }

    static void OnFramebufferResize(Vector2D<int> newSize)
    {
        Console.WriteLine("OnFramebufferResize!");
        if (_sceneView != null)
            _sceneView.Resize(renderNextFrameAfterResize: true);
    }

    static void OnClose()
    {
        Console.WriteLine("OnClose!");
    }

    private static void OnKeyboardKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
        {
            _window!.Close();
        }
    }
}