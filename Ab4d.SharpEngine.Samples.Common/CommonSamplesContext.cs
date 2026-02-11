using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

#if VULKAN
using Ab4d.SharpEngine.Vulkan;
using Ab4d.SharpEngine.OverlayPanels;
using GpuDevice = Ab4d.SharpEngine.Vulkan.VulkanDevice;
#elif WEB_GL
using Ab4d.SharpEngine.WebGL;
using GpuDevice = Ab4d.SharpEngine.WebGL.WebGLDevice;
#endif

namespace Ab4d.SharpEngine.Samples.Common;

public abstract class CommonSamplesContext : ICommonSamplesContext
{
    public GpuDevice? GpuDevice => CurrentSharpEngineSceneView?.GpuDevice ?? null;

    public ISharpEngineSceneView? CurrentSharpEngineSceneView { get; private set; }

    public IBitmapIO? BitmapIO { get; }

    //public DiagnosticsWindow? CurrentDiagnosticsWindow { get; set; }

#if VULKAN
    private BitmapTextCreator? _bitmapTextCreator;
    private TextBlockFactory? _textBlockFactory;
    private Task<TextBlockFactory>? _textBlockFactoryLoadingTask;

    public PresentationTypes PreferredPresentationType { get; set; } = PresentationTypes.SharedTexture;
#endif

    public int PreferredSuperSamplingCount { get; set; } = 4;

    public bool WaitForVSync { get; set; } = true;


    public EngineCreateOptions PreferredEngineCreateOptions { get; private set; }


    public event EventHandler? CurrentSharpEngineSceneViewChanged;


    protected CommonSamplesContext(string applicationName, IBitmapIO? bitmapIO)
    {
#if VULKAN
        PreferredEngineCreateOptions = new EngineCreateOptions()
        {
            ApplicationName = applicationName,
            DeviceSelectionType = EngineCreateOptions.DeviceSelectionTypes.DefaultDevice, // Select default device (same as wpf)
            CustomDeviceId = 0,     // no preferred device
        };
#else
        PreferredEngineCreateOptions = new EngineCreateOptions();
#endif

        BitmapIO = bitmapIO;
    }

    protected void SetCurrentSharpEngineSceneView(ISharpEngineSceneView? sharpEngineSceneView)
    {
        if (ReferenceEquals(CurrentSharpEngineSceneView, sharpEngineSceneView))
            return;

        CurrentSharpEngineSceneView = sharpEngineSceneView;
        OnCurrentSharpEngineSceneViewChanged();
    }

    protected void OnCurrentSharpEngineSceneViewChanged()
    {
        CurrentSharpEngineSceneViewChanged?.Invoke(this, EventArgs.Empty);
    }

#if VULKAN
    public TextBlockFactory GetTextBlockFactory()
    {
        if (CurrentSharpEngineSceneView == null)
            throw new InvalidOperationException("Cannot call GetTextBlockFactory when CurrentSharpEngineSceneView is not yet set.");

        if (_textBlockFactory != null && _textBlockFactory.Scene != CurrentSharpEngineSceneView.Scene)
        {
            _textBlockFactory.Dispose();
            _textBlockFactory = null;
        }
        
        if (_textBlockFactory == null)
        {
            // Create TextBlockFactory that will use the default BitmapTextCreator (get by BitmapTextCreator.GetDefaultBitmapTextCreator).
            _textBlockFactory = new TextBlockFactory(CurrentSharpEngineSceneView.Scene);
        }
        else
        {
            // Reset existing TextBlockFactory to default values:
            ResetTextBlockFactory();
        }

        return _textBlockFactory;
    }

    public Task<TextBlockFactory> GetTextBlockFactoryAsync()
    {
        // If already loaded, return synchronously
        if (_textBlockFactory != null)
            return Task.FromResult(_textBlockFactory);
        
        // If loading already started, return the same task
        if (_textBlockFactoryLoadingTask != null)
            return _textBlockFactoryLoadingTask;

        // Start loading and store the task
        _textBlockFactoryLoadingTask = GetTextBlockFactoryIntAsync();

        return _textBlockFactoryLoadingTask;
    }
    
    private async Task<TextBlockFactory> GetTextBlockFactoryIntAsync()
    {
        if (CurrentSharpEngineSceneView == null)
            throw new InvalidOperationException("Cannot call GetTextBlockFactory when CurrentSharpEngineSceneView is not yet set.");

        if (_textBlockFactory != null && _textBlockFactory.Scene != CurrentSharpEngineSceneView.Scene)
        {
            _textBlockFactory.Dispose();
            _textBlockFactory = null;
        }

        if (_textBlockFactory == null)
        {
            if (GpuDevice == null)
                throw new Exception("Cannot create TextBlockFactory because GpuDevice is null");
            
            _bitmapTextCreator = await BitmapTextCreator.GetDefaultBitmapTextCreatorAsync(CurrentSharpEngineSceneView.Scene);

            // Create TextBlockFactory that will use the default BitmapTextCreator (get by BitmapTextCreator.GetDefaultBitmapTextCreator).
            _textBlockFactory = new TextBlockFactory(_bitmapTextCreator);
        }
        else
        {
            // Reset existing TextBlockFactory to default values:
            ResetTextBlockFactory();
        }

        return _textBlockFactory;
    }

#elif WEB_GL

    public abstract Task<TextBlockFactory> GetTextBlockFactoryAsync();

#endif

#if VULKAN
    protected void ResetTextBlockFactory()
    {
        if (_textBlockFactory == null)
            return;

        _textBlockFactory.TextColor = Color4.Black;
        _textBlockFactory.FontSize = 14;
        _textBlockFactory.BackgroundHorizontalPadding = 8;
        _textBlockFactory.BackgroundVerticalPadding = 4;
        _textBlockFactory.BackgroundColor = Color4.Transparent;
        _textBlockFactory.BorderThickness = 0;
        _textBlockFactory.BorderColor = Color4.Black;
        _textBlockFactory.BackMaterialColor = Color4.Black;
    }
#endif

    #region GetRandom... methods
    protected Random rnd = new Random();

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
                rnd = new Random(CommonRandomSeed);
            else
                rnd = new Random();
        }
    }

    /// <summary>
    /// Recreates the random number generator. This method is called when the BeginTest is called.
    /// </summary>
    public void ResetRandomNumbersGenerator()
    {
        if (_useCommonRandomSeed)
            rnd = new Random(CommonRandomSeed);
    }

    public double GetRandomDouble()
    {
        return rnd.NextDouble();
    }

    public int GetRandomInt(int maxValue)
    {
        return (int)(GetRandomDouble() * maxValue);
    }

    public byte GetRandomByte()
    {
        return (byte)(GetRandomDouble() * 255);
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

    public StandardMaterial GetRandomStandardMaterial(bool isTransparent = false, string? name = null)
    {
        Color3 color = GetRandomColor3();
        float opacity = isTransparent ? GetRandomFloat() : 1;

        var standardMaterial = new StandardMaterial(color, opacity, name);
        return standardMaterial;
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