using System.Diagnostics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
using System.Numerics;
using System.Runtime.InteropServices;
using SkiaSharp.Views.Maui;
using Colors = Ab4d.SharpEngine.Common.Colors;
using Ab4d.SharpEngine.Core;

namespace Ab4d.SharpEngine.Samples.Maui;

public partial class MainPage : ContentPage
{
    private BoxModelNode? _boxModelNode;
    private StandardMaterial? _boxMaterial;

    private MauiCameraController? _mauiCameraController;
    
    private Random? _rnd;
    private SharpEngineSceneView _sharpEngineSceneView;
    private TargetPositionCamera? _targetPositionCamera;

    public MainPage()
	{
		InitializeComponent();


        _sharpEngineSceneView = new SharpEngineSceneView();

        bool isSupported = SetupPlatform(_sharpEngineSceneView.CreateOptions);

        if (!isSupported)
            return;

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
            RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed,
            ZoomMode = CameraZoomMode.MousePosition,
        };

        // On Windows set camera move condition to left mouse button + Control key; on other platforms use both left and right mouse buttons
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _mauiCameraController.MoveCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.ControlKey;
        else
            _mauiCameraController.MoveCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed;
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

            // If the libMoltenVK.dylib does not exist, then we try to read the file from the Vulkan SKD folder (if installed)
            if (!fileExist)
            {
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


        _boxMaterial = StandardMaterials.Gold;

        _boxModelNode = new BoxModelNode()
        {
            Position     = new Vector3(0, 0, 0),
            PositionType = PositionTypes.Bottom | PositionTypes.Center,
            Size         = new Vector3(80, 40, 60),
            Material     = _boxMaterial
        };

        scene.RootNode.Add(_boxModelNode);


        var wireGridNode = new WireGridNode()
        {
            CenterPosition   = new Vector3(0, 0, 0),
            Size             = new Vector2(160, 160),
            WidthCellsCount  = 8,
            HeightCellsCount = 8
        };

        scene.RootNode.Add(wireGridNode);


        var sceneView = _sharpEngineSceneView.SceneView;

        sceneView.BackgroundColor = Common.Colors.LightSkyBlue;

        _targetPositionCamera = new TargetPositionCamera()
        {
            Heading  = 30,
            Attitude = -20,
            Distance = 300,
        };

        // Each time camera is changed, we need to render the scene again
        _targetPositionCamera.CameraChanged += (sender, args) => _sharpEngineSceneView.Refresh();

        sceneView.Camera = _targetPositionCamera;
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
        if (_boxMaterial == null)
            return;

        // Change color of the material
        _rnd ??= new Random();
        _boxMaterial.DiffuseColor = new Color3(_rnd.NextSingle(), _rnd.NextSingle(), _rnd.NextSingle());

        // Render the scene again
        _sharpEngineSceneView.Refresh();
    }
}

