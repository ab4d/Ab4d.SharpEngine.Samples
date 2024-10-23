using System.Numerics;
using Ab4d.SharpEngine.Common;

namespace Ab4d.SharpEngine.Samples.ImGui;

public class ImGuiRenderer : IDisposable
{
    private readonly SceneView _sceneView;
    private ImGuiNET.ImGuiIOPtr _io;

    private readonly ImGuiRenderingStep _imGuiRenderingStep;
    private readonly bool _isImGuiRenderingStepCreatedHere;

    private DateTime _previousFrameTime;

    /// <summary>
    /// SubmitUI is called on each update. There the user should generate the ImGui interface by calling ImGui methods.
    /// The function should return true to render the new ImGui or false to skip rendering a new ImGui frame.
    /// Usually true should be returned because the UI can change based on the mouse and keyboard events.
    /// </summary>
    public Func<bool>? SubmitUI { get; set; }

    /// <summary>
    /// Constructor with specified Func where the user should generate the ImGui interface by calling ImGiu methods.
    /// The function should return true to render the new ImGui or false to skip rendering a new ImGui frame.
    /// Usually true should be returned because the UI can change based on the mouse and keyboard events.
    /// </summary>
    /// <param name="sceneView">parent SceneView</param>
    /// <param name="submitUI">Func where the user should generate the ImGui interface by calling ImGiu methods.</param>
    public ImGuiRenderer(SceneView sceneView, Func<bool> submitUI)
        : this(sceneView)
    {
        SubmitUI = submitUI;
    }

    /// <summary>
    /// Constructor with specified Func where the user should generate the ImGui interface by calling ImGiu methods.
    /// The function should return true to render the new ImGui or false to skip rendering a new ImGui frame.
    /// Usually true should be returned because the UI can change based on the mouse and keyboard events.
    /// </summary>
    /// <param name="sceneView">parent SceneView</param>
    /// <param name="imGuiRenderingStep">optional ImGuiRenderingStep; when not set, then ImGuiRenderer creates a new ImGuiRenderingStep and adds it to the SceneView</param>
    /// <param name="submitUI">Func where the user should generate the ImGui interface by calling ImGiu methods.</param>
    public ImGuiRenderer(SceneView sceneView, ImGuiRenderingStep? imGuiRenderingStep, Func<bool> submitUI)
        : this(sceneView, imGuiRenderingStep)
    {
        SubmitUI = submitUI;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="sceneView">parent SceneView</param>
    /// <param name="imGuiRenderingStep">optional ImGuiRenderingStep; when not set, then ImGuiRenderer creates a new ImGuiRenderingStep and adds it to the SceneView</param>
    public ImGuiRenderer(SceneView sceneView, ImGuiRenderingStep? imGuiRenderingStep = null)
    {
        _sceneView = sceneView;

        // Create ImGui context
        ImGuiNET.ImGui.CreateContext();

        // Initialize the I/O object.
        _io = ImGuiNET.ImGui.GetIO();
        _io.BackendFlags |= ImGuiNET.ImGuiBackendFlags.RendererHasVtxOffset;
        _io.ConfigFlags |= ImGuiNET.ImGuiConfigFlags.NavEnableKeyboard |
                           ImGuiNET.ImGuiConfigFlags.DockingEnable;
        _io.Fonts.Flags |= ImGuiNET.ImFontAtlasFlags.NoBakedLines;

        _io.DisplaySize = new Vector2(sceneView.Width, sceneView.Height);
        _io.DeltaTime = 1.0f / 60.0f;
        _previousFrameTime = DateTime.Now;

        // We need to call GetTexDataAsRGBA32 before first call to ImGui.NewFrame.
        // The data is also retrieved by ImGuiRenderingStep implementation; here, we only need to initialize it.
        _io.Fonts.GetTexDataAsRGBA32(out IntPtr _, out _, out _, out _);
        _io.Fonts.SetTexID(1); // NOTE: font texture ID is also explicitly set by ImGuiRenderingStep implementation.

        if (imGuiRenderingStep != null)
        {
            _imGuiRenderingStep = imGuiRenderingStep;
        }
        else
        {
            _imGuiRenderingStep = new ImGuiRenderingStep(sceneView, _io, "ImGuiRenderingStep");

            if (sceneView.DefaultRenderObjectsRenderingStep == null)
                throw new SharpEngineException("SceneView.DefaultRenderObjectsRenderingStep is null");

            sceneView.RenderingSteps.AddAfter(sceneView.DefaultRenderObjectsRenderingStep, _imGuiRenderingStep);
            _isImGuiRenderingStepCreatedHere = true;
        }

        // This allows UI to be animated
        _sceneView.SceneUpdating += OnSceneViewOnSceneUpdating;
    }

    private void OnSceneViewOnSceneUpdating(object? sender, EventArgs args)
    {
        Update();
    }

    public void Dispose()
    {
        _sceneView.SceneUpdating -= OnSceneViewOnSceneUpdating;

        if (_isImGuiRenderingStepCreatedHere)
        {
            _sceneView.RenderingSteps.Remove(_imGuiRenderingStep);
            _imGuiRenderingStep.Dispose();
        }
    }

    public void Update()
    {
        if (SubmitUI == null)
            return;

        var now = DateTime.Now;
        _io.DeltaTime = (float)(now - _previousFrameTime).TotalSeconds;

        _previousFrameTime = now;

        // Check if display size changed
        var displaySize = new Vector2(_sceneView.Width, _sceneView.Height);
        if (displaySize != _io.DisplaySize)
            _io.DisplaySize = displaySize;

        ImGuiNET.ImGui.NewFrame();

        // UI handling provided by child implementation
        var shouldUpdate = SubmitUI();

        ImGuiNET.ImGui.Render();
        ImGuiNET.ImGui.EndFrame();

        if (shouldUpdate)
        {
            _sceneView.NotifyChange(SceneViewDirtyFlags.SpritesChanged);
        }
    }
};