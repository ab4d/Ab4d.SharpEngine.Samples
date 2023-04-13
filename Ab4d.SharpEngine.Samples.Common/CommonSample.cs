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
    private Random _rnd = new Random();

    protected ICommonSamplesContext context;

    protected VulkanDevice? GpuDevice => context.GpuDevice;

    protected IBitmapIO BitmapIO => context.BitmapIO;

    private Camera? _createdCamera;
    protected TargetPositionCamera? targetPositionCamera;

    private ICommonSampleUIProvider? _uiProvider;
    private ICommonSampleUIElement? _errorMessageLabel;
    private ICommonSampleUIPanel? _errorMessagePanel;
    private string? _errorMessageToShow;

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

        // Add lights
        scene.Lights.Add(new AmbientLight(new Color3(0.3f, 0.3f, 0.3f)));
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

    protected void ShowErrorMessage(string errorMessage)
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
    }

    protected void ClearErrorMessage()
    {
        if (_errorMessagePanel != null)
            _errorMessagePanel.SetIsVisible(false);

        _errorMessageToShow = null;
    }

    #region GetRandom... methods
    // 100 random numbers that can be used in UnitTest runner to always get the same random number
    // so the test always produces the same rendering.
    // The code to generate this array is below
    private static readonly double[] GeneratedRandomNumbers = new double[] {
            0.113057, 0.429577, 0.320983, 0.777786, 0.377901, 0.218592, 0.226066, 0.245044, 0.322041, 0.472609,
            0.312617, 0.135042, 0.925296, 0.367406, 0.277916, 0.884741, 0.714167, 0.993987, 0.831666, 0.992762,
            0.681360, 0.362502, 0.690220, 0.020323, 0.634811, 0.182818, 0.065965, 0.313507, 0.607723, 0.400958,
            0.942609, 0.317841, 0.283829, 0.332411, 0.854868, 0.643056, 0.257254, 0.012379, 0.507136, 0.893058,
            0.638393, 0.498678, 0.982999, 0.633355, 0.650469, 0.017164, 0.917994, 0.182759, 0.077305, 0.618418,
            0.466133, 0.352135, 0.603332, 0.755276, 0.842538, 0.750556, 0.739357, 0.300661, 0.142975, 0.195083,
            0.152626, 0.912559, 0.637321, 0.921083, 0.530000, 0.994775, 0.851213, 0.592885, 0.512538, 0.634860,
            0.627487, 0.701788, 0.486851, 0.938608, 0.354369, 0.182683, 0.379503, 0.056865, 0.369854, 0.617647,
            0.264824, 0.883206, 0.236202, 0.989305, 0.934825, 0.590475, 0.714509, 0.528553, 0.489874, 0.104313,
            0.903699, 0.956594, 0.869404, 0.312053, 0.740432, 0.725834, 0.861357, 0.061917, 0.103355, 0.655694,
        };

    //private string GenerateRandomNumbersString()
    //{
    //    string randomNumbersText = "private static readonly double[] GeneratedRandomNumbers = new double[] {\r\n";
    //    for (int i = 0; i < 10; i++)
    //    {
    //        randomNumbersText += "    ";

    //        for (int j = 0; j < 10; j++)
    //        {
    //            randomNumbersText += string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.000000}, ", _rnd.NextDouble());
    //        }

    //        randomNumbersText += "\r\n";
    //    }

    //    randomNumbersText += "};";

    //    return randomNumbersText;
    //}

    /// <summary>
    /// When true then the pre-generated list of random numbers is used instead of actual random numbers.
    /// This always generate the same random number and produces the same rendering.
    /// </summary>
    public bool UseGeneratedRandomNumbers { get; set; }

    private int _randomNumberIndex;

    /// <summary>
    /// Start using the first generated random number. This method is called when the BeginTest is called.
    /// </summary>
    public void ResetGeneratedRandomNumbersIndex()
    {
        _randomNumberIndex = 0;
    }

    public int GetRandomInt(int maxValue)
    {
        return (int)(GetRandomDouble() * maxValue);
    }

    public float GetRandomFloat()
    {
        return (float)GetRandomDouble();
    }

    public double GetRandomDouble()
    {
        double randomNumber;

        if (UseGeneratedRandomNumbers)
        {
            randomNumber = GeneratedRandomNumbers[_randomNumberIndex];
            _randomNumberIndex = (_randomNumberIndex + 1) % GeneratedRandomNumbers.Length;
        }
        else
        {
            randomNumber = _rnd.NextDouble();
        }

        return randomNumber;
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