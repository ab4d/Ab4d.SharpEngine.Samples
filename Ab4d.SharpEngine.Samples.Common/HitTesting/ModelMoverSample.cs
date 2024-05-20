using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

public class ModelMoverSample : CommonSample
{
    public override string Title => "ModelMover sample";

    private ModelMover? _modelMover;

    private readonly StandardMaterial _commonMaterial;
    private readonly StandardMaterial _selectedMaterial;

    private SphereModelNode? _movingSphere;
    private Vector3 _startCenterPosition;
    private GroupNode? _testSpheresGroupNode;

    public ModelMoverSample(ICommonSamplesContext context)
        : base(context)
    {
        _commonMaterial = StandardMaterials.Silver;
        _selectedMaterial = StandardMaterials.Orange;

        RotateCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed;
        MoveCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed | MouseAndKeyboardConditions.ControlKey;

        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateScene(Scene scene)
    {
        _testSpheresGroupNode = new GroupNode("TestSpheresGroup");
        scene.RootNode.Add(_testSpheresGroupNode);

        for (int i = 0; i < 10; i++)
        {
            var sphereModelNode = new SphereModelNode(GetRandomPosition(new Vector3(0, 0, 0), new Vector3(400, 200, 400)), radius: 20, material: _commonMaterial);
            _testSpheresGroupNode.Add(sphereModelNode);
        }


        var wireGridNode = new WireGridNode()
        {
            Size = new Vector2(400, 400),
            WidthCellsCount = 8,
            HeightCellsCount = 8
        };

        scene.RootNode.Add(wireGridNode);


        if (targetPositionCamera != null)
            targetPositionCamera.Distance = 800;
    }

    /// <inheritdoc />
    protected override void OnInputEventsManagerInitialized(ManualInputEventsManager inputEventsManager)
    {
        if (_testSpheresGroupNode == null || Scene == null)
            return;

        // Use InputEventsManager to subscribe to click event on the spheres
        var getAllSpheres = _testSpheresGroupNode.GetAllChildren<ModelNode>();
        var multiModelNodesEventsSource = new MultiModelNodesEventsSource(getAllSpheres);

        multiModelNodesEventsSource.PointerClick += (sender, args) =>
        {
            // start moving object on click
            if (args.RayHitResult != null)
                StartMovingObject(args.RayHitResult.HitSceneNode);
        };

        inputEventsManager.RegisterEventsSource(multiModelNodesEventsSource);


        // In this sample we show ModelMover on top of all other objects.
        // This is done by enabling clearing depth buffer before rendering OverlayRenderingLayer.
        // Also, the objects shown by the ModelMover need to be put into the OverlayRenderingLayer (setting CustomRenderingLayer below).
        // See also Advanced/BackgroundAndOverlayRenderingSample for more information.
        if (Scene.OverlayRenderingLayer != null)
            Scene.OverlayRenderingLayer.ClearDepthStencilBufferBeforeRendering = true;

        // Create ModelMover
        // Note that ModelMover is not a SceneNode and cannot be added to the RootNote.
        // Instead, the models that are added to the scene are defined in _modelMover.ModelMoverGroupNode (see below).
        _modelMover = new ModelMover(inputEventsManager);
        
        // To show ModelMover on top of other objects, se the CustomRenderingLayer to OverlayRenderingLayer.
        _modelMover.CustomRenderingLayer = Scene.OverlayRenderingLayer;

        // Handle ModelMoveStarted and ModelMoved
        _modelMover.ModelMoveStarted += (sender, args) =>
        {
            if (_movingSphere != null)
                _startCenterPosition = _movingSphere.CenterPosition;
        };

        _modelMover.ModelMoved += (sender, args) =>
        {
            if (_movingSphere != null)
                _movingSphere.CenterPosition = _startCenterPosition + args.MoveVector;
        };

        _modelMover.ModelMoveEnded += (sender, args) =>
        {
            // Nothing to do here in this sample
        };

        //var groupNode = new GroupNode("TestGroupNode");
        //groupNode.Transform = new AxisAngleRotateTransform(new Vector3(0, 1, 0), 45);
        //groupNode.Add(_modelMover.ModelMoverGroupNode);
        //scene.RootNode.Add(groupNode);


        // !!! IMPORTAMT !!!
        // To show ModelMover we need to add ModelMoverGroupNode (as GroupNode) to the Scene.RootNode

        Scene.RootNode.Add(_modelMover.ModelMoverGroupNode);


        // Start moving the first sphere
        StartMovingObject(getAllSpheres[0]);
    }

    private void StartMovingObject(SceneNode? sceneNode)
    {
        if (_modelMover == null)
            return;

        if (_movingSphere != null)
        {
            _movingSphere.Material = _commonMaterial;
            _movingSphere = null;
        }

        var newMovingSphere = sceneNode as SphereModelNode;

        if (newMovingSphere == null)
        {
            _modelMover.IsEnabled = false; // This will also hide _modelMover.ModelMoverGroupNode
            return;
        }

        newMovingSphere.Material = _selectedMaterial;

        _movingSphere = newMovingSphere;

        _modelMover.Position = newMovingSphere.GetCenterPosition();
        _modelMover.IsEnabled = true;
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        _modelMover?.Dispose(); // This will unsubscribe all pointer / mouse events from InputEventsManager
        base.OnDisposed();
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Show X axis", _modelMover!.IsXAxisShown, isChecked => _modelMover!.IsXAxisShown = isChecked);
        ui.CreateCheckBox("Show Y axis", _modelMover!.IsYAxisShown, isChecked => _modelMover!.IsYAxisShown = isChecked);
        ui.CreateCheckBox("Show Z axis", _modelMover!.IsZAxisShown, isChecked => _modelMover!.IsZAxisShown = isChecked);

        ui.AddSeparator();

        ui.CreateCheckBox("Show movable planes", _modelMover!.ShowMovablePlanes, isChecked => _modelMover!.ShowMovablePlanes = isChecked);
    }
}