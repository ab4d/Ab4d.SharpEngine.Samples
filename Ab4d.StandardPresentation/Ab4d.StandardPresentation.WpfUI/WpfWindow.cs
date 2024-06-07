using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Ab4d;
using Ab4d.SharpEngine.Common;
using Ab4d.StandardPresentation.WpfUI.Common;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseWheelEventArgs = Ab4d.StandardPresentation.MouseWheelEventArgs;

namespace Ab4d.StandardPresentation.WpfUI
{
    public class WpfWindow : IPresentationControl
    {
        private D3DHost _d3dHost;

        private System.Windows.Window _wpfWindow;

        private Action _renderCallback;
        private Border _rootBorder;

        private DispatcherTimer _frameTimer;
        private bool _isRenderingEventSubscribed;
        private TimeSpan _lastRenderTime;

        public float DpiScaleX { get; private set; } = 1.0f;
        public float DpiScaleY { get; private set; } = 1.0f;

        public float Left => (float)(_wpfWindow.Left * DpiScaleX);
        public float Top  => (float)(_wpfWindow.Top  * DpiScaleY);
               
        public float Width => (float)(_wpfWindow.ActualWidth * DpiScaleX);
        public float Height => (float)(_wpfWindow.ActualHeight * DpiScaleX);
        

        public string Title
        {
            get => _wpfWindow.Title;
            set => _wpfWindow.Title = value;
        }

        public bool IsMinimized { get; private set; }

        public IntPtr WindowHandle { get; private set; }

        public System.Windows.Window Window => _wpfWindow;


        public event EventHandler Loaded;

        public event EventHandler Closing;
        public event EventHandler Closed;

        public event SizeChangeEventHandler SizeChanged;


        public event Ab4d.StandardPresentation.MouseButtonEventHandler MouseDown;
        public event Ab4d.StandardPresentation.MouseButtonEventHandler MouseUp;
        public event Ab4d.StandardPresentation.MouseMoveEventHandler MouseMove;
        public event Ab4d.StandardPresentation.MouseWheelEventHandler MouseWheel;


        public WpfWindow(int width, int height, string name = "")
        {
            _wpfWindow        = new System.Windows.Window();
            _wpfWindow.Width  = width;
            _wpfWindow.Height = height;
            _wpfWindow.Title  = name ?? "";


            _wpfWindow.Loaded += delegate (object sender, RoutedEventArgs args)
            {
                SaveActualDpiScaleValues();
            };

            _wpfWindow.Activated += delegate (object sender, EventArgs args)
            {
                SaveActualDpiScaleValues();
            };

            _wpfWindow.StateChanged += delegate (object sender, EventArgs args)
            {
                if (IsMinimized)
                {
                    if (_wpfWindow.WindowState != WindowState.Minimized)
                    {
                        IsMinimized = false;
                        OnSizeChanged();

                        //if (_renderCallback != null)
                        //    _renderCallback();

                        SubscribeCompositionTargetRendering();
                    }
                }
                else
                {
                    if (_wpfWindow.WindowState == WindowState.Minimized)
                    {
                        IsMinimized = true;
                        OnSizeChanged();

                        UnSubscribeCompositionTargetRendering();
                    }
                }
            };

            _wpfWindow.Closing += delegate (object sender, CancelEventArgs args)
            {
                OnClosing();
            };

            _wpfWindow.Closed += delegate (object sender, EventArgs args)
            {
                OnClosed();
            };



            // We set RootBorder's Background to Transparent so that we get mouse events on DXSceneView - without that (with setting Background to null) the mouse events in DirectXOverlay does not work
            // The real background color (from BackgroundColor property) will be used to clear the back buffer so the back buffer will show that color
            // Background will be rendered when clearing back buffer with BackgroundColor
            _rootBorder            = new Border();
            _rootBorder.Background = Brushes.Aqua;

            _rootBorder.MouseDown += delegate (object sender, System.Windows.Input.MouseButtonEventArgs args)
            {
                OnMouseDown(args);
            };

            _rootBorder.MouseUp += delegate (object sender, System.Windows.Input.MouseButtonEventArgs args)
            {
                OnMouseUp(args);
            };

            _rootBorder.MouseMove += delegate (object sender, System.Windows.Input.MouseEventArgs args)
            {
                OnMouseMove(args);
            };

            _rootBorder.MouseWheel += delegate (object sender, System.Windows.Input.MouseWheelEventArgs args)
            {
                OnMouseWheel(args);
            };


            RecreateD3DHost();

            _wpfWindow.Content = _rootBorder;
        }

        public void RecreateD3DHost()
        {
            _d3dHost = new D3DHost();
            _d3dHost.HorizontalAlignment = HorizontalAlignment.Stretch;
            _d3dHost.VerticalAlignment   = VerticalAlignment.Stretch;

            // We need Handle to the D3DHost before we can initialize DirectX
            // Therefore we subscribe to HandleCreated and create the DirectX in the event handler
            _d3dHost.HandleCreated += OnHandleCreated;

            // It looks like we do not need to render in rendering event (as long as we are subscribed to WPF rendering)
            _d3dHost.Painting     += OnPainting;
            _d3dHost.SizeChanging += OnSizeChanging;

            _d3dHost.Loaded += D3dHostOnLoaded;


            _rootBorder.Child = _d3dHost;
        }

        public void Show()
        {
            _wpfWindow.Show();
        }

        public void Close()
        {
            _wpfWindow.Close();
        }

        private void D3dHostOnLoaded(object sender, RoutedEventArgs e)
        {
            SaveActualDpiScaleValues(); // After the window is loaded, we can read the dpi settings
            OnLoaded();
        }

        private void OnHandleCreated(object sender, HandleCreatedEventArgs e)
        {
            WindowHandle = e.Handle; // Store the handle

            // Do not call OnLoaded here because the actual surface is not yet ready - we need to wait for the D3DHost.Loaded event
        }


        private void OnPainting(object sender, EventArgs e)
        {
            // Called when D3DHost get WM_PAINT event
            // Render the scene now
            //RenderScene(forceRender: true, forceUpdate: false);

            if (!IsMinimized && _renderCallback != null)
                _renderCallback();
        }

        private void OnSizeChanging(object sender, EventArgs e)
        {
            // This method is called when D3DHost is resized
            // If we do not render in this method (and wait until CompositionTarget.Rendering event to render the scene because of SizeChanged notification, the screen is flickering very much.
            // Note that this method is called several times (I think in most cases 3 times) when the window is resized.
            // I tried to store the rendered size so the Render method would be called only once, but this does not works - nothing is shown.
            // So it looks like we will have to live with slow resizing.

            // It is also important to Resize here, because here we have the correct Window size
            // If we would resize in ProcessDXSceneSizeChanged, we would have problems on Windows 10 on discrete GPU (see comments in ProcessDXSceneSizeChanged)
            //var d3dSize = _d3dHost.ClientWindowSize;
            //int newWidth = d3dSize.Width;
            //int newHeight = d3dSize.Height;

            OnSizeChanged();
        }



        public void SetSize(int width, int height)
        {
            _wpfWindow.Width  = width;
            _wpfWindow.Height = height;
        }

        public void StartRenderLoop(Action renderCallback)
        {
            if (renderCallback == null)
                throw new ArgumentNullException(nameof(renderCallback));

            if (_renderCallback != null)
                throw new Exception("RenderLoop already started for the WinFormsWindow");

            _renderCallback = renderCallback;

            // _frameTimer is used to call InvalidateVisual after each 15 milliseconds
            // This ensures that when using D3DHost we have CompositionTarget.Rendering called at 60 FPS
            // Without this when only the 3D scene is changing (for example when only camera is animating) and not other WPF element is changed, then WPF reduces the frame rate to 30 FPS
            // This than also reduces the camera changes rate to only 30 per frame.
            // With calling InvalidateVisual we do not add any noticeable overhead and ensure that CompositionTarget.Rendering stays at 60 FPS
            // _frameTimer is Started in RenderScene method
            _frameTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(15), DispatcherPriority.Background, delegate
            {
                _frameTimer.Stop();
                _d3dHost.InvalidateVisual();
            }, _wpfWindow.Dispatcher);

            SubscribeCompositionTargetRendering();

            if (Application.Current == null)
            {
                var application = new System.Windows.Application();
                application.MainWindow = _wpfWindow;
                application.Run();
            }
        }

        public void GetMouseState(out float x, out float y, out PointerButtons pressedButtons)
        {
            var mousePosition = Mouse.GetPosition(_wpfWindow);
            x = (float)mousePosition.X;
            y = (float)mousePosition.Y;

            pressedButtons = PointerButtons.None;

            if (Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                pressedButtons |= PointerButtons.Left;

            if (Mouse.MiddleButton == System.Windows.Input.MouseButtonState.Pressed)
                pressedButtons |= PointerButtons.Middle;

            if (Mouse.RightButton == System.Windows.Input.MouseButtonState.Pressed)
                pressedButtons |= PointerButtons.Right;

            if (Mouse.XButton1 == System.Windows.Input.MouseButtonState.Pressed)
                pressedButtons |= PointerButtons.XButton1;

            if (Mouse.XButton2 == System.Windows.Input.MouseButtonState.Pressed)
                pressedButtons |= PointerButtons.XButton2;
        }

        public ulong CreateVulkanSurface(IntPtr vulkanInstance)
        {
            return 0; // Do not create a surface here - use WindowHandle to create the surface in the calling class
        }

        public bool IsMouseCaptureSupported => true;

        public void CaptureMouse()
        {
            _rootBorder.CaptureMouse();
        }

        public void ReleaseMouseCapture()
        {
            _rootBorder.ReleaseMouseCapture();
        }

        public bool IsKeyPressed(string keyName)
        {
            return false;
        }

        public KeyboardModifiers GetKeyboardModifiers()
        {
            var modifiers = KeyboardModifiers.None;

            if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0)
                modifiers |= KeyboardModifiers.ShiftKey;

            if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
                modifiers |= KeyboardModifiers.ControlKey;

            if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) != 0)
                modifiers |= KeyboardModifiers.AltKey;

            if ((Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Windows) != 0)
                modifiers |= KeyboardModifiers.SuperKey;

            return modifiers;
        }


        private void SubscribeCompositionTargetRendering()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(_wpfWindow))
                return;

            if (!_isRenderingEventSubscribed)
            {
                CompositionTarget.Rendering += this.CompositionTargetRendering;
                _isRenderingEventSubscribed = true;
            }
        }

        private void UnSubscribeCompositionTargetRendering()
        {
            if (_isRenderingEventSubscribed)
            {
                CompositionTarget.Rendering -= this.CompositionTargetRendering;
                _isRenderingEventSubscribed = false;
            }
        }

        private void CompositionTargetRendering(object sender, EventArgs e)
        {
            var args = (System.Windows.Media.RenderingEventArgs)e;

            // It's possible for Rendering to call back twice in the same frame 
            // so only render when we haven't already rendered in this frame.
            if (_lastRenderTime == args.RenderingTime)
                return;

            //try
            //{
                //RenderScene();
                if (_renderCallback != null)
                    _renderCallback();

                _frameTimer.Start(); // call this.InvalidateVisual() after 15ms to ensure CompositionTarget.Rendering stays at 60 FPS (see InitializeD3DHost for more detailed comment)
            //}
            //catch (Exception ex)
            //{
            //    try
            //    {
            //        Ab4d.Vulkan.Log.FatalException(null, "Unhandled exception in RenderScene: " + ex.Message, ex);
            //    }
            //    catch
            //    { }
            //}

            _lastRenderTime = args.RenderingTime;
        }

        private void SaveActualDpiScaleValues()
        {
            // We can get the system DPI scale from 
            // PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11 and M22
            var presentationSource = PresentationSource.FromVisual(_wpfWindow);

            if (presentationSource != null && presentationSource.CompositionTarget != null)
            {
                DpiScaleX = (float)presentationSource.CompositionTarget.TransformToDevice.M11;
                DpiScaleY = (float)presentationSource.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                DpiScaleX = 1.0f;
                DpiScaleY = 1.0f;
            }
        }


        protected void OnLoaded()
        {
            Loaded?.Invoke(this, null);
        }

        protected void OnClosing()
        {
            UnSubscribeCompositionTargetRendering();

            Closing?.Invoke(this, null);
        }

        protected void OnClosed()
        {
            Closed?.Invoke(this, null);
        }

        protected void OnSizeChanged()
        {
            SizeChanged?.Invoke(this, new SizeChangeEventArgs((float)_wpfWindow.Width, (float)_wpfWindow.Height));
        }


        protected void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            MouseDown?.Invoke(this, new Ab4d.StandardPresentation.MouseButtonEventArgs(ConvertMouseButton(e.ChangedButton)));
        }

        protected void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            MouseUp?.Invoke(this, new Ab4d.StandardPresentation.MouseButtonEventArgs(ConvertMouseButton(e.ChangedButton)));
        }

        protected void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            var position = e.GetPosition(_rootBorder);
            MouseMove?.Invoke(this, new Ab4d.StandardPresentation.MouseMoveEventArgs((float)position.X, (float)position.Y));
        }

        protected void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
        {
            MouseWheel?.Invoke(this, new MouseWheelEventArgs(0, e.Delta));
        }

        private PointerButtons ConvertMouseButton(System.Windows.Input.MouseButton buttons)
        {
            switch (buttons)
            {
                case System.Windows.Input.MouseButton.Left:
                    return PointerButtons.Left;
                case System.Windows.Input.MouseButton.Middle:
                    return PointerButtons.Middle;
                case System.Windows.Input.MouseButton.Right:
                    return PointerButtons.Right;
                case System.Windows.Input.MouseButton.XButton1:
                    return PointerButtons.XButton1;
                case System.Windows.Input.MouseButton.XButton2:
                    return PointerButtons.XButton2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
            }
        }
        
        public void Dispose()
        {
            UnSubscribeCompositionTargetRendering();

            _d3dHost?.Dispose();
            _d3dHost = null;
        }
    }
}