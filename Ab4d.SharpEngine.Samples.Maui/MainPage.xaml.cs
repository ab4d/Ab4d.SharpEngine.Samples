using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using System.Runtime.InteropServices;
using Colors = Ab4d.SharpEngine.Common.Colors;

namespace Ab4d.SharpEngine.Samples.Maui;

public partial class MainPage : ContentPage
{
    private StandardMaterial? _hashMaterial;
    private MeshModelNode? _hashModelNode;

    private MauiCameraController? _mauiCameraController;
    
    private Random? _rnd;
    private SharpEngineSceneView _sharpEngineSceneView;
    private TargetPositionCamera? _targetPositionCamera;

    public MainPage()
	{
        // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
        // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
        Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                              licenseType: "SamplesLicense",
                                              license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");


        InitializeComponent();

        // To enable full Ab4d.SharpEngine logging uncomment the followig two lines:
		//Ab4d.SharpEngine.Utilities.Log.LogLevel = Ab4d.SharpEngine.Common.LogLevels.All;
		//Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;

        _sharpEngineSceneView = new SharpEngineSceneView();

        bool isSupported = SetupPlatform(_sharpEngineSceneView.CreateOptions);

        if (!isSupported)
            return;


        // Uncomment the following two lines to change the default anti-aliasing settings.
        // By default, multi-sampling count (MSAA) is set to 4 for DiscreteGpu and IntegratedGpu and 1 for others (see SharpEngineSceneView.GetDefaultMultiSampleCount method).
        // By default, super-sampling count (SSAA) is set to This method return 4 for DiscreteGpu, 2 for non-mobile IntegratedGpu and 1 for others (see SharpEngineSceneView.GetDefaultSuperSamplingCount method).
        //_sharpEngineSceneView.MultisampleCount = 1;
        //_sharpEngineSceneView.SupersamplingCount = 1;

        // Uncomment the following two lines to enable logging of warnings and errors
        //Ab4d.SharpEngine.Utilities.Log.LogLevel = LogLevels.Warn;
        //Ab4d.SharpEngine.Utilities.Log.IsLoggingToDebugOutput = true;


        _sharpEngineSceneView.GpuDeviceCreationFailed += delegate(object sender, DeviceCreateFailedEventArgs args)
        {
            InfoLabel.Text = args.Exception.Message;
            InfoLabel.IsVisible = true;
        };
       

        CreateTestScene();

        RootGrid.Children.Insert(0, _sharpEngineSceneView); // insert behind the button and other UI elements


        // Because mouse and keyboard are badly supported in MAUI the MauiCameraController
        // has some limitations compared to MouseCameraControllers for other frameworks.
        // See comments in the MauiCameraController.cs
        _mauiCameraController = new MauiCameraController(_sharpEngineSceneView)
        {
            RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed,
            ZoomMode = CameraZoomMode.PointerPosition,
        };

        // On mobile OS show info label on top to prevent drawing it over the buttons
        if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
            InfoLabel2.VerticalOptions = LayoutOptions.Start;
        

        // On Windows set camera move condition to left mouse button + Control key; on other platforms use both left and right mouse buttons
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _mauiCameraController.MoveCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey;
        else
            _mauiCameraController.MoveCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed;

        Unloaded += (sender, args) =>
        {
            _sharpEngineSceneView.Dispose();
        };
    }

    private bool SetupPlatform(EngineCreateOptions engineCreateOptions)
    {
        if (RuntimeInformation.RuntimeIdentifier.Contains("ios") && RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
        {
            // The libMoltenVK.dylib library that is required on iOS is provided only for Arm64 platform (version for mac catalyst supports both x64 and arm64)
            InfoLabel.Text = $"Unsupported ProcessArchitecture for iOS: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}. Required Arm64! If this is running in a simulator, then start the app on a real device or on simulator with Arm64 architecture.";
            InfoLabel.IsVisible = true;
            return false;
        }

        if (RuntimeInformation.RuntimeIdentifier.Contains("catalyst"))
        {
            string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libMoltenVK.dylib");
            bool fileExist = System.IO.File.Exists(fileName);

            if (fileExist)
			{
				// On macOS we need to set the CustomVulkanLoaderLibrary even if the library is present in the app's folder.
				engineCreateOptions.CustomVulkanLoaderLibrary = fileName;
			}
			else
            {
				// If the libMoltenVK.dylib does not exist, then we try to read the file from the Vulkan SKD folder (if installed)
                var usersFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // get "/Users/[user_name]/
                var vulkanSdkFolder = System.IO.Path.Combine(usersFolder, "VulkanSDK");
                
                if (System.IO.Directory.Exists(vulkanSdkFolder))
                {
                    var allSubfolders = System.IO.Directory.GetDirectories(vulkanSdkFolder).OrderBy(f => f).ToList();

                    if (allSubfolders.Count > 0)
                    {
                        var lastVulkanSdkFolder = allSubfolders.Last();

                        var moltenVkFile = System.IO.Path.Combine(lastVulkanSdkFolder, "MoltenVK/dylib/macOS/libMoltenVK.dylib");

                        if (System.IO.File.Exists(moltenVkFile))
                        {
                            // Use the libMoltenVK from Vulkan SKD folder
                            engineCreateOptions.CustomVulkanLoaderLibrary = moltenVkFile;
                            return true;
                        }
                    }
                }

                InfoLabel.Text = $"Cannot find libMoltenVK.dylib library that is required to run Vulkan on Mac Catalyst. ";
                InfoLabel.IsVisible = true;
                return false;
            }
        }

        return true;
    }

    private void CreateTestScene()
    {
        if (this.Resources == null)
            return;

        var scene = _sharpEngineSceneView.Scene;


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

        
        //_boxMaterial = StandardMaterials.Gold;

        //_boxModelNode = new BoxModelNode()
        //{
        //    Position     = new Vector3(0, 0, 0),
        //    PositionType = PositionTypes.Bottom | PositionTypes.Center,
        //    Size         = new Vector3(80, 40, 60),
        //    Material     = _boxMaterial
        //};

        //scene.RootNode.Add(_boxModelNode);
        

        var wireGridNode = new WireGridNode()
        {
            CenterPosition   = new Vector3(0, -0.1f, 0),
            Size             = new Vector2(200, 200),
            WidthCellsCount  = 5,
            HeightCellsCount = 5
        };

        scene.RootNode.Add(wireGridNode);

        
        _sharpEngineSceneView.Scene.SetAmbientLight(0.3f);

        var sceneView = _sharpEngineSceneView.SceneView;

        sceneView.BackgroundColor = Common.Colors.LightSkyBlue;

        _targetPositionCamera = new TargetPositionCamera()
        {
            Heading  = 30,
            Attitude = -35,
            Distance = 500,
        };

        // Each time camera is changed, we need to render the scene again
        _targetPositionCamera.CameraChanged += (sender, args) => _sharpEngineSceneView.Refresh();

        sceneView.Camera = _targetPositionCamera;


        if (_sharpEngineSceneView.Scene.GpuDevice != null)
            LoadSampleTexture();
        else
            _sharpEngineSceneView.GpuDeviceCreated += (sender, args) => LoadSampleTexture();
    }

    private void LoadSampleTexture()
    {
        var gpuDevice = _sharpEngineSceneView.Scene.GpuDevice;
        if (gpuDevice == null)
            return;

        // To load a bitmap you can use SkiaSharpBitmapIO or PngBitmapIO.
        // The SkiaSharpBitmapIO can load more bitmap formats, but it requires SkiaSharp library (alreay used by MAUI).
        // The PngBitmapIO can load only PNG files, but it does not require any additional libraries because it is part of Ab4d.SharpEngine library.
        
        var bitmapIO = new SkiaSharpBitmapIO();
        //var bitmapIO = new PngBitmapIO();
        
        // To load a bitmap its file must be placed in the Resources/Raw folder and its Build Action must be set to MauiAsset.
        // The the file's stream can be loaded by FileSystem.OpenAppPackageFileAsync:
        using Stream stream = FileSystem.OpenAppPackageFileAsync("dotnet_bot.png").Result;

        // After you have the stream that you can create the RawImageData class:
        var rawImageData = bitmapIO.LoadBitmap(stream, "png");

        // Then create a GpuImage from the RawImageData. 
        var texture = new GpuImage(gpuDevice, rawImageData);
        
        //var textureMaterial = new StandardMaterial("dotnet_bot.png", bitmapIO) { IsTwoSided = true };
        
        // Now we can the StandardMaterial that will show the texture
        var textureMaterial = new StandardMaterial(texture) { IsTwoSided = true };


        // Alternatively, we can create the StandardMaterial with a file name and specifiy bitmapIO to load the bitmap
        // when the GpuDevice is created (this requires that the file is deployed to the app's base folder).
        //var textureMaterial = new StandardMaterial("dotnet_bot.png", bitmapIO) { IsTwoSided = true };

        
        var planeModel = new PlaneModelNode(centerPosition: new Vector3(0, 61.5f, -100), size: new Vector2(200, 123), normal: new Vector3(0, 0, 1), heightDirection: new Vector3(0, 1, 0), name: "DotNetBotImagePlane")
        {
            Material = textureMaterial,
        };

        _sharpEngineSceneView.Scene.RootNode.Add(planeModel);


        // Add rectangle lines around the texture
        var rectangleNode = new RectangleNode(planeModel.Position, PositionTypes.Center, planeModel.Size, 
                                              widthDirection: new Vector3(1, 0, 0), heightDirection: new Vector3(0, 1, 0), 
                                              lineColor: Colors.Black, lineThickness: 2);
        
        _sharpEngineSceneView.Scene.RootNode.Add(rectangleNode);
    }

    private void OnChangeCameraButtonClicked(object sender, EventArgs e)
    {
        if (_targetPositionCamera == null)
            return;

        // Rotate the camera
        _targetPositionCamera.Heading += 10;

        // Render the scene again
        _sharpEngineSceneView.Refresh();
    }

    private void OnChangeColorButtonClicked(object sender, EventArgs e)
    {
        if (_hashMaterial == null)
            return;

        // Change color of the material
        _rnd ??= new Random();
        _hashMaterial.DiffuseColor = new Color3(_rnd.NextSingle(), _rnd.NextSingle(), _rnd.NextSingle());

        // Render the scene again
        _sharpEngineSceneView.Refresh();
    }
}

