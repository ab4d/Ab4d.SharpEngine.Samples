using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using System;
using System.Numerics;
using Ab4d.SharpEngine.Samples.UnoPlatform;
using Newtonsoft.Json.Linq;

namespace Ab4d.SharpEngine.UnoPlatform
{
    /// <summary>
    /// PointerCameraController for Uno Platform that can control the camera through pointer events.
    /// Handles platform-specific differences in pointer behavior across Android, iOS, WASM and Skia.
    /// </summary>
    public class PointerCameraController : ManualPointerCameraController
    {
        private UIElement? _eventsSourceElement;
        private UIElement? _subscribedElement;
        private SharpEngineSceneView _sharpEngineSceneView;
        private uint? _capturedPointerId;

        /// <summary>
        /// Gets or sets the UIElement that is used to subscribe to pointer events.
        /// If not set, SharpEngineSceneView element is used as event source (if not null).
        /// </summary>
        public UIElement? EventsSourceElement
        {
            get => _eventsSourceElement;
            set
            {
                if (_eventsSourceElement == value)
                    return;

                if (_eventsSourceElement != null)
                    UnsubscribeFromEvents();

                _eventsSourceElement = value;

                if (value != null)
                    SubscribeToEvents();
            }
        }

        public SharpEngineSceneView SharpEngineSceneView
        {
            get => _sharpEngineSceneView;
            set
            {
                if (_sharpEngineSceneView == value)
                    return;

                if (EventsSourceElement == _sharpEngineSceneView)
                    EventsSourceElement = null;

                _sharpEngineSceneView = value;

                if (value != null)
                {
                    SceneView = value.SceneView;
                    CapturePointerAction = () => 
                    {
                       // if (_capturedPointerId.HasValue)
                            //value.CapturePointer(_capturedPointerId.Value);
                    };
                    ReleasePointerCaptureAction = () =>
                    {
                        if (_capturedPointerId.HasValue)
                        {
                           // value.ReleasePointerCapture(_capturedPointerId.Value);
                            _capturedPointerId = null;
                        }
                    };
                }
                else
                {
                    SceneView = null;
                    CapturePointerAction = null;
                    ReleasePointerCaptureAction = null;
                }

                if (EventsSourceElement == null && value != null)
                    EventsSourceElement = value;
            }
        }
        
        public PointerCameraController(
            SharpEngineSceneView sharpEngineSceneView,
            UIElement? eventsSourceElement = null): base(sharpEngineSceneView.SceneView)
        {
            if (eventsSourceElement != null)
                EventsSourceElement = eventsSourceElement;
            
            _sharpEngineSceneView = sharpEngineSceneView;
            SceneView = sharpEngineSceneView.SceneView;
        }

        private void SubscribeToEvents()
        {
            if (_eventsSourceElement == null || _eventsSourceElement == _subscribedElement)
                return;

            UnsubscribeFromEvents();

            _eventsSourceElement.PointerMoved += OnPointerMoved;
            _eventsSourceElement.PointerPressed += OnPointerPressed;
            _eventsSourceElement.PointerReleased += OnPointerReleased;
            _eventsSourceElement.PointerCanceled += OnPointerCanceled;
            _eventsSourceElement.PointerCaptureLost += OnPointerCaptureLost;

            #if HAS_UNO_SKIA || __WASM__
            _eventsSourceElement.PointerWheelChanged += OnPointerWheelChanged;
            #endif

            _subscribedElement = _eventsSourceElement;
        }

        private void UnsubscribeFromEvents()
        {
            if (_subscribedElement == null)
                return;

            _subscribedElement.PointerMoved -= OnPointerMoved;
            _subscribedElement.PointerPressed -= OnPointerPressed;
            _subscribedElement.PointerReleased -= OnPointerReleased;
            _subscribedElement.PointerCanceled -= OnPointerCanceled;
            _subscribedElement.PointerCaptureLost -= OnPointerCaptureLost;

            #if HAS_UNO_SKIA || __WASM__
            _subscribedElement.PointerWheelChanged -= OnPointerWheelChanged;
            #endif

            _subscribedElement = null;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (Camera == null || _eventsSourceElement == null)
                return;

            _capturedPointerId = e.Pointer.PointerId;

            var position = e.GetCurrentPoint(_eventsSourceElement).Position;
            var pressedButtons = GetPressedButtons(e);
            var keyboardModifiers = GetKeyboardModifiers(e);

            ProcessPointerPressed(
                new Vector2((float)position.X, (float)position.Y),
                pressedButtons,
                keyboardModifiers);
               SharpEngineSceneView.Refresh();
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (Camera == null || _eventsSourceElement == null)
                return;

            var position = e.GetCurrentPoint(_eventsSourceElement).Position;
            var pressedButtons = GetPressedButtons(e);
            var keyboardModifiers = GetKeyboardModifiers(e);

            ProcessPointerMoved(
                new Vector2((float)position.X, (float)position.Y),
                pressedButtons,
                keyboardModifiers);
            
            SharpEngineSceneView.Refresh();
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (Camera == null)
                return;

            var pressedButtons = GetPressedButtons(e);
            var keyboardModifiers = GetKeyboardModifiers(e);

            ProcessPointerReleased(pressedButtons, keyboardModifiers);
            _capturedPointerId = null;
            
            SharpEngineSceneView.Refresh();
        }

        private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            if (Camera == null)
                return;

            ProcessPointerReleased(PointerButtons.None, KeyboardModifiers.None);
            _capturedPointerId = null;
            
            SharpEngineSceneView.Refresh();
        }

        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (Camera == null)
                return;

            ProcessPointerReleased(PointerButtons.None, KeyboardModifiers.None);
            _capturedPointerId = null;
            
            SharpEngineSceneView.Refresh();
        }

        #if HAS_UNO_SKIA || __WASM__
        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (Camera == null || _eventsSourceElement == null)
                return;

            var position = e.GetCurrentPoint(_eventsSourceElement).Position;
            var properties = e.GetCurrentPoint(_eventsSourceElement).Properties;
            
            ProcessPointerWheelChanged(
                new Vector2((float)position.X, (float)position.Y),
                properties.MouseWheelDelta);
            
            SharpEngineSceneView.Refresh();
        }
        #endif

        private static KeyboardModifiers GetKeyboardModifiers(PointerRoutedEventArgs e)
        {
            var keyboardModifiers = KeyboardModifiers.None;
            var keyModifiers = e.KeyModifiers;

            if ((keyModifiers & Windows.System.VirtualKeyModifiers.Control) != 0)
                keyboardModifiers |= KeyboardModifiers.ControlKey;
            if ((keyModifiers & Windows.System.VirtualKeyModifiers.Shift) != 0)
                keyboardModifiers |= KeyboardModifiers.ShiftKey;
            if ((keyModifiers & Windows.System.VirtualKeyModifiers.Menu) != 0)
                keyboardModifiers |= KeyboardModifiers.AltKey;
            if ((keyModifiers & Windows.System.VirtualKeyModifiers.Windows) != 0)
                keyboardModifiers |= KeyboardModifiers.SuperKey;

            return keyboardModifiers;
        }

        private PointerButtons GetPressedButtons(PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint(_eventsSourceElement).Properties;
            var pressedButtons = PointerButtons.None;

            if (properties.IsLeftButtonPressed)
                pressedButtons |= PointerButtons.Left;
            if (properties.IsRightButtonPressed)
                pressedButtons |= PointerButtons.Right;
            if (properties.IsMiddleButtonPressed)
                pressedButtons |= PointerButtons.Middle;
            
            // XButton states are not reliably available on all platforms
            // Only add them if the platform supports them
            #if HAS_UNO_SKIA
            if (properties.IsXButton1Pressed)
                pressedButtons |= PointerButtons.XButton1;
            if (properties.IsXButton2Pressed)
                pressedButtons |= PointerButtons.XButton2;
            #endif

            return pressedButtons;
        }
    }
}
