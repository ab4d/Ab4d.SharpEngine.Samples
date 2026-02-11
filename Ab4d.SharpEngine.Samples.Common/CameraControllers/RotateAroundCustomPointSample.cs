using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.CameraControllers;

public class RotateAroundCustomPointSample : CommonSample
{
    public override string Title => "Rotate around a custom point";
    public override string? Subtitle => "Use left pointer or mouse button to rotate the camera around the specified custom 3D position";

    private ManualPointerCameraController? _pointerCameraController;
    private WireCrossNode? _rotationCenterWireCross;

    public RotateAroundCustomPointSample(ICommonSamplesContext context) 
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

        this.RotateAroundPointerPosition = false;
        this.ZoomMode = CameraZoomMode.ViewCenter;
    }

    protected override void OnDisposed()
    {
        if (_pointerCameraController != null)
        {
            // Unsubscribe event handlers
            _pointerCameraController.CameraRotateStarted -= OnCameraRotateStarted;
            _pointerCameraController.CameraRotateEnded   -= OnCameraRotateEnded;
            _pointerCameraController =  null;
        }

        base.OnDisposed();
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

        // Show rotation center position
        pointerCameraController.CameraRotateStarted += OnCameraRotateStarted;
        pointerCameraController.CameraRotateEnded   += OnCameraRotateEnded;
        
        // Use standard initialization code for pointerCameraController
        base.InitializePointerCameraController(pointerCameraController);
    }

    private void OnCameraRotateStarted(object? sender, EventArgs args)
    {
        ShowRotationCenterPosition();
    }
    
    private void OnCameraRotateEnded(object? sender, EventArgs args)
    {
        HideRotationCenterPosition();
    }

    private void ShowRotationCenterPosition()
    {
        if (Scene == null || targetPositionCamera == null)
            return;

        if (_rotationCenterWireCross == null)
        {
            _rotationCenterWireCross = new WireCrossNode(position: new Vector3(0, 0, 0), lineColor: Colors.Blue, lineLength: 30, lineThickness: 2);
            Scene.RootNode.Add(_rotationCenterWireCross);
        }

        // When RotationCenterPosition is not set (is null), then camera is rotated around TargetPosition (shown at the center of the view)
        _rotationCenterWireCross.Position = targetPositionCamera.RotationCenterPosition ?? targetPositionCamera.TargetPosition;
        _rotationCenterWireCross.Visibility = SceneNodeVisibility.Visible;
    }

    private void HideRotationCenterPosition()
    {
        if (_rotationCenterWireCross == null)
            return;

        _rotationCenterWireCross.Visibility = SceneNodeVisibility.Hidden;
    }

    private void ChangeCenterPosition(int selectedIndex)
    {
        Vector3? rotationCenterPosition; // RotationCenterPosition is nullable Vector3 type
        bool rotateAroundPointerPosition;

        switch (selectedIndex)
        {
            case 0: // "None", 
                rotationCenterPosition = null;
                rotateAroundPointerPosition = false;
                break;
            
            case 1: // "Red box (-40 5 -30)", 
                rotationCenterPosition = new Vector3(-40, 5, -30);
                rotateAroundPointerPosition = false;
                break;
            
            case 2: // "Yellow box (0 5 0)", 
                rotationCenterPosition = new Vector3(0, 5, 0);
                rotateAroundPointerPosition = false;
                break;
            
            case 3: // "Orange box (40 5 30)", 
                rotationCenterPosition = new Vector3(40, 5, 30);
                rotateAroundPointerPosition = false;
                break;
            
            case 4: // "Position under mouse", 
                rotationCenterPosition = null;
                rotateAroundPointerPosition = true;
                break;

            default:
                rotationCenterPosition = null;
                rotateAroundPointerPosition = false;
                break;
        }

        if (targetPositionCamera != null)
            targetPositionCamera.RotationCenterPosition = rotationCenterPosition;

        if (_pointerCameraController != null)
            _pointerCameraController.RotateAroundPointerPosition = rotateAroundPointerPosition;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("RotationCenterPosition:", isHeader: true);
        ui.CreateRadioButtons(new string[]
            {
                "None (rotate around center)", 
                "Red box (-40 5 -30)", 
                "Yellow box (0 5 0)", 
                "Orange box (40 5 30)", 
                "Position under the mouse",
            },
            (selectedIndex, selectedText) => ChangeCenterPosition(selectedIndex), selectedItemIndex: 1);

        ui.AddSeparator();
        ui.CreateButton("Reset camera", ResetCamera);
    }
}