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

    private ManualPointerCameraController? _pointerCameraController;

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

        ResetCamera();

        // The following values will be used when the PointerCameraController is created.
        // Note that PointerCameraController is platform specific because it needs to handle pointer or mouse events.
        // But processing of the events is done by a common ManualPointerCameraController.

        this.QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed;
        this.RotateAroundPointerPosition = false;
        this.ZoomMode = CameraZoomMode.CameraRotationCenterPosition;
    }

    private void ResetCamera()
    {
        if (targetPositionCamera == null) 
            return;

        targetPositionCamera.Heading = 30;
        targetPositionCamera.Attitude = -20;
        targetPositionCamera.Distance = 200;
        targetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
        targetPositionCamera.RotationCenterPosition = new Vector3(-40, 5, -30); // Center of Red box (default option in this sample)
    }

    public override void InitializePointerCameraController(ManualPointerCameraController pointerCameraController)
    {
        // Save pointerCameraController so we can change it later
        _pointerCameraController = pointerCameraController;
        
        // Use standard initialization code for pointerCameraController
        base.InitializePointerCameraController(pointerCameraController);
    }
    
    private void ChangeCenterPosition(int selectedIndex)
    {
        CameraZoomMode zoomMode;
        Vector3? rotationCenterPosition = null; // RotationCenterPosition is nullable Vector3 type
        bool rotateAroundPointerPosition = false;

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
            
            case 4: // "Position under pointer or mouse", 
                zoomMode = CameraZoomMode.PointerPosition;
                rotationCenterPosition = null;
                rotateAroundPointerPosition = true;
                break;

            default:
                zoomMode = CameraZoomMode.ViewCenter;
                rotationCenterPosition = null;
                rotateAroundPointerPosition = false;
                break;
        }

        if (targetPositionCamera != null)
            targetPositionCamera.RotationCenterPosition = rotationCenterPosition;

        if (_pointerCameraController != null)
        {
            _pointerCameraController.ZoomMode = zoomMode;
            _pointerCameraController.RotateAroundPointerPosition = rotateAroundPointerPosition;
        }
    }

    private void SetupQuickZoom(bool isChecked)
    {
        if (_pointerCameraController == null)
            return;

        if (isChecked)
            _pointerCameraController.QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed;
        else
            _pointerCameraController.QuickZoomConditions = PointerAndKeyboardConditions.Disabled;
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

        ui.AddSeparator();
        ui.CreateButton("Reset camera", ResetCamera);
    }
}