using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public class FreeCameraSample : CommonSample
{
    public override string Title => "FreeCameraSample";
    public override string? Subtitle => "FreeCameraSample is defined by CameraPosition, TargetPosition and UpDirection. It can be rotated freely.";

    private FreeCamera? _freeCamera;

    private ICommonSampleUIProvider? _uiProvider;

    private WireCrossNode? _targetPositionCrossNode;

    private ICommonSampleUIElement? _viewWidthSlider;

    public FreeCameraSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var testScene = TestScenes.GetTestScene(TestScenes.StandardTestScenes.HouseWithTrees, finalSize: new Vector3(400, 400, 400));
        scene.RootNode.Add(testScene);

        _targetPositionCrossNode = new WireCrossNode(new Vector3(0, 0, 0), Colors.Red, lineLength: 50, lineThickness: 2);
        scene.RootNode.Add(_targetPositionCrossNode);
    }

    protected override Camera OnCreateCamera()
    {
        _freeCamera = new FreeCamera();

        ResetCamera();

        _freeCamera.CameraChanged += (sender, args) =>
        {
            _uiProvider?.UpdateAllValues();

            if (_targetPositionCrossNode != null)
                _targetPositionCrossNode.Position = _freeCamera.TargetPosition;
        };

        return _freeCamera;
    }

    private void ResetCamera()
    {
        if (_freeCamera == null)
            return;

        _freeCamera.CameraPosition = new Vector3(-60, 80, 400);
        _freeCamera.TargetPosition = new Vector3(0, 0, 0);
        
        //_freeCamera.UpDirection = new Vector3(0, 1, 0);
        _freeCamera.CalculateUpDirectionFromPositions();

        _freeCamera.ViewWidth = 300;
    }


    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        _uiProvider = ui;

        if (_freeCamera == null)
            return;

        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("TargetPositionCamera", isHeader: true);
        ui.CreateLabel("Rotate camera: left mouse button");
        ui.CreateLabel("Move camera: CTRL + left mouse button");

        ui.AddSeparator();

        ui.CreateKeyValueLabel("TargetPosition:", () => $"({_freeCamera.TargetPosition.X:F0}, {_freeCamera.TargetPosition.Y:F0}, {_freeCamera.TargetPosition.Z:F0})");
        ui.CreateKeyValueLabel("CameraPosition:", () => $"({_freeCamera.CameraPosition.X:F0}, {_freeCamera.CameraPosition.Y:F0}, {_freeCamera.CameraPosition.Z:F0})");
        ui.CreateKeyValueLabel("UpDirection:", () => $"({_freeCamera.UpDirection.X:F2}, {_freeCamera.UpDirection.Y:F2}, {_freeCamera.UpDirection.Z:F2})");

        ui.AddSeparator();
        ui.CreateComboBox(new[] { "None", "Y axis", "Z axis" },
            itemChangedAction: OnRotationUpAxisChanged,
            selectedItemIndex: 0,
            width: 120,
            keyText: "RotationUpAxis:",
            keyTextWidth: 100);

        ui.AddSeparator();
        ui.CreateComboBox(new[] { "Perspective", "Orthographic" },
                          itemChangedAction: OnProjectionTypeChanged,
                          selectedItemIndex: 0,
                          width: 120,
                          keyText: "ProjectionType:",
                          keyTextWidth: 100);

        ui.AddSeparator();
        _viewWidthSlider = ui.CreateSlider(10, 1000,
                                           () => _freeCamera.ViewWidth,
                                           newValue => _freeCamera.ViewWidth = newValue,
                                           width: 120,
                                           keyText: "ViewWidth:",
                                           keyTextWidth: 100,
                                           formatShownValueFunc: sliderValue => sliderValue.ToString("F0"))
                                        .SetIsVisible(false);
        
        ui.AddSeparator();
        ui.CreateButton("Reset camera", () => ResetCamera());

        //ui.CreateButton("Fit into view", () => FitIntoView());
    }

    private void OnProjectionTypeChanged(int itemIndex, string? itemText)
    {
        if (_freeCamera == null)
            return;

        if (itemIndex == 0)
        {
            _freeCamera.ProjectionType = ProjectionTypes.Perspective;
            _viewWidthSlider?.SetIsVisible(false);
        }
        else
        {
            _freeCamera.ProjectionType = ProjectionTypes.Orthographic;
            _viewWidthSlider?.SetIsVisible(true);
        }
    }
    
    private void OnRotationUpAxisChanged(int itemIndex, string? itemText)
    {
        if (_freeCamera == null)
            return;

        switch (itemIndex)
        {
            case 1:
                _freeCamera.RotationUpAxis = Vector3.UnitY;
                break;
            
            case 2:
                _freeCamera.RotationUpAxis = Vector3.UnitZ;
                break;

            default:
                _freeCamera.RotationUpAxis = null;
                break;
        }
    }
}