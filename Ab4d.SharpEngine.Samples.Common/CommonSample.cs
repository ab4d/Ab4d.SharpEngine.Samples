using System;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common;

public abstract class CommonSample
{
    protected ICommonSamplesContext context;

    protected VulkanDevice? GpuDevice => context.GpuDevice;

    protected IBitmapIO BitmapIO => context.BitmapIO;

    private Camera? _createdCamera;
    protected TargetPositionCamera? targetPositionCamera;

    private ICommonSampleUIProvider? _uiProvider;
    private ICommonSampleUIElement? _errorMessageLabel;
    private ICommonSampleUIPanel? _errorMessagePanel;
    private string? _errorMessageToShow;
    private DateTime _timeToHideErrorMessage;

    public Scene? Scene { get; private set; }
    public SceneView? SceneView { get; private set; }

    public abstract string Title { get; }
    public virtual string? Subtitle { get; }

    public bool IsDisposed { get; private set; }


    public MouseAndKeyboardConditions RotateCameraConditions { get; set; } = MouseAndKeyboardConditions.LeftMouseButtonPressed;
    public MouseAndKeyboardConditions MoveCameraConditions { get; set; } = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.ControlKey;
    public MouseAndKeyboardConditions QuickZoomConditions { get; set; } = MouseAndKeyboardConditions.Disabled;
    public bool RotateAroundMousePosition { get; set; }
    public CameraZoomMode ZoomMode { get; set; } = CameraZoomMode.CameraRotationCenterPosition;


    protected CommonSample(ICommonSamplesContext context)
    {
        this.context = context;
    }

    public void InitializeSharpEngineView(ISharpEngineSceneView sharpEngineView)
    {
        if (sharpEngineView.GpuDevice == null)
            sharpEngineView.Initialize();

        InitializeScene(sharpEngineView.Scene);
        InitializeSceneView(sharpEngineView.SceneView);
    }

    public void InitializeScene(Scene scene)
    {
        Scene = scene;

        _createdCamera = OnCreateCamera();
        targetPositionCamera = _createdCamera as TargetPositionCamera;

        OnCreateScene(scene);
        OnCreateLights(scene);
    }
    
    public void InitializeSceneView(SceneView sceneView)
    {
        SceneView = sceneView;

        sceneView.Camera = _createdCamera;

        OnSceneViewInitialized(sceneView);
    }

    protected virtual void OnSceneViewInitialized(SceneView sceneView)
    {
    }

    protected abstract void OnCreateScene(Scene scene);

    protected virtual void OnCreateLights(Scene scene)
    {
        if (scene.Lights.Count > 0) // In case any light is created in OnCreateScene, then do not change that in OnCreateLights
            return;

        // Set ambient light (illuminates the objects from all directions)
        scene.SetAmbientLight(intensity: 0.3f);

        // We could also add AmbientLight manually:
        //scene.Lights.Add(new AmbientLight(0.3f));
        //scene.Lights.Add(new AmbientLight(new Color3(0.3f, 0.3f, 0.3f)));
    }

    protected virtual Camera OnCreateCamera()
    {
        var defaultTargetPositionCamera = new TargetPositionCamera()
        {
            Heading         = -40,
            Attitude        = -25,
            Distance        = 1500,
            ViewWidth       = 1000,
            TargetPosition  = new Vector3(0, 0, 0),
            ShowCameraLight = ShowCameraLightType.Auto
        };

        return defaultTargetPositionCamera;
    }

    public void CreateUI(ICommonSampleUIProvider uiProvider)
    {
        _uiProvider = uiProvider;

        uiProvider.ClearAll();

        OnCreateUI(uiProvider);

        if (_errorMessageToShow != null)
        {
            ShowErrorMessage(_errorMessageToShow);
            _errorMessageToShow = null;
        }
    }

    protected virtual void OnCreateUI(ICommonSampleUIProvider ui)
    {

    }


    public void ProcessFileDropped(string fileName)
    {
        OnFileDropped(fileName);
    }

    protected virtual void OnFileDropped(string fileName)
    {
    }

    public virtual void Update()
    {
    }

    public void Dispose()
    {
        UnsubscribeSceneUpdating();
        SceneView?.RemoveAllSpriteBatches();

        IsDisposed = true;
        OnDisposed();
    }

    protected virtual void OnDisposed()
    {

    }

    public GpuImage? GetCommonTexture(VulkanDevice? gpuDevice, string textureName)
    {
        // TODO: Add caching

        if (gpuDevice == null)
            return null; 

        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Textures", textureName);
        var textureImage = TextureLoader.CreateTexture(fileName, BitmapIO, gpuDevice);
        
        return textureImage;
    }

    protected void ShowErrorMessage(string errorMessage, int showTimeMs = 0)
    {
        if (_errorMessageLabel == null)
        {
            if (_uiProvider != null)
            {
                _errorMessagePanel = _uiProvider.CreateStackPanel(PositionTypes.Center, addBorder: false, isSemiTransparent: true);
                _errorMessageLabel = _uiProvider.CreateLabel(errorMessage).SetColor(Colors.Red);
                _uiProvider.SetCurrentPanel(null);
            }
        }

        if (_errorMessagePanel != null)
        {
            _errorMessagePanel.SetIsVisible(true);

            if (_errorMessageLabel != null)
                _errorMessageLabel.SetText(errorMessage);
        }
        else
        {
            _errorMessageToShow = errorMessage;
        }

        if (showTimeMs > 0)
            SubscribeSceneUpdating(DateTime.Now.AddMilliseconds(showTimeMs));
    }

    private void SceneViewOnSceneUpdating(object? sender, EventArgs e)
    {
        bool unsubscribe = false;

        if (_timeToHideErrorMessage == DateTime.MinValue)
        {
            unsubscribe = true;
        }
        else if (DateTime.Now >= _timeToHideErrorMessage)
        {
            _errorMessagePanel?.SetIsVisible(false); // Hide error message
            unsubscribe = true;
        }

        if (unsubscribe)
            UnsubscribeSceneUpdating(); 
    }

    private void SubscribeSceneUpdating(DateTime timeToHideErrorMessage)
    {
        if (SceneView == null || timeToHideErrorMessage < DateTime.Now)
            return;

        if (_timeToHideErrorMessage == DateTime.MinValue) // not yet subscribed?
            SceneView.SceneUpdating += SceneViewOnSceneUpdating;

        _timeToHideErrorMessage = timeToHideErrorMessage;
    }

    private void UnsubscribeSceneUpdating()
    {
        if (SceneView == null || _timeToHideErrorMessage == DateTime.MinValue)
            return;

        SceneView.SceneUpdating -= SceneViewOnSceneUpdating;
        _timeToHideErrorMessage = DateTime.MinValue;
    }


    protected void ClearErrorMessage()
    {
        if (_errorMessagePanel != null)
            _errorMessagePanel.SetIsVisible(false);

        _errorMessageToShow = null;
    }

    #region GetRandom... methods
    private Random _rnd = new Random();

    private const int CommonRandomSeed = 1234;

    private bool _useCommonRandomSeed;

    /// <summary>
    /// When true then the random generator use common seed (1234) so that the list of generated random numbers is always the same.
    /// This can be used to make the tests that use random numbers reproducible. 
    /// </summary>
    public bool UseCommonRandomSeed
    {
        get => _useCommonRandomSeed;
        set
        {
            _useCommonRandomSeed = value;
            if (value)
                _rnd = new Random(CommonRandomSeed);
            else
                _rnd = new Random();
        }
    }

    /// <summary>
    /// Recreates the random number generator. This method is called when the BeginTest is called.
    /// </summary>
    public void ResetRandomNumbersGenerator()
    {
        if (_useCommonRandomSeed)
            _rnd = new Random(CommonRandomSeed);
    }

    public double GetRandomDouble()
    {
        return _rnd.NextDouble();
    }

    public int GetRandomInt(int maxValue)
    {
        return (int)(GetRandomDouble() * maxValue);
    }

    public float GetRandomFloat()
    {
        return (float)GetRandomDouble();
    }

    public Vector3 GetRandomPosition(Vector3 centerPosition, Vector3 areaSize)
    {
        var randomPosition = new Vector3((GetRandomFloat() - 0.5f) * areaSize.X + centerPosition.X,
                                         (GetRandomFloat() - 0.5f) * areaSize.Y + centerPosition.Y,
                                         (GetRandomFloat() - 0.5f) * areaSize.Z + centerPosition.Z);

        return randomPosition;
    }

    public Vector3 GetRandomDirection()
    {
        var randomVector = new Vector3(GetRandomFloat() * 2 - 1, GetRandomFloat() * 2 - 1, GetRandomFloat() * 2 - 1);
        randomVector = Vector3.Normalize(randomVector);
        return randomVector;
    }

    /// <summary>
    /// Gets random color using HSV so that the color is never dark or bright.
    /// </summary>
    /// <returns>Color3</returns>
    public Color3 GetRandomHsvColor3(float saturation = 1f, float brightness = 1f)
    {
        var randomColor = Color3.FromHsv(GetRandomFloat() * 360f, saturation, brightness);
        return randomColor;
    }

    /// <summary>
    /// Gets random color using HSV so that the color is never dark or bright.
    /// </summary>
    /// <returns>Color4</returns>
    public Color4 GetRandomHsvColor4(float saturation = 1f, float brightness = 1f)
    {
        var randomColor = Color4.FromHsv(GetRandomFloat() * 360f, saturation, brightness);
        return randomColor;
    }

    public Color3 GetRandomColor3()
    {
        var randomColor = new Color3(GetRandomFloat(), GetRandomFloat(), GetRandomFloat());
        return randomColor;
    }

    public Color4 GetRandomColor4()
    {
        var randomColor = new Color4(GetRandomFloat(), GetRandomFloat(), GetRandomFloat(), GetRandomFloat());
        return randomColor;
    }
    #endregion
}