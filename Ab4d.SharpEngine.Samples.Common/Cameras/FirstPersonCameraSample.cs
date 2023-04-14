using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public class FirstPersonCameraSample : CommonSample
{
    public override string Title => "FirstPersonCamera";
    public override string? Subtitle => "FirstPersonCamera sample sees the scene from the CameraPosition";

    private ICommonSampleUIProvider? _uiProvider;

    private FirstPersonCamera? _firstPersonCamera;

    private float _buttonMoveDistance = 20; 
    private float _keyboardMoveDistance = 5; 

    public FirstPersonCameraSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var testScene = TestScenes.GetTestScene(TestScenes.StandardTestScenes.HouseWithTrees, finalSize: new Vector3(200, 200, 200));

        scene.RootNode.Add(testScene);
    }

    protected override Camera OnCreateCamera()
    {
        _firstPersonCamera = new FirstPersonCamera();
        ResetCamera();

        _firstPersonCamera.CameraChanged += (sender, args) =>
            _uiProvider?.UpdateAllValues();

        return _firstPersonCamera;
    }

    private void ResetCamera()
    {
        if (_firstPersonCamera == null)
            return;

        _firstPersonCamera.CameraPosition = new Vector3(22, -2, 125);
        _firstPersonCamera.Heading = -16;
        _firstPersonCamera.Attitude = 0;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        _uiProvider = ui;

        if (_firstPersonCamera == null)
            return;

        bool isKeyDownSupported = ui.RegisterKeyDown(OkKeyDown);


        var rootStackPanel = ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("FirstPersonCamera", isHeader: true);
        ui.CreateLabel("Rotate camera: left mouse button");
        ui.CreateLabel("Move camera: CTRL + left mouse button");

        ui.AddSeparator();

        ui.CreateKeyValueLabel("CameraPosition:", () => $"({_firstPersonCamera.CameraPosition.X:F0}, {_firstPersonCamera.CameraPosition.Y:F0}, {_firstPersonCamera.CameraPosition.Z:F0})");

        ui.AddSeparator();


        ui.CreateSlider(-180, 180, () => _firstPersonCamera.Heading, newValue => _firstPersonCamera.Heading = newValue, width: 160, keyText: "Heading:", keyTextWidth: 60, formatShownValueFunc: sliderValue => sliderValue.ToString("F0") + "°");
        ui.CreateSlider(-180, 180, () => _firstPersonCamera.Attitude, newValue => _firstPersonCamera.Attitude = newValue, width: 160, keyText: "Attitude:", keyTextWidth: 60, formatShownValueFunc: sliderValue => sliderValue.ToString("F0") + "°");


        // Handle cases when keyboard events are not supported (for example WinUI)
        string forwardText = isKeyDownSupported ? "Forward (W)" : "Forward";
        string leftText    = isKeyDownSupported ? "Left (A)" : "Left";
        string rightText   = isKeyDownSupported ? "Right (D)" : "Right";
        string backText    = isKeyDownSupported ? "Back (S)" : "Back";

        ui.CreateButton(forwardText, () => _firstPersonCamera.MoveForward(_buttonMoveDistance), width: 100, alignLeft: true).SetMargin(75, 10, 0, 0);

        ui.CreateStackPanel(alignment: PositionTypes.Left, isVertical: false, addBorder: false, parent: rootStackPanel).SetMargin(0, 5, 0, 0);
        ui.CreateButton(leftText, () => _firstPersonCamera.MoveLeft(_buttonMoveDistance), width: 80).SetMargin(0, 0, 0, 0);
        ui.CreateButton(backText, () => _firstPersonCamera.MoveBackward(_buttonMoveDistance), width: 80).SetMargin(5, 0, 0, 0);
        ui.CreateButton(rightText, () => _firstPersonCamera.MoveRight(_buttonMoveDistance), width: 80).SetMargin(5, 0, 0, 0);

        ui.SetCurrentPanel(rootStackPanel);


        ui.AddSeparator();
        ui.CreateButton("Reset camera", () => ResetCamera(), width: 250, alignLeft: true);
    }

    private bool OkKeyDown(string key)
    {
        if (_firstPersonCamera == null)
            return false;

        bool isHandled = false;

        switch (key.ToLower())
        {
            case "w":
                _firstPersonCamera.MoveForward(_keyboardMoveDistance);
                isHandled = true;
                break;
            
            case "s":
                _firstPersonCamera.MoveBackward(_keyboardMoveDistance);
                isHandled = true;
                break;
           
            case "a":
                _firstPersonCamera.MoveLeft(_keyboardMoveDistance);
                isHandled = true;
                break;
            
            case "d":
                _firstPersonCamera.MoveRight(_keyboardMoveDistance);
                isHandled = true;
                break;
        }

        return isHandled;
    }
}