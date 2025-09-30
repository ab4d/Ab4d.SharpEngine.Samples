using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

// This sample can be very easily copied to a console application that can even run on a web server.
// To Vulkan API with Ab4d.SharpEngine on a web server (or on a computer without a GPU), you just need
// to copy a few native dlls that provide a software implementation of Vulkan API.
// See instructions here: https://www.ab4d.com/SharpEngine/using-vulkan-in-virtual-machine-mesa-llvmpipe.aspx

public class OffScreenRenderingSample : CommonSample
{
    public override string Title => "Off-screen rendering";
    public override string Subtitle => "This samples does not shown any 3D graphics in this window\nbut instead renders to an off-screen buffer that is then saved to a png file.\nClick on 'Save rendered scene to a file' to see the saved file.\n\nThe sample shows how Ab4d.SharpEngine can be used in a console app (can also run on a server).";

    private TargetPositionCamera _targetPositionCamera;
    private VulkanDevice? _gpuDevice;
    private Scene? _scene;
    private SceneView? _sceneView;

    public OffScreenRenderingSample(ICommonSamplesContext context)
        : base(context)
    {
        // Create _targetPositionCamera here, so we can use it in the slider that sets the Heading property
        _targetPositionCamera = new TargetPositionCamera()
        {
            Heading = 30,
            Attitude = -20,
            Distance = 300,
        };
    }

    /// <inheritdoc />
    protected override void OnCreateScene(Scene scene)
    {
        // Nothing to do here because we will create our own scene for off-screen rendering
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (_sceneView != null)
        {
            _sceneView.Dispose();
            _sceneView = null;
        }
        
        if (_scene != null)
        {
            _scene.Dispose();
            _scene = null;
        }
        
        if (_gpuDevice != null)
        {
            _gpuDevice.Dispose();
            _gpuDevice = null;
        }

        base.OnDisposed();
    }

    [MemberNotNull(nameof(_gpuDevice))]
    [MemberNotNull(nameof(_scene))]
    [MemberNotNull(nameof(_sceneView))]
    private void InitializeSharpEngine()
    {
        var engineCreateOptions = new EngineCreateOptions();
        _gpuDevice = VulkanDevice.Create(engineCreateOptions);

        _scene = new Scene(_gpuDevice);

        _sceneView = new SceneView(_scene);
        
        _sceneView.BackgroundColor = Colors.LightSkyBlue;
        _sceneView.Initialize(_gpuDevice, width: 1980, height: 1024, dpiScaleX: 1, dpiScaleY: 1, multisampleCount: 4, supersamplingCount: 4);

        _sceneView.Camera = _targetPositionCamera;
    }

    private void InitializeSceneObjects()
    {
        if (_scene == null)
            return;

        var boxModelNode = new BoxModelNode(centerPosition: new Vector3(0, 20, 0), size: new Vector3(100, 40, 80), material: StandardMaterials.Green);
        _scene.RootNode.Add(boxModelNode);

        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -0.1f, 0),
            Size = new Vector2(160, 160),
        };

        _scene.RootNode.Add(wireGridNode);
    }

    private void RenderScene()
    {
        if (_sceneView == null)
        {
            try
            {
                InitializeSharpEngine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing SharpEngine: " + ex.Message);
                return;
            }
        }

        InitializeSceneObjects();

        var rawImageData = _sceneView.RenderToRawImageData(format: StandardBitmapFormats.Bgra);

        // NOTE:
        // If you need to render multiple images, then you can reuse the rawImageData object (just pass it to the next RenderToRawImageData call).
        
        // Ab4d.SharpEngine library includes the png reader and writer so the same code can be used on any OS.
        var pngBitmapIO = new PngBitmapIO();
        string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SharpEngineRender.png");
        pngBitmapIO.SaveBitmap(rawImageData, fileName);

        System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Camera.Heading:");

        ui.CreateSlider(minValue: 0, maxValue: 360, 
            () => _targetPositionCamera.Heading,
            (newValue) => _targetPositionCamera.Heading = newValue,
            width: 160,
            keyText: "",
            formatShownValueFunc: newValue => $"{newValue:0}");

        ui.AddSeparator();

        ui.CreateButton("Save rendered scene to a file", RenderScene);
    }
}