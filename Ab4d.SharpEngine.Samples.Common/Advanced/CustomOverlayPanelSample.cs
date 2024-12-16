using Ab4d.SharpEngine.Animation;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.Vulkan;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public sealed class CustomOverlayPanelSample : CommonSample
{
    public override string Title { get; } = "Custom Overlay Panel";
    public override string Subtitle { get; } = "This samples shows how to render a custom scene to a texture that is shown as a sprite on top of another 3D scene.";

    private readonly int _overlayPanelWidth = 256;
    private readonly int _overlayPanelHeight = 256;
     
    private SpriteBatch? _spriteBatch;
     
    private Scene? _overlayPanelScene;
    private SceneView? _overlayPanelSceneView;
     
    private RawImageData? _rawImageData;
    private GpuImage? _gpuImage;

    public CustomOverlayPanelSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnDisposed()
    {
        if (Scene != null && _spriteBatch != null)
            Scene.RemoveSpriteBatch(_spriteBatch);

        if (SceneView != null)
            SceneView.SceneUpdating -= ParentSceneView_SceneUpdating;

        base.OnDisposed();
    }

    protected override void OnCreateScene(Scene scene)
    {
        var teapotMesh = TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Teapot,
                                                position: new Vector3(0, 0, 0),
                                                positionType: PositionTypes.Bottom,
                                                finalSize: new Vector3(100, 100, 100));

        scene.RootNode.Add(new MeshModelNode(teapotMesh, StandardMaterials.Silver.SetSpecular(32)));


        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -0.1f, 0),
            Size = new Vector2(200, 200),
        };

        scene.RootNode.Add(wireGridNode);
    }

    protected override void OnSceneViewInitialized(SceneView parentSceneView)
    {
        // Create a TargetPositionCamera for the main scene (with teapot)
        parentSceneView.Camera = new TargetPositionCamera
        {
            Heading = 30,
            Attitude = -20,
            Distance = 300
        };

        // Initialize overlay 3D scene
        InitializeOverlayScene(parentSceneView);

        base.OnSceneViewInitialized(parentSceneView);
    }

    private void InitializeOverlayScene(SceneView parentSceneView)
    {
        var gpuDevice = parentSceneView.GpuDevice!;

        _overlayPanelScene = new Scene(gpuDevice);


        _overlayPanelSceneView = new SceneView(_overlayPanelScene) 
        {
            //BackgroundColor = Colors.Aqua // Transparent by default
        };


        var overlayPanelCamera = new TargetPositionCamera
        {
            Heading = 30,
            Attitude = -20,
            Distance = 4
        };

        overlayPanelCamera.StartRotation(40);

        _overlayPanelSceneView.Camera = overlayPanelCamera;
        

        _overlayPanelSceneView.Initialize(_overlayPanelWidth, _overlayPanelHeight, multisampleCount: 4, supersamplingCount: 2);


        // Create a 3D box that will be shown in the overlay SceneView
        var boxModel = new BoxModelNode(centerPosition: Vector3.Zero, size: new Vector3(2f, 0.2f, 1.3f), material: StandardMaterials.Gold);
        _overlayPanelScene.RootNode.Add(boxModel);


        // Create RawImageData and GpuImage that will be used to show the rendered SceneView as a sprite (see _parentSceneView_SceneUpdating)
        _rawImageData = new RawImageData(width: _overlayPanelWidth, 
                                         height: _overlayPanelHeight,
                                         stride: 4 * _overlayPanelWidth,
                                         format: Format.R8G8B8A8Unorm,
                                         data: new byte[4 * _overlayPanelWidth * _overlayPanelHeight],
                                         checkTransparency: false)
        {
            HasTransparentPixels = true
        };

        _gpuImage = new GpuImage(gpuDevice, _rawImageData);


        // Create SpriteBatch
        _spriteBatch = parentSceneView.CreateOverlaySpriteBatch("OverlaySceneViewSpriteBatch");


        // Subscribe to SceneUpdating - there we will update the sprite
        parentSceneView.SceneUpdating += ParentSceneView_SceneUpdating;
    }
    
    private void ParentSceneView_SceneUpdating(object? o, EventArgs e)
    {
        if (_overlayPanelSceneView == null || _gpuImage == null || _spriteBatch == null || _rawImageData == null)
            return;

        // Render Overlay 3D scene to _rawImageData
        // NOTE that this copies the rendered texture to CPU memory.
        // In the next version it will be possible to render to a GpuImage that would stay in GPU memory.
        _overlayPanelSceneView.RenderToRawImageData(_rawImageData);

        // Copy the _rawImageData to a GpuImage that is rendered as a sprite
        _gpuImage.CopyDataToImage(_rawImageData.Data);


        _spriteBatch.Begin(useAbsoluteCoordinates: true);
        _spriteBatch.SetCoordinateCenter(PositionTypes.BottomLeft);
        
        _spriteBatch.SetSpriteTexture(_gpuImage);
        _spriteBatch.DrawSprite(topLeftPosition: new Vector2(30, 286), spriteSize: new Vector2(256, 256));
        
        _spriteBatch.End();
    }
}