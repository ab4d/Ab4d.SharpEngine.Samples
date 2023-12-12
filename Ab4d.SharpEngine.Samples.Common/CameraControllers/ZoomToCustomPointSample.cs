using System;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.CameraControllers;

public class ZoomToCustomPointSample : CommonSample
{
    public override string Title => "Zoom to a custom point";
    public override string? Subtitle => "Use mouse wheel or quick zoom (left and right mouse button) to zoom the camera around the specified custom 3D position";

    private ManualMouseCameraController? _mouseCameraController;

    public ZoomToCustomPointSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var rootBoxNode = new BoxModelNode(new Vector3(0, -2, 0), new Vector3(100, 4, 100), StandardMaterials.Green);
        scene.RootNode.Add(rootBoxNode);


        var standardBoxMaterial = StandardMaterials.Gray;

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                StandardMaterial material;

                if (x == 0 && y == 0)
                    material = StandardMaterials.Red;
                else if (x == 2 && y == 1)
                    material = StandardMaterials.Yellow;
                else if (x == 4 && y == 2)
                    material = StandardMaterials.Orange;
                else
                    material = standardBoxMaterial;

                var boxNode = new BoxModelNode(new Vector3(-40 + x * 20, 5, -30 + y * 30), new Vector3(10, 10, 10), material);
                scene.RootNode.Add(boxNode);
            }
        }

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 200;
            targetPositionCamera.RotationCenterPosition = new Vector3(-40, 5, -30); // Center of Red box (default option in this sample)
        }

        // The following values will be used when the MouseCameraController is created.
        // Note that MouseCameraController is platform specific because it needs to handle mouse events.
        // But processing of the events is done by a common ManualMouseCameraController.

        this.QuickZoomConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed;
        this.RotateAroundMousePosition = false;
        this.ZoomMode = CameraZoomMode.CameraRotationCenterPosition;
    }

    public override void InitializeMouseCameraController(ManualMouseCameraController mouseCameraController)
    {
        // Save mouseCameraController so we can change it later
        _mouseCameraController = mouseCameraController;
        
        // Use standard initialization code for mouseCameraController
        base.InitializeMouseCameraController(mouseCameraController);
    }
    
    private void ChangeCenterPosition(int selectedIndex)
    {
        CameraZoomMode zoomMode;
        Vector3? rotationCenterPosition = null; // RotationCenterPosition is nullable Vector3 type
        bool rotateAroundMousePosition = false;

        switch (selectedIndex)
        {
            case 0: // "Center to SceneView (default)", 
                zoomMode = CameraZoomMode.ViewCenter;
                break;
            
            case 1: // "Red box (-40 5 -30)", 
                zoomMode = CameraZoomMode.CameraRotationCenterPosition;
                rotationCenterPosition = new Vector3(-40, 5, -30);
                break;
            
            case 2: // "Yellow box (0 5 0)", 
                zoomMode = CameraZoomMode.CameraRotationCenterPosition;
                rotationCenterPosition = new Vector3(0, 5, 0);
                break;
            
            case 3: // "Orange box (40 5 30)", 
                zoomMode = CameraZoomMode.CameraRotationCenterPosition;
                rotationCenterPosition = new Vector3(40, 5, 30);
                break;
            
            case 4: // "Position under mouse", 
                zoomMode = CameraZoomMode.MousePosition;
                rotationCenterPosition = null;
                rotateAroundMousePosition = true;
                break;

            default:
                zoomMode = CameraZoomMode.ViewCenter;
                rotationCenterPosition = null;
                rotateAroundMousePosition = false;
                break;
        }

        if (targetPositionCamera != null)
            targetPositionCamera.RotationCenterPosition = rotationCenterPosition;

        if (_mouseCameraController != null)
        {
            _mouseCameraController.ZoomMode = zoomMode;
            _mouseCameraController.RotateAroundMousePosition = rotateAroundMousePosition;
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Zoom center position:", isHeader: true);
        ui.CreateRadioButtons(new string[]
            {
                "Center to SceneView (default)", 
                "Red box (-40 5 -30)", 
                "Yellow box (0 5 0)", 
                "Orange box (40 5 30)", 
                "Position under the mouse",
            },
            (selectedIndex, selectedText) => ChangeCenterPosition(selectedIndex), selectedItemIndex: 1);

        ui.AddSeparator();
        ui.CreateCheckBox("QuickZoom (left + right button)", isInitiallyChecked: true, isChecked => SetupQuickZoom(isChecked));
    }

    private void SetupQuickZoom(bool isChecked)
    {
        if (_mouseCameraController == null)
            return;

        if (isChecked)
            _mouseCameraController.QuickZoomConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed | MouseAndKeyboardConditions.RightMouseButtonPressed;
        else
            _mouseCameraController.QuickZoomConditions = MouseAndKeyboardConditions.Disabled;
    }
}