using Ab4d.SharpEngine.Cameras;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public class FitIntoViewSample : CommonSample
{
    public override string Title => "FitIntoView";

    private bool _automaticallyFitIntoView = true;
    private bool _adjustTargetPosition = true;
    private bool _isWireGridIncluded = true;
    private bool _areBoxesIncluded = true;
    private float _marginAdjustmentFactor = 1.0f;

    private bool _isAdjustingDistance;

    private bool _isTargetPositionCamera = true; // false: FreeCamera
    private ProjectionTypes _projectionType = ProjectionTypes.Orthographic;

    private Random _rnd = new Random();

    private Camera? _selectedCamera;
    private GroupNode? _boxesGroup;
    private WireGridNode? _wireGridNode;

    private Vector3[] _corners;

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

    protected override Camera OnCreateCamera()
    {
        return CreateCamera();
    }

    private void FitIntoView()
    {
        if (_boxesGroup == null || Scene == null)
            return;

        _isAdjustingDistance = true; // Prevent infinite recursion (FitIntoView is called from CameraChanged)

        if (_selectedCamera is IFitIntoViewCamera fitIntoViewCamera)
        {
            if (_isWireGridIncluded && _areBoxesIncluded)
            {
                // When we want to include all objects into FitIntoView, then we can call FitIntoView that does not take sceneNode as the first parameter
                fitIntoViewCamera.FitIntoView(fitIntoViewType: FitIntoViewType.CheckAllPositions, // CheckAllPositions can take some time bigger scenes. In this case you can use the CheckBounds
                                              adjustTargetPosition: _adjustTargetPosition,        // Adjust TargetPosition to better fit into view; set to false to preserve the current TargetPosition
                                              adjustmentFactor: _marginAdjustmentFactor,          // adjustmentFactor can be used to specify the margin
                                              waitUntilSceneViewSizeIsValid: true);               // waitUntilSceneViewSizeIsValid is set to true by default. This is used when the FitIntoView is called before the size of the SceneView is set - in this case the FitIntoView will be called when the size is set.
            }
            else if (_areBoxesIncluded)
            {
                // Use only objects inside _boxesGroup for FitIntoView
                fitIntoViewCamera.FitIntoView(_boxesGroup,
                                              fitIntoViewType: FitIntoViewType.CheckAllPositions,
                                              adjustTargetPosition: _adjustTargetPosition,
                                              adjustmentFactor: _marginAdjustmentFactor,
                                              waitUntilSceneViewSizeIsValid: true);
            }
            else if (_isWireGridIncluded && _wireGridNode != null)
            {
                // When we have only WireGrid, we could call FitIntoView by passing WireGrid's BoundingBox or its corners:
                //var boundingBox = new BoundingBox(new Vector3(-50, 0, -50), new Vector3(50, 0, 50));
                var boundingBox = _wireGridNode.GetLocalBoundingBox();

                _corners ??= new Vector3[8]; // reuse the corners array
                boundingBox.GetCorners(_corners);
                //var cornerPositions = boundingBox.GetCorners(); // The following always creates a new array


                fitIntoViewCamera.FitIntoView(_corners,
                                              adjustTargetPosition: _adjustTargetPosition,
                                              adjustmentFactor: _marginAdjustmentFactor,
                                              waitUntilSceneViewSizeIsValid: true);
            }
            // else - no FitIntoView
        }

        _isAdjustingDistance = false;
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
            var randomCenterPosition = new Vector3(_rnd.NextSingle() * 100 - 50, _rnd.NextSingle() * 60 - 30, _rnd.NextSingle() * 100 - 50);

            var boxVisual3D = new BoxModelNode()
            {
                Position = randomCenterPosition,
                PositionType = PositionTypes.Center,
                Size = new Vector3(10, 8, 10),
                Material = StandardMaterials.Silver
            };

            _boxesGroup.Add(boxVisual3D);
        }
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

        _selectedCamera.CameraChanged += OnCameraChanged;

        if (SceneView != null)
            SceneView.Camera = _selectedCamera;

        return _selectedCamera;
    }

    // Call FitIntoView when automatic fit into view is enabled
    private void UpdateFitIntoView()
    {
        if (_automaticallyFitIntoView && !_isAdjustingDistance)
            FitIntoView();
    }

    private void OnCameraChanged(object? sender, EventArgs e)
    {
        UpdateFitIntoView();
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(alignment: PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Auto fit", _automaticallyFitIntoView, isChecked =>
        {
            _automaticallyFitIntoView = isChecked;
            UpdateFitIntoView();
        });

        ui.CreateCheckBox("AdjustTargetPosition (?):When checked the camera will also reset to view the center of the scene. When unchecked the camera will look at (0, 0, 0)", _adjustTargetPosition, isChecked =>
        {
            _adjustTargetPosition = isChecked;
            if (!isChecked && _selectedCamera is ITargetPositionCamera selectedTargetPositionCamera)
                selectedTargetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
        });


        ui.AddSeparator();

        ui.CreateLabel("Included objects:");

        ui.CreateCheckBox("Boxes", _areBoxesIncluded, isChecked =>
        {
            _areBoxesIncluded = isChecked;
            UpdateFitIntoView();
        });

        ui.CreateCheckBox("WireGrid", _isWireGridIncluded, isChecked =>
        {
            _isWireGridIncluded = isChecked;
            UpdateFitIntoView();
        });


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

            UpdateFitIntoView();
        }, 0);


        ui.AddSeparator();

        ui.CreateButton("Fit into view", () => FitIntoView());
        ui.CreateButton("Recreate scene", () =>
        {
            CreateRandomScene();
            UpdateFitIntoView();
        });


        ui.AddSeparator();

        ui.CreateLabel("Camera type:");
        ui.CreateRadioButtons(new string[] { "TargetPositionCamera", "FreeCamera" }, (itemIndex, itemText) =>
        {
            _isTargetPositionCamera = itemIndex == 0;
            CreateCamera();
            UpdateFitIntoView();
        }, selectedItemIndex: _isTargetPositionCamera ? 0 : 1);


        ui.AddSeparator();

        ui.CreateLabel("Projection type:");
        ui.CreateRadioButtons(new string[] { "Perspective", "Orthographic" }, (itemIndex, itemText) =>
        {
            _projectionType = itemIndex == 0 ? ProjectionTypes.Perspective : ProjectionTypes.Orthographic;
            CreateCamera();
            UpdateFitIntoView();
        }, selectedItemIndex: _projectionType == ProjectionTypes.Perspective ? 0 : 1);

        base.OnCreateUI(ui);
    }
}