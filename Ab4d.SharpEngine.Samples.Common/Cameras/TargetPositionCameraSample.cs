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
    private ICommonSampleUIElement? _fieldOfViewSlider;
    private ICommonSampleUIElement? _isHorizontalFieldOfViewCheckBox;
    private ICommonSampleUIElement? _viewWidthSlider;
    
    private bool _isSmoothRotation = true;
    private string? _headingChangeText = "40";
    private string? _attitudeChangeText = "0";

    private ICommonSampleUIElement? _startStopRotationButton;

    public TargetPositionCameraSample(ICommonSamplesContext context) 
        : base(context)
    {
        ShowCameraAxisPanel = true;
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        await base.ShowCommonSceneAsync(scene, CommonScenes.HouseWithTrees,
                                        position: new Vector3(0, -10, 0),
                                        positionType: PositionTypes.Bottom | PositionTypes.Center,
                                        finalSize: new Vector3(400, 400, 400));

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

        if (_targetPositionCamera.IsRotating)
        {
            _targetPositionCamera.StopRotation(); // Stop immediately
            _startStopRotationButton?.SetText("Start camera rotation");
        }

        _targetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
        _targetPositionCamera.Heading = -30;
        _targetPositionCamera.Attitude = -20;
        _targetPositionCamera.Distance = 600;
        _targetPositionCamera.ViewWidth = 600;
    }
    
    private void OnProjectionTypeChanged(int itemIndex, string? itemText)
    {
        if (_targetPositionCamera == null)
            return;

        if (itemIndex == 0)
        {
            _targetPositionCamera.ProjectionType = ProjectionTypes.Perspective;
            
            _distanceSlider?.SetIsVisible(true);
            _fieldOfViewSlider?.SetIsVisible(true);
            _isHorizontalFieldOfViewCheckBox?.SetIsVisible(true);
            
            _viewWidthSlider?.SetIsVisible(false);
        }
        else
        {
            _targetPositionCamera.ProjectionType = ProjectionTypes.Orthographic;
            
            _distanceSlider?.SetIsVisible(false);
            _fieldOfViewSlider?.SetIsVisible(false);
            _isHorizontalFieldOfViewCheckBox?.SetIsVisible(false);  
            
            _viewWidthSlider?.SetIsVisible(true);
        }
    }
    
    private void StartStopCameraRotation()
    {
        if (_targetPositionCamera == null)
            return;

        if (_targetPositionCamera.IsRotating)
        {
            if (_isSmoothRotation)
                _targetPositionCamera.StopRotation(decelerationSpeed: 40, easingFunction: EasingFunctions.QuadraticEaseOutFunction);
            else
                _targetPositionCamera.StopRotation(); // Stop immediately

            _startStopRotationButton?.SetText("Start camera rotation");
        }
        else
        {
            int headingChange = Int32.Parse(_headingChangeText ?? "40");
            int attitudeChange = Int32.Parse(_attitudeChangeText ?? "0");

            if (_isSmoothRotation)
                _targetPositionCamera.StartRotation(headingChange, attitudeChange, accelerationSpeed: 40, easingFunction: EasingFunctions.QuadraticEaseInFunction);
            else
                _targetPositionCamera.StartRotation(headingChange, attitudeChange); // Start immediately

            _startStopRotationButton?.SetText("Stop camera rotation");
        }
    }

    private float GetCameraHeading()
    {
        if (_targetPositionCamera == null)
            return 0.0f;

        // When rotating the camera, then the Heading and Attitude can change to angles that are bigger than 180 or 360 degrees.
        // To show those angles in sliders in this sample we normalize the angles to values from -180 to +180.
        // We could also call GetNormalizedHeading to get the Heading without changing its value.
        //return _targetPositionCamera.GetNormalizedHeading(normalizeTo180Degrees: true);

        _targetPositionCamera.NormalizeAngles(normalizeTo180Degrees: true);
        return _targetPositionCamera.Heading;
    }
    
    private float GetCameraAttitude()
    {
        if (_targetPositionCamera == null)
            return 0.0f;

        // When rotating the camera, then the Heading and Attitude can change to angles that are bigger than 180 or 360 degrees.
        // To show those angles in sliders in this sample we normalize the angles to values from -180 to +180.
        // We could also call GetNormalizedAttitude to get the Heading without changing its value.
        //return _targetPositionCamera.GetNormalizedAttitude(normalizeTo180Degrees: true);

        _targetPositionCamera.NormalizeAngles(normalizeTo180Degrees: true);
        return _targetPositionCamera.Attitude;
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

        ui.CreateKeyValueLabel("TargetPosition:", () => $"{_targetPositionCamera.TargetPosition.X:F0}, {_targetPositionCamera.TargetPosition.Y:F0}, {_targetPositionCamera.TargetPosition.Z:F0} (red cross)").SetColor(Colors.Red);

        ui.AddSeparator();


        ui.CreateSlider(-180, 180, () => GetCameraHeading(),  newValue => _targetPositionCamera.Heading = newValue,  width: 160, keyText: "Heading:",  keyTextWidth: 60, formatShownValueFunc: sliderValue => $"{sliderValue:F0}°");
        ui.CreateSlider(-180, 180, () => GetCameraAttitude(), newValue => _targetPositionCamera.Attitude = newValue, width: 160, keyText: "Attitude:", keyTextWidth: 60, formatShownValueFunc: sliderValue => $"{sliderValue:F0}°");


        ui.AddSeparator();

        ui.CreateLabel("ProjectionType:");
        ui.CreateRadioButtons(new[] { "Perspective (?):The perspective projection is similar to the real world where the objects that are farther away from the camera appear smaller.\nPerspective projection uses Distance and FieldOfView properties to define the visible area.", 
                                      "Orthographic (?):In orthographic projection the distance of the objects from the camera does not change the size of the objects on the screen.\nOrthographic projection is usually used for technical drawings because the objects sizes are preserved and the lines that are parallel in 3D stay parallel on the screen.\nOrthographic projection does not use Distance and FieldOfView properties but ViewWidth to define the visible area." },
                              checkedItemChangedAction: OnProjectionTypeChanged, 
                              selectedItemIndex: 0);


        ui.AddSeparator();
        _distanceSlider = ui.CreateSlider(10, 1000,
                                          () => _targetPositionCamera.Distance,
                                          newValue => _targetPositionCamera.Distance = newValue,
                                          width: 140,
                                          keyText: "Distance:",
                                          keyTextWidth: 80,
                                          formatShownValueFunc: sliderValue => sliderValue.ToString("F0"));
        
        _fieldOfViewSlider = ui.CreateSlider(10, 120,
                                          () => _targetPositionCamera.FieldOfView,
                                          newValue => _targetPositionCamera.FieldOfView = newValue,
                                          width: 140,
                                          keyText: "FieldOfView:",
                                          keyTextWidth: 80,
                                          formatShownValueFunc: sliderValue => $"{sliderValue:F0}°");

        _isHorizontalFieldOfViewCheckBox = ui.CreateCheckBox("IsHorizontalFieldOfView", 
                                                             isInitiallyChecked: _targetPositionCamera.IsHorizontalFieldOfView, 
                                                             isChecked => _targetPositionCamera.IsHorizontalFieldOfView = isChecked);


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

        ui.CreateLabel("Camera rotation:", isHeader: true);

        ui.CreateCheckBox("Smooth start / stop", isInitiallyChecked: true, isChecked => _isSmoothRotation = isChecked);

        var degreesTexts = new string[] { "-80", "-40", "0", "10", "20", "40", "60", "80", "120" };
        ui.CreateComboBox(degreesTexts, (selectedIndex, selectedText) => _headingChangeText = selectedText,  5, width: 60, keyText: "Heading change (°/s):",  keyTextWidth: 140);
        ui.CreateComboBox(degreesTexts, (selectedIndex, selectedText) => _attitudeChangeText = selectedText, 2, width: 60, keyText: "Attitude change (°/s):", keyTextWidth: 140);

        _startStopRotationButton = ui.CreateButton("Start camera rotation", StartStopCameraRotation);
        
        ui.AddSeparator();
        ui.AddSeparator();
        ui.CreateButton("Reset camera", () => ResetCamera());
    }
}