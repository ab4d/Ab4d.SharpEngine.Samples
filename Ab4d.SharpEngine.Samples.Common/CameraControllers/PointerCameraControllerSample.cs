using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

// ReSharper disable once CheckNamespace
namespace Ab4d.SharpEngine.Samples.Common.CameraControllers;

public class PointerCameraControllerSample : CommonSample
{
    public override string Title => "PointerCameraController";
    public override string? Subtitle => "PointerCameraController enables rotating, moving and zooming the camera with the pointer or mouse.";

    private ManualPointerCameraController? _pointerCameraController;

    private string _cameraControllerState = "";
    private ICommonSampleUIElement? _controllerStateLabel;

    private bool _isRotating;
    private bool _isMoving;
    private bool _isQuickZooming;

    private float _initialHeading;
    private float _initialAttitude;
    private Vector3 _initialTargetPosition;
    private float _initialDistance;

    public PointerCameraControllerSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -40;
            targetPositionCamera.Attitude = -30;
            targetPositionCamera.Distance = 500;
            targetPositionCamera.ViewWidth = 500;

            targetPositionCamera.CameraChanged += OnTargetPositionCameraChanged;
        }

        await base.ShowCommonSceneAsync(scene, CommonScenes.HouseWithTrees);
    }

    protected override void OnDisposed()
    {
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged -= OnTargetPositionCameraChanged;

        if (_pointerCameraController != null)
        {
            _pointerCameraController.CameraRotateStarted -= OnCameraControllerRotateStarted;
            _pointerCameraController.CameraRotateEnded -= OnCameraControllerRotateEnded;
            _pointerCameraController.CameraMoveStarted -= OnCameraControllerMoveStarted;
            _pointerCameraController.CameraMoveEnded -= OnCameraControllerMoveEnded;
            _pointerCameraController.CameraQuickZoomStarted -= OnCameraControllerQuickZoomStarted;
            _pointerCameraController.CameraQuickZoomEnded -= OnCameraControllerQuickZoomEnded;

            _pointerCameraController = null;
        }

        base.OnDisposed();
    }

    public override void InitializePointerCameraController(ManualPointerCameraController pointerCameraController)
    {
        // Save pointerCameraController so we can change it later
        _pointerCameraController = pointerCameraController;

        pointerCameraController.RotateCameraConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed; // this is also a default setting
        pointerCameraController.MoveCameraConditions   = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey; // this is also a default setting
        pointerCameraController.QuickZoomConditions    = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed; // by default, quick zoom is disabled
        pointerCameraController.ZoomMode               = CameraZoomMode.PointerPosition;

        pointerCameraController.IsPointerWheelZoomEnabled  = true;
        pointerCameraController.RotateAroundPointerPosition = true;
        pointerCameraController.IsXAxisInverted = false;
        pointerCameraController.IsYAxisInverted = false;
        pointerCameraController.UsePointerPositionForMovementSpeed = true;

        pointerCameraController.PointerWheelDistanceChangeFactor = 1.05f; // how fast is the wheel zoom
        pointerCameraController.PointerMoveThreshold = 0f;                // Immediately start rotation or movement; when bigger than 0, then we need to move the mouse for that amount before starting event. This can be used to also support click on the same button.
        pointerCameraController.MaxCameraDistance = float.NaN;            // Unlimited

        pointerCameraController.CameraRotateStarted += OnCameraControllerRotateStarted;
        pointerCameraController.CameraRotateEnded += OnCameraControllerRotateEnded;
        
        pointerCameraController.CameraMoveStarted += OnCameraControllerMoveStarted;
        pointerCameraController.CameraMoveEnded += OnCameraControllerMoveEnded;
        
        pointerCameraController.CameraQuickZoomStarted += OnCameraControllerQuickZoomStarted;
        pointerCameraController.CameraQuickZoomEnded += OnCameraControllerQuickZoomEnded;

        // Prevent default PointerCameraController initialization
        //base.InitializePointerCameraController(pointerCameraController);
    }

    private void OnCameraControllerRotateStarted(object? sender, EventArgs e)
    {
        _isRotating = true;

        if (targetPositionCamera != null)
        {
            _initialHeading = targetPositionCamera.Heading;
            _initialAttitude = targetPositionCamera.Attitude;
        }

        UpdateControllerState(); 
    }
    
    private void OnCameraControllerRotateEnded(object? sender, EventArgs e)
    {
        _isRotating = false;
        UpdateControllerState();   
    }
    
    private void OnCameraControllerMoveStarted(object? sender, EventArgs e)
    {
        _isMoving = true;

        if (targetPositionCamera != null)
            _initialTargetPosition = targetPositionCamera.TargetPosition;

        UpdateControllerState(); 
    }
    
    private void OnCameraControllerMoveEnded(object? sender, EventArgs e)
    {
        _isMoving = false;
        UpdateControllerState();
    }
    
    private void OnCameraControllerQuickZoomStarted(object? sender, EventArgs e)
    {
        _isQuickZooming = true;

        if (targetPositionCamera != null)
            _initialDistance = targetPositionCamera.Distance; // When using Orthographic projection, we need to save ViewWidth

        UpdateControllerState(); 
    }
    
    private void OnCameraControllerQuickZoomEnded(object? sender, EventArgs e)
    {
        _isQuickZooming = false;
        UpdateControllerState();
    }

    private void OnTargetPositionCameraChanged(object? sender, EventArgs e)
    {
        UpdateControllerState();
    }

    private void UpdateControllerState()
    {
        if (targetPositionCamera == null)
            return;

        string message;

        if (_isRotating)
            message = $"Heading: {(targetPositionCamera.Heading - _initialHeading):+0;-0;0}; Attitude: {(targetPositionCamera.Attitude - _initialAttitude):+0;-0;0}";
        else if (_isMoving)
            message = $"TargetPosition X: {(targetPositionCamera.TargetPosition.X - _initialTargetPosition.X):+0;-0;0};  Y:{(targetPositionCamera.TargetPosition.Y - _initialTargetPosition.Y):+0;-0;0};  Z: {(targetPositionCamera.TargetPosition.Z - _initialTargetPosition.Z):+0;-0;0}";
        else if (_isQuickZooming)
            message = $"Distance: {(targetPositionCamera.Distance - _initialDistance):+0;-0;0}";
        else
            message = "";
            
        _cameraControllerState = "Status: " + message;
        _controllerStateLabel?.UpdateValue();
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        var allConditionsTexts = new string[]
        {
            "Disabled",
            "Left mouse button", 
            "Right mouse button", 
            "Middle mouse button", 
            "Left + Right mouse buttons", 
            "Shift + Left mouse button", 
            "Shift + Right mouse button", 
            "Control + Left mouse button", 
            "Control + Right mouse button", 
            "Alt + Left mouse button", 
            "Alt + Right mouse button", 
        };

        var allConditions = new PointerAndKeyboardConditions[]
        {
            PointerAndKeyboardConditions.Disabled,
            PointerAndKeyboardConditions.LeftPointerButtonPressed,
            PointerAndKeyboardConditions.RightPointerButtonPressed,
            PointerAndKeyboardConditions.MiddlePointerButtonPressed,
            PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed,
            PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ShiftKey,
            PointerAndKeyboardConditions.RightPointerButtonPressed | PointerAndKeyboardConditions.ShiftKey,
            PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,
            PointerAndKeyboardConditions.RightPointerButtonPressed | PointerAndKeyboardConditions.ControlKey,
            PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.AltKey,
            PointerAndKeyboardConditions.RightPointerButtonPressed | PointerAndKeyboardConditions.AltKey,
        };

        ui.CreateLabel("RotateCameraConditions: (?):Only a few possible combinations are listed in this ComboBox.\nAll other combinations of buttons and modifier keys are possible.\n\nRotateCameraConditions default value: left button");
        ui.CreateComboBox(allConditionsTexts,
            (selectedIndex, selectedText) => _pointerCameraController!.RotateCameraConditions = allConditions[selectedIndex],
            selectedItemIndex: 1);

        ui.AddSeparator();

        ui.CreateLabel("MoveCameraConditions: (?):Only a few possible combinations are listed in this ComboBox.\nAll other combinations of buttons and modifier keys are possible.\n\nMoveCameraConditions default value: control + left button");
        ui.CreateComboBox(allConditionsTexts,
            (selectedIndex, selectedText) => _pointerCameraController!.MoveCameraConditions = allConditions[selectedIndex],
            selectedItemIndex: 7);
        
        ui.AddSeparator();

        ui.CreateLabel("QuickZoomConditions: (?):Only a few possible combinations are listed in this ComboBox.\nAll other combinations of buttons and modifier keys are possible.\n\nQuickZoomConditions default value: disabled");
        ui.CreateComboBox(allConditionsTexts,
            (selectedIndex, selectedText) => _pointerCameraController!.QuickZoomConditions = allConditions[selectedIndex],
            selectedItemIndex: 4);

        ui.AddSeparator();

        ui.CreateLabel("ZoomMode: (?):Possible values:\n- ViewCenter: Zooms into the center of the SceneView.\n- CameraRotationCenterPosition: Zooms into the 3D position defined by the TargetPositionCamera.RotationCenterPosition or FreeCamera.RotationCenterPosition property (not defined in this sample).\n- PointerPosition: Zooms into the 3D position that is 'behind' current pointer or mouse position. If there is no 3D object behind pointer or mouse position, then camera is zoomed into the SceneView's center.");
        ui.CreateComboBox([ "ViewCenter", 
                            "CameraRotationCenterPosition", 
                            "PointerPosition" ], 
            (selectedIndex, selectedText) => _pointerCameraController!.ZoomMode = (CameraZoomMode)selectedIndex,
            selectedItemIndex: 2);

        //ui.CreateComboBox([ "ViewCenter (?):Zooms into the center of the SceneView", 
        //                    "CameraRotationCenterPosition (?):Zooms into the 3D position defined by the TargetPositionCamera.RotationCenterPosition or FreeCamera.RotationCenterPosition property (not defined in this sample)", 
        //                    "PointerPosition (?):Zooms into the 3D position that is 'behind' current pointer or mouse position. If there is no 3D object behind pointer or mouse position, then camera is zoomed into the SceneView's center" ], 
        //    (selectedIndex, selectedText) => _pointerCameraController!.ZoomMode = (CameraZoomMode)selectedIndex,
        //    selectedItemIndex: 2);

        ui.AddSeparator();

        ui.CreateCheckBox("IsPointerWheelZoomEnabled", true, isChecked => _pointerCameraController!.IsPointerWheelZoomEnabled = isChecked);
        ui.CreateCheckBox("RotateAroundPointerPosition", true, isChecked => _pointerCameraController!.RotateAroundPointerPosition = isChecked);
        ui.CreateCheckBox("IsXAxisInverted", false, isChecked => _pointerCameraController!.IsXAxisInverted = isChecked);
        ui.CreateCheckBox("IsYAxisInverted", false, isChecked => _pointerCameraController!.IsYAxisInverted = isChecked);
        ui.CreateCheckBox("UsePointerPositionForMovementSpeed (?):When UsePointerPositionForMovementSpeed is true (CheckBox is checked) then the camera movement speed is determined by the distance to the 3D object behind the pointer or mouse. When no 3D object is behind the pointer or when UsePointerPositionForMovementSpeed is set to false, then movement speed is determined by the distance from the camera to the TargetPosition is used. Default value is true.", true, isChecked => _pointerCameraController!.UsePointerPositionForMovementSpeed = isChecked);
        
        ui.AddSeparator();

        var pointerWheelDistanceChangeFactors = new float[] { 1.01f, 1.025f, 1.05f, 1.075f, 1.1f, 1.2f };
        ui.CreateComboBox(pointerWheelDistanceChangeFactors.Select(v => v.ToString("N2")).ToArray(),
            (selectedIndex, selectedText) => _pointerCameraController!.PointerWheelDistanceChangeFactor = pointerWheelDistanceChangeFactors[selectedIndex],
            selectedItemIndex: 2,
            keyText: "PointerWheelDistanceChangeFactor (?):PointerWheelDistanceChangeFactor specifies a value that is used when zooming with mouse wheel. When zooming out the Camera's Distance or CameraWidth is multiplied with this value. When zooming in the Camera's Distance or CameraWidth is divided with this value. Default value is 1.05. Bigger value increases the speed of zooming with mouse wheel.");

        var pointerMoveThresholds = new float[] { 0.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 10.0f };
        ui.CreateComboBox(pointerMoveThresholds.Select(v => v.ToString("N0")).ToArray(),
            (selectedIndex, selectedText) => _pointerCameraController!.PointerMoveThreshold = pointerMoveThresholds[selectedIndex],
            selectedItemIndex: 0,
            keyText: "PointerMoveThreshold (?):This property specifies how much user needs to move the pointer or mouse before rotation, movement or quick zoom are started.}\n\nBecause PointerCameraController does not handle pointer or mouse events until pointer or mouse is moved for the specified amount, the events can be get by the user code (for example to handle mouse click; it is not needed to use Preview mouse events for that).\n\nWhen 0 (by default), then rotation, movement or quick zoom are started immediately when the correct pointer buttons and keyboard modifiers are pressed (no pointer or mouse movement needed).");

        var maxCameraDistances = new float[] { float.NaN, 500, 1000, 5000 };
        var maxCameraDistancesTexts = new string[] { "float.NaN", "500", "1000", "5000" };
        ui.CreateComboBox(maxCameraDistancesTexts,
            (selectedIndex, selectedText) => _pointerCameraController!.MaxCameraDistance = maxCameraDistances[selectedIndex],
            selectedItemIndex: 0,
            keyText: "MaxCameraDistance (?):When MaxCameraDistance is set to a value that is not float.NaN, than it specifies the maximum Distance of the camera or the maximum CameraWidth when OrthographicCamera is used.\nThis property can be set to a reasonable number to prevent float imprecision when the camera distance is very big. Default value is float.NaN.");

        ui.AddSeparator();
        _controllerStateLabel = ui.CreateKeyValueLabel(keyText: null, () => _cameraControllerState);

        UpdateControllerState();
    }
}