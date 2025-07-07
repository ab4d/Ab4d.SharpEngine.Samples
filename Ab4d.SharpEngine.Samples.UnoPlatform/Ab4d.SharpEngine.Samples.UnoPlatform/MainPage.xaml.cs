using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.UnoPlatform;

namespace Ab4d.SharpEngine.Samples.UnoPlatform;

public sealed partial class MainPage : Page
{
    private PointerCameraController? _pointerCameraController;
    private StandardMaterial? _hashMaterial;
    private MeshModelNode? _hashModelNode;
    private WireGridNode? _wireGridNode;
    private TargetPositionCamera? _targetPositionCamera;
    private readonly IThemeService _themeService;
    
    public MainPage()
    {
        // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
        // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
        Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                              licenseType: "SamplesLicense",
                                              license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");

        InitializeComponent();


        // Uncomment the following two lines to change the default anti-aliasing settings.
        // By default, multi-sampling count (MSAA) is set to 4 for DiscreteGpu and IntegratedGpu and 1 for others (see SharpEngineSceneView.GetDefaultMultiSampleCount method).
        // By default, super-sampling count (SSAA) is set to This method return 4 for DiscreteGpu, 2 for non-mobile IntegratedGpu and 1 for others (see SharpEngineSceneView.GetDefaultSuperSamplingCount method).
        //SharpEngineSceneView.MultisampleCount = 1;
        //SharpEngineSceneView.SupersamplingCount = 1;

        // Uncomment the following two lines to enable logging of warnings and errors
        //Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Warn;
        //Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;

        
        CreateTestScene();
        
        _themeService = this.GetThemeService();
        _themeService.ThemeChanged += OnThemeChanged;
        
        SetupMouseCameraController();
    }

    private void OnThemeChanged(object? sender, AppTheme e)
    {
        SetTheme();
    }

    private void SetupMouseCameraController()
    {
        _targetPositionCamera = new TargetPositionCamera()
        {
            Heading = -40,
            Attitude = -30,
            Distance = 500,
            ViewWidth = 500,
            TargetPosition = new Vector3(0, 0, 0),
            ShowCameraLight = ShowCameraLightType.Always
        };

        SharpEngineSceneView.SceneView.Camera = _targetPositionCamera;
        
        _pointerCameraController = new PointerCameraController(SharpEngineSceneView, InputOverlay)
        {
            RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,                                                     // this is already the default value but is still set up here for clarity
            MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,             // this is already the default value but is still set up here for clarity
            QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed, // quick zoom is disabled by default
            ZoomMode = CameraZoomMode.PointerPosition,
            RotateAroundPointerPosition = true,
        };
    }
    
    private void SetTheme()
    {
        var backgroundColorBrush = Application.Current.Resources["SolidBackgroundFillColorBaseBrush"] as SolidColorBrush;
        if (backgroundColorBrush != null)
        {
            SharpEngineSceneView.SceneView.BackgroundColor = backgroundColorBrush.ToColor4();
        }

        var majorLineColorBrush = Application.Current.Resources["TextFillColorTertiaryBrush"] as SolidColorBrush;
        if (majorLineColorBrush != null && _wireGridNode != null)
        {
            _wireGridNode.MajorLineColor = majorLineColorBrush.ToColor4();
        }
        
        var minorLineColorBrush = Application.Current.Resources["TextFillColorDisabledBrush"] as SolidColorBrush;
        if (minorLineColorBrush != null && _wireGridNode != null)
        {
            _wireGridNode.MinorLineColor = minorLineColorBrush.ToColor4();
        }

        SharpEngineSceneView.Refresh();
    }
    
    private void CreateTestScene()
    {
        if (Resources == null)
            return;

        var scene = SharpEngineSceneView.Scene;

        float hashModelSize = 100;
        float hashModelBarThickness = 16;
        float hashModelBarOffset = 20;

        var hashSymbolMesh = MeshFactory.CreateHashSymbolMesh(new Vector3(0, 0, 0),
            shapeYVector: new Vector3(0, 0, 1),
            extrudeVector: new Vector3(0, hashModelBarThickness, 0),
            size: hashModelSize,
            barThickness: hashModelBarThickness,
            barOffset: hashModelBarOffset,
            name: "HashSymbolMesh");

        _hashMaterial = new StandardMaterial(Color3.FromByteRgb(255, 197, 0));

        _hashModelNode = new MeshModelNode(hashSymbolMesh, "HashSymbolModel")
        {
            Material = _hashMaterial,
            Transform = new StandardTransform()
        };

        scene.RootNode.Add(_hashModelNode);
        
        _wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -0.1f, 0),
            Size = new Vector2(200, 200),
            WidthCellsCount = 5,
            HeightCellsCount = 5,
        };

        scene.RootNode.Add(_wireGridNode);


        var sceneView = SharpEngineSceneView.SceneView;

        sceneView.BackgroundColor = Colors.LightSkyBlue;

        _targetPositionCamera = new TargetPositionCamera()
        {
            Heading = 30,
            Attitude = -35,
            Distance = 300,
        };

        // Each time camera is changed, we need to render the scene again
        _targetPositionCamera.CameraChanged += (sender, args) => SharpEngineSceneView.Refresh();

        sceneView.Camera = _targetPositionCamera;
    }

    private void OnChangeCameraButtonClicked(object sender, EventArgs e)
    {
        if (_targetPositionCamera == null)
            return;

        // Rotate the camera
        _targetPositionCamera.Heading += 10;

        // Render the scene again
        SharpEngineSceneView.Refresh();
    }

    private void OnChangeColorButtonClicked(object sender, EventArgs e)
    {
        if (_hashMaterial == null)
            return;

        // Change color of the material
        _hashMaterial.DiffuseColor = new Color3(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle());

        // Render the scene again
        SharpEngineSceneView.Refresh();
    }

    private void OnChangeThemeButtonClicked(object sender, RoutedEventArgs e)
    {
        if (_themeService.IsDark)
        {
            _themeService.SetThemeAsync(AppTheme.Light);
        }
        else
        {
            _themeService.SetThemeAsync(AppTheme.Dark);
        }
    }
}
