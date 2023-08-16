using System;
using System.Numerics;
using System.Windows;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.Vulkan;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Common;

public class AvaloniaSamplesContext : ICommonSamplesContext
{
    public static readonly AvaloniaSamplesContext Current = new AvaloniaSamplesContext();

    public VulkanDevice? GpuDevice => CurrentSharpEngineSceneView?.GpuDevice ?? null;

    private SkiaSharpBitmapIO _bitmapIO = new SkiaSharpBitmapIO();
    public IBitmapIO BitmapIO => _bitmapIO;

    private TextBlockFactory? _textBlockFactory;

    public ISharpEngineSceneView? CurrentSharpEngineSceneView { get; private set; }

    //public DiagnosticsWindow? CurrentDiagnosticsWindow { get; set; }


    public PresentationTypes PreferredPresentationType { get; set; } = PresentationTypes.SharedTexture;

    public int PreferredSuperSamplingCount { get; set; } = 4;

    public bool WaitForVSync { get; set; } = true;


    public EngineCreateOptions PreferredEngineCreateOptions { get; private set; }


    public event EventHandler? CurrentSharpEngineSceneViewChanged;


    private AvaloniaSamplesContext()
    {
        PreferredEngineCreateOptions = new EngineCreateOptions()
        {
            ApplicationName = "SharpEngine AvaloniaUI Samples",
            EnableStandardValidation = true,
            DeviceSelectionType = EngineCreateOptions.DeviceSelectionTypes.DefaultDevice, // Select default device (same as wpf)
            CustomDeviceId = 0,     // no preferred device
        };

        PreferredEngineCreateOptions.RequiredDeviceExtensionNames.AddRange(SharpEngineSceneView.RequiredDeviceExtensionNames);
    }

    public void RegisterCurrentSharpEngineSceneView(SharpEngineSceneView? sharpEngineSceneView)
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

    public TextBlockFactory GetTextBlockFactory()
    {
        if (CurrentSharpEngineSceneView == null)
            throw new InvalidOperationException("Cannot call GetTextBlockFactory when CurrentSharpEngineSceneView is not yet set.");

        if (_textBlockFactory == null)
        {
            _textBlockFactory = new TextBlockFactory(CurrentSharpEngineSceneView.Scene, this.BitmapIO);
            //_textBlockFactory.BackgroundColor = Colors.LightYellow;
            //_textBlockFactory.BorderThickness = 1;
            //_textBlockFactory.BorderColor = Colors.DimGray;
        }

        return _textBlockFactory;
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

    public Color GetRandomAvaloniaColor()
    {
        var randomColor = Color.FromRgb(GetRandomByte(), GetRandomByte(), GetRandomByte());
        return randomColor;
    }

    public Color GetRandomAvaloniaColorWithAlpha()
    {
        var randomColor = Color.FromArgb(GetRandomByte(), GetRandomByte(), GetRandomByte(), GetRandomByte());
        return randomColor;
    }
    #endregion
}