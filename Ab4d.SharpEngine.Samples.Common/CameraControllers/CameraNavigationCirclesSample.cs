using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.OverlayPanels;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.CameraControllers;

public class CameraNavigationCirclesSample : CommonSample
{
    private ManualInputEventsManager? _inputEventsManager;
    private CameraNavigationCircles? _cameraNavigationCircles;

    public override string Title => "CameraNavigationCircles";
    //public override string? Subtitle => "Use left pointer or mouse button to rotate the camera around the specified custom 3D position";

    public CameraNavigationCirclesSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        if (targetPositionCamera != null)
            targetPositionCamera.Distance = 400;

        // Create hash symbol
        var hashSymbolMesh = MeshFactory.CreateHashSymbolMesh(centerPosition: new Vector3(0, 0, 0),
                                                              shapeYVector: new Vector3(0, 0, -1),
                                                              extrudeVector: new Vector3(0, 16, 0),
                                                              size: 100,
                                                              barThickness: 16,
                                                              barOffset: 20,
                                                              name: "HashSymbolMesh");

        var hashModel = new Ab4d.SharpEngine.SceneNodes.MeshModelNode(hashSymbolMesh, "HashSymbolModel")
        {
            Material = new StandardMaterial(Color3.FromByteRgb(255, 197, 0)),
            Transform = new StandardTransform()
        };

        scene.RootNode.Add(hashModel);
    }

    protected override void OnInputEventsManagerInitialized(ManualInputEventsManager inputEventsManager)
    {
        // NOTE:
        // This method gets inputEventsManager that is ManualInputEventsManager and not InputEventsManager.
        // ManualInputEventsManager is a genetic platform-independent object that is defined in Ab4d.SharpEngine.
        // The InputEventsManager is platform specific and a different class is defined for WPF, Avalonia, and other platforms.

        _inputEventsManager = inputEventsManager;

        _cameraNavigationCircles = new CameraNavigationCircles(_inputEventsManager.SceneView, 
                                                               _inputEventsManager, 
                                                               width: 150, height: 150, 
                                                               adjustSizeByDpiScale: true, 
                                                               alignment: PositionTypes.BottomLeft)
        {
            Position = new Vector2(10, 10),
            Alignment = PositionTypes.BottomLeft,

            ModifierKeyForOppositeAxis = KeyboardModifiers.ShiftKey
        };

        // It is also possible to create CameraNavigationCircles without InputEventsManager and only with a SceneView (use another constructor and set inputEventsManager to null),
        // In this case user needs to manually call ProcessPointerMove, ProcessPointerDown and ProcessPointerUp methods.


        // Subscribe events:
        _cameraNavigationCircles.AxisClicked += (sender, args) => LogMessage("AxisClicked: " + args.AxisName);
        _cameraNavigationCircles.AxisSelected += (sender, args) => LogMessage("AxisSelected: " + args.AxisName);
        _cameraNavigationCircles.AxisDeselected += (sender, args) => LogMessage("AxisDeselected: " + args.AxisName);
        _cameraNavigationCircles.BackgroundCircleEnter += (sender, args) => LogMessage("BackgroundCircleEnter");
        _cameraNavigationCircles.BackgroundCircleLeave += (sender, args) => LogMessage("BackgroundCircleLeave");

        base.OnInputEventsManagerInitialized(inputEventsManager);
    }

    private void LogMessage(string message)
    {
        //InfoTextBox.Text = message + "\n" + InfoTextBox.Text;
    }
}