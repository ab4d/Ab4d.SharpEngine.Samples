using System;
using System.Numerics;
using System.Reflection;
using System.Xml;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.OverlayPanels;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;

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

    public ManualInputEventsManager? InputEventsManager { get; private set; }

    public abstract string Title { get; }
    public virtual string? Subtitle { get; }

    public bool IsDisposed { get; private set; }

    public PointerAndKeyboardConditions RotateCameraConditions { get; set; } = PointerAndKeyboardConditions.LeftPointerButtonPressed;
    public PointerAndKeyboardConditions MoveCameraConditions { get; set; } = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey;
    public PointerAndKeyboardConditions QuickZoomConditions { get; set; } = PointerAndKeyboardConditions.Disabled;
    public bool RotateAroundPointerPosition { get; set; }
    public CameraZoomMode ZoomMode { get; set; } = CameraZoomMode.CameraRotationCenterPosition;
    public bool IsPointerWheelZoomEnabled { get; set; } = true;

    private bool _showCameraAxisPanel;

    /// <summary>
    /// Gets or sets a Boolean that specifies if CameraAxisPanel is created for this sample.
    /// If this is set before SceneView is created, then CameraAxisPanel will be created when SceneView is initialized (before calling <see cref="OnSceneViewInitialized"/>).
    /// The created CameraAxisPanel is set to <see cref="CameraAxisPanel"/> property.
    /// If SceneView was already initialized when this property is set, then the CameraAxisPanel will be created (or disposed when set to false) immediately.
    /// </summary>
    public bool ShowCameraAxisPanel
    {
        get => _showCameraAxisPanel;
        set
        {
            if (!value && CameraAxisPanel != null)
            {
                CameraAxisPanel.Dispose(); // Remove it from SceneView
                CameraAxisPanel = null;
            }

            // If SceneView is already initialized, then we can create the CameraAxisPanel here
            if (value && SceneView != null && SceneView.Camera != null)
                CameraAxisPanel = CreateCameraAxisPanel(SceneView.Camera);

            _showCameraAxisPanel = value; // If SceneView is not yet initialized, then we will create CameraAxisPanel in InitializeSceneView
        }
    }

    public CameraAxisPanel? CameraAxisPanel { get; private set; }

    
    protected CommonSample(ICommonSamplesContext context)
    {
        this.context = context;
    }

    public static List<XmlNode> LoadSamples(string samplesXmlFilePath, string uiFramework, Action<string>? showErrorAction)
    {
        var filteredXmlNodeList = new List<XmlNode>();

        var xmlDcoument = new XmlDocument();
        xmlDcoument.Load(samplesXmlFilePath);

        if (xmlDcoument.DocumentElement == null)
        {
            showErrorAction?.Invoke("Cannot load Samples.xml");
            return filteredXmlNodeList;
        }


        var xmlNodeList = xmlDcoument.DocumentElement.SelectNodes("/Samples/Sample");

        if (xmlNodeList == null || xmlNodeList.Count == 0)
        {
            showErrorAction?.Invoke("No samples in Samples.xml");
            return filteredXmlNodeList;
        }
        

        foreach (XmlNode xmlNode in xmlNodeList)
        {
            if (xmlNode.Attributes != null)
            {
                var conditionAttribute = xmlNode.Attributes["Condition"];

                if (conditionAttribute != null)
                {
                    var AllConditionsText = conditionAttribute.Value;

                    var allConditions = AllConditionsText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                    bool isIncluded = false;
                    bool isSkipped = false;

                    foreach (var oneCondition in allConditions)
                    {
                        if (!oneCondition.StartsWith("Is", StringComparison.OrdinalIgnoreCase))
                            throw new Exception("Invalid Condition in Samples.xml: " + oneCondition);

                        bool negateCondition = oneCondition.Contains("Not", StringComparison.OrdinalIgnoreCase);
                        int uiFrameworkPosition = negateCondition ? 5 : 2; // Skip "IsNot" or "Is"

                        string uiFrameworkInCondition = oneCondition.Substring(uiFrameworkPosition);

                        if (negateCondition)
                        {
                            if (uiFramework == uiFrameworkInCondition)
                                isSkipped = true; // for example, skip Wpf if condition is IsNotWpf
                        }
                        else
                        {
                            if (uiFramework == uiFrameworkInCondition)
                                isIncluded = true; // for example, only include this sample when condition IsWpf and uiFramework is Wpf
                        }
                    }

                    if (isSkipped || !isIncluded)
                        continue;
                }
            }

            filteredXmlNodeList.Add(xmlNode);
        }

        return filteredXmlNodeList;
    }

    public static object? CreateSampleObject(string uiFramework, string sampleLocation, object commonSamplesContext, Action<string>? showErrorAction)
    {
        sampleLocation = sampleLocation.Replace("{uiFramework}", uiFramework);

        string assemblyName = uiFramework;
        if (assemblyName == "Avalonia")
            assemblyName += "UI"; // Assembly name has AvaloniaUI and not just Avalonia

        // Try to create common sample type from page attribute
        var sampleType = Type.GetType($"Ab4d.SharpEngine.Samples.{assemblyName}.{sampleLocation}, Ab4d.SharpEngine.Samples.{assemblyName}", throwOnError: false);

        if (sampleType == null)
            sampleType = Type.GetType($"Ab4d.SharpEngine.Samples.Common.{sampleLocation}, Ab4d.SharpEngine.Samples.Common", throwOnError: false);

        if (sampleType == null)
        {
            showErrorAction?.Invoke("Sample not found: " + Environment.NewLine + sampleLocation);
            return null;
        }

        var constructors = sampleType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

        // Try to find a constructor that takes ICommonSamplesContext, else use constructor without any parameters
        ConstructorInfo? selectedConstructorInfo = null;
        bool isCommonSampleType = false;

        foreach (var constructorInfo in constructors)
        {
            var parameterInfos = constructorInfo.GetParameters();

            // First try to get constructor that takes ICommonSamplesContext
            if (parameterInfos.Any(p => p.ParameterType == typeof(ICommonSamplesContext)))
            {
                selectedConstructorInfo = constructorInfo;
                isCommonSampleType = true;
                break;
            }

            // ... else use constructor without any parameters
            if (selectedConstructorInfo == null && parameterInfos.Length == 0)
            {
                selectedConstructorInfo = constructorInfo;
                isCommonSampleType = false;
            }
        }

        if (selectedConstructorInfo == null)
        {
            showErrorAction?.Invoke("No constructor without parameters or with ICommonSamplesContext found for the sample:" + Environment.NewLine + sampleLocation);
            return null;
        }


        object createdSample;

        if (isCommonSampleType)
            createdSample = selectedConstructorInfo.Invoke(new object?[] { commonSamplesContext });
        else
            createdSample = selectedConstructorInfo.Invoke(null); // Create sample control (calling constructor without parameters)

        return createdSample;
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

        if (ShowCameraAxisPanel && _createdCamera != null)
            CameraAxisPanel = CreateCameraAxisPanel(_createdCamera);

        OnSceneViewInitialized(sceneView);
    }

    public void InitializeInputEventsManager(ManualInputEventsManager inputEventsManager)
    {
        InputEventsManager = inputEventsManager;
        OnInputEventsManagerInitialized(inputEventsManager);
    }

    public virtual CameraAxisPanel CreateCameraAxisPanel(ICamera camera)
    {
        if (SceneView == null)
            throw new InvalidOperationException("Cannot create CameraAxisPanel because SceneView is not yet initialized");

        var cameraAxisPanel = new CameraAxisPanel(SceneView, camera, width: 100, height: 100, adjustSizeByDpiScale: true)
        {
            Position = new Vector2(10, 10),
            Alignment = PositionTypes.BottomLeft
        };

        return cameraAxisPanel;
    }

    private void ResetCameraAxisPanel()
    {
        if (CameraAxisPanel != null)
        {
            CameraAxisPanel.Position = new Vector2(10, 10);
            CameraAxisPanel.Alignment = PositionTypes.BottomLeft;
        }
    }

    protected virtual void OnSceneViewInitialized(SceneView sceneView)
    {
    }

    protected abstract void OnCreateScene(Scene scene);

    /// <summary>
    /// OnInputEventsManagerInitialized can be overridden to initialize the InputEventsManager.
    /// </summary>
    /// <param name="inputEventsManager">ManualInputEventsManager</param>
    protected virtual void OnInputEventsManagerInitialized(ManualInputEventsManager inputEventsManager)
    {
    }

    protected virtual void OnCreateLights(Scene scene)
    {
        int lightsCount = scene.Lights.Count(l => l is not AmbientLight && l is not CameraLight);
        if (lightsCount > 0) // In case any light is created in OnCreateScene, then do not change that in OnCreateLights
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

    public virtual void InitializePointerCameraController(ManualPointerCameraController pointerCameraController)
    {
        pointerCameraController.RotateCameraConditions      = this.RotateCameraConditions;
        pointerCameraController.MoveCameraConditions        = this.MoveCameraConditions;
        pointerCameraController.QuickZoomConditions         = this.QuickZoomConditions;
        pointerCameraController.RotateAroundPointerPosition = this.RotateAroundPointerPosition;
        pointerCameraController.ZoomMode                    = this.ZoomMode;
        pointerCameraController.IsPointerWheelZoomEnabled   = this.IsPointerWheelZoomEnabled;
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
        if (CameraAxisPanel != null)
        {
            CameraAxisPanel.Dispose();
            CameraAxisPanel = null;
        }

        UnsubscribeSceneUpdating();
        SceneView?.RemoveAllSpriteBatches();

        ResetCameraAxisPanel(); // Reset position of CameraAxisPanel - it may be changed, for example in AssimpImporterSample when FileLoad panel is shown in bottom left

        IsDisposed = true;
        OnDisposed();

        if (Scene != null)
            Scene.RootNode.DisposeAllChildren(disposeMeshes: true, disposeMaterials: true, disposeTextures: true);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
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
        var textureImage = TextureLoader.CreateTexture(fileName, gpuDevice, BitmapIO);
        
        return textureImage;
    }

    protected void ShowErrorMessage(string errorMessage)
    {
        ShowErrorMessage(errorMessage, showTimeMs: 0);
    }

    protected void ShowErrorMessage(string errorMessage, int showTimeMs)
    {
        if (_errorMessageLabel == null)
        {
            if (_uiProvider != null)
            {
                _errorMessagePanel = _uiProvider.CreateStackPanel(PositionTypes.Center, addBorder: true, isSemiTransparent: true);
                _errorMessageLabel = _uiProvider.CreateLabel(errorMessage, maxWidth: 600).SetColor(Colors.Red).SetStyle("bold");
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