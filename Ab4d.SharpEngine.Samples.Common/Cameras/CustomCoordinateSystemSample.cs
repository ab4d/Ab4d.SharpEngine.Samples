//#define TEST_ALL_CAMERA_TYPES // When unchecked, then it is possible to change camera type and projection type

using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Cameras;

public class CustomCoordinateSystemSample : CommonSample
{
    public override string Title => "Custom coordinate system";
    public override string? Subtitle => "This sample shows how to change from default coordinate system (YUpRightHanded) to a custom coordinate system.";

    private ICommonSampleUIElement? _descriptionLabel;
    private ICommonSampleUIElement? _axesDirectionsLabel;

    public CustomCoordinateSystemSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var boxModel = new BoxModelNode(centerPosition: new Vector3(0, 20, 0),
                                        size: new Vector3(80, 40, 60),
                                        material: StandardMaterials.Gold,
                                        name: "Gold BoxModel")
        {
            BackMaterial = StandardMaterials.Red
        };
        scene.RootNode.Add(boxModel);
        
        
        // Add smaller red box in the X direction
        var redModel = new BoxModelNode(centerPosition: new Vector3(50, 20, 0),
                                        size: new Vector3(20, 20, 20),
                                        material: StandardMaterials.Red,
                                        name: "Red BoxModel");
        scene.RootNode.Add(redModel);
        
        // Add smaller green box in the Y direction
        var greenModel = new BoxModelNode(centerPosition: new Vector3(0, 50, 0),
                                        size: new Vector3(20, 20, 20),
                                        material: StandardMaterials.Green,
                                        name: "Green BoxModel");
        scene.RootNode.Add(greenModel);
        
        // Add smaller blue box in the Y direction
        var blueModel = new BoxModelNode(centerPosition: new Vector3(0, 20, 40),
                                        size: new Vector3(20, 20, 20),
                                        material: StandardMaterials.Blue,
                                        name: "Blue BoxModel");
        scene.RootNode.Add(blueModel);


        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -0.1f, 0),
            Size = new Vector2(200, 200),
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 0, 1),
            MinorLineColor = Colors.Gray,
            MajorLineColor = Colors.Gray
        };

        scene.RootNode.Add(wireGridNode);

        ShowCameraAxisPanel = true;

        // CAD application usually use z-up right-handed coordinate system:
        // Z axis up, X axis to the right and Y axis into the screen
        ChangeCoordinateSystem(CoordinateSystems.ZUpRightHanded);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 220;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 500;
            targetPositionCamera.ViewWidth = 500;
        }
    }

    protected override void OnDisposed()
    {
        // Reset coordinate system back to default (YUpRightHanded):

        Scene?.SetCoordinateSystem(Scene.DefaultCoordinateSystem);

        // This is the same as:
        //Scene?.SetCoordinateSystem(CoordinateSystems.YUpRightHanded);

        base.OnDisposed();
    }

    private void ChangeCoordinateSystem(CoordinateSystems newCoordinateSystem)
    {
        if (Scene == null)
            return;

        Scene.SetCoordinateSystem(newCoordinateSystem);

#if TEST_ALL_CAMERA_TYPES
        UpdateCameraAfterCoordinateSystemChanged();
#endif
        

        if (_descriptionLabel != null)
        {
            string description = newCoordinateSystem switch
            {
                CoordinateSystems.YUpRightHanded => "The default coordinate system with Y axis up, X axis to the right and Z axis out of the screen. This coordinate system is also used in WPF 3D, Ab3d.PowerToys, Ab3d.DXEngine, OpenGL and Maya.",
                CoordinateSystems.YUpLeftHanded  => "Y axis up, X axis to the right and Z axis into the screen. This coordinate system is used in DirectX and Unity.",
                CoordinateSystems.ZUpRightHanded => "Standard CAD coordinate system with Z axis up, X axis to the right and Y axis into the screen. This coordinate system is used in most CAD applications, 3D Studio Max and Blender.",
                CoordinateSystems.ZUpLeftHanded  => "Z axis up, X axis to the right and Y axis out of the screen. This coordinate system is used in Unreal engine.",
                _ => ""
            };

            _descriptionLabel.SetText(description);
        }

        if (_axesDirectionsLabel != null)
        {
            var upVector = Scene.GetUpVector();
            var forwardVector = Scene.GetIntoTheScreenVector();
            var rightDirection = Scene.GetRightDirectionVector();

            _axesDirectionsLabel.SetText($"Up: {upVector:F0}\nForward: {forwardVector:F0}\nRight: {rightDirection:F0}");

            // You can also use the following properties and methods related to the current coordinate system:
            //var sceneIsDefaultCoordinateSystem = Scene.IsDefaultCoordinateSystem;
            //var sceneIsRightHandedCoordinateSystem = Scene.IsRightHandedCoordinateSystem;
            //var coordinateSystems = Scene.GetCoordinateSystem();
            //var coordinateSystemTransform = Scene.GetCoordinateSystemTransform();
            //var coordinateSystemInvertedTransform = Scene.GetCoordinateSystemInvertedTransform();

            // There are also static methods in the CameraUtils:
            //Ab4d.SharpEngine.Utilities.CameraUtils.GetUpVector(CoordinateSystems.ZUpRightHanded);
            //Ab4d.SharpEngine.Utilities.CameraUtils.GetIntoTheScreenVector(CoordinateSystems.ZUpRightHanded);
            //Ab4d.SharpEngine.Utilities.CameraUtils.GetRightDirectionVector(CoordinateSystems.ZUpRightHanded);
            //Ab4d.SharpEngine.Utilities.CameraUtils.GetCoordinateSystemTransformMatrix(CoordinateSystems.ZUpRightHanded);
            //Ab4d.SharpEngine.Utilities.CameraUtils.GetCoordinateSystemInvertedTransformMatrix(CoordinateSystems.ZUpRightHanded);
        }
    }


    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Coordinate system:", isHeader: true);

        var allCoordinateSystems = new string[] { "YUpRightHanded (Default)", "YUpLeftHanded", "ZUpRightHanded (CAD standard)", "ZUpLeftHanded "};
        ui.CreateRadioButtons(allCoordinateSystems, (selectedIndex, selectedText) =>
        {
            var newCoordinateSystem = (CoordinateSystems)selectedIndex;
            ChangeCoordinateSystem(newCoordinateSystem);
        }, selectedItemIndex: 2);

        ui.AddSeparator();

        ui.CreateLabel("Directions:");
        _axesDirectionsLabel = ui.CreateLabel("");

        ui.AddSeparator();

        _descriptionLabel = ui.CreateLabel("", width: 250, height: 100).SetStyle("italic");

        if (Scene != null)
            ChangeCoordinateSystem(Scene.GetCoordinateSystem()); // This will update the _descriptionLabel

        
#if TEST_ALL_CAMERA_TYPES
        ui.AddSeparator();

        ui.CreateLabel("Camera:", isHeader: true);

        ui.CreateComboBox(new string[] { "TargetPositionCamera", "FreeCamera", "FirstPersonCamera" }, (selectedIndex, selectedText) =>
        {
            SetupCameraType(selectedText);
        }, selectedItemIndex: 0);

        ui.CreateRadioButtons(new string[] { "Perspective", "Orthographic" }, (selectedIndex, selectedText) =>
        {
            SetupProjectionType(isPerspectiveCamera: selectedIndex == 0);
        }, selectedItemIndex: 0);
#endif
    }



#if TEST_ALL_CAMERA_TYPES
    private CameraAxisPanel? _cameraAxisPanel;

    private void SetupProjectionType(bool isPerspectiveCamera)
    {
        if (SceneView == null || SceneView.Camera == null)
            return;

        SceneView.Camera.ProjectionType = isPerspectiveCamera ? ProjectionTypes.Perspective : ProjectionTypes.Orthographic;
    }

    private void SetupCameraType(string? cameraType)
    {
        if (SceneView == null)
            return;

        var currentPerspectiveType = SceneView.Camera?.ProjectionType ?? ProjectionTypes.Perspective;

        if (cameraType == "TargetPositionCamera")
        {
            var newTargetPositionCamera = new TargetPositionCamera()
            {
                TargetPosition = new Vector3(0, 0, 0),
                Heading = 220,
                Attitude = -20,
                Distance = 400,
                ViewWidth = 400
            };

            SceneView.Camera = newTargetPositionCamera;
        }
        else if (cameraType == "FreeCamera")
        {
            var newFreeCamera = new FreeCamera()
            {
                TargetPosition = new Vector3(0, 0, 0),
                //CameraPosition = new Vector3(0, 400, 0), // CameraPosition is set in UpdateCameraAfterCoordinateSystemChanged
                ViewWidth = 400
            };

            SceneView.Camera = newFreeCamera;
            UpdateCameraAfterCoordinateSystemChanged(); // This will set the CameraPosition so that we will see something
        }
        else if (cameraType == "FirstPersonCamera")
        {            
            var newFirstPersonCamera = new FirstPersonCamera()
            {
                Heading = 0,
                Attitude = 0,
                ViewWidth = 400
            };

            SceneView.Camera = newFirstPersonCamera;
            UpdateCameraAfterCoordinateSystemChanged();
        }
        else
        {
            return;
        }

        SceneView.Camera.ProjectionType = currentPerspectiveType;


        if (this.CameraAxisPanel != null)
            this.CameraAxisPanel.Dispose();
        
        if (_cameraAxisPanel != null)
            _cameraAxisPanel.Dispose();

        _cameraAxisPanel = new CameraAxisPanel(SceneView);
        _cameraAxisPanel.Position = new Vector2(10, 10);
        _cameraAxisPanel.Alignment = PositionTypes.BottomLeft;
    }


    private void UpdateCameraAfterCoordinateSystemChanged()
    {
        if (Scene == null || SceneView == null)
            return;

        var coordinateSystems = Scene.GetCoordinateSystem();

        if (SceneView.Camera is FreeCamera freeCamera)
        {
            if (coordinateSystems == CoordinateSystems.YUpRightHanded)
                freeCamera.CameraPosition = new Vector3(0, 0, 400);
            else if (coordinateSystems == CoordinateSystems.YUpLeftHanded)
                freeCamera.CameraPosition = new Vector3(0, 0, -400);
            else if (coordinateSystems == CoordinateSystems.ZUpLeftHanded)
                freeCamera.CameraPosition = new Vector3(0, 400, 0);
            else
                freeCamera.CameraPosition = new Vector3(0, 400, 0);

            freeCamera.CalculateUpDirectionFromPositions();
        }
        else if (SceneView.Camera is FirstPersonCamera firstPersonCamera)
        {
            if (coordinateSystems == CoordinateSystems.YUpRightHanded)
                firstPersonCamera.CameraPosition = new Vector3(0, 0, 400);
            else if (coordinateSystems == CoordinateSystems.YUpLeftHanded)
                firstPersonCamera.CameraPosition = new Vector3(0, 0, -400);
            else if (coordinateSystems == CoordinateSystems.ZUpLeftHanded)
                firstPersonCamera.CameraPosition = new Vector3(0, 400, 0);
            else
                firstPersonCamera.CameraPosition = new Vector3(0, -400, 0);
        }
    }
#endif
}