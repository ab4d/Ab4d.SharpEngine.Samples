using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using System.Numerics;
using System;
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

        //var meshGeometry3D = (MeshGeometry3D) originalDragonModel3D.Geometry;

        //// Update positions in MeshGeometry3D to a desired size
        //var transformedMeshGeometry3D = Ab3d.Utilities.MeshUtils.PositionAndScaleMeshGeometry3D(meshGeometry3D,
        //                                                                                        position: new Point3D(0, 0, 0),
        //                                                                                        positionType: PositionTypes.Bottom,
        //                                                                                        finalSize: new Size3D(50, 50, 50),
        //                                                                                        preserveAspectRatio: true,
        //                                                                                        transformNormals: true);

        //var backgroundDragonModel3D = new GeometryModel3D(transformedMeshGeometry3D, new DiffuseMaterial(Brushes.Blue));
        //backgroundDragonModel3D.Transform = new TranslateTransform3D(-30, 0, 0);

        //// We could also use:
        ////var backgroundDragonModel3D = originalDragonModel3D.Clone();
        ////Ab3d.Utilities.ModelUtils.ChangeMaterial(backgroundDragonModel3D, newMaterial: new DiffuseMaterial(Brushes.Blue), newBackMaterial: null);
        ////Ab3d.Utilities.TransformationsHelper.AddTransformation(backgroundDragonModel3D, new TranslateTransform3D(-30, 0, 0));

        //AddBackgroundObject(backgroundDragonModel3D);


        //var overlayDragonModel3D = new GeometryModel3D(transformedMeshGeometry3D, new DiffuseMaterial(Brushes.Red));
        //overlayDragonModel3D.Transform = new TranslateTransform3D(30, 0, 0);

        //AddOverlayObject(overlayDragonModel3D);
    }

    private void AddCustomRenderedLines(Scene scene)
    {
        // Add 3D lines to the background
        // The most important is to render those lines before any other objects are rendered.
        // This is done with setting CustomRenderingQueue property where we specify the BackgroundRenderingQueue.
        // We can also disable depth reading and writing (to render them regardless of any previously rendered objects).
        AddLines(scene,
                 new Vector3(-90, 0, 30), 
                 positionsCount: 10, 
                 lineColor: Colors.Blue, 
                 customRenderingQueue: scene.BackgroundRenderingLayer);

        // First add 3D lines that are rendered without any special setting
        // readZBuffer and writeZBuffer will be set to true so they will "obey" the depth rules - will be hidden behind objects closer to the camera.
        AddLines(scene,
                 new Vector3(-25, 0, 30), 
                 positionsCount: 10, 
                 lineColor: Colors.LightGray, 
                 customRenderingQueue: null);

        // Add 3D lines that will be rendered on top of other 3D objects.
        // This is achieved with disabling depth reading and writing.
        // We also put that line into the OverlayRenderingQueue
        // (though this is not needed, because the ThickLineEffect that renders the 3D lines can use the ReadZBuffer and WriteZBuffer values from LineMaterial)
        AddLines(scene,
                 new Vector3(40, 0, 30), 
                 positionsCount: 10, 
                 lineColor: Colors.Red, 
                 customRenderingQueue: scene.OverlayRenderingLayer);


        //// Instead of using ScreenSpaceLineNode and LineMaterial that support ReadZBuffer and WriteZBuffer,
        //// we could also use out custom rendering steps and then define standard WPF lines
        //// and use SetDXAttribute to set CustomRenderingQueue to BackgroundRenderingQueue or OverlayRenderingQueue:
        //var lineVisual3D = new LineVisual3D()
        //{
        //    StartPosition = new Point3D(-100, 10, 20),
        //    EndPosition   = new Point3D(100,  10, 20),
        //    LineColor     = Colors.Orange,
        //    LineThickness = 10
        //};

        ////lineVisual3D.SetDXAttribute(DXAttributeType.CustomRenderingQueue, MainDXViewportView.DXScene.BackgroundRenderingQueue);
        //lineVisual3D.SetDXAttribute(DXAttributeType.CustomRenderingQueue, MainDXViewportView.DXScene.OverlayRenderingQueue);

        //MainViewport.Children.Add(lineVisual3D);


        // Using SetDXAttribute will not work on TextVisual3D, CenteredTextVisual3D and WireGridVisual3D
        // (the reason is that those objects create a more complex hierarchy and the DXAttribute are not propagated to the child objects).
        // To solve this you can use the following trick:

        //var textVisual3D = new TextVisual3D()
        //{
        //    Position = new Point3D(-90, 50, -20),
        //    Text = "TextVisual3D",
        //    FontSize = 30,
        //    LineThickness = 4,
        //    TextColor = Colors.Silver
        //};

        //// OnDXResourcesInitializedAction is called when the DXEngine creates the SceneNode from the and initializes it (so its children are created)
        //textVisual3D.SetDXAttribute(DXAttributeType.OnDXResourcesInitializedAction, new Action<object>(node =>
        //{
        //    var sceneNode = node as SceneNode;
        //    if (sceneNode == null)
        //        return;

        //    sceneNode.ForEachChildNode(new Action<SceneNode>(childSceneNode =>
        //    {
        //        var screenSpaceLineNode = childSceneNode as ScreenSpaceLineNode;
        //        if (screenSpaceLineNode != null)
        //            screenSpaceLineNode.CustomRenderingQueue = MainDXViewportView.DXScene.OverlayRenderingQueue;
        //    }));
        //}));
    }

    private void AddLines(Scene scene, Vector3 startPosition, int positionsCount, Color4 lineColor, RenderingLayer? customRenderingQueue = null)
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

        //// ThickLineEffect that renders the 3D lines can use the ReadZBuffer and WriteZBuffer values from LineMaterial.
        ////
        //// When ReadZBuffer is false (true by default), then line is rendered without checking the depth buffer -
        //// so it is always rendered even it is is behind some other 3D object and should not be visible from the camera).
        ////
        //// When WriteZBuffer is false (true by default), then when rendering the 3D line, the depth of the line is not
        //// written to the depth buffer. So No other object will be made hidden by the line even if that object is behind the line.
        //var lineMaterial = new LineMaterial()
        //{
        //    LineColor     = lineColor,
        //    LineThickness = 2,
        //};

        //_disposables.Add(lineMaterial);

        var multiLineNode = new MultiLineNode(positions, isLineStrip: false, lineThickness: 2, lineColor: lineColor, name: $"Lines_{lineColor.ToKnownColorString()}");

        if (customRenderingQueue != null)
            multiLineNode.CustomRenderingLayer = customRenderingQueue;

        scene.RootNode.Add(multiLineNode);


        //var screenSpaceLineNode = new ScreenSpaceLineNode(positions, isLineStrip: false, isLineClosed: false, lineMaterial: lineMaterial);
        
        //// It is also needed that the 3D line is put to the Background or Overlay rendering queue so that it is rendered before or after other 3D objects.
        //screenSpaceLineNode.CustomRenderingQueue = customRenderingQueue;

        //var sceneNodeVisual3D = new SceneNodeVisual3D(screenSpaceLineNode);
        //MainViewport.Children.Add(sceneNodeVisual3D);
    }

    //private void AddBackgroundObject(RenderedNode renderedNode, Scene scene)
    //{
    //    renderedNode

    //    geometryModel3D.SetDXAttribute(DXAttributeType.CustomRenderingQueue, MainDXViewportView.DXScene.BackgroundRenderingQueue);

    //    var modelVisual3D = geometryModel3D.CreateModelVisual3D();
    //    MainViewport.Children.Add(modelVisual3D);
    //}

    //private void AddOverlayObject(Model3D geometryModel3D)
    //{
    //    geometryModel3D.SetDXAttribute(DXAttributeType.CustomRenderingQueue, MainDXViewportView.DXScene.OverlayRenderingQueue);

    //    var modelVisual3D = geometryModel3D.CreateModelVisual3D();
    //    MainViewport.Children.Add(modelVisual3D);
    //}

    //private void AddOverlayObject(ModelVisual3D modelVisual3D)
    //{
    //    modelVisual3D.SetDXAttribute(DXAttributeType.CustomRenderingQueue, MainDXViewportView.DXScene.OverlayRenderingQueue);
    //    MainViewport.Children.Add(modelVisual3D);
    //}

    //private void AddStandardRenderedObjects()
    //{
    //    for (int x = -3; x <= 3; x++)
    //    {
    //        for (int y = -3; y <= 3; y++)
    //        {
    //            var boxVisual3D = new BoxVisual3D()
    //            {
    //                CenterPosition = new Point3D(x * 30, 0, y * 30),
    //                Size = new Size3D(10, 10, 10),
    //                Material = new DiffuseMaterial(Brushes.Yellow)
    //            };

    //            MainViewport.Children.Add(boxVisual3D);
    //        }
    //    }
    //}


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

    //private void ClearDepthBufferRadioButton_OnChecked(object sender, RoutedEventArgs e)
    //{
    //    if (MainDXViewportView.DXScene == null)
    //        return;

    //    SetupClearingDepthBuffer();
    //    MainDXViewportView.Refresh();
    //}

    //private void DisableDepthReadRadioButton_OnChecked(object sender, RoutedEventArgs e)
    //{
    //    if (MainDXViewportView.DXScene == null)
    //        return;

    //    SetupDisabledDepthRead();
    //    MainDXViewportView.Refresh();
    //}

    //private void OnOverlayHitTestingCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
    //{
    //    if (MainDXViewportView.DXScene == null)
    //        return;

    //    UpdateHitTestingOverlay();
    //}

    //private void OnBackgroundHitTestingCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
    //{
    //    if (MainDXViewportView.DXScene == null)
    //        return;

    //    UpdateHitTestingBackground();
    //}


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
        ui.CreateLabel("RED: Overlay objects").SetColor(Colors.Red);
        ui.AddSeparator();
        ui.CreateLabel("BLACK CROSS: Rotation center");
    }
}