using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.glTF.Schema;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.RenderingLayers;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class RenderingLayerCustomCameraSample : CommonSample
{
    public override string Title { get; } = "CustomCamera in RenderingLayer";
    public override string Subtitle { get; } = "This sample shows how to create a new RenderingLayer that render its objects by a custom camera";

    private bool _synchronizeCameras = true;
    private bool _useCustomAmbientLight = false;
    private bool _useCustomViewport = true;

    private TargetPositionCamera? _renderingLayerCustomCamera;
    private RenderingLayer? _customRenderingLayer;

    public RenderingLayerCustomCameraSample(ICommonSamplesContext context) 
        : base(context)
    {
    }
    
    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (SceneView != null)
            SceneView.SceneUpdating -= SceneViewOnSceneUpdating;

        if (Scene != null && _customRenderingLayer != null)
            Scene.RemoveRenderingLayer(_customRenderingLayer);
        
        base.OnDisposed();
    }

    protected override void OnCreateScene(Scene scene)
    {
        AddCustomRenderingLayer(scene);

        var blueBox = new BoxModelNode("BlueBox")
        {
            Position = new Vector3(0, 0, 0),
            Size = new Vector3(140, 40, 140),
            Material = StandardMaterials.Blue
        };

        scene.RootNode.Add(blueBox);


        // Add GreenBox to the same position, but put it to _customRenderingLayer
        // Because of that, it will be rendered by a _renderingLayerCustomCamera so it will be rendered at bottom left corner of the screen
        var greenBox = new BoxModelNode("GreenBox")
        {
            Position = new Vector3(0, 0, 0),
            Size = new Vector3(80, 80, 80),
            Material = StandardMaterials.Green,
            CustomRenderingLayer = _customRenderingLayer,
            IsHitTestVisible = false // IMPORTAN: We need to prevent getting hit results because this object is render by another camera
        };

        scene.RootNode.Add(greenBox);


        // YellowBox is defined after GreenBox, but because it is added to OverlayRenderingLayer
        // it is rendered before GreenBox that is in _customRenderingLayer.
        var yellowBox = new BoxModelNode("YellowBox")
        {
            Position = new Vector3(120, 0, 0),
            Size = new Vector3(100, 40, 40),
            Material = StandardMaterials.Yellow,
            CustomRenderingLayer = scene.OverlayRenderingLayer,
        };

        scene.RootNode.Add(yellowBox);


        if (targetPositionCamera != null)
            targetPositionCamera.Distance = 700;
    }

    private void AddCustomRenderingLayer(Scene scene)
    {
        _renderingLayerCustomCamera = new TargetPositionCamera("RenderingLayerCustomCamera")
        {
            Heading = 0,
            Attitude = 0,
            Distance = 300,
        };

        _customRenderingLayer = new RenderingLayer("CustomRenderingLayer");
     
        _customRenderingLayer.CustomCamera = _renderingLayerCustomCamera;

        if (_useCustomAmbientLight)
            _customRenderingLayer.CustomAmbientLightColor = Color3.White; // Use full ambient light
        
        _customRenderingLayer.CustomViewport = new Viewport(0.1f, 0.7f, 0.2f, 0.2f, 0, 1);
        _customRenderingLayer.IsCustomViewportInRelativeValues = true;

        // By default, absolute coordinates are used:
        //_customRenderingLayer.CustomViewport = new Viewport(50, 50, 200, 150, 0, 1);

        // Clear depth buffer before rendering _customRenderingLayer
        // This will not prevent rendering objects in this rendering layer
        // because other objects from other rendering layers are closer to the camera.
        _customRenderingLayer.ClearDepthStencilBufferBeforeRendering = true;

        if (scene.OverlayRenderingLayer != null)
            scene.AddRenderingLayerAfter(_customRenderingLayer, scene.OverlayRenderingLayer);
    }

    /// <inheritdoc />
    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.SceneUpdating += SceneViewOnSceneUpdating;

        base.OnSceneViewInitialized(sceneView);
    }
    
    private void SceneViewOnSceneUpdating(object? sender, EventArgs e)
    {
        if (_synchronizeCameras && targetPositionCamera != null && _renderingLayerCustomCamera != null)
        {
            _renderingLayerCustomCamera.Heading  = targetPositionCamera.Heading;
            _renderingLayerCustomCamera.Attitude = targetPositionCamera.Attitude;
        }
    }

    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Synchronize cameras", _synchronizeCameras, isChecked => _synchronizeCameras = isChecked);

        ui.CreateCheckBox("Use custom AmbientLight", _useCustomAmbientLight, isChecked =>
        {
            if (_customRenderingLayer != null)
            {
                if (isChecked)
                    _customRenderingLayer.CustomAmbientLightColor = Color3.White;
                else
                    _customRenderingLayer.CustomAmbientLightColor = null;
            }
        });
        
        ui.CreateCheckBox("Use custom Viewport", _useCustomViewport, isChecked =>
        {
            if (_customRenderingLayer != null)
            {
                if (isChecked)
                    _customRenderingLayer.CustomViewport = new Viewport(0.1f, 0.7f, 0.2f, 0.2f, 0, 1);
                else
                    _customRenderingLayer.CustomViewport = null;
            }
        });

        ui.CreateButton("Change custom camera", () =>
        {
            if (_renderingLayerCustomCamera != null)
                _renderingLayerCustomCamera.Distance *= 1.2f;
        });
    }
}