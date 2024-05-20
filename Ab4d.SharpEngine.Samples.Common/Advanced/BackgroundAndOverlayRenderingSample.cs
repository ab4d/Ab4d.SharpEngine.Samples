using System;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using System.Numerics;
using Ab4d.SharpEngine.RenderingLayers;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class BackgroundAndOverlayRenderingSample : CommonSample
{
    public override string Title => "Rendering 3D objects and lines in the background or over other 3D objects";
    
    private WireCrossNode? _rotationCenterWireCross;
    
    private bool _clearBackgroundDepthBuffer = true;
    private bool _clearOverlayDepthBuffer = true;
    private bool _lowerPriorityBackgroundHitTesting = true;
    private bool _higherPriorityOverlayHitTesting = true;

    public BackgroundAndOverlayRenderingSample(ICommonSamplesContext context)
        : base(context)
    {
        RotateAroundMousePosition = true;
        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateScene(Scene scene)
    {
        UpdateClearingDepthBuffer(scene);
        UpdateHitTestingPriorities(scene);

        AddStandardRenderedObjects(scene);

        AddCustomRenderedObjects(scene);
        AddCustomRenderedLines(scene);

        _rotationCenterWireCross = new WireCrossNode(position: new Vector3(0, 0, 0), lineColor: Color3.Black, lineLength: 40, lineThickness: 4, "RotationCenterWireCross") { Visibility = SceneNodeVisibility.Hidden };
        scene.RootNode.Add(_rotationCenterWireCross);
    }

    /// <inheritdoc />
    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Distance = 360;

            // Animate camera from (Heading: -40; Attitude: -50) to (Heading: -5; Attitude: -5) in 3 seconds using CubicEaseInOutFunction

            // We call RotateTo here in OnSceneViewInitialized because there the targetPositionCamera
            // is already set to the SceneView.Camera. This is required when calling RotateTo.
            // In OnCreateScene the targetPositionCamera is created but not set to the SceneView.

            targetPositionCamera.Heading = -40;
            targetPositionCamera.Attitude = -50;

            targetPositionCamera.RotateTo(targetAttitude: -5, 
                                          targetHeading: -5, 
                                          animationDurationInMilliseconds: 2000, 
                                          easingFunction: Ab4d.SharpEngine.Common.EasingFunctions.CubicEaseInOutFunction);
        }

        base.OnSceneViewInitialized(sceneView);
    }

    /// <inheritdoc />
    public override void InitializeMouseCameraController(ManualMouseCameraController mouseCameraController)
    {
        // Show wire-cross that shows the RotationCenterPosition when rotating the camera
        mouseCameraController.CameraRotateStarted += (sender, args) =>
        {
            if (_rotationCenterWireCross != null && targetPositionCamera != null)
            {
                _rotationCenterWireCross.Position = targetPositionCamera.RotationCenterPosition ?? targetPositionCamera.TargetPosition;
                _rotationCenterWireCross.Visibility = SceneNodeVisibility.Visible;
            }
        };

        mouseCameraController.CameraRotateEnded += (sender, args) =>
        {
            if (_rotationCenterWireCross != null)
                _rotationCenterWireCross.Visibility = SceneNodeVisibility.Hidden;
        };

        base.InitializeMouseCameraController(mouseCameraController);
    }

    private void AddStandardRenderedObjects(Scene scene)
    {
        for (int x = -3; x <= 3; x++)
        {
            for (int y = -3; y <= 3; y++)
            {
                var boxModel = new BoxModelNode()
                {
                    Position = new Vector3(x * 30, 0, y * 30),
                    Size = new Vector3(10, 10, 10),
                    Material = StandardMaterials.LightGray,
                    Name = $"GrayBox_{x}_{y}"
                };

                scene.RootNode.Add(boxModel);
            }
        }
    }

    private void AddCustomRenderedObjects(Scene scene)
    {
        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\Models\teapot-hires.obj");

        var readerObj = new ReaderObj();
        var backgroundTeapotModel = readerObj.ReadSceneNodes(fileName);
        backgroundTeapotModel.Name = "BackgroundTeapotModel";

        ModelUtils.ChangeMaterial(backgroundTeapotModel, StandardMaterials.Blue);

        ModelUtils.PositionAndScaleSceneNode(backgroundTeapotModel,
                                             position: new Vector3(-60, 0, -40),
                                             positionType: PositionTypes.Center,
                                             finalSize: new Vector3(80, 80, 80));

        // Set CustomRenderingLayer on backgroundTeapotModel to scene.BackgroundRenderingLayer
        // This will put the RenderingItems that are created from backgroundTeapotModel to the BackgroundRenderingLayer.
        // 
        // To set CustomRenderingLayer:
        // - On ModelNodes or LineNodes, you can use the CustomRenderingLayer property.
        // - On GroupNode, you need to call ModelUtils.SetCustomRenderingLayer method.

        //backgroundTeapotModel.CustomRenderingLayer = scene.BackgroundRenderingLayer;
        Ab4d.SharpEngine.Utilities.ModelUtils.SetCustomRenderingLayer(backgroundTeapotModel, scene.BackgroundRenderingLayer);
        
        // Add backgroundTeapotModel to scene
        scene.RootNode.Add(backgroundTeapotModel);

        
        var overlayTeapotModel = readerObj.ReadSceneNodes(fileName);
        overlayTeapotModel.Name = "OverlayTeapotModel";

        ModelUtils.ChangeMaterial(overlayTeapotModel, StandardMaterials.Red);

        ModelUtils.PositionAndScaleSceneNode(overlayTeapotModel,
                                             position: new Vector3(60, 0, -40),
                                             positionType: PositionTypes.Center,
                                             finalSize: new Vector3(80, 80, 80));

        // Add overlayTeapotModel to OverlayRenderingLayer

        Ab4d.SharpEngine.Utilities.ModelUtils.SetCustomRenderingLayer(overlayTeapotModel, scene.OverlayRenderingLayer);
        scene.RootNode.Add(overlayTeapotModel);
    }

    private void AddCustomRenderedLines(Scene scene)
    {
        // Add 3D lines to the background
        // The most important is to render those lines before any other objects are rendered.
        // This is done with setting CustomRenderingLayer property where we specify the BackgroundRenderingLayer.
        // We also need to clear the depth buffer after BackgroundRenderingLayer is rendered (this is done in UpdateClearingDepthBuffer method).
        AddLines(scene,
                 new Vector3(-90, 0, 30), 
                 positionsCount: 10, 
                 lineColor: Colors.Blue, 
                 customRenderingLayer: scene.BackgroundRenderingLayer);

        // Add Gray lines that are rendered normally.
        AddLines(scene,
                 new Vector3(-25, 0, 30), 
                 positionsCount: 10, 
                 lineColor: Colors.LightGray, 
                 customRenderingLayer: null);

        // Add 3D lines that will be rendered on top of other 3D objects.
        // This is done by putting them into the OverlayRenderingLayer.
        // We also need to clear the depth buffer after BackgroundRenderingLayer is rendered (this is done in UpdateClearingDepthBuffer method).
        AddLines(scene,
                 new Vector3(40, 0, 30), 
                 positionsCount: 10, 
                 lineColor: Colors.Red, 
                 customRenderingLayer: scene.OverlayRenderingLayer);
    }

    private void AddLines(Scene scene, Vector3 startPosition, int positionsCount, Color4 lineColor, RenderingLayer? customRenderingLayer = null)
    {
        Vector3[] positions = new Vector3[positionsCount * 2];
        Vector3 position = startPosition;

        int index = 0;
        for (int i = 0; i < positionsCount; i++)
        {
            positions[index] = position;
            positions[index + 1] = position + new Vector3(50, 0, 0);

            index += 2;
            position += new Vector3(0, 0, 10);
        }

        var multiLineNode = new MultiLineNode(positions, isLineStrip: false, lineThickness: 2, lineColor: lineColor, name: $"Lines_{lineColor.ToKnownColorString()}");

        if (customRenderingLayer != null)
            multiLineNode.CustomRenderingLayer = customRenderingLayer;

        scene.RootNode.Add(multiLineNode);
    }

    private void UpdateClearingDepthBuffer(Scene? scene)
    {
        if (scene == null)
            return;

        if (scene.BackgroundRenderingLayer != null)
            scene.BackgroundRenderingLayer.ClearDepthStencilBufferAfterRendering = _clearBackgroundDepthBuffer;

        if (scene.OverlayRenderingLayer != null)
            scene.OverlayRenderingLayer.ClearDepthStencilBufferBeforeRendering = _clearOverlayDepthBuffer;
    }

    private void UpdateHitTestingPriorities(Scene? scene)
    {
        if (scene == null)
            return;

        // Update BackgroundRenderingLayer and OverlayRenderingLayer in DefaultHitTestOptions
        //
        // When OverlayRenderingLayer is specified, then the objects that are assigned to that rendering layer will be considered closer to the camera than objects from other rendering layers.
        // This means that the GetClosestHitObject method will return objects from the specified rendering queue even if in 3D space they are behind objects from other rendering layers.
        //
        // When BackgroundRenderingLayer is specified, then the objects that are assigned to that rendering layer will be considered farther from the camera than objects from other rendering layers.
        // This means that the GetClosestHitObject method will return objects from any other rendering queue if they exist before returning objects from the specified rendering layer.
        //
        // Note that when calling GetAllHitObjects, then those two property do not have effect.

        scene.DefaultHitTestOptions.BackgroundRenderingLayer = _lowerPriorityBackgroundHitTesting ? scene.BackgroundRenderingLayer : null;
        scene.DefaultHitTestOptions.OverlayRenderingLayer = _higherPriorityOverlayHitTesting ? scene.OverlayRenderingLayer : null;
    }

    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Enable background objects", _clearBackgroundDepthBuffer, isChecked =>
        {
            _clearBackgroundDepthBuffer = isChecked;
            UpdateClearingDepthBuffer(this.Scene);
        });
        
        ui.CreateCheckBox("Enable overlay objects", _clearOverlayDepthBuffer, isChecked =>
        {
            _clearOverlayDepthBuffer = isChecked;
            UpdateClearingDepthBuffer(this.Scene);
        });
        
        ui.AddSeparator();

        ui.CreateCheckBox("Lower priority for BG objects (?):When checked then GetClosestHitObject method will consider objects in the BackgroundLayer as farther away from the camera as other 3D objects.\nYou can check that by rotating the camera around the last gray box that is shown in front of blue teapot but is actually farther away from the camera.", 
            _lowerPriorityBackgroundHitTesting, isChecked =>
        {
            _lowerPriorityBackgroundHitTesting = isChecked;
            UpdateHitTestingPriorities(this.Scene);
        });
        
        ui.CreateCheckBox("Prioritize overlay objects (?):When checked then GetClosestHitObject method will consider objects in the OverlayLayer as closer to the camera as other 3D objects.\nYou can check that by rotating the camera around the first gray box that is front of red teapot (it is not visible despite being closer the camera than the teapot).", 
            _higherPriorityOverlayHitTesting, isChecked =>
        {
            _higherPriorityOverlayHitTesting = isChecked;
            UpdateHitTestingPriorities(this.Scene);
        });
        

        ui.CreateLabel("Legend:", isHeader: true);
        ui.CreateLabel("BLUE: Background objects").SetColor(Colors.Blue);
        ui.CreateLabel("GRAY: Standard rendered objects").SetColor(Colors.DimGray);
        ui.CreateLabel("RED: Overlay objects (always on top)").SetColor(Colors.Red);
        ui.AddSeparator();
        ui.CreateLabel("BLACK CROSS: Rotation center");
    }
}