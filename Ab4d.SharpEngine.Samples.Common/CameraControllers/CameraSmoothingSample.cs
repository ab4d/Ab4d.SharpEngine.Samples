using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.CameraControllers;

public class CameraSmoothingSample : CommonSample
{
    public override string Title => "Smooth camera rotation, movement and zooming";

    public override string? Subtitle => "Rotate camera: left mouse button\nMove camera: CTRL + left mouse button\nChange distance: mouse wheel\nQuick zoom: left and right mouse buttons";

    private ManualPointerCameraController? _pointerCameraController;
    private CameraController.CameraSmoothingPresets _savedSmoothing;
    private PointerAndKeyboardConditions _savedQuckZoomSettings;

    public CameraSmoothingSample(ICommonSamplesContext context) 
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var boxModelNode = new BoxModelNode(new Vector3(0, 15, 0), new Vector3(60, 30, 40), StandardMaterials.LightGreen);
        scene.RootNode.Add(boxModelNode);
        
        var sphereModelNode = new SphereModelNode(new Vector3(100, 15, 0), 30, StandardMaterials.SkyBlue);
        scene.RootNode.Add(sphereModelNode);
        
        var pyramidModelNode = new PyramidModelNode(new Vector3(-100, 0, 0), new Vector3(50, 50, 50), StandardMaterials.Gold);
        scene.RootNode.Add(pyramidModelNode);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading  = 20;
            targetPositionCamera.Attitude = -30;
            targetPositionCamera.Distance = 400;
        }
    }

    protected override void OnDisposed()
    {
        if (_pointerCameraController != null)
        {
            _pointerCameraController.CameraSmoothing = _savedSmoothing;
            _pointerCameraController.QuickZoomConditions = _savedQuckZoomSettings;
        }
        
        base.OnDisposed();
    }

    public override void InitializePointerCameraController(ManualPointerCameraController pointerCameraController)
    {
        base.InitializePointerCameraController(pointerCameraController);

        // Enable quick zoom (usually it is disabled by default)
        _savedQuckZoomSettings = pointerCameraController.QuickZoomConditions; 
        pointerCameraController.QuickZoomConditions = PointerAndKeyboardConditions.LeftPointerButtonPressed | PointerAndKeyboardConditions.RightPointerButtonPressed;
        
        // Set CameraSmoothing to Slow so this effect is better visible.
        // Usually, the app should use Normal or Fast preset.
        _savedSmoothing = pointerCameraController.CameraSmoothing;
        pointerCameraController.CameraSmoothing = CameraController.CameraSmoothingPresets.Slow;
        
        _pointerCameraController = pointerCameraController;
    }
    
    private void UseCustomVerySlowSmoothing(ManualPointerCameraController pointerCameraController)
    {
        // Set very slow smoothing 
        // The CameraSmoothing presets have the following values:
        // Slow: 9
        // Normal: 16
        // Fast: 22
        var customSmoothFactor = 3; // bigger values mean faster smoothing
        
        // You can also enable individual camera smoothing modes by calling the SetCameraSmoothingMode method:
        pointerCameraController.SetAdvancedSmoothSettings(isSmoothRotationEnabled: true,
                                                          isSmoothMovementEnabled: true,
                                                          isSmoothZoomEnabled: true,
                                                          customSmoothFactor: customSmoothFactor, // optional nullable parameter that sets the custom smooth factor (bigger values mean faster smoothing)
                                                          customRotationSmoothingFunction: null, // optional
                                                          customMovementSmoothingFunction: null, // optional
                                                          customZoomSmoothingFunction: null); // optional
        
        // You can also provide custom smoothing functions for rotation, movement, and zoom by calling the SetAdvancedSmoothSettings method (or with the previously used overload).
        // See the empty custom smoothing functions below (commented).
        // pointerCameraController.SetAdvancedSmoothSettings(customRotationSmoothingFunction: CustomRotationSmoothingFunction,
        //                                                   customMovementSmoothingFunction: CustomMovementSmoothingFunction,
        //                                                   customZoomSmoothingFunction: CustomZoomSmoothingFunction);
    }

    // private void CustomRotationSmoothingFunction(float targetHeading, float targetAttitude, float elapsedTime, ref float newHeading, ref float newAttitude)
    // {
    // }
    //
    // private void CustomMovementSmoothingFunction(Vector2 targetPointerMoveVector, float elapsedTime, ref Vector2 newPointerMoveVector)
    // {
    // }
    //
    // private void CustomZoomSmoothingFunction(float targetDistance, float targetCameraWidth, float elapsedTime, ref float newDistance, ref float newCameraWidth)
    // {
    // }
    

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);
        
        var smoothingPossibilities = Enum.GetNames<CameraController.CameraSmoothingPresets>().ToList();
        smoothingPossibilities.Add("Custom (Very slow)");
        
        ui.CreateComboBox(smoothingPossibilities.ToArray(), (selectedIndex, selectedText) =>
            {
                if (selectedText != null && _pointerCameraController != null)
                {
                    if (selectedText == "Custom (Very slow)")
                    {
                        UseCustomVerySlowSmoothing(_pointerCameraController);
                    }
                    else
                    {
                        var smoothingPreset = Enum.Parse<CameraController.CameraSmoothingPresets>(selectedText);
                        _pointerCameraController.CameraSmoothing = smoothingPreset;
                    }
                }
            }, selectedItemIndex: 1, // 1 = Slow
            width: 140,
            keyText: "CameraSmoothing:");
    }
}