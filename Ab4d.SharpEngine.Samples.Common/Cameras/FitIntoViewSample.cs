using Ab4d.SharpEngine.Cameras;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public class FitIntoViewSample : CommonSample
{
    public override string Title => "FitIntoView";

    private bool _automaticallyFitIntoView = false;
    private bool _adjustTargetPosition = true;
    private bool _isWireGridIncluded = true;
    private bool _areBoxesIncluded = true;
    private bool _showSceneBoundingBox = false;
    private float _marginAdjustmentFactor = 1.0f;

    private FitIntoViewType _fitIntoViewType = FitIntoViewType.CheckAllPositions;

    private bool _isInFitIntoViewCall;

    private bool _isTargetPositionCamera = true; // false: FreeCamera
    private ProjectionTypes _projectionType = ProjectionTypes.Perspective;

    private Random _rnd = new Random();

    private Camera? _selectedCamera;
    private GroupNode? _boxesGroup;
    private WireGridNode? _wireGridNode;

    private Vector3[]? _corners;
    private WireBoxNode? _sceneWireBoundingBoxNode;

    public FitIntoViewSample(ICommonSamplesContext context)
        : base(context)
    {

    }

    protected override void OnCreateScene(Scene scene)
    {
        CreateRandomScene();

        // It is possible to call FitIntoView even before the size of the Viewport3D is know (though this size is required to actually execute fit into view).
        // In this case and when waitUntilCameraIsValid parameter is true (by default),
        // then the camera will execute FitIntoView when the size of Viewport3D is set.
        // 
        // We do not call this here, because in this sample FitIntoView is called in the FitIntoView method defined below.
        //TargetPositionCamera1.FitIntoView(waitUntilCameraIsValid: true);

        // We need to wait until Loaded event because the MainViewport needs to have its size defined for FitIntoView to work
        FitIntoView();
    }

    protected override void OnDisposed()
    {
        if (_selectedCamera != null)
        {
            _selectedCamera.CameraChanged -= OnCameraChanged;
            _selectedCamera = null;
        }
        
        base.OnDisposed();
    }

    protected override Camera OnCreateCamera()
    {
        return CreateCamera();
    }

    private void FitIntoView(bool isAnimated = false)
    {
        if (_boxesGroup == null || Scene == null)
            return;

        _isInFitIntoViewCall = true; // Prevent infinite recursion (FitIntoView is called from CameraChanged)

        int animationDuration;
        Func<float, float>? easingFunction;

        if (isAnimated)
        {
            animationDuration = 330; // 1/3 second
            easingFunction = Ab4d.SharpEngine.Common.EasingFunctions.CubicEaseInOutFunction;
        }
        else
        {
            // To immediately change the camera without any animation, we can call FitIntoView method
            // that does not take animationDuration, easingFunction parameters.
            // But here we simplify the code and just set animationDuration to 0 - this also prevent creating an animation.
            
            animationDuration = 0;
            easingFunction = null;
        }
        
        if (_selectedCamera is IFitIntoViewCamera fitIntoViewCamera)
        {
            if (_isWireGridIncluded && _areBoxesIncluded)
            {
                // When we want to include all objects into FitIntoView, then we can call FitIntoView that does not take sceneNode as the first parameter
                fitIntoViewCamera.FitIntoView(animationDuration, easingFunction,
                                              fitIntoViewType: _fitIntoViewType,
                                              adjustTargetPosition: _adjustTargetPosition,        // Adjust TargetPosition to better fit into view; set to false to preserve the current TargetPosition
                                              adjustmentFactor: _marginAdjustmentFactor,          // adjustmentFactor can be used to specify the margin
                                              waitUntilCameraIsValid: true);                      // waitUntilSceneViewSizeIsValid is set to true by default. This is used when the FitIntoView is called before the size of the SceneView is set - in this case the FitIntoView will be called when the size is set.
            }
            else if (_areBoxesIncluded)
            {
                // Use only objects inside _boxesGroup for FitIntoView
                fitIntoViewCamera.FitIntoView(_boxesGroup,
                                              animationDuration, easingFunction,
                                              fitIntoViewType: _fitIntoViewType,
                                              adjustTargetPosition: _adjustTargetPosition,
                                              adjustmentFactor: _marginAdjustmentFactor,
                                              waitUntilCameraIsValid: true);
            }
            else if (_isWireGridIncluded && _wireGridNode != null)
            {
                fitIntoViewCamera.FitIntoView(_wireGridNode,
                                              animationDuration, easingFunction,
                                              fitIntoViewType: _fitIntoViewType,
                                              adjustTargetPosition: _adjustTargetPosition,
                                              adjustmentFactor: _marginAdjustmentFactor,
                                              waitUntilCameraIsValid: true);
                
                // // When we have only WireGrid, we could call FitIntoView by passing WireGrid's BoundingBox or its corners:
                // //var boundingBox = new BoundingBox(new Vector3(-50, 0, -50), new Vector3(50, 0, 50));
                // var boundingBox = _wireGridNode.GetLocalBoundingBox();
                //
                // _corners ??= new Vector3[8]; // reuse the corners array
                // boundingBox.GetCorners(_corners);
                // //var cornerPositions = boundingBox.GetCorners(); // The following always creates a new array
                //
                //
                // fitIntoViewCamera.FitIntoView(_corners,
                //                               animationDuration, easingFunction,
                //                               adjustTargetPosition: _adjustTargetPosition,
                //                               adjustmentFactor: _marginAdjustmentFactor,
                //                               waitUntilCameraIsValid: true);
            }
            // else - no FitIntoView
        }

        _isInFitIntoViewCall = false;
    }

    private void CreateRandomScene()
    {
        if (Scene == null)
            return;

        Scene.RootNode.Clear();


        _wireGridNode = new WireGridNode()
        {
            Size = new Vector2(100, 100),
            WidthCellsCount = 10,
            HeightCellsCount = 10
        };

        Scene.RootNode.Add(_wireGridNode);


        _boxesGroup = new GroupNode("BoxesNode");
        Scene.RootNode.Add(_boxesGroup);

        for (int i = 0; i < 6; i++)
        {
            var randomCenterPosition = new Vector3(GetRandomFloat() * 100 - 50, GetRandomFloat() * 60 - 30, GetRandomFloat() * 100 - 50);

            var boxVisual3D = new BoxModelNode()
            {
                Position = randomCenterPosition,
                PositionType = PositionTypes.Center,
                Size = new Vector3(10, 8, 10),
                Material = StandardMaterials.Silver
            };

            _boxesGroup.Add(boxVisual3D);
        }
        
        
        _sceneWireBoundingBoxNode = new WireBoxNode(Colors.Red, 1, "SceneBoundingBox");
        UpdateSceneWireBoundingBox();
        
        Scene.RootNode.Add(_sceneWireBoundingBoxNode);
    }

    private void UpdateSceneWireBoundingBox()
    {
        if (_sceneWireBoundingBoxNode == null)
            return;

        var boundingBox = BoundingBox.Undefined;
        
        if (_isWireGridIncluded && _wireGridNode != null)
            boundingBox.Add(_wireGridNode.GetLocalBoundingBox());

        if (_areBoxesIncluded && _boxesGroup != null)
            boundingBox.Add(_boxesGroup.GetLocalBoundingBox());

        _sceneWireBoundingBoxNode.Visibility = _showSceneBoundingBox && !boundingBox.IsZeroSize ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        
        _sceneWireBoundingBoxNode.Position = boundingBox.GetCenterPosition();
        _sceneWireBoundingBoxNode.Size = boundingBox.GetSize();
    }

    private Camera CreateCamera()
    {
        if (_selectedCamera != null)
        {
            _selectedCamera.CameraChanged -= OnCameraChanged;
            _selectedCamera = null;
        }

        if (_isTargetPositionCamera)
        {
            var newTargetPositionCamera = new TargetPositionCamera()
            {
                TargetPosition = new Vector3(0, 0, 0),
                Heading = 30,
                Attitude = -20,
                Distance = 160,
                ViewWidth = 160,
                ProjectionType = _projectionType
            };

            _selectedCamera = newTargetPositionCamera;
        }
        else
        {
            var newFreeCamera = new FreeCamera()
            {
                TargetPosition = new Vector3(0, 0, 0),
                CameraPosition = new Vector3(-100, 100, 500),
                ViewWidth = 160,
                ProjectionType = _projectionType
            };

            _selectedCamera = newFreeCamera;
        }
        
        if (SceneView != null)
        {
            SceneView.Camera = _selectedCamera;
            _selectedCamera.Update();
        }
        
        _selectedCamera.CameraChanged += OnCameraChanged;

        return _selectedCamera;
    }

    // Call FitIntoView when automatic fit into view is enabled
    private void UpdateFitIntoView(bool forceUpdate = true)
    {
        if (!_isInFitIntoViewCall && (_automaticallyFitIntoView || forceUpdate))
            FitIntoView();
    }

    private void OnCameraChanged(object? sender, EventArgs e)
    {
        UpdateFitIntoView(forceUpdate: false); // FitIntoView only when _automaticallyFitIntoView is true
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(alignment: PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Auto fit", _automaticallyFitIntoView, isChecked =>
        {
            _automaticallyFitIntoView = isChecked;
            UpdateFitIntoView(forceUpdate: isChecked);
        });

        ui.CreateCheckBox("AdjustTargetPosition (?):When checked the camera will also reset to view the center of the scene. When unchecked the camera will look at (0, 0, 0)", _adjustTargetPosition, isChecked =>
        {
            _adjustTargetPosition = isChecked;
            if (!isChecked && _selectedCamera is ITargetPositionCamera selectedTargetPositionCamera)
                selectedTargetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
            else
                UpdateFitIntoView(forceUpdate: isChecked);
        });


        ui.AddSeparator();

        ui.CreateLabel("Included objects:");

        ui.CreateCheckBox("Boxes", _areBoxesIncluded, isChecked =>
        {
            _areBoxesIncluded = isChecked;
            UpdateFitIntoView(forceUpdate: false);
            UpdateSceneWireBoundingBox();
        });

        ui.CreateCheckBox("WireGrid", _isWireGridIncluded, isChecked =>
        {
            _isWireGridIncluded = isChecked;
            UpdateFitIntoView(forceUpdate: false);
            UpdateSceneWireBoundingBox();
        });


        ui.AddSeparator();

        ui.CreateLabel(
@"FitIntoViewType: (?):CheckBounds: Check BoundingBoxes of all SceneNodes.
This is not as precise as CheckAllPositions but can be much faster when there are objects with a lot of positions in the scene.

CheckAllPositions: Check Positions of all SceneNodes.
This is more precise than CheckBounds but can take much longer when there are objects with a lot of positions in the scene.");
        
        ui.CreateComboBox(new string[] { 
                "CheckBounds", 
                "CheckAllPositions" },
            (selectedIndex, selectedText) =>
            {
                _fitIntoViewType = (FitIntoViewType)selectedIndex;
                UpdateFitIntoView(forceUpdate: false);

                // Show wire bounding box when CheckBounds is selected. This shows what is included in th FitIntoView check
                if (_sceneWireBoundingBoxNode != null)
                    _sceneWireBoundingBoxNode.Visibility = _fitIntoViewType == FitIntoViewType.CheckBounds ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
            }, 
            selectedItemIndex:(int)_fitIntoViewType);


        ui.AddSeparator();

        ui.CreateLabel("AdjustmentFactor:");
        ui.CreateComboBox(new string[] { "1.0 (no margin)", "1.1 (10% margin)", "1.2 (20% margin)" }, (itemIndex, itemText) =>
        {
            _marginAdjustmentFactor = itemIndex switch
            {
                0 => 1f,
                1 => 1.1f,
                2 => 1.2f,
                _ => 1f
            };

            UpdateFitIntoView(forceUpdate: false);
        }, 0);


        ui.AddSeparator();

        ui.CreateButton("Fit into view (?): To test this method, uncheck the 'Auto fit' checkbox,\nrotate and move the camera and then click this button.", () => FitIntoView(isAnimated: false));
        ui.CreateButton("Fit into view (animated) (?): To test this method, uncheck the 'Auto fit' checkbox,\nrotate and move the camera and then click this button.", () => FitIntoView(isAnimated: true));
        
        
        ui.AddSeparator();

        ui.CreateLabel("Camera type:");
        ui.CreateRadioButtons(new string[] { "TargetPositionCamera", "FreeCamera" }, (itemIndex, itemText) =>
        {
            _isTargetPositionCamera = itemIndex == 0;
            CreateCamera();
            UpdateFitIntoView(forceUpdate: false);
        }, selectedItemIndex: _isTargetPositionCamera ? 0 : 1);


        ui.AddSeparator();

        ui.CreateLabel("Projection type:");
        ui.CreateRadioButtons(new string[] { "Perspective", "Orthographic" }, (itemIndex, itemText) =>
        {
            _projectionType = itemIndex == 0 ? ProjectionTypes.Perspective : ProjectionTypes.Orthographic;
            CreateCamera();
            UpdateFitIntoView(forceUpdate: false);
        }, selectedItemIndex: _projectionType == ProjectionTypes.Perspective ? 0 : 1);

        
        ui.AddSeparator();
        
        ui.CreateButton("Recreate scene", () =>
        {
            CreateRandomScene();
            UpdateFitIntoView(forceUpdate: false);
        });
        
        base.OnCreateUI(ui);
    }
}