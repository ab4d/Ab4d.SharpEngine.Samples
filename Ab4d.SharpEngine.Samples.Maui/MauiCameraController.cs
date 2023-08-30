using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using SkiaSharp.Views.Maui;
using System.Runtime.InteropServices;
using Ab4d.SharpEngine.Cameras;
using SKCanvasView = SkiaSharp.Views.Maui.Controls.SKCanvasView;

namespace Ab4d.SharpEngine.Samples.Maui;

public class MauiCameraController : ManualMouseCameraController
{
    private SKCanvasView _skCanvasView;

    private IPlatformInputHelper? _platformInputHelper;
    private PanGestureRecognizer? _panGestureRecognizer;
    private PinchGestureRecognizer? _pinchGesture;

    private bool _isRotationCenterPositionSaved;
    private Vector3? _savedRotationCenterPosition;
    private Vector2 _lastPinchPosition;

    [Flags]
    public enum InputMethods
    {
        None = 0,
        TouchEvent = 1,
        PanGestureRecognizer = 2,
        PinchGestureRecognizer = 4,
        DirectPlatformInput = 8
    }

    private InputMethods _usedInputMethod = InputMethods.None;
    private float _dpiScale;

    public InputMethods UsedInputMethod
    {
        get => _usedInputMethod;
        set
        {
            if (_usedInputMethod == value)
                return;

            _usedInputMethod = value;
            UpdateInputMethod();
        }
    }


    public MauiCameraController(SharpEngineSceneView sharpEngineSceneView)
        : this(sharpEngineSceneView, sharpEngineSceneView.SceneView)
    {
    }

    public MauiCameraController(SKCanvasView skCanvasView, SceneView sceneView)
        : base(sceneView)
    {
        _skCanvasView = skCanvasView;

        // Set defaults:
        RotateCameraConditions = MouseAndKeyboardConditions.LeftMouseButtonPressed;
        MoveCameraConditions   = MouseAndKeyboardConditions.RightMouseButtonPressed;


        // In windows use PlatformInputHelper to get mouse buttons and keyboard modifiers state
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            UsedInputMethod = InputMethods.TouchEvent | InputMethods.DirectPlatformInput | InputMethods.PinchGestureRecognizer; // pinch is used on windows with touch-screen
        }
        else if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            UsedInputMethod = InputMethods.PanGestureRecognizer | InputMethods.PinchGestureRecognizer;
        }
        else if (OperatingSystem.IsMacCatalyst())
        {
            UsedInputMethod = InputMethods.TouchEvent;
        }

        skCanvasView.Loaded += (sender, args) =>
        {
            _dpiScale = (float)DeviceDisplay.MainDisplayInfo.Density;
            if (_dpiScale < 1 || float.IsNaN(_dpiScale))
                _dpiScale = 1;
        };
    }

    private void UpdateInputMethod()
    {
        if (UsedInputMethod.HasFlag(InputMethods.TouchEvent))
        {
            if (!_skCanvasView.EnableTouchEvents)
            {
                _skCanvasView.EnableTouchEvents = true;
                _skCanvasView.Touch += SkCanvasViewOnTouch;
            }
        }
        else
        {
            if (_skCanvasView.EnableTouchEvents)
            {
                _skCanvasView.EnableTouchEvents = false;
                _skCanvasView.Touch -= SkCanvasViewOnTouch;
            }
        }

        if (UsedInputMethod.HasFlag(InputMethods.PanGestureRecognizer))
        {
            if (_panGestureRecognizer == null)
            {
                _panGestureRecognizer = new PanGestureRecognizer();
                _panGestureRecognizer.PanUpdated += PanGestureRecognizerOnPanUpdated;
                _skCanvasView.GestureRecognizers.Add(_panGestureRecognizer);
            }
        }
        else
        {
            if (_panGestureRecognizer != null)
            {
                _skCanvasView.GestureRecognizers.Remove(_panGestureRecognizer);
                _panGestureRecognizer.PanUpdated -= PanGestureRecognizerOnPanUpdated;
                _panGestureRecognizer = null;
            }
        }

        if (UsedInputMethod.HasFlag(InputMethods.PinchGestureRecognizer))
        {
            if (_pinchGesture == null)
            {
                _pinchGesture = new PinchGestureRecognizer();
                _pinchGesture.PinchUpdated += PinchGestureOnPinchUpdated;
                _skCanvasView.GestureRecognizers.Add(_pinchGesture);
            }
        }
        else
        {
            if (_pinchGesture != null)
            {
                _skCanvasView.GestureRecognizers.Remove(_pinchGesture);
                _pinchGesture.PinchUpdated -= PinchGestureOnPinchUpdated;
                _pinchGesture = null;
            }
        }

        if (UsedInputMethod.HasFlag(InputMethods.DirectPlatformInput))
        {
            if (_platformInputHelper == null)
                _platformInputHelper = new Ab4d.SharpEngine.Samples.Maui.PlatformInputHelper();
        }
        else
        {
            _platformInputHelper = null;
        }
    }

    private void PanGestureRecognizerOnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        //System.Diagnostics.Debug.WriteLine($"OnPanUpdated: {e.StatusType}  dx: {e.TotalX}  dy: {e.TotalY}");

        if (SceneView == null)
            return;

        // We only use Pan gesture when rotation or movement is bound to left mouse button
        if (RotateCameraConditions == MouseAndKeyboardConditions.LeftMouseButtonPressed ||
            MoveCameraConditions == MouseAndKeyboardConditions.LeftMouseButtonPressed)
        {
            // We need to multiply TotalX and TotalY with dpi scale
            float dx = (float)e.TotalX * _dpiScale;
            float dy = (float)e.TotalY * _dpiScale;

            // The ProcessXYZ methods below require an actual screen position of the mouse
            // We do not get that here but we have only offset of the pan from the start position.
            // Therefore we always assume that the pan has started at the start of the view
            var mousePosition = new Vector2((float)(SceneView.Width * 0.5f + dx), (float)(SceneView.Height * 0.5f + dy));

            if (e.StatusType == GestureStatus.Started)
            {
                base.ProcessMouseDown(mousePosition, MouseButtons.Left, KeyboardModifiers.None);
            }
            else if (e.StatusType == GestureStatus.Running)
            {
                base.ProcessMouseMove(mousePosition, MouseButtons.Left, KeyboardModifiers.None);
            }
            else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            {
                base.ProcessMouseUp(MouseButtons.None, KeyboardModifiers.None);
            }
        }
    }

    private void PinchGestureOnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        //System.Diagnostics.Debug.WriteLine($"OnPinchUpdated: {e.Status} scale: {e.Scale}  ScaleOrigin: {e.ScaleOrigin}");

        // ScaleOrigin is relative (from 0 to 1) so it does not need to be multiplied by dpi scale
        var originPosition = new Vector2((float)(SceneView!.Width * e.ScaleOrigin.X), (float)(SceneView.Height * e.ScaleOrigin.Y));

        if (ZoomMode == CameraZoomMode.MousePosition &&
            e.Status == GestureStatus.Started && 
            SceneView != null && SceneView.IsInitialized)
        {
            // ScaleOrigin is relative (from 0 to 1) so it does not need to be multiplied by dpi scale
            _lastPinchPosition = originPosition;

            var hitResult = SceneView.GetClosestHitObject(_lastPinchPosition.X, _lastPinchPosition.Y);
            if (hitResult != null && SceneView.Camera is IRotationCenterPositionCamera rotationCenterPositionCamera)
            {
                _savedRotationCenterPosition = rotationCenterPositionCamera.RotationCenterPosition;
                _isRotationCenterPositionSaved = true;

                rotationCenterPositionCamera.RotationCenterPosition = hitResult.HitPosition;
            }
        }
        else if (e.Status == GestureStatus.Running)
        {
            // We also use pinch (two fingers) to move the camera
            var dx = originPosition.X - _lastPinchPosition.X;
            var dy = originPosition.Y - _lastPinchPosition.Y;

            if (!MathUtils.IsZero(dx) && !MathUtils.IsZero(dy))
            {
                this.MoveCamera(dx, dy);
                _lastPinchPosition = originPosition;
            }


            this.ChangeCameraDistance(1f / (float)e.Scale);
        }
        if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
        {
            if (_isRotationCenterPositionSaved && SceneView != null && SceneView.Camera is IRotationCenterPositionCamera rotationCenterPositionCamera)
            {
                rotationCenterPositionCamera.RotationCenterPosition = _savedRotationCenterPosition;
                _isRotationCenterPositionSaved = false;
            }
        }
    }

    //private void TapGestureRecognizerOnTapped(object? sender, TappedEventArgs e)
    //{
    //    System.Diagnostics.Debug.WriteLine($"OnTapped: buttons: {e.Buttons} position: {e.GetPosition(_skCanvasView)}");
    //}

    private void SkCanvasViewOnTouch(object? sender, SKTouchEventArgs e)
    {
        bool isHandled = false;

        var actionType = e.ActionType;

        var mousePosition = new Vector2(e.Location.X, e.Location.Y);
        
        // Uncomment the following line to see the data that is passed to Touch event:
        //System.Diagnostics.Debug.WriteLine($"Touch: {actionType} at {mousePosition}, MouseButton: {e.MouseButton}");

        MouseButtons pressedMouseButtons;
        KeyboardModifiers keyboardModifiers;

        // Update _pressedMouseButtons

        // The following code is not reliable:
        // - when left button is pressed and then also right button is pressed, there is no additional Pressed event and the Move event still shows only Left mouse button.
        // - if button press / release happens outside of the control, we do not get any event
        //if (actionType == SKTouchAction.Pressed)
        //{
        //    var mouseButtons = ConvertToMouseButtons(e.MouseButton);
        //    _pressedMouseButtons |= mouseButtons; // Add released button to current button flags
        //}
        //else if (actionType == SKTouchAction.Released)
        //{
        //    var mouseButtons = ConvertToMouseButtons(e.MouseButton);
        //    _pressedMouseButtons &= ~mouseButtons; // Remove released button from current button flags
        //}
        //else if (actionType == SKTouchAction.Exited)
        //{
        //    _pressedMouseButtons = MouseButtons.None;
        //}
        //else if (actionType == SKTouchAction.Entered)
        //{
        //    _pressedMouseButtons = ConvertToMouseButtons(e.MouseButton); // This seems to contain the currently pressed event
        //}

        if (_platformInputHelper != null && _platformInputHelper.IsCurrentMouseButtonAvailable)
        {
            pressedMouseButtons = _platformInputHelper.GetCurrentMouseButtons();

            if (_platformInputHelper.IsCurrentKeyboardModifierAvailable)
                keyboardModifiers = _platformInputHelper.GetCurrentKeyboardModifiers();
            else
                keyboardModifiers = KeyboardModifiers.None; // cannot get keyboard modifiers or they are not supported on this platform
        }
        else
        {
            // The following code is the most reliable based on testing
            pressedMouseButtons = ConvertToMouseButtons(e.MouseButton);
            keyboardModifiers = KeyboardModifiers.None;
        }

        if (actionType == SKTouchAction.Pressed)
        {
            // Start rotate
            isHandled = base.ProcessMouseDown(mousePosition, pressedMouseButtons, keyboardModifiers);
        }
        else if (actionType == SKTouchAction.Released)
        {
            // End rotate
            isHandled = base.ProcessMouseUp(MouseButtons.None, keyboardModifiers);
        }
        else if (actionType == SKTouchAction.Moved)
        {
            // Rotate / move
            isHandled = base.ProcessMouseMove(mousePosition, pressedMouseButtons, keyboardModifiers);
        }


        if (actionType == SKTouchAction.WheelChanged)
        {
            isHandled = base.ProcessMouseWheel(mousePosition, e.WheelDelta);
        }

        e.Handled = isHandled;
    }

    private MouseButtons ConvertToMouseButtons(SKMouseButton skMouseButton) => skMouseButton switch
    {
        SKMouseButton.Left   => MouseButtons.Left,
        SKMouseButton.Middle => MouseButtons.Middle,
        SKMouseButton.Right  => MouseButtons.Right,
        _                    => MouseButtons.None
    };
}