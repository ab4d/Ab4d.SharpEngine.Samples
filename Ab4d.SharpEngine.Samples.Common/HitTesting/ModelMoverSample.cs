using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

public class ModelMoverSample : CommonSample
{
    public override string Title => "ModelMover sample";
    public override string Subtitle => "Move selected sphere around. Click on other sphere to select it.\nRotate the camera with the right mouse button.";

    private ModelMover? _modelMover;

    private readonly StandardMaterial _commonMaterial;
    private readonly StandardMaterial _selectedMaterial;
    private readonly StandardMaterial _invalidPositionMaterial;

    private SphereModelNode? _movingSphere;
    private Vector3 _startCenterPosition;
    private GroupNode? _testSpheresGroupNode;
    private PlanarShadowMeshCreator? _planarShadowMeshCreator;
    private MeshModelNode? _shadowModel;

    private bool _preventCollisions = true;

    public ModelMoverSample(ICommonSamplesContext context)
        : base(context)
    {
        _commonMaterial = StandardMaterials.Silver;
        _selectedMaterial = StandardMaterials.Orange;
        _invalidPositionMaterial = StandardMaterials.Red;

        RotateCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed;
        MoveCameraConditions = MouseAndKeyboardConditions.RightMouseButtonPressed | MouseAndKeyboardConditions.ControlKey;

        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateScene(Scene scene)
    {
        _testSpheresGroupNode = new GroupNode("TestSpheresGroup");
        scene.RootNode.Add(_testSpheresGroupNode);

        // Add 10 test spheres (we use while because some random positions may be already occupied by other spheres so we need more random positions)
        while (_testSpheresGroupNode.Count < 10)
        {
            var randomPosition = GetRandomPosition(new Vector3(0, 75, 0), new Vector3(350, 100, 350));

            if (!IsPositionFree(randomPosition, freeRadius: 50, _testSpheresGroupNode))
                continue; // Find another position

            var sphereModelNode = new SphereModelNode(randomPosition, radius: 20, material: _commonMaterial);
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


        SetupPlanarShadow(scene);
    }

    /// <inheritdoc />
    protected override void OnInputEventsManagerInitialized(ManualInputEventsManager inputEventsManager)
    {
        if (_testSpheresGroupNode == null || Scene == null)
            return;

        // Use InputEventsManager to subscribe to click event on the spheres
        var allTestModels = _testSpheresGroupNode.GetAllChildren<ModelNode>();
        var multiModelNodesEventsSource = new MultiModelNodesEventsSource(allTestModels);

        multiModelNodesEventsSource.PointerClick += (sender, args) =>
        {
            // start moving object on left click
            if (args.PressedButtons == MouseButtons.Left)
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
            else
                _startCenterPosition = Vector3.Zero;
        };

        _modelMover.ModelMoved += (sender, args) =>
        {
            if (_movingSphere == null)
                return;

            var newPosition = _startCenterPosition + args.MoveVector;


            // prevent moving sphere below plane with y = 0
            if (newPosition.Y < 25)
            {
                args.PreventMove = true; // Set PreventMove to true to prevent automatic moving of ModelMover
                return;
            }

            
            int sphereIndex = _testSpheresGroupNode.IndexOf(_movingSphere);
            
            // prevent collision
            if (_preventCollisions && !IsPositionFree(newPosition, 40, _testSpheresGroupNode, skippedSphereIndex: sphereIndex))
            {
                _movingSphere.Material = _invalidPositionMaterial; // Show red material
                args.PreventMove = true; // Set PreventMove to true to prevent automatic moving of ModelMover
                return;
            }

            _movingSphere.Material = _selectedMaterial;
            _movingSphere.CenterPosition = newPosition;


            if (_planarShadowMeshCreator != null && _shadowModel != null)
            {
                _planarShadowMeshCreator.UpdateGroupNode();
                _planarShadowMeshCreator.ApplyDirectionalLight(directionalLightDirection: new Vector3(0, -1, 0)); // Top down shadow

                _shadowModel.Mesh = _planarShadowMeshCreator.ShadowMesh;
            }
        };

        _modelMover.ModelMoveEnded += (sender, args) =>
        {
            // Nothing to do here in this sample
        };



        // !!! IMPORTAMT !!!
        // To show ModelMover we need to add ModelMoverGroupNode (as GroupNode) to the Scene.RootNode

        Scene.RootNode.Add(_modelMover.ModelMoverGroupNode);


        // Start moving the first sphere
        StartMovingObject(allTestModels[0]);
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
    
    private bool IsPositionFree(Vector3 spherePosition, float freeRadius, GroupNode groupNode, int skippedSphereIndex = -1)
    {
        bool isFree = true;

        for (int i = 0; i < groupNode.Count; i++)
        {
            if (i == skippedSphereIndex)
                continue;

            if (groupNode[i] is SphereModelNode oneSphere &&
                (spherePosition - oneSphere.CenterPosition).Length() < freeRadius)
            {
                isFree = false; 
                break;
            }
        }

        return isFree;
    }

    private void SetupPlanarShadow(Scene scene)
    {
        if (_testSpheresGroupNode == null)
            return;

        // Create PlanarShadowMeshCreator
        _planarShadowMeshCreator = new PlanarShadowMeshCreator(_testSpheresGroupNode);
        _planarShadowMeshCreator.SetPlane(planeCenterPosition: new Vector3(0, 0, 0), planeNormal: new Vector3(0, 1, 0), planeHeightVector: new Vector3(0, 0, 1), planeSize: new Vector2(1000, 1000));
        _planarShadowMeshCreator.ClipToPlane = false; // No need to clip shadow to plane because plane is big enough (when having smaller plane, turn this on - this creates a lot of additional objects on GC)

        _planarShadowMeshCreator.ApplyDirectionalLight(directionalLightDirection: new Vector3(0, -1, 0)); // Top down shadow

        if (_planarShadowMeshCreator.ShadowMesh != null)
        {
            _shadowModel = new MeshModelNode(_planarShadowMeshCreator.ShadowMesh, StandardMaterials.DimGray, "PlanarShadowModel");
            _shadowModel.Transform = new Ab4d.SharpEngine.Transformations.TranslateTransform(0, 0.05f, 0); // Lift the shadow 3D model slightly above the ground

            scene.RootNode.Add(_shadowModel);
        }
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
        
        ui.AddSeparator();
        
        ui.CreateCheckBox("Prevent collisions", _preventCollisions, isChecked => _preventCollisions = isChecked);
    }
}