using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Ab4d;
using Ab4d.SharpEngine.Common;
using GLFW;
using Microsoft.Win32.SafeHandles;
using SizeChangeEventArgs = Ab4d.SizeChangeEventArgs;

namespace GlfwUI
{
    public class GlfwWindow : IPresentationControl
    {
        private Window _glfwWindow;

        private string _name;

        //private double _lastMouseX, _lastMouseY;

        //private bool _isWindowClosed;

        private float _xPos;
        private float _yPos;

        private float _width;
        private float _height;
        
        private string[] _keyNames;
        private Keys[] _keyValues;

        private SizeCallback _sizeCallback;
        private WindowCallback _closeCallback;
        private PositionCallback _windowPositionCallback;
        private IconifyCallback _windowIconifyCallback;
        private MouseButtonCallback _mouseButtonCallback;
        private MouseCallback _cursorPositionCallback;
        private MouseCallback _scrollCallback;
        private KeyCallback _keyCallback;
        //private string _lastReportText;

        private ModifierKeys _currentModifierKeys;

        public float Left => _xPos;
        public float Top => _yPos;
               
        public float Width => _width;
        public float Height => _height;

        public float DpiScaleX { get; private set; } = 1.0f;
        public float DpiScaleY { get; private set; } = 1.0f;

        private string _title;
        public string Title {
            get => _title;
            set
            {
                Glfw.SetWindowTitle(_glfwWindow, value);
                _title = value;
            }
        }

        public bool IsClosed { get; private set; }

        public bool IsMinimized { get; private set; }

        private bool _isWindowHandleRead;
        private IntPtr _windowHandle;

        public IntPtr WindowHandle
        {
            get
            {
                if (!_isWindowHandleRead && _glfwWindow != Window.None)
                {
                    if (System.OperatingSystem.IsWindows())
                        _windowHandle = GetWin32Window(_glfwWindow);
                    else
                        _windowHandle = IntPtr.Zero;
                    
                    _isWindowHandleRead = true;
                }

                return _windowHandle;
            }
        }


        public event EventHandler Loaded;

        public event EventHandler Closing;
        public event EventHandler Closed;

        public event Ab4d.SizeChangeEventHandler SizeChanged;


        public event Ab4d.MouseButtonEventHandler MouseDown;
        public event Ab4d.MouseButtonEventHandler MouseUp;
        public event Ab4d.MouseMoveEventHandler MouseMove;
        public event Ab4d.MouseWheelEventHandler MouseWheel;

        //public int FrameRateLimit = 60; // Set to 0 to disable it

        public Action<string> LogCallback;

        public Version GetLibraryVersion => Glfw.Version;



        [DllImport("glfw", EntryPoint = "glfwCreateWindowSurface", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CreateWindowSurface(IntPtr vulkan, IntPtr window, IntPtr allocator, out ulong surface);
        
        [DllImport("glfw", EntryPoint = "glfwGetWin32Window", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetWin32Window(IntPtr window);

        [DllImport("user32.dll", EntryPoint = "SetCapture")]
        private static extern IntPtr Win32SetCapture(IntPtr hWnd);
        
        [DllImport("user32.dll", EntryPoint = "ReleaseCapture")]
        private static extern bool Win32ReleaseCapture();


        public GlfwWindow(int width, int height, string name = "")
        {
            bool isInitialized = Glfw.Init();

            if (!isInitialized)
                throw new System.Exception("Cannot initialize GLFW");

            Glfw.SetErrorCallback(ErrorHandler);

            _width  = width;
            _height = height;
            _name   = name;

            Glfw.WindowHint(Hint.ClientApi,    ClientApi.None);  // This is required when using Vulkan
            Glfw.WindowHint(Hint.Visible,      Constants.False); // Do not show the window when it is created - show it in the Show method
            //Glfw.WindowHint(Hint.RefreshRate,  60);
            //Glfw.WindowHint(Hint.Doublebuffer, Constants.False);
            

            _glfwWindow = Glfw.CreateWindow((int)_width, (int)_height, _name, Monitor.None, Window.None);
            _title = name;

            // IMPORTANT:
            // We need to store delegates to local fields, otherwise the delegate is collected by GC (because it is not used in any managed object - only in native method call)

            _sizeCallback = SizeCallback;
            Glfw.SetWindowSizeCallback(_glfwWindow, _sizeCallback);

            _closeCallback = CloseCallback;
            Glfw.SetCloseCallback(_glfwWindow, _closeCallback);

            _windowPositionCallback = WindowPositionCallback;
            Glfw.SetWindowPositionCallback(_glfwWindow, _windowPositionCallback);

            _windowIconifyCallback = WindowIconifyCallback;
            Glfw.SetWindowIconifyCallback(_glfwWindow, _windowIconifyCallback);

            _mouseButtonCallback = MouseButtonCallback;
            Glfw.SetMouseButtonCallback(_glfwWindow, _mouseButtonCallback);

            _cursorPositionCallback = CursorPositionCallback;
            Glfw.SetCursorPositionCallback(_glfwWindow, _cursorPositionCallback);

            _scrollCallback = ScrollCallback;
            Glfw.SetScrollCallback(_glfwWindow, _scrollCallback);

            _keyCallback = KeyCallback;
            Glfw.SetKeyCallback(_glfwWindow, _keyCallback);
        }

        private void Callback(IntPtr window, bool focusing)
        {
            
        }

        //~GlfwWindow()
        //{

        //}

        public bool IsMouseCaptureSupported => System.OperatingSystem.IsWindows(); // Supported only on Windows

        public void CaptureMouse()
        {
            if (!IsMouseCaptureSupported)
                throw new NotSupportedException();

            if (WindowHandle != IntPtr.Zero)
                Win32SetCapture(WindowHandle);
        }

        public void ReleaseMouseCapture()
        {
            if (!IsMouseCaptureSupported)
                throw new NotSupportedException();

            Win32ReleaseCapture();
        }

        public bool IsKeyPressed(string keyName)
        {
            var keyValue = GetKeyValue(keyName);

            if (keyValue == Keys.Unknown)
            {
#if DEBUG
                throw new System.Exception("Invalid key name");
#else
                return false;
#endif
            }

            var inputState = Glfw.GetKey(_glfwWindow, keyValue);
            return (inputState == InputState.Press);
        }

        public KeyboardModifiers GetKeyboardModifiers()
        {
            var modifiers = KeyboardModifiers.None;

            if ((_currentModifierKeys & GLFW.ModifierKeys.Shift) != 0)
                modifiers |= KeyboardModifiers.ShiftKey;

            if ((_currentModifierKeys & GLFW.ModifierKeys.Control) != 0)
                modifiers |= KeyboardModifiers.ControlKey;

            if ((_currentModifierKeys & GLFW.ModifierKeys.Alt) != 0)
                modifiers |= KeyboardModifiers.AltKey;

            if ((_currentModifierKeys & GLFW.ModifierKeys.Super) != 0)
                modifiers |= KeyboardModifiers.SuperKey;

            return modifiers;
        }

        private Keys GetKeyValue(string keyName)
        {
            if (_keyNames == null)
            {
                _keyNames  = Enum.GetNames<Keys>();
                _keyValues = new Keys[_keyNames.Length];

                var keyValues = Enum.GetValues<Keys>();

                for (var i = 0; i < _keyNames.Length; i++)
                    _keyValues[i] = keyValues[i];
            }

            for (var i = 0; i < _keyNames.Length; i++)
            {
                if (_keyNames[i].Equals(keyName, StringComparison.OrdinalIgnoreCase))
                    return _keyValues[i];
            }

            return Keys.Unknown;
        }

        public void SetSize(int width, int height)
        {
            if (_glfwWindow != Window.None)
                Glfw.SetWindowSize(_glfwWindow, width, height);

            _width  = width;
            _height = height;
        }

        public void Show()
        {
            Glfw.ShowWindow(_glfwWindow);

            // Get size of window's frame
            Glfw.GetWindowFrameSize(_glfwWindow, out var left, out var top, out var right, out var bottom);

            // Read actual position and size
            Glfw.GetWindowPosition(_glfwWindow, out var x, out var y);
            _xPos = (float)(x - left);
            _yPos = (float)(y - top);

            Glfw.GetWindowSize(_glfwWindow, out var width, out var height);
            //_width  = (float) (width + left + right); // Hm for Win 10 it is better not to include window border in size
            //_height = (float) (height + top + bottom);

            _width  = (float) width;
            _height = (float) height;

            OnLoaded();

            Log("GLFW window shown");
        }

        public void Close()
        {
            OnClosing();

            Glfw.DestroyWindow(_glfwWindow);

            IsClosed = true;
            OnClosed();
        }

        public void StartRenderLoop(Action renderCallback)
        {
            if (renderCallback == null)
                throw new ArgumentNullException(nameof(renderCallback));


            while (ProcessEvents())
            {
                //LimitFrameRate();
                renderCallback();
            }
        }

        // Returns false if the window is closing
        public bool ProcessEvents()
        {
            Glfw.PollEvents();
            return !Glfw.WindowShouldClose(_glfwWindow) && !IsClosed;
        }


        public void GetMouseState(out float x, out float y, out MouseButtons pressedButtons)
        {
            if (_glfwWindow == Window.None)
                throw new InvalidOperationException("Cannot call GetMouseState before the Show is called");


            Glfw.GetCursorPosition(_glfwWindow, out double xPos, out double yPos);
            x = (float)xPos;
            y = (float)yPos;


            pressedButtons = MouseButtons.None;

            var buttonState = Glfw.GetMouseButton(_glfwWindow, GLFW.MouseButton.Left);
            if (buttonState == InputState.Press || buttonState == InputState.Repeat)
                pressedButtons |= MouseButtons.Left;

            buttonState = Glfw.GetMouseButton(_glfwWindow, GLFW.MouseButton.Middle);
            if (buttonState == InputState.Press || buttonState == InputState.Repeat)
                pressedButtons |= MouseButtons.Middle;

            buttonState = Glfw.GetMouseButton(_glfwWindow, GLFW.MouseButton.Right);
            if (buttonState == InputState.Press || buttonState == InputState.Repeat)
                pressedButtons |= MouseButtons.Right;

            buttonState = Glfw.GetMouseButton(_glfwWindow, GLFW.MouseButton.Button4);
            if (buttonState == InputState.Press || buttonState == InputState.Repeat)
                pressedButtons |= MouseButtons.XButton1;

            buttonState = Glfw.GetMouseButton(_glfwWindow, GLFW.MouseButton.Button5);
            if (buttonState == InputState.Press || buttonState == InputState.Repeat)
                pressedButtons |= MouseButtons.XButton2;
        }

        public ulong CreateVulkanSurface(IntPtr vulkanInstance)
        {
            var windowPtr = (IntPtr)_glfwWindow; // Use implicit cast operator to cast to IntPtr (read private handle)

            // NOTE: The .net wrapper wrongly expects IntPtr as an out parameters (instead of ulong)
            //int result = GLFW.Vulkan.CreateWindowSurface(vulkanInstance, windowPtr, IntPtr.Zero, out IntPtr surfacePtr);
            int result = CreateWindowSurface(vulkanInstance, windowPtr, IntPtr.Zero, out ulong surfaceHandle);

            if (result >= 0)
                return surfaceHandle;

            return 0;
        }

        public string[] GetRequiredVulkanInstanceExtensions()
        {
            return GLFW.Vulkan.GetRequiredInstanceExtensions();
        }
        
        public bool GetRequiredVulkanInstanceExtensions(IntPtr instance, IntPtr device, uint family)
        {
            return GLFW.Vulkan.GetPhysicalDevicePresentationSupport(instance, device, family);
        }

        public bool IsVulkanSupported()
        {
            GLFW.Vulkan.GetRequiredInstanceExtensions();

            return GLFW.Vulkan.IsSupported;
        }

        private void CursorPositionCallback(IntPtr window, double x, double y)
        {
            OnMouseMove(x, y);
        }

        private void MouseButtonCallback(IntPtr window, GLFW.MouseButton button, GLFW.InputState state, GLFW.ModifierKeys modifiers)
        {
            if (state == InputState.Press)
                OnMouseDown(button);
            else
                OnMouseUp(button);
        }

        private void KeyCallback(IntPtr window, Keys key, int scanCode, InputState state, ModifierKeys mods)
        {
            _currentModifierKeys = mods;
        }

        private void ScrollCallback(IntPtr window, double x, double y)
        {
            OnMouseWheel(x, y);
        }

        private void WindowPositionCallback(IntPtr window, double x, double y)
        {
            Glfw.GetWindowFrameSize(_glfwWindow, out var left, out var top, out var right, out var bottom);

            if (x == 0 && y == 0)
            {
                Glfw.GetWindowPosition(_glfwWindow, out var x2, out var y2);
                x = x2;
                y = y2;
            }

            _xPos = (float)(x - left);
            _yPos = (float)(y - top);
        }
        
        private void WindowIconifyCallback(IntPtr window, bool focusing)
        {
            IsMinimized = focusing;
        }


        private void CloseCallback(IntPtr window)
        {
            Close();
        }

        private void SizeCallback(IntPtr window, int width, int height)
        {
            _width = (float)width;
            _height = (float)height;

            OnSizeChanged(width, height);
        }

        protected void OnLoaded()
        {
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        protected void OnClosing()
        {
            Closing?.Invoke(this, EventArgs.Empty);
        }

        protected void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        protected void OnSizeChanged(int width, int height)
        {
            SizeChanged?.Invoke(this, new SizeChangeEventArgs(width, height));
        }

        protected void OnMouseDown(GLFW.MouseButton button)
        {
            MouseDown?.Invoke(this, new Ab4d.MouseButtonEventArgs(ConvertMouseButton(button)));
        }

        protected void OnMouseUp(GLFW.MouseButton button)
        {
            MouseUp?.Invoke(this, new Ab4d.MouseButtonEventArgs(ConvertMouseButton(button)));
        }

        protected void OnMouseMove(double x, double y)
        {
            MouseMove?.Invoke(this, new Ab4d.MouseMoveEventArgs((float)x, (float)y));
        }

        protected void OnMouseWheel(double dx, double dy)
        {
            MouseWheel?.Invoke(this, new MouseWheelEventArgs((float)dx, (float)dy));
        }

        private MouseButtons ConvertMouseButton(GLFW.MouseButton button)
        {
            switch (button)
            {
                case GLFW.MouseButton.Left:
                    return MouseButtons.Left;
                case GLFW.MouseButton.Middle:
                    return MouseButtons.Middle;
                case GLFW.MouseButton.Right:
                    return MouseButtons.Right;
                case GLFW.MouseButton.Button4:
                    return MouseButtons.XButton1;
                case GLFW.MouseButton.Button5:
                    return MouseButtons.XButton2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, null);
            }
        }

        //private Ab4d.MouseEventArgs CreateMouseEventArgsFromWinForms(System.Windows.Forms.MouseEventArgs e)
        //{
        //    var pressedButtons = ConvertMouseButtons(e.Button);
        //    return new Ab4d.MouseEventArgs(e.X, e.Y, pressedButtons);
        //}

        public void CloseWindow()
        {
            //if (_isWindowClosed)
            //    return;

            //Glfw.close
        }

        private void ErrorHandler(ErrorCode code, IntPtr message)
        {
            var messageText = StringFromNativeUtf8(message);
            Log($"GLFW error ({code}): " + messageText);
        }

        // https://stackoverflow.com/questions/10773440/conversion-in-net-native-utf-8-managed-string
        private static string StringFromNativeUtf8(IntPtr nativeUtf8)
        {
            int len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        private void Log(string message)
        {
            if (LogCallback != null)
                LogCallback(message);
        }

        // LimitFrameRate is commented because WaitForVSync is a much better way to control frame rate

        //private bool ReportFrameTimes = true;

        //private StringBuilder _reportStringBuilder;
        //private List<double> _frameTimes;
        //private int _lastFrameSecond;
        //private int _frameNumber;

        //private Stopwatch _frameLimiterStopwatch;
        //private long _lastRenderTimeInTicks;

        //private DateTime _lastRenderTime;

        //private void LimitFrameRate()
        //{
        //    if (FrameRateLimit == 0)
        //        return;

        //    if (_frameLimiterStopwatch == null)
        //    {
        //        // We use Stopwatch instead of DateTine.Now because later does not use high precision timer
        //        _frameLimiterStopwatch = new Stopwatch(); 
        //        _frameLimiterStopwatch.Start();

        //        _lastRenderTimeInTicks = 0;
        //        return; // Do not limit the first frame
        //    }

        //    long startWaitTimeInTicks = _frameLimiterStopwatch.ElapsedTicks;

        //    long ticksPerFrame               = 10000000L / FrameRateLimit;
        //    long timeToStartRenderingInTicks = _lastRenderTimeInTicks + ticksPerFrame;
        //    long renderTimeInTicks           = startWaitTimeInTicks - _lastRenderTimeInTicks;


        //    if (ReportFrameTimes)
        //    {
        //        int currentSecond = DateTime.Now.Second;

        //        if (_frameTimes == null)
        //        {
        //            _frameTimes          = new List<double>(FrameRateLimit);
        //            _reportStringBuilder = new StringBuilder();

        //            _lastFrameSecond = currentSecond;
        //            _frameNumber     = 0;
        //        }
        //        else
        //        {
        //            if (_lastFrameSecond != currentSecond)
        //            {
        //                _lastReportText = _reportStringBuilder.ToString();
        //                _reportStringBuilder.Clear();

        //                _lastFrameSecond = currentSecond;
        //                _frameNumber     = 0;
        //            }
        //        }
        //    }

        //    int  sleepsCount = 0;
        //    int  yieldsCount = 0;
        //    long timeAfterSleep = 0;

        //    if (timeToStartRenderingInTicks > startWaitTimeInTicks)
        //    {
        //        // First use loop with Sleep(1) that takes 2 ms less then the required wait time
        //        long sleepEndTimeInTicks = timeToStartRenderingInTicks - 20000;
        //        while (_frameLimiterStopwatch.ElapsedTicks < sleepEndTimeInTicks)
        //        {
        //            System.Threading.Thread.Sleep(1); // Sleep(1) usually takes less than 1ms (but in some rare cases it can take also much more; but this is much more precise then Sleep(requiredMs)
        //            sleepsCount++;
        //        }

        //        if (ReportFrameTimes)
        //            timeAfterSleep = _frameLimiterStopwatch.ElapsedTicks;

        //        // For more accuracy we use Thread.Yield
        //        while (_frameLimiterStopwatch.ElapsedTicks < timeToStartRenderingInTicks)
        //        {
        //            System.Threading.Thread.Yield(); // Note that using Yield still shows some CPU  usage, though there is actually no usage
        //            yieldsCount++;
        //        }
        //    }
        //    else
        //    {
        //        timeAfterSleep = startWaitTimeInTicks;
        //    }

        //    long endWaitTimeInTicks = _frameLimiterStopwatch.ElapsedTicks;

        //    if (ReportFrameTimes)
        //    {
        //        long renderAndWaitTimeInTicks = endWaitTimeInTicks - _lastRenderTimeInTicks;
        //        long timeToWaitInTicks        = (timeToStartRenderingInTicks > startWaitTimeInTicks) ? timeToStartRenderingInTicks - startWaitTimeInTicks : 0;

        //        _reportStringBuilder.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
        //            "{0} {1:0.00} + {2:0.00}: (({3} * 1ms = {4:0.00}) + ({5} * yield = {6:0.00}) = {7:0.00}) = {8:0.00} ({9:0.00})\r\n", 
        //            _frameNumber, renderTimeInTicks / 10000.0, timeToWaitInTicks / 10000.0,
        //            sleepsCount, (timeAfterSleep - startWaitTimeInTicks) / 10000.0, 
        //            yieldsCount, (endWaitTimeInTicks - timeAfterSleep) / 10000.0,
        //            (endWaitTimeInTicks - startWaitTimeInTicks) / 10000.0,
        //            renderAndWaitTimeInTicks / 10000.0,
        //            (renderAndWaitTimeInTicks - ticksPerFrame) / 10000.0);

        //        _frameNumber++;
        //    }


        //    _lastRenderTimeInTicks = endWaitTimeInTicks;
        //}


        public void Dispose()
        {
            //if (!_isWindowClosed)

            //_renderForm?.Dispose();
            //_renderForm = null;
        }
    }
}