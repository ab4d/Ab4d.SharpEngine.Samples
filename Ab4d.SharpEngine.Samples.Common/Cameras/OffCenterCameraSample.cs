using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public class OffCenterCameraSample : CommonSample
{
    public override string Title => "Off-center scene rendering";

    private WireCrossNode? _targetPositionCrossNode;
    private WireCrossNode? _rotationCenterPositionCrossNode;
    private ICommonSampleUIElement? _targetPositionLabel;
    private ICommonSampleUIElement? _rotationCenterPositionLabel;

    public OffCenterCameraSample(ICommonSamplesContext context)
        : base(context)
    {

    }

    protected override void OnCreateScene(Scene scene)
    {
        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -0.1f, 0),
            Size = new Vector2(100, 100),
            WidthCellsCount = 10,
            HeightCellsCount = 10,
            MinorLineColor = Colors.Gray,
            MinorLineThickness = 2,
        };
        scene.RootNode.Add(wireGridNode);

        var boxModelNode = new BoxModelNode(centerPosition: new Vector3(0, 0, 0), size: new Vector3(15, 6, 15), material: StandardMaterials.Gold);
        scene.RootNode.Add(boxModelNode);

        _targetPositionCrossNode = new WireCrossNode(position: new Vector3(30, 0, 0), lineLength: 30, lineColor: Colors.Blue, lineThickness: 3);
        scene.RootNode.Add(_targetPositionCrossNode);     
        
        _rotationCenterPositionCrossNode = new WireCrossNode(position: new Vector3(0, 0, 0), lineLength: 25, lineColor: Colors.Red, lineThickness: 4);
        scene.RootNode.Add(_rotationCenterPositionCrossNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 150;

            // The 3D scene is rendered in such a way that the Camera's TargetPosition is rendered in the center
            // of the SceneView (this is the position the camera is looking at).
            // 
            // This means that if we want to move the center of the scene, we need to adjust the value of the TargetPosition property.
            // For example, to move the rendered scene to the left, we set the TargetPosition to a position right off the center position
            // - in our case to (30, 0, 0). This will move our scene that is located around (0, 0, 0) to the left.
            // 
            // But we still want to preserve the center or rotation - we want to rotate around the center of the coordinate axes.
            // To solve that we set the RotationCenterPosition to the desired position of rotation: (0, 0, 0).
            // 
            // Notes:
            // - MouseCameraController.RotateAroundPointerPosition must be false.
            // - RotationCenterPosition is supported only by TargetPositionCamera and FreeCamera.
            // - To change the camera's Heading and Attitude from code (not by user interaction) and use RotationCenterPosition,
            //   call the SetCameraRotation method instead of changing Heading and Attitude properties.",
            // 
            targetPositionCamera.TargetPosition = new Vector3(30, 0, 0);
            targetPositionCamera.RotationCenterPosition = new Vector3(0, 0, 0); // RotationCenterPosition is nullable and null by default
        }

        // We need to disable the RotateAroundPointerPosition, otherwise the RotationCenterPosition is changed to the position behind the pointe (mouse)
        RotateAroundPointerPosition = false;

        ShowCameraAxisPanel = true;
    }

    private void ChangeSceneCenterPosition(int selectedIndex)
    {
        if (targetPositionCamera == null)
            return;
       
        switch (selectedIndex)
        {
            case 0: // Left
                targetPositionCamera.TargetPosition = new Vector3(30, 0, 0);
                targetPositionCamera.RotationCenterPosition = new Vector3(0, 0, 0);
                break;
            
            case 1: // Center
                targetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
                targetPositionCamera.RotationCenterPosition = new Vector3(0, 0, 0);
                break;
            
            case 2: // Right
                targetPositionCamera.TargetPosition = new Vector3(-30, 0, 0);
                targetPositionCamera.RotationCenterPosition = new Vector3(0, 0, 0);
                break;
        }

        OnSceneCenterChanged();
    }
    
    private void OnSceneCenterChanged()
    {
        if (targetPositionCamera == null)
            return;

        if (_targetPositionCrossNode != null)
            _targetPositionCrossNode.Position = targetPositionCamera.TargetPosition;
        
        if (_rotationCenterPositionCrossNode != null)
            _rotationCenterPositionCrossNode.Position = targetPositionCamera.RotationCenterPosition!.Value;
        
        _targetPositionLabel?.UpdateValue();
        _rotationCenterPositionLabel?.UpdateValue();
    }
    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(alignment: PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Current camera settings:", isHeader: true);
        
        _targetPositionLabel = ui.CreateKeyValueLabel("TargetPosition (blue cross): ", () => targetPositionCamera?.TargetPosition.ToString() ?? "").SetColor(Colors.Blue);
        _rotationCenterPositionLabel = ui.CreateKeyValueLabel("RotationCenterPosition (red cross): ", () => targetPositionCamera?.RotationCenterPosition.ToString() ?? "").SetColor(Colors.Red);
        
        
        ui.CreateLabel("Scene center position:", isHeader: true);

        ui.CreateRadioButtons(new string[]
        {
            "Render scene on the LEFT side",
            "Default (CENTER) scene rendering",
            "Render scene on the RIGHT side"
        }, (selectedIndex, selectedText) => ChangeSceneCenterPosition(selectedIndex), selectedItemIndex: 0);
        
        
        ui.AddSeparator();
        
        ui.CreateLabel(
@"The 3D scene is rendered in such a way that the Camera's TargetPosition is rendered in the center of the SceneView (this is the position the camera is looking at).

This means that if we want to move the center of the scene, we need to adjust the value of the TargetPosition property. For example, to move the rendered scene to the left, we set the TargetPosition to a position right off the center position - in our case to (30, 0, 0). This will move our scene that is located around (0, 0, 0) to the left.

But we still want to preserve the center or rotation - we want to rotate around the center of the coordinate axes. To solve that we set the RotationCenterPosition to the desired position of rotation: (0, 0, 0).

Notes:
- MouseCameraController.RotateAroundPointerPosition must be false.
- RotationCenterPosition is supported only by TargetPositionCamera and FreeCamera.
- To change the camera's Heading and Attitude from code (not by user interaction) and use RotationCenterPosition, call the SetCameraRotation method instead of changing Heading and Attitude properties.",
width: 450);
        
        base.OnCreateUI(ui);
    }    
}