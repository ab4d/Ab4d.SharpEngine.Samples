//#define WRITE_INFO_TO_OUTPUT

using System;
using System.Numerics;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Common;
using Avalonia.Input;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Common;

/// <summary>
/// GesturesCameraController extends the standard MouseCameraController is processes Avalonia's mouse events for SharpEngine.
/// This class adds support for Scroll and Pinch gestures.
/// </summary>
public class GesturesCameraController : MouseCameraController
{
    public bool IsPinchGestureEnabled { get; set; } = true;

    public bool IsScrollGestureEnabled { get; set; } = true;

    public bool RotateCameraWithScrollGesture { get; set; } = true;

    private float _previousPinchScale = 1;
    private Vector3? _lastPinchRotationCenterPosition;

    public GesturesCameraController(SharpEngineSceneView sharpEngineSceneView, IInputElement? eventsSourceElement = null) : base(sharpEngineSceneView, eventsSourceElement)
    {
        Gestures.ScrollGestureEvent.AddClassHandler<SharpEngineSceneView>(ScrollGestureHandler);
        Gestures.ScrollGestureEndedEvent.AddClassHandler<SharpEngineSceneView>(ScrollGestureEndedHandler);
        Gestures.PinchEvent.AddClassHandler<SharpEngineSceneView>(PinchEventHandler);
        Gestures.PinchEndedEvent.AddClassHandler<SharpEngineSceneView>(PinchEndedEventHandler);
    }


    private void ScrollGestureHandler(SharpEngineSceneView target, ScrollGestureEventArgs args)
    {
        if (!IsScrollGestureEnabled || !ReferenceEquals(target, this.SharpEngineSceneView))
            return;


        float mouseDx = (float)-args.Delta.X;
        float mouseDy = (float)-args.Delta.Y;

        if (RotateCameraWithScrollGesture)
            RotateCamera(mouseDx, mouseDy);
        else
            MoveCamera(mouseDx, mouseDy);
        
        args.Handled = true;

#if WRITE_INFO_TO_OUTPUT
        System.Diagnostics.Debug.WriteLine($"Delta: {args.Delta}, ShouldEndScrollGesture: {args.ShouldEndScrollGesture}");
#endif
    }
    
    private void ScrollGestureEndedHandler(SharpEngineSceneView target, ScrollGestureEndedEventArgs args)
    {
        if (!IsScrollGestureEnabled || !ReferenceEquals(target, this.SharpEngineSceneView))
            return;

        EndMouseProcessing(); // This is required to prevent invalid camera rotation after next touch (this will not be needed anymore in the next version)

        args.Handled = true;

#if WRITE_INFO_TO_OUTPUT
        System.Diagnostics.Debug.WriteLine("ScrollGestureEnded");
#endif
    }

    private void PinchEventHandler(SharpEngineSceneView target, PinchEventArgs args)
    {
        if (!IsPinchGestureEnabled || !ReferenceEquals(target, this.SharpEngineSceneView))
            return;

        var mousePosition = new Vector2((float)args.ScaleOrigin.X, (float)args.ScaleOrigin.Y);
        var scale = (float)args.Scale;


        bool resetRotationCenterPosition = false;
        Vector3? savedRotationCenterPosition = null;

        // If ZoomMode is set to MousePosition and we have started a new zooming (there is more then 1 seconds from last zoom event),
        // then we use hit testing to get new Camera's RotationCenterPosition
        if (this.ZoomMode == CameraZoomMode.MousePosition)
        {
            // Save RotationCenterPosition so it can be reset at the end of this method
            savedRotationCenterPosition = GetCameraRotationCenterPosition();
            resetRotationCenterPosition = true;

            if (lastZoomMousePosition != mousePosition)
            {
                bool isSupportedCamera = UpdateRotationCenterPosition(mousePosition, calculatePositionWhenNoObjectIsHit: true);
                if (!isSupportedCamera)
                    throw new NotSupportedException("MousePosition ZoomMode can be used only when MouseCameraController is controlling TargetPositionCamera or FreeCamera.");

                lastZoomMousePosition            = mousePosition;
                _lastPinchRotationCenterPosition = GetCameraRotationCenterPosition();
            }
            else
            {
                SetCameraRotationCenterPosition(_lastPinchRotationCenterPosition);
            }
        }


        float scaleDiff = _previousPinchScale / scale;
        _previousPinchScale = scale;

        this.ChangeCameraDistance(scaleDiff, zoomToRotationCenterPosition: true);


        // Set the RotationCenterPosition back to its previous value
        if (resetRotationCenterPosition)
            SetCameraRotationCenterPosition(savedRotationCenterPosition);

#if WRITE_INFO_TO_OUTPUT
        System.Diagnostics.Debug.WriteLine($"Scale: {args.Scale}, pos: {args.ScaleOrigin}");
#endif

        args.Handled = true;
    }

    private void PinchEndedEventHandler(SharpEngineSceneView target, PinchEndedEventArgs args)
    {
        if (!IsPinchGestureEnabled || !ReferenceEquals(target, this.SharpEngineSceneView))
            return;

        _previousPinchScale = 1;
        lastZoomMousePosition = new Vector2(float.NaN, float.NaN);
        
        args.Handled = true;

#if WRITE_INFO_TO_OUTPUT
        System.Diagnostics.Debug.WriteLine("PinchEnded");
#endif
    }
}