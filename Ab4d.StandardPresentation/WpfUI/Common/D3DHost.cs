using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Ab4d.SharpEngine.Utilities;

namespace WpfUI.Common
{
    // See a great article about problems of using HwndHost in WPF
    // http://blogs.msdn.com/b/dwayneneed/archive/2013/02/26/mitigating-airspace-issues-in-wpf-applications.aspx

    /// <summary>
    /// D3DHost is a control that is used to show the 3D scene with when the <see cref="Ab4d.SharpEngine.Common.PresentationTypes.DirectXOverlay"/> is used for the <see cref="Ab3d.DirectX.Controls.DXView.PresentationType"/> property.
    /// The control is derived from System.Windows.Interop.HwndHost and provides a windows handle based area that can be used to display the 3D scene with using the DirectX SwapChain.
    /// The area occupied with D3DHost cannot be used by any other WPF element (this is possible when using <see cref="Ab3d.DirectX.Controls.DXImage"/> control.
    /// </summary>
    internal class D3DHost : HwndHost
    {
        private static readonly string LogArea = typeof(D3DHost).FullName!;

        /// <summary>
        /// Gets or sets a static Boolean that specifies if rendering is not bound to WPF's frame rate but is rendering as many frames as possible (when set to true).
        /// </summary>
        public static bool RenderAsManyFramesAsPossible = false; // When this is set to false, the WM_PAINT message does not call BeginPaint and EndPaint and this initiates as many render calls as possible (you also need to make the scene dirty - for example with rotating the camera on each OnRenderScene and not only on WPF Rendering event)


        private IntPtr _hwndParent;

        private bool _isWindowCreated;
        private HandleRef _windowHandle;

        private bool _isBuildWindowCoreCalled;

        /// <summary>
        /// Gets a boolean that specifies if the created window is initially painted
        /// </summary>
        public bool IsInitiallyPainted { get; private set; }

        /// <summary>
        /// Gets the width of the created window.
        /// </summary>
        public int ClientWindowWidth { get; private set; }

        /// <summary>
        /// Gets the height of the created window.
        /// </summary>
        public int ClientWindowHeight { get; private set; }


        /// <summary>
        /// Called when Window handle is created automatically inside BuildWindowCore - when Window handle is created manually with calling CreateWindow method, this event is not called.
        /// </summary>
        public event HandleCreatedEventHandler HandleCreated;

        /// <summary>
        /// Called when WM_PAINT message is passed to D3DHost WndProc
        /// </summary>
        public event EventHandler Painting;
        
        /// <summary>
        /// Called when WM_SIZE message is passed to D3DHost WndProc
        /// </summary>
        public event EventHandler SizeChanging;

        /// <summary>
        /// Constructor
        /// </summary>
        public D3DHost()
        {
            this.Loaded += delegate(object sender, RoutedEventArgs args)
            {
                Log.Info?.Write(LogArea, "D3DHost.Loaded");

                if (IsInitiallyPainted)
                {
                    // UH: Loaded event happened after the initial WM_Paint windows message was received
                    // This is not unusual - might happen when WPF's UI thread is busy and in this case the Window's message pump is faster and sends WM_PAINT message before WPF's Load event
                    // It far it looks like it does not lead to any problems.
                    Log.Trace?.Write(LogArea, "IsInitiallyPainted is true in D3DHost.Loaded");

                    // In this case we set the IsInitiallyPainted false,
                    // but this then lead to not updated D3DHost reagion if WM_PAINT was send before Loaded event.
                    // Therefore we must no reset the IsInitiallyPainted
                    // IsInitiallyPainted = false;
                }
            };

#if FULL_LOGGING
            this.Unloaded += delegate(object sender, RoutedEventArgs args)
            {
                My.Log.Info?.Write(LogArea, "D3DHost.Unloaded");
            };
#endif
        }

        private IntPtr CreateWindow(IntPtr hwndParent, int width, int height)
        {
            if (_windowHandle.Handle != IntPtr.Zero)
                throw new Exception("Cannot call CreateWindow before the previously created Window is destroyed");

#if FULL_LOGGING
            if (My.Log.IsEnabled)
                My.Log.Info?.Write(LogArea, "D3DHost.CreateWindow - size: {0} x {1}", width, height);
#endif

            _hwndParent = hwndParent;

            // Using HwndHost: MSDN: http://msdn.microsoft.com/en-us/library/aa970061.aspx
            // CreateWindowEx: MSDN: http://msdn.microsoft.com/en-us/library/windows/desktop/ms632680%28v=vs.85%29.aspx

            // We use system class (see: http://msdn.microsoft.com/en-us/library/windows/desktop/ms633574(v=vs.85).aspx#system)

            IntPtr hwndHost = NativeMethods.CreateWindowEx(0, // extended style
                                                           "static",  // Class name - we do not register our own class but reuse a System class: http://msdn.microsoft.com/en-us/library/windows/desktop/ms633574%28v=vs.85%29.aspx#system
                                                           "", // Title
                                                           NativeStructs.WS_CHILD | NativeStructs.WS_VISIBLE,
                                                           0, 0, // x, y
                                                           (uint)width, (uint)height,
                                                           hwndParent,
                                                           IntPtr.Zero, // no menu
                                                           IntPtr.Zero,
                                                           IntPtr.Zero);

            _windowHandle = new HandleRef(this, hwndHost);

            ClientWindowWidth  = width;
            ClientWindowHeight = height;

            _isWindowCreated = true; // we need to use this bool because we cannot set _windowHandle to null or set _windowHandle.Handle to IntPtr.Zero

            return hwndHost;
        }

        // See: http://msdn.microsoft.com/en-us/library/ms752055.aspx

        /// <summary>
        /// BuildWindowCore
        /// </summary>
        /// <param name="hwndParent">hwndParent</param>
        /// <returns>HandleRef</returns>
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            // Usually the window is created in BuildWindowCore
            // But to simplify the code (and awoid setting the D3DHost as ParentBorder child to call BuildWindowCore) we can call CreateWindow to get window handle immediatelly

            Log.Trace?.Write(LogArea, "D3DHost.BuildWindowCore");

            IsInitiallyPainted = false;

            if (!_isWindowCreated)
            {
                // CreateWindow was not called manually - so we need to call it now. This will also set the _windowHandle
                int width, height;
                GetClientAreaSize(this, out width, out height);

                CreateWindow(hwndParent.Handle, width, height);

                if (HandleCreated != null)
                    HandleCreated(this, new HandleCreatedEventArgs(_windowHandle.Handle));
            }
            else if (hwndParent.Handle != _hwndParent)
            {
                throw new Exception("Cannot create BuildWindowCore because hwndParent in BuildWindowCore is different than in call to CreateWindow");
            }

            _isBuildWindowCoreCalled = true;

            return _windowHandle;
        }

        //[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static Size GetClientAreaSize(FrameworkElement frameworkElement)
        {
            Size size;

            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (frameworkElement.ActualWidth != 0 && !Double.IsNaN(frameworkElement.ActualWidth))
            {
                size = new Size(frameworkElement.ActualWidth, frameworkElement.ActualHeight);
            }
            else if (frameworkElement.Width != 0 && !Double.IsNaN(frameworkElement.Width) && frameworkElement.Height != 0 && !Double.IsNaN(frameworkElement.Height))
            {
                size = new Size(frameworkElement.Width, frameworkElement.Height);
            }
            else
            {
                size = Size.Empty;
            }
            // ReSharper restore CompareOfFloatsByEqualityOperator

            return size;
        }

        // Returns true if size is valid
        //[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static bool GetClientAreaSize(FrameworkElement frameworkElement, out int width, out int height)
        {
            Size size = GetClientAreaSize(frameworkElement);

            if (size.IsEmpty)
            {
                width = height = 0;
                return false;
            }
            else
            {
                width = Convert.ToInt32(size.Width);
                height = Convert.ToInt32(size.Height);
            }

            return (width > 0 && height > 0);
        }

        /// <summary>
        /// WndProc
        /// </summary>
        /// <param name="hwnd">hwnd</param>
        /// <param name="msg">msg</param>
        /// <param name="wParam">wParam</param>
        /// <param name="lParam">lParam</param>
        /// <param name="handled">handled</param>
        /// <returns>IntPtr</returns>
        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeStructs.WM_PAINT)
            {
                // WM_PAINT message: http://msdn.microsoft.com/en-us/library/windows/desktop/dd145213(v=vs.85).aspx
                // An application should call the GetUpdateRect function to determine whether the window has an update region. If GetUpdateRect returns zero, the application should not call the BeginPaint and EndPaint functions.

                // From MSDN for RECT parameter (second):
                // An application can set this parameter to NULL to determine whether an update region exists for the window. 
                // If this parameter is NULL, GetUpdateRect returns nonzero if an update region exists, and zero if one does not.
                // This provides a simple and efficient means of determining whether a WM_PAINT message resulted from an invalid area.

                int updateRegionExist = NativeMethods.GetUpdateRect(hwnd, IntPtr.Zero, 0);

                if (updateRegionExist != 0)
                {
                    Log.Trace?.Write(LogArea, "D3DHost.WndProc WM_PAINT message");
                    handled = true;
                }
                else
                {
                    Log.Trace?.Write(LogArea, "D3DHost.WndProc WM_PAINT message without update region");
                    handled = false;
                }

                IsInitiallyPainted = true;

                if (RenderAsManyFramesAsPossible)
                {
                    // No BeginPaint and EndPaint - this will create an infinite loop (for empty scene can called up to 100000x per second)
                    OnPainting();
                }
                else
                {
                    // From: https://social.msdn.microsoft.com/Forums/vstudio/en-US/f4be8840-406a-45eb-9d26-b14af23f069d/windowhost-resize-and-subsequent-redraw-difficulties?forum=wpf
                    // VERY VERY IMPORTANT!!!!!
                    // When WE OURSELVES are handling a WM_PAINT message WE MUST wrap any
                    // painting inside of calls to BeginPaint() and EndPaint() as seen here.
                    // If we DO NOT when we set 'handled = true', underlying OS calls see that
                    // the update region has NOT changed and will instantly send another WM_PAINT
                    // message (which turns into an infinite loop of WM_PAINT messages being sent
                    // to this WndProc). By simply performing the calls seen here we tell the
                    // OS that we are performing all the painting necessary for our window and
                    // to not send any more WM_PAINT messages for the given UI interaction...
                    NativeStructs.PAINTSTRUCT pStruct;

                    // Tell OS that we are beginning our own painting
                    NativeMethods.BeginPaint(hwnd, out pStruct);

                    OnPainting();

                    // Tell OS that we have ended our painting
                    NativeMethods.EndPaint(hwnd, ref pStruct);
                }
            }
            else if (msg == NativeStructs.WM_ERASEBKND)
            {
                // Prevent erasing background (waste of CPU because we will render to whole area in the following WM_PAINT event - this can also reduce flicering when screen is refreshed after erase and before paint
                handled = true;
            }
            else if (msg == NativeStructs.WM_SIZE)
            {
                Log.Trace?.Write(LogArea, "D3DHost.WndProc WM_SIZE message");

                NativeStructs.RECT rect;
                NativeMethods.GetClientRect(_windowHandle.Handle, out rect);

                ClientWindowWidth  = rect.Right;
                ClientWindowHeight = rect.Bottom;

                Log.Trace?.Write(LogArea, "D3DHost.ClientWindowSize: {0} x {1}", ClientWindowWidth, ClientWindowHeight);

                OnSizeChanging();

                handled = true;
            }

            if (handled)
                return IntPtr.Zero; 

            // Call base message pump
            return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        }
        
        /// <summary>
        /// DestroyWindowCore
        /// </summary>
        /// <param name="hwnd">hwnd</param>
        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            Log.Trace?.Write(LogArea, "D3DHost.DestroyWindowCore");

            if (_isWindowCreated)
            {
                Debug.Assert(hwnd.Handle == _windowHandle.Handle, "DestroyWindowCore called with different hwnd");

                NativeMethods.DestroyWindow(hwnd.Handle);

                _isWindowCreated = false;
            }

            _isBuildWindowCoreCalled = false;
        }

        // As stated in (http://blogs.msdn.com/b/dwayneneed/archive/2013/02/26/mitigating-airspace-issues-in-wpf-applications.aspx - just before "Mitigating clipping issued" in the middle of the article)
        // we need to also render in OnRender - this is event that is triggered by WPF composition engine and not Windows composition engine as in WinProc WM_PAINT
        // This prevents trails because when dragging around the mouse events have bigger priority than rendering and therefore they can delay the rendering (instructed by OS) and leaving the area behind black

        /// <summary>
        /// OnRender
        /// </summary>
        /// <param name="drawingContext">drawingContext</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            Log.Trace?.Write(LogArea, "D3DHost.OnRender");

            if (IsInitiallyPainted) // To prevent duplicate rendering on first frame, we wait until Paint Wnd message come
                OnPainting();
        }

        /// <summary>
        /// OnPainting
        /// </summary>
        protected void OnPainting()
        {
            if (Painting != null)
                Painting(this, null);
        }

        /// <summary>
        /// OnSizeChanging
        /// </summary>
        protected void OnSizeChanging()
        {
            if (SizeChanging != null)
                SizeChanging(this, null);
        }

        /// <summary>
        /// Returns a handle to used window.
        /// </summary>
        /// <returns>a handle to used window</returns>
        public IntPtr GetHWnd()
        {
            return _windowHandle.Handle;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">disposing</param>
        protected override void Dispose(bool disposing)
        {
            Log.Trace?.Write(LogArea, "D3DHost.Dispose");

            try
            {
                if (disposing)
                {
                    // HACK HACK HACK: 
                    // When Window handle is created with CreateWindow but BuildWindowCore was not called (for example if subsequent device or resources creation failed)
                    // than Dispose will not automatically call DestroyWindowCore (probably becasue it expect that the window handle is created only in BuildWindowCore)
                    // In this case we need to manually call the DestroyWindowCore
                    if (_isWindowCreated && !_isBuildWindowCoreCalled)
                        DestroyWindowCore(_windowHandle);
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
