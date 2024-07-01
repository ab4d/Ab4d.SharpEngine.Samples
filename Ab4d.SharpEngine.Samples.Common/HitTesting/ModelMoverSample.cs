using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
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

    private float _modelMoverRotationAngle;

    private StandardTransform? _modelMoverCustomTransform;

    private SphereModelNode? _movingSphere;
    private Vector3 _startCenterPosition;
    private GroupNode? _testSpheresGroupNode;
    private PlanarShadowMeshCreator? _planarShadowMeshCreator;
    private MeshModelNode? _shadowModel;

    private bool _preventCollisions = true;
    private bool _preventMovingBlowPlane = true;

    public ModelMoverSample(ICommonSamplesContext context)
        : base(context)
    {
        _commonMaterial = StandardMaterials.Silver;
        _selectedMaterial = StandardMaterials.Orange;
        _invalidPositionMaterial = StandardMaterials.Red;

        RotateCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed;
        MoveCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed | PointerAndKeyboardConditions.ControlKey;

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

        multiModelNodesEventsSource.PointerClicked += (sender, args) =>
        {
            // start moving object on left click
            if (args.PressedButtons == PointerButtons.Left)
                StartMovingObject(args.RayHitResult.HitSceneNode);
        };

        inputEventsManager.RegisterEventsSource(multiModelNodesEventsSource);


        // In this sample we show ModelMover on top of all other objects.
        // This is done by enabling clearing depth buffer before rendering OverlayRenderingLayer.
        // Also, the objects shown by the ModelMover need to be put into the OverlayRenderingLayer (setting CustomRenderingLayer below).
        // See also Advanced/BackgroundAndOverlayRenderingSample for more information on how to render 3D objects on top of below other 3D objects.
        if (Scene.OverlayRenderingLayer != null)
            Scene.OverlayRenderingLayer.ClearDepthStencilBufferBeforeRendering = true;

        // Create ModelMover by using the default constructor
        // Note that ModelMover is not a SceneNode and cannot be added to the RootNote.
        // Instead, the models that are added to the scene are defined in _modelMover.ModelMoverGroupNode (see below).
        _modelMover = new ModelMover(inputEventsManager);
        
        // To customize the look of the ModelRotator use the following constructor (here the default values are used):
        //_modelMover = new ModelMover(inputEventsManager,
        //                             xAxisVector: new Vector3(1, 0, 0),
        //                             yAxisVector: new Vector3(0, 1, 0),
        //                             zAxisVector: new Vector3(0, 0, 1),
        //                             axisLength: 50, 
        //                             axisRadius: 2, 
        //                             axisArrowRadius: 6);

        // To show ModelMover on top of other objects, se the CustomRenderingLayer to OverlayRenderingLayer.
        _modelMover.CustomRenderingLayer = Scene.OverlayRenderingLayer;

        // By default, the IsAutomaticallyMoved property is set to true.
        // It automatically updates the Position of the ModelMover when it is moved.
        // If we want to have a custom transformation for the ModelMover, then set that to false
        // (see handling of the "Use custom transform" button click at the end of this file).
        //_modelMover.IsAutomaticallyMoved = true;

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


            // prevent moving sphere below plane (actually because sphere radius is 20, we allow floating 5 units above the plane)
            if (_preventMovingBlowPlane && newPosition.Y < 25)
            {
                newPosition = new Vector3(newPosition.X, 25, newPosition.Z);
                
                // We can change the args.MoveVector and in case ModelMover.IsAutomaticallyMoved is true,
                // the updated MoveVector will be used to move the ModelMover.
                args.MoveVector = newPosition - _startCenterPosition;

                // Instead of changing the args.MoveVector, we could also prevent moving the ModelMover by setting PreventMove to true:
                //args.PreventMove = true; // Set PreventMove to true to prevent automatic moving of ModelMover
                //return;
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

            // If we are using custom transformation, update its translation
            if (_modelMoverCustomTransform != null)
                _modelMoverCustomTransform.SetTranslate(newPosition);
            // else - we do not need to do anything because by default the IsAutomaticallyMoved is set to true


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

        // If we are using custom transformation, update its translation
        if (_modelMoverCustomTransform != null)
            _modelMoverCustomTransform.SetTranslate(newMovingSphere.GetCenterPosition());
        else
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
        ui.CreateCheckBox("Prevent moving below plane", _preventMovingBlowPlane, isChecked => _preventMovingBlowPlane = isChecked);
        ui.CreateCheckBox("Prevent collisions", _preventCollisions, isChecked => _preventCollisions = isChecked);

        ui.AddSeparator();
        ui.CreateButton("Rotate ModelMover", () =>
        {
            _modelMoverRotationAngle += 30;

            // To rotate the ModelMover, we need to call SetRotation (this is required to provide correct directions of the arrows for pointer dragging)
            // Here we rotate ModelMover around Y (up) axis
            _modelMover.SetRotation(0, _modelMoverRotationAngle, 0);

            // When we use a custom transform, we also need to set rotation there
            if (_modelMoverCustomTransform != null)
                _modelMoverCustomTransform.RotateY = _modelMoverRotationAngle;
        });
        
        ui.CreateButton("Use custom transform", () =>
        {
            if (_modelMoverCustomTransform != null)
                return; // Custom transform is already used

            // Use StandardTransform as a custom transformation in this sample
            _modelMoverCustomTransform = new StandardTransform();
            _modelMoverCustomTransform.SetTranslate(_modelMover.Position);
            _modelMoverCustomTransform.SetScale(1.5f); // set uniform scale to 1.5
            _modelMoverCustomTransform.RotateY = _modelMoverRotationAngle;

            // Prevent automatic moving of ModelMover and ...
            _modelMover.IsAutomaticallyMoved = false;

            // ... and set our custom transform on the ModelGroup that shows ModelRotator.
            // This replaces an internal transformation that is used by default for ModelMoverGroupNode.Transform.
            _modelMover.ModelMoverGroupNode.Transform = _modelMoverCustomTransform;
        });
    }
}