using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

public class ModelRotatorSample : CommonSample
{
    public override string Title => "ModelRotator sample";
    public override string Subtitle => "Rotate the selected teapot. Click on other teapot to select it.\nRotate the camera with the right mouse button.";

    private ModelRotator? _modelRotator;

    private readonly StandardMaterial _commonMaterial;
    private readonly StandardMaterial _selectedMaterial;

    private float _startRotateX;
    private float _startRotateY;
    private float _startRotateZ;

    private ModelNode? _rotatingModel;
    private GroupNode? _testModelsGroupNode;
    private PlanarShadowMeshCreator? _planarShadowMeshCreator;
    private MeshModelNode? _shadowModel;

    public ModelRotatorSample(ICommonSamplesContext context)
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
        float teapotSize = 80;

        var teapotMesh = TestScenes.GetTestMesh(TestScenes.StandardTestScenes.TeapotLowResolution, finalSize: new Vector3(teapotSize, teapotSize, teapotSize));

        _testModelsGroupNode = new GroupNode("TestModelsGroup");
        scene.RootNode.Add(_testModelsGroupNode);

        // Add 10 test teapots (we use while because some random positions may be already occupied by other teapots so we need more random positions)
        while (_testModelsGroupNode.Count < 10)
        {
            var randomPosition = GetRandomPosition(new Vector3(0, 75, 0), new Vector3(350, 100, 350));

            // Check if the randomPosition is free (if there are no other teapot with center that is closer than teapotSize * 1.2
            if (!IsPositionFree(randomPosition, freeRadius: teapotSize * 1.2f, _testModelsGroupNode))
                continue; // Find another position

            var teapotModel = new MeshModelNode(teapotMesh, _commonMaterial, $"Teapot_{_testModelsGroupNode.Count}")
            {
                Transform = new StandardTransform(translateX: randomPosition.X, translateY: randomPosition.Y, translateZ: randomPosition.Z)
            };

            _testModelsGroupNode.Add(teapotModel);
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

    protected override void OnInputEventsManagerInitialized(ManualInputEventsManager inputEventsManager)
    {
        if (_testModelsGroupNode == null || Scene == null)
            return;

        // Use InputEventsManager to subscribe to click event on the spheres
        var allTestModels = _testModelsGroupNode.GetAllChildren<ModelNode>();
        var multiModelNodesEventsSource = new MultiModelNodesEventsSource(allTestModels);

        multiModelNodesEventsSource.PointerClicked += (sender, args) =>
        {
            // start moving object on left click
            if (args.PressedButtons == MouseButtons.Left)
                StartRotatingObject(args.RayHitResult.HitSceneNode);
        };

        inputEventsManager.RegisterEventsSource(multiModelNodesEventsSource);


        // In this sample we show ModelRotator on top of all other objects.
        // This is done by enabling clearing depth buffer before rendering OverlayRenderingLayer.
        // Also, the objects shown by the ModelRotator need to be put into the OverlayRenderingLayer (setting CustomRenderingLayer below).
        // See also Advanced/BackgroundAndOverlayRenderingSample for more information.
        if (Scene.OverlayRenderingLayer != null)
            Scene.OverlayRenderingLayer.ClearDepthStencilBufferBeforeRendering = true;

        // Create ModelRotator
        // Note that ModelRotator is not a SceneNode and cannot be added to the RootNote.
        // Instead, the models that are added to the scene are defined in _modelRotator.ModelRotatorGroupNode (see below).
        _modelRotator = new ModelRotator(inputEventsManager);
        
        // To show ModelRotator on top of other objects, se the CustomRenderingLayer to OverlayRenderingLayer.
        _modelRotator.CustomRenderingLayer = Scene.OverlayRenderingLayer;

        // Handle events:
        _modelRotator.ModelRotateStarted += (sender, args) =>
        {
            if (_rotatingModel != null && _rotatingModel.Transform is StandardTransform standardTransform)
            {
                _startRotateX = standardTransform.RotateX;
                _startRotateY = standardTransform.RotateY;
                _startRotateZ = standardTransform.RotateZ;
            }
            else
            {
                _startRotateX = 0;
                _startRotateY = 0;
                _startRotateZ = 0;
            }
        };

        _modelRotator.ModelRotated += (sender, args) =>
        {
            if (_rotatingModel == null)
                return;

            _rotatingModel.Material = _selectedMaterial;
            if (_rotatingModel.Transform is StandardTransform standardTransform)
            {
                standardTransform.RotateX = _startRotateX + args.RotateX;
                standardTransform.RotateY = _startRotateY + args.RotateY;
                standardTransform.RotateZ = _startRotateZ + args.RotateZ;
            }


            if (_planarShadowMeshCreator != null && _shadowModel != null)
            {
                _planarShadowMeshCreator.UpdateGroupNode();
                _planarShadowMeshCreator.ApplyDirectionalLight(directionalLightDirection: new Vector3(0, -1, 0)); // Top down shadow

                _shadowModel.Mesh = _planarShadowMeshCreator.ShadowMesh;
            }
        };

        _modelRotator.ModelRotateEnded += (sender, args) =>
        {
            // Nothing to do here in this sample
        };



        // !!! IMPORTAMT !!!
        // To show ModelRotator we need to add ModelRotatorGroupNode (as GroupNode) to the Scene.RootNode

        Scene.RootNode.Add(_modelRotator.ModelRotatorGroupNode);


        // Start rotating the first teapot
        StartRotatingObject(allTestModels[0]);
    }

    private void StartRotatingObject(SceneNode? sceneNode)
    {
        if (_modelRotator == null)
            return;

        if (_rotatingModel != null)
        {
            _rotatingModel.Material = _commonMaterial;
            _rotatingModel = null;
        }

        var newRotatingModel = sceneNode as ModelNode;

        if (newRotatingModel == null)
        {
            _modelRotator.IsEnabled = false; // This will also hide _modelScalar.ModelScalarGroupNode
            return;
        }

        newRotatingModel.Material = _selectedMaterial;

        _rotatingModel = newRotatingModel;

        _modelRotator.Position = newRotatingModel.GetCenterPosition();
        _modelRotator.IsEnabled = true;
    }
    
    private bool IsPositionFree(Vector3 spherePosition, float freeRadius, GroupNode groupNode, int skippedSphereIndex = -1)
    {
        bool isFree = true;

        for (int i = 0; i < groupNode.Count; i++)
        {
            if (i == skippedSphereIndex)
                continue;

            if (groupNode[i] is ModelNode oneModel &&
                (spherePosition - oneModel.GetCenterPosition()).Length() < freeRadius)
            {
                isFree = false; 
                break;
            }
        }

        return isFree;
    }

    private void SetupPlanarShadow(Scene scene)
    {
        if (_testModelsGroupNode == null)
            return;

        // Create PlanarShadowMeshCreator
        _planarShadowMeshCreator = new PlanarShadowMeshCreator(_testModelsGroupNode);
        _planarShadowMeshCreator.SetPlane(planeCenterPosition: new Vector3(0, 0, 0), planeNormal: new Vector3(0, 1, 0), planeHeightVector: new Vector3(0, 0, 1), planeSize: new Vector2(1000, 1000));
        _planarShadowMeshCreator.ClipToPlane = false; // No need to clip shadow to plane because plane is big enough (when having smaller plane, turn this on - this creates a lot of additional objects on GC)
        _planarShadowMeshCreator.SimplifyNormalCalculation = false; // Because we show both front and back shadow material, we need to disable simplified normal calculation (see https://www.ab4d.com/help/SharpEngine/html/P_Ab4d_SharpEngine_Utilities_PlanarShadowMeshCreator_SimplifyNormalCalculation.htm)
        
        _planarShadowMeshCreator.ApplyDirectionalLight(directionalLightDirection: new Vector3(0, -1, 0)); // Top down shadow

        if (_planarShadowMeshCreator.ShadowMesh != null)
        {
            _shadowModel = new MeshModelNode(_planarShadowMeshCreator.ShadowMesh, material: StandardMaterials.DimGray, "PlanarShadowModel");
            _shadowModel.BackMaterial = _shadowModel.Material; // Set BackMaterial to prevent showing hole in the shadow that would show if only front-material would be used
            _shadowModel.Transform = new TranslateTransform(0, 0.05f, 0); // Lift the shadow 3D model slightly above the ground

            scene.RootNode.Add(_shadowModel);
        }
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        _modelRotator?.Dispose(); // This will unsubscribe all pointer / mouse events from InputEventsManager
        base.OnDisposed();
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Show rotation X axis", _modelRotator!.IsXAxisRotationCircleShown, isChecked => _modelRotator!.IsXAxisRotationCircleShown = isChecked);
        ui.CreateCheckBox("Show rotation Y axis", _modelRotator!.IsYAxisRotationCircleShown, isChecked => _modelRotator!.IsYAxisRotationCircleShown = isChecked);
        ui.CreateCheckBox("Show rotation Z axis", _modelRotator!.IsZAxisRotationCircleShown, isChecked => _modelRotator!.IsZAxisRotationCircleShown = isChecked);
    }
}