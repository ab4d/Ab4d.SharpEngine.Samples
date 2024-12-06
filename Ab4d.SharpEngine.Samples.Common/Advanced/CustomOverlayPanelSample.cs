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

    readonly int _overlayPanelWidth = 256;
    readonly int _overlayPanelHeight = 256;
    readonly PositionTypes _alignment = PositionTypes.Center;
    readonly Vector2 _offset = new(50, 50);

    SpriteBatch? _spriteBatch;

    Scene? _overlayPanelScene;
    SceneView? _overlayPanelSceneView;

    readonly RawImageData _rawImageData;
    GpuImage? _gpuImage;

    public CustomOverlayPanelSample(ICommonSamplesContext context) : base(context)
    {
        _rawImageData = new(
            _overlayPanelWidth, _overlayPanelHeight,
            4 * _overlayPanelWidth,
            Format.R8G8B8A8Unorm,
            new byte[4 * _overlayPanelWidth * _overlayPanelHeight],
            false) {
            HasTransparentPixels = true
        };
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (Scene != null && _spriteBatch != null)
            Scene.RemoveSpriteBatch(_spriteBatch);

        if (SceneView != null)
            SceneView.SceneUpdating += _parentSceneView_SceneUpdating;

        base.OnDisposed();
    }

    protected override void OnCreateScene(Scene scene)
    {
        _spriteBatch = scene.CreateOverlaySpriteBatch();

        var teapotMesh = TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Teapot,
                                                position: new Vector3(0, 0, 0),
                                                positionType: PositionTypes.Center,
                                                finalSize: Vector3.One);

        scene.RootNode.Add(new MeshModelNode(teapotMesh, StandardMaterials.Blue));
    }

    protected override void OnSceneViewInitialized(SceneView parentSceneView)
    {
        parentSceneView.Camera = new TargetPositionCamera { Distance = 3 };

        var gpuDevice = parentSceneView.GpuDevice!;

        _overlayPanelScene = new(gpuDevice);
        _overlayPanelSceneView = new(_overlayPanelScene) {
            Camera = new TargetPositionCamera { Distance = 5 }
        };
        _overlayPanelSceneView.Initialize(_overlayPanelWidth, _overlayPanelHeight);

        _setupOverlayPanelScene(_overlayPanelScene);

        _gpuImage = new(gpuDevice, _rawImageData);

        parentSceneView.SceneUpdating += _parentSceneView_SceneUpdating;

        base.OnSceneViewInitialized(parentSceneView);
    }

    void _parentSceneView_SceneUpdating(object? o, EventArgs e)
    {
        var parentSceneView = SceneView!;

        _overlayPanelSceneView!.RenderToRawImageData(_rawImageData);
        _gpuImage!.CopyDataToImage(_rawImageData.Data);

        _spriteBatch!.Begin();
        _spriteBatch.SetSpriteTexture(_gpuImage);
        _spriteBatch.SetCoordinateCenter(_alignment);
        _spriteBatch.DrawSprite(_offset / new Vector2(parentSceneView.Width, parentSceneView.Height));
        _spriteBatch.End();
    }

    void _setupOverlayPanelScene(Scene scene)
    {
        var boxModel = new BoxModelNode(Vector3.Zero, Vector3.One, StandardMaterials.Gold);

        scene.RootNode.Add(boxModel);

        var animation = AnimationBuilder.CreateTransformationAnimation(scene);
        animation.SetDuration(5_000);
        animation.Loop = true;

        animation.AddRotateKeyframe(0, Vector3.Zero);
        animation.AddRotateKeyframe(1, new(360, 360, 0));

        animation.AddTarget(boxModel);

        animation.Start();
    }
}
