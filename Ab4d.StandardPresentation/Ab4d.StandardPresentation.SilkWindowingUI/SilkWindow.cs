using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Ab4d;
using Ab4d.SharpEngine.Common;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using MouseButtons = Ab4d.SharpEngine.Common.MouseButtons;

namespace Ab4d.StandardPresentation.SilkWindowingUI
{
    public class SilkWindow : IPresentationControl
    {
        public static bool IsAnyPlatformSupported => Window.Platforms.Count > 0;

        public static bool IsViewOnlyPlatform
        {
            get
            {
                // Prevented "Not implemented exception in Silk.Net.Glfw"
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return false;

                return Window.IsViewOnly;
            }
        }

        public static bool PrioritizeSdl()
        {
            bool isSupported = IsPlatformSupported("Sdl");

            if (isSupported)
                Window.PrioritizeSdl();

            return isSupported;
        }
        
        public static bool PrioritizeGlfw()
        {
            bool isSupported = IsPlatformSupported("Glfw");

            if (isSupported)
                Window.PrioritizeGlfw();

            return isSupported;
        }


        private static bool IsPlatformSupported(string platformName)
        {
            return Window.Platforms.Any(p => p.GetType().FullName!.IndexOf(platformName, StringComparison.OrdinalIgnoreCase) != -1); // SdlPlatform type is internal so we cannot check for type (checking for type name is also used in PrioritizeSdl)
        }


        private IWindow _window;

        private Action _renderCallback;
        private IInputContext _inputContext;

        private Vector2 _lastMousePosition;
        private MouseButtons _lastPressedButtons;

        private MouseButtons[] _allMouseButtons = new MouseButtons[] { MouseButtons.Left, MouseButtons.Middle, MouseButtons.Right, MouseButtons.XButton1, MouseButtons.XButton2 };

        private string[] _keyNames;
        private Key[] _keyValues;

        private float _xPos;
        private float _yPos;

        private float _width;
        private float _height;

        private Silk.NET.SDL.Sdl _sdl;


        public float Left => _xPos;
        public float Top => _yPos;

        public float Width => _width;
        public float Height => _height;

        public float DpiScaleX { get; private set; } = 1.0f;
        public float DpiScaleY { get; private set; } = 1.0f;

        public string Title
        {
            get => _window.Title;
            set => _window.Title = value;
        }


        public bool IsMinimized { get; private set; }

        public IntPtr WindowHandle => IntPtr.Zero;

        public event EventHandler Loaded;
        public event EventHandler Closing;
        public event EventHandler Closed;
        public event SizeChangeEventHandler SizeChanged;
        public event MouseButtonEventHandler MouseDown;
        public event MouseButtonEventHandler MouseUp;
        public event MouseMoveEventHandler MouseMove;
        public event MouseWheelEventHandler MouseWheel;



        public SilkWindow(int width, int height, string name = "")
        {
            var windowOptions = WindowOptions.DefaultVulkan;
            windowOptions.Size = new Vector2D<int>(width, height);
            windowOptions.Title = name;
            
            _window = Window.Create(windowOptions);
            
            if (_window.GetType().Name.Contains("Sdl", StringComparison.OrdinalIgnoreCase))
                _sdl = Silk.NET.Windowing.Sdl.SdlWindowing.GetExistingApi(_window);

            _window.Load += delegate()
            {
                _xPos = _window.Position.X;
                _yPos = _window.Position.Y;

                _width  = _window.Size.X;
                _height = _window.Size.Y;

                if (_inputContext == null)
                    _inputContext = _window.CreateInput();
                
                CheckInput(triggerEvents: false); // Just update current values

                OnLoaded();
            };

            _window.Move += newPositions =>
            {
                _xPos = newPositions.X;
                _yPos = newPositions.Y;
            };

            _window.StateChanged += state =>
            {
                IsMinimized = state == WindowState.Minimized;
            };

            _window.Resize += newSize =>
            {
                _width = newSize.X;
                _height = newSize.Y;
            };

            _window.FramebufferResize += newSize =>
            {
                _width  = newSize.X;
                _height = newSize.Y;

                OnSizeChanged(newSize.X, newSize.Y);
            };

            _window.Render += delta =>
            {
                CheckInput(triggerEvents: true);

                if (_renderCallback != null)
                    _renderCallback();
            };

            _window.Closing += () =>
            {
                OnClosing();
            };
        }
        

        private void CheckInput(bool triggerEvents)
        {
            if (_inputContext == null)
                return;

            if (_inputContext.Mice.Count > 0)
            {
                var pressedButtons = GetPressedButtons(_inputContext.Mice[0]);

                bool isMouseButtonChanged = pressedButtons != _lastPressedButtons;
                var previousPressedButtons = _lastPressedButtons;
                _lastPressedButtons = pressedButtons;


                var  mousePosition = _inputContext.Mice[0].Position;
                bool isMouseMoved  = mousePosition != _lastMousePosition;

                _lastMousePosition = mousePosition; // Set _lastPressedButtons before calling any mouse events so that if user calls StartRenderLoop he will have a new state


                float mouseWheelChange;

                if (_inputContext.Mice[0].ScrollWheels.Count > 0)
                {
                    mouseWheelChange = _inputContext.Mice[0].ScrollWheels[0].Y;
                }
                else
                {
                    mouseWheelChange = 0;
                }

                if (triggerEvents)
                {
                    if (isMouseButtonChanged)
                    {
                        for (var i = 0; i < _allMouseButtons.Length; i++)
                        {
                            var oneButton = _allMouseButtons[i];

                            if (((pressedButtons & oneButton) != 0) && ((previousPressedButtons & oneButton) == 0)) // Now pressed but before it was not
                                OnMouseDown(oneButton);

                            if (((pressedButtons & oneButton) == 0) && ((previousPressedButtons & oneButton) != 0)) // Now released but before it was pressed
                                OnMouseUp(oneButton);
                        }
                    }

                    if (isMouseMoved)
                        OnMouseMove(mousePosition.X, mousePosition.Y);

                    if (mouseWheelChange != 0)
                        OnMouseWheel(0, mouseWheelChange);
                }
            }
        }

        private MouseButtons GetPressedButtons(IMouse mouse)
        {
            MouseButtons pressedButtons = MouseButtons.None;

            if (_inputContext.Mice[0].IsButtonPressed(Silk.NET.Input.MouseButton.Left))
                pressedButtons |= MouseButtons.Left;
            
            if (_inputContext.Mice[0].IsButtonPressed(Silk.NET.Input.MouseButton.Right))
                pressedButtons |= MouseButtons.Right;
            
            if (_inputContext.Mice[0].IsButtonPressed(Silk.NET.Input.MouseButton.Middle))
                pressedButtons |= MouseButtons.Middle; 
            
            if (_inputContext.Mice[0].IsButtonPressed(Silk.NET.Input.MouseButton.Button4))
                pressedButtons |= MouseButtons.XButton1;
            
            if (_inputContext.Mice[0].IsButtonPressed(Silk.NET.Input.MouseButton.Button5))
                pressedButtons |= MouseButtons.XButton2;

            return pressedButtons;
        }

        public void SetSize(int width, int height)
        {
            _window.Size = new Vector2D<int>(width, height);
        }

        public void Show()
        {
            _window.Initialize(); // For safety the window should be initialized before querying the VkSurface
        }

        public void Close()
        {
            _window.Close();
        }

        public void StartRenderLoop(Action renderCallback)
        {
            _renderCallback = renderCallback;
            _window.Run();
        }

        public void GetMouseState(out float x, out float y, out MouseButtons pressedButtons)
        {
            x = _lastMousePosition.X;
            y = _lastMousePosition.Y;
            pressedButtons = _lastPressedButtons;
        }

        public unsafe ulong CreateVulkanSurface(IntPtr vulkanInstance)
        {
            if (_window.VkSurface != null)
            {
                var vkHandle                = new VkHandle(vulkanInstance);
                var vkNonDispatchableHandle = _window.VkSurface.Create(vkHandle, (byte*)null);

                return vkNonDispatchableHandle.Handle;
            }

            return 0;
        }

        public bool IsMouseCaptureSupported => _sdl != null;

        public void CaptureMouse()
        {
            if (_sdl == null)
                throw new NotSupportedException();

            _sdl.CaptureMouse(Silk.NET.SDL.SdlBool.True);
        }

        public void ReleaseMouseCapture()
        {
            if (_sdl == null)
                throw new NotSupportedException();

            _sdl.CaptureMouse(Silk.NET.SDL.SdlBool.False);
        }


        public bool IsKeyPressed(string keyName)
        {
            if (_inputContext == null || _inputContext.Keyboards.Count == 0)
                return false;

            var keyValue = GetKeyValue(keyName);

            return _inputContext.Keyboards[0].IsKeyPressed(keyValue);
        }

        private Key GetKeyValue(string keyName)
        {
            if (_keyNames == null)
            {
                _keyNames  = Enum.GetNames<Silk.NET.Input.Key>();
                _keyValues = new Key[_keyNames.Length];

                var keyValues = Enum.GetValues<Silk.NET.Input.Key>();
                
                for (var i = 0; i < _keyNames.Length; i++)
                    _keyValues[i] = keyValues[i];
            }

            for (var i = 0; i < _keyNames.Length; i++)
            {
                if (_keyNames[i].Equals(keyName, StringComparison.OrdinalIgnoreCase))
                    return _keyValues[i];
            }

            return Key.Unknown;
        }

        public KeyboardModifiers GetKeyboardModifiers()
        {
            if (_inputContext == null || _inputContext.Keyboards.Count == 0)
                return KeyboardModifiers.None;

            var modifiers = KeyboardModifiers.None;

            if (_inputContext.Keyboards[0].IsKeyPressed(Key.ShiftLeft) || _inputContext.Keyboards[0].IsKeyPressed(Key.ShiftRight))
                modifiers |= KeyboardModifiers.ShiftKey;
            
            if (_inputContext.Keyboards[0].IsKeyPressed(Key.ControlLeft) || _inputContext.Keyboards[0].IsKeyPressed(Key.ControlRight))
                modifiers |= KeyboardModifiers.ControlKey;

            if (_inputContext.Keyboards[0].IsKeyPressed(Key.AltLeft) || _inputContext.Keyboards[0].IsKeyPressed(Key.AltRight))
                modifiers |= KeyboardModifiers.AltKey;
            
            if (_inputContext.Keyboards[0].IsKeyPressed(Key.SuperLeft) || _inputContext.Keyboards[0].IsKeyPressed(Key.SuperRight))
                modifiers |= KeyboardModifiers.SuperKey;

            return modifiers;
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

        protected void OnMouseDown(MouseButtons button)
        {
            MouseDown?.Invoke(this, new Ab4d.StandardPresentation.MouseButtonEventArgs(button));
        }

        protected void OnMouseUp(MouseButtons button)
        {
            MouseUp?.Invoke(this, new Ab4d.StandardPresentation.MouseButtonEventArgs(button));
        }

        protected void OnMouseMove(double x, double y)
        {
            MouseMove?.Invoke(this, new Ab4d.StandardPresentation.MouseMoveEventArgs((float)x, (float)y));
        }

        protected void OnMouseWheel(double dx, double dy)
        {
            MouseWheel?.Invoke(this, new MouseWheelEventArgs((float)dx, (float)dy));
        }




        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
