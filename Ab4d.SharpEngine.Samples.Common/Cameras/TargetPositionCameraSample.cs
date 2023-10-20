using System;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public class TargetPositionCameraSample : CommonSample
{
    public override string Title => "TargetPositionCamera";
    public override string? Subtitle => "TargetPositionCamera rotates around the TargetPosition.\nLEFT MOUSE BUTTON: rotate camera; LEFT MOUSE BUTTON + CTRL: move camera";

    private ICommonSampleUIProvider? _uiProvider;

    private WireCrossNode? _targetPositionCrossNode;

    private TargetPositionCamera? _targetPositionCamera;
    private ICommonSampleUIElement? _distanceSlider;
    private ICommonSampleUIElement? _viewWidthSlider;

    public TargetPositionCameraSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var testScene = TestScenes.GetTestScene(TestScenes.StandardTestScenes.HouseWithTrees, new Vector3(0, -10, 0), PositionTypes.Bottom | PositionTypes.Center, finalSize: new Vector3(400, 400, 400));
        scene.RootNode.Add(testScene);

        _targetPositionCrossNode = new WireCrossNode(new Vector3(0, 0, 0), Colors.Red, lineLength: 50, lineThickness: 2);
        scene.RootNode.Add(_targetPositionCrossNode);
    }

    protected override Camera OnCreateCamera()
    {
        _targetPositionCamera = new TargetPositionCamera();
        ResetCamera();

        _targetPositionCamera.CameraChanged += (sender, args) =>
        {
            _uiProvider?.UpdateAllValues();

            if (_targetPositionCrossNode != null)
                _targetPositionCrossNode.Position = _targetPositionCamera.TargetPosition;
        };

        return _targetPositionCamera;
    }

    private void ResetCamera()
    {
        if (_targetPositionCamera == null)
            return;

        _targetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
        _targetPositionCamera.Heading = -20;
        _targetPositionCamera.Attitude = -20;
        _targetPositionCamera.Distance = 600;
        _targetPositionCamera.ViewWidth = 600;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        _uiProvider = ui;

        if (_targetPositionCamera == null)
            return;

        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("TargetPositionCamera", isHeader: true);
        ui.CreateLabel("Rotate camera: left mouse button");
        ui.CreateLabel("Move camera: CTRL + left mouse button");

        ui.AddSeparator();

        ui.CreateKeyValueLabel("TargetPosition:", () => $"({_targetPositionCamera.TargetPosition.X:F0}, {_targetPositionCamera.TargetPosition.Y:F0}, {_targetPositionCamera.TargetPosition.Z:F0})");

        ui.AddSeparator();


        ui.CreateSlider(-180, 180, () => _targetPositionCamera.Heading, newValue => _targetPositionCamera.Heading = newValue, width: 160, keyText: "Heading:", keyTextWidth: 60, formatShownValueFunc: sliderValue => sliderValue.ToString("F0") + "°");
        ui.CreateSlider(-180, 180, () => _targetPositionCamera.Attitude, newValue => _targetPositionCamera.Attitude = newValue, width: 160, keyText: "Attitude:", keyTextWidth: 60, formatShownValueFunc: sliderValue => sliderValue.ToString("F0") + "°");


        ui.AddSeparator();

        ui.CreateComboBox(new[] { "Perspective", "Orthographic" },
                          itemChangedAction: OnProjectionTypeChanged, 
                          selectedItemIndex: 0,
                          width: 120,
                          keyText: "ProjectionType:",
                          keyTextWidth: 100);


        ui.AddSeparator();
        _distanceSlider = ui.CreateSlider(10, 1000,
                                          () => _targetPositionCamera.Distance,
                                          newValue => _targetPositionCamera.Distance = newValue,
                                          width: 120,
                                          keyText: "Distance:",
                                          keyTextWidth: 100,
                                          formatShownValueFunc: sliderValue => sliderValue.ToString("F0"));

        ui.AddSeparator();
        _viewWidthSlider = ui.CreateSlider(10, 1000,
                                           () => _targetPositionCamera.ViewWidth,
                                           newValue => _targetPositionCamera.ViewWidth = newValue,
                                           width: 120,
                                           keyText: "ViewWidth:",
                                           keyTextWidth: 100,
                                           formatShownValueFunc: sliderValue => sliderValue.ToString("F0"))
                                        .SetIsVisible(false);

        ui.AddSeparator();
        ui.CreateButton("Reset camera", () => ResetCamera());
    }

    private void OnProjectionTypeChanged(int itemIndex, string? itemText)
    {
        if (_targetPositionCamera == null)
            return;

        if (itemIndex == 0)
        {
            _targetPositionCamera.ProjectionType = ProjectionTypes.Perspective;
            _distanceSlider?.SetIsVisible(true);
            _viewWidthSlider?.SetIsVisible(false);
        }
        else
        {
            _targetPositionCamera.ProjectionType = ProjectionTypes.Orthographic;
            _distanceSlider?.SetIsVisible(false);
            _viewWidthSlider?.SetIsVisible(true);
        }
    }
}