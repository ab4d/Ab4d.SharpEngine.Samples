//#define WRITE_INFO_TO_OUTPUT

using System;
using System.Numerics;
using Ab4d.SharpEngine.AvaloniaUI;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.CrossPlatform;

/// <summary>
/// GesturesCameraController extends the standard PointerCameraController.
/// It processes Avalonia's mouse events for SharpEngine.
/// This class adds support for Scroll and Pinch gestures.
/// </summary>
public class GesturesCameraController : PointerCameraController
{
    public bool IsPinchGestureEnabled { get; set; } = true;

    public bool IsScrollGestureEnabled { get; set; } = true;

    public bool RotateCameraWithScrollGesture { get; set; } = true;
    
    public bool RotateWithPinchGesture { get; set; } = false;

    private float _previousPinchScale = 1;
    private Vector3? _lastPinchRotationCenterPosition;

    public GesturesCameraController(SharpEngineSceneView sharpEngineSceneView, InputElement? eventsSourceElement = null) 
        : base(sharpEngineSceneView, eventsSourceElement)
    {
        if (eventsSourceElement == null)
            eventsSourceElement = sharpEngineSceneView;
        
        eventsSourceElement.GestureRecognizers.Add(new PinchGestureRecognizer());

        sharpEngineSceneView.AddHandler(Gestures.PinchEvent, PinchEventHandler);
        sharpEngineSceneView.AddHandler(Gestures.PinchEndedEvent, PinchEndedEventHandler);


        eventsSourceElement.GestureRecognizers.Add(new ScrollGestureRecognizer() { CanHorizontallyScroll = true, CanVerticallyScroll = true });

        eventsSourceElement.AddHandler(Gestures.ScrollGestureEvent, ScrollGestureHandler);
        eventsSourceElement.AddHandler(Gestures.ScrollGestureEndedEvent, ScrollGestureEndedHandler);
    }


    private void ScrollGestureHandler(object? sender, ScrollGestureEventArgs args)
    {
        if (!IsScrollGestureEnabled)
            return;

        if (EventsSourceElement != null && !ReferenceEquals(sender, EventsSourceElement))
            return;


        float pointerDx = (float)-args.Delta.X;
        float pointerDy = (float)-args.Delta.Y;

        if (RotateCameraWithScrollGesture)
            RotateCamera(pointerDx, pointerDy);
        else
            MoveCamera(pointerDx, pointerDy);

        args.Handled = true;

#if WRITE_INFO_TO_OUTPUT
        System.Diagnostics.Debug.WriteLine($"Delta: {args.Delta}, ShouldEndScrollGesture: {args.ShouldEndScrollGesture}");
#endif
    }

    private void ScrollGestureEndedHandler(object? sender, ScrollGestureEndedEventArgs args)
    {
        if (!IsScrollGestureEnabled)
            return;

        if (EventsSourceElement != null && !ReferenceEquals(sender, EventsSourceElement))
            return;


        EndPointerProcessing(); // This is required to prevent invalid camera rotation after next touch (this will not be needed anymore in the next version)
        
        args.Handled = true;

#if WRITE_INFO_TO_OUTPUT
        System.Diagnostics.Debug.WriteLine("ScrollGestureEnded");
#endif
    }

    private void PinchEventHandler(object? sender, PinchEventArgs args)
    {
        if (!IsPinchGestureEnabled && !RotateWithPinchGesture)
            return;

        if (EventsSourceElement != null && !ReferenceEquals(sender, EventsSourceElement))
            return;


        var pointerPosition = new Vector2((float)args.ScaleOrigin.X, (float)args.ScaleOrigin.Y);
        bool resetRotationCenterPosition = false;
        bool isCameraRotationCenterPositionUpdated = false;
        
        // Save RotationCenterPosition so it can be reset at the end of this method
        Vector3? savedRotationCenterPosition = GetCameraRotationCenterPosition();


        if (RotateWithPinchGesture && Camera != null)
        {
            if (args.AngleDelta != 0)
            {
                // Solve the issue in Avalonia v11.2.5 with the angle that is not correctly calculated when the angle is more then 180 degrees
                // See: https://github.com/AvaloniaUI/Avalonia/issues/18376
                var angleDelta = (float)args.AngleDelta;
                if (angleDelta > 180)
                    angleDelta -= 360;
                else if (angleDelta < -180)
                    angleDelta += 360;

                float headingChange = 0;
                float attitudeChange = 0;
                
                if (this.IsHeadingRotationEnabled)
                {
                    headingChange = angleDelta;

                    // We need to invert the heading change when the object is vertically inverted because of attitude angles between 0 and 180 degrees
                    if (Camera is ISphericalCamera sphericalCamera)
                    {
                        var normalizedAttitude = Ab4d.SharpEngine.Utilities.CameraUtils.NormalizeAngleTo360(sphericalCamera.Attitude);
                        if (0 < normalizedAttitude && normalizedAttitude < 180)
                            headingChange = -headingChange;
                    }
                }
                else if (this.IsAttitudeRotationEnabled)
                {
                    attitudeChange = angleDelta;
                }

                if (this.IsXAxisInverted)
                    headingChange = -headingChange;

                if (this.IsYAxisInverted)
                    attitudeChange = -attitudeChange;


                if (RotateAroundPointerPosition)
                {
                    if (lastZoomPointerPosition != pointerPosition)
                    {
                        bool isSupportedCamera = UpdateRotationCenterPosition(pointerPosition, calculatePositionWhenNoObjectIsHit: true);
                        if (!isSupportedCamera)
                            throw new NotSupportedException("PointerPosition ZoomMode can be used only when PointerCameraController is controlling TargetPositionCamera or FreeCamera.");

                        lastZoomPointerPosition = pointerPosition;
                        _lastPinchRotationCenterPosition = GetCameraRotationCenterPosition();
                    }
                    else
                    {
                        SetCameraRotationCenterPosition(_lastPinchRotationCenterPosition);
                    }

                    resetRotationCenterPosition = true;
                    isCameraRotationCenterPositionUpdated = true;
                }
                
                Camera.RotateCamera(headingChange, attitudeChange);
            }

#if WRITE_INFO_TO_OUTPUT
            System.Diagnostics.Debug.WriteLine($"AngleDelta: {args.AngleDelta};  Angle: {args.Angle}");
#endif
        }

        if (!IsPinchGestureEnabled)
        {
            // Set the RotationCenterPosition back to its previous value
            if (resetRotationCenterPosition)
                SetCameraRotationCenterPosition(savedRotationCenterPosition);

            return;
        }


        var scale = (float)args.Scale;


        // If ZoomMode is set to PointerPosition and we have started a new zooming (there is more then 1 seconds from last zoom event),
        // then we use hit testing to get new Camera's RotationCenterPosition
        if (ZoomMode == CameraZoomMode.PointerPosition && !isCameraRotationCenterPositionUpdated)
        {
            resetRotationCenterPosition = true;

            if (lastZoomPointerPosition != pointerPosition)
            {
                bool isSupportedCamera = UpdateRotationCenterPosition(pointerPosition, calculatePositionWhenNoObjectIsHit: true);
                if (!isSupportedCamera)
                    throw new NotSupportedException("PointerPosition ZoomMode can be used only when PointerCameraController is controlling TargetPositionCamera or FreeCamera.");

                lastZoomPointerPosition = pointerPosition;
                _lastPinchRotationCenterPosition = GetCameraRotationCenterPosition();
            }
            else
            {
                SetCameraRotationCenterPosition(_lastPinchRotationCenterPosition);
            }
        }


        float scaleDiff = _previousPinchScale / scale;
        _previousPinchScale = scale;

        ChangeCameraDistance(scaleDiff, zoomToRotationCenterPosition: true);


        // Set the RotationCenterPosition back to its previous value
        if (resetRotationCenterPosition)
            SetCameraRotationCenterPosition(savedRotationCenterPosition);

#if WRITE_INFO_TO_OUTPUT
        System.Diagnostics.Debug.WriteLine($"Scale: {args.Scale}, pos: {args.ScaleOrigin}");
#endif

        args.Handled = true;
    }

    private void PinchEndedEventHandler(object? sender, PinchEndedEventArgs args)
    {
        if (!IsPinchGestureEnabled && !RotateWithPinchGesture)
            return;

        if (EventsSourceElement != null && !ReferenceEquals(sender, EventsSourceElement))
            return;

        _previousPinchScale = 1;
        lastZoomPointerPosition = new Vector2(float.NaN, float.NaN);

        args.Handled = true;

#if WRITE_INFO_TO_OUTPUT
        System.Diagnostics.Debug.WriteLine("PinchEnded");
#endif
    }
}