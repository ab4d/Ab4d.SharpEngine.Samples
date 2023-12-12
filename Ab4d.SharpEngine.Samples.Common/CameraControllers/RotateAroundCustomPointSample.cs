﻿using System;
using System.Numerics;
using System.Xml.Linq;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using static System.Formats.Asn1.AsnWriter;

namespace Ab4d.SharpEngine.Samples.Common.CameraControllers;

public class RotateAroundCustomPointSample : CommonSample
{
    public override string Title => "Rotate around a custom point";
    public override string? Subtitle => "Use left mouse button to rotate the camera around the specified custom 3D position";

    private ManualMouseCameraController? _mouseCameraController;
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

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 200;
            targetPositionCamera.RotationCenterPosition = new Vector3(-40, 5, -30);
        }

        // The following values will be used when the MouseCameraController is created.
        // Note that MouseCameraController is platform specific because it needs to handle mouse events.
        // But processing of the events is done by a common ManualMouseCameraController.

        this.RotateAroundMousePosition = false;
        this.ZoomMode = CameraZoomMode.ViewCenter;
    }

    public override void InitializeMouseCameraController(ManualMouseCameraController mouseCameraController)
    {
        // Save mouseCameraController so we can change it later
        _mouseCameraController = mouseCameraController;

        // Show rotation center position
        mouseCameraController.CameraRotateStarted += (sender, args) => ShowRotationCenterPosition();
        mouseCameraController.CameraRotateEnded += (sender, args) => HideRotationCenterPosition();
        
        // Use standard initialization code for mouseCameraController
        base.InitializeMouseCameraController(mouseCameraController);
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
    }

    private void ChangeCenterPosition(int selectedIndex)
    {
        Vector3? rotationCenterPosition; // RotationCenterPosition is nullable Vector3 type
        bool rotateAroundMousePosition;

        switch (selectedIndex)
        {
            case 0: // "None", 
                rotationCenterPosition = null;
                rotateAroundMousePosition = false;
                break;
            
            case 1: // "Red box (-40 5 -30)", 
                rotationCenterPosition = new Vector3(-40, 5, -30);
                rotateAroundMousePosition = false;
                break;
            
            case 2: // "Yellow box (0 5 0)", 
                rotationCenterPosition = new Vector3(0, 5, 0);
                rotateAroundMousePosition = false;
                break;
            
            case 3: // "Orange box (40 5 30)", 
                rotationCenterPosition = new Vector3(40, 5, 30);
                rotateAroundMousePosition = false;
                break;
            
            case 4: // "Position under mouse", 
                rotationCenterPosition = null;
                rotateAroundMousePosition = true;
                break;

            default:
                rotationCenterPosition = null;
                rotateAroundMousePosition = false;
                break;
        }

        if (targetPositionCamera != null)
            targetPositionCamera.RotationCenterPosition = rotationCenterPosition;

        if (_mouseCameraController != null)
            _mouseCameraController.RotateAroundMousePosition = rotateAroundMousePosition;
    }
}