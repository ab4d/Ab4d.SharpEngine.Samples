using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ab4d;
using Ab4d.SharpEngine.Common;
using Ab4d.StandardPresentation.WinFormsUI.SharpDX.Windows.Desktop;
using MouseButtons = System.Windows.Forms.MouseButtons;

namespace Ab4d.StandardPresentation.WinFormsUI
{
    public class WinFormsWindow : IPresentationControl
    {
        private RenderForm _renderForm;

        private bool _isRenderLoopStarted;

        public float DpiScaleX { get; private set; } = 1.0f;
        public float DpiScaleY { get; private set; } = 1.0f;

        public float Left => _renderForm.Left;
        public float Top => _renderForm.Top;
               
        public float Width => _renderForm.ClientSize.Width;
        public float Height => _renderForm.ClientSize.Height;

        public string Title
        {
            get => _renderForm.Text;
            set => _renderForm.Text = value;
        }

        public bool IsMinimized { get; private set; }

        public IntPtr WindowHandle => _renderForm.Handle;


        public event EventHandler Loaded;
        
        public event EventHandler Closing;
        public event EventHandler Closed;
        
        public event SizeChangeEventHandler SizeChanged;


        public event MouseButtonEventHandler MouseDown;
        public event MouseButtonEventHandler MouseUp;
        public event MouseMoveEventHandler MouseMove;
        public event MouseWheelEventHandler MouseWheel;
        

        public WinFormsWindow(int width, int height, string name = "")
        {
            _renderForm = new RenderForm(name ?? "")
            {
                Width = width,
                Height = height,
                AllowUserResizing = true
            };

            _renderForm.SizeChanged += delegate(object sender, EventArgs args)
            {
                IsMinimized = _renderForm.WindowState == FormWindowState.Minimized;
                OnSizeChanged(_renderForm.Width, _renderForm.Height);
            };

            // UserResized is called only after the resize is completed
            //_renderForm.UserResized += delegate (object sender, EventArgs args)
            //{
            //    OnSizeChanged(_renderForm.Width, _renderForm.Height);
            //};

            _renderForm.Load += delegate (object sender, EventArgs args)
            {
                OnLoaded();
            };

            _renderForm.Closing += delegate(object sender, CancelEventArgs args)
            {
                OnClosing();
            };

            _renderForm.Closed += delegate(object sender, EventArgs args)
            {
                OnClosed();
            };


            _renderForm.MouseDown += delegate(object sender, System.Windows.Forms.MouseEventArgs args)
            {
                OnMouseDown(args);
            };
            
            _renderForm.MouseUp += delegate(object sender, System.Windows.Forms.MouseEventArgs args)
            {
                OnMouseUp(args);
            };
            
            _renderForm.MouseMove += delegate(object sender, System.Windows.Forms.MouseEventArgs args)
            {
                OnMouseMove(args);
            };
            
            _renderForm.MouseWheel += delegate(object sender, System.Windows.Forms.MouseEventArgs args)
            {
                OnMouseWheel(args);
            };
        }

        public void SetSize(int width, int height)
        {
            _renderForm.ClientSize = new Size(width, height);
        }

        public void Show()
        {
            _renderForm.Show();
        }

        public void Close()
        {
            _renderForm.Close();
        }

        public void StartRenderLoop(Action renderCallback)
        {
            if (renderCallback == null) 
                throw new ArgumentNullException(nameof(renderCallback));

            if (_isRenderLoopStarted)
                throw new Exception("RenderLoop already started for the WinFormsWindow");

            RenderLoop.Run(_renderForm, delegate
            {
                if (!IsMinimized)
                    renderCallback();
            });

            _isRenderLoopStarted = true;
        }

        public void GetMouseState(out float x, out float y, out Ab4d.SharpEngine.Common.PointerButtons pressedButtons)
        {
            var mousePosition = Cursor.Position;

            if (_renderForm != null)
                mousePosition = _renderForm.PointToClient(mousePosition);

            x = mousePosition.X;
            y = mousePosition.Y;

            pressedButtons = ConvertMouseButton(Control.MouseButtons);
        }

        public ulong CreateVulkanSurface(IntPtr vulkanInstance)
        {
            return 0; // Do not create a surface here - use WindowHandle to create the surface in the calling class
        }

        public bool IsMouseCaptureSupported => true;

        public void CaptureMouse()
        {
            _renderForm.Capture = true;
        }

        public void ReleaseMouseCapture()
        {
            _renderForm.Capture = false;
        }

        public bool IsKeyPressed(string keyName)
        {
            return false;
        }

        public KeyboardModifiers GetKeyboardModifiers()
        {
            var modifiers = KeyboardModifiers.None;

            if ((Control.ModifierKeys & Keys.Shift) != 0)
                modifiers |= KeyboardModifiers.ShiftKey;

            if ((Control.ModifierKeys & Keys.Control) != 0)
                modifiers |= KeyboardModifiers.ControlKey;

            if ((Control.ModifierKeys & Keys.Alt) != 0)
                modifiers |= KeyboardModifiers.AltKey;

            if ((Control.ModifierKeys & (Keys.LWin | Keys.RWin)) != 0)
                modifiers |= KeyboardModifiers.SuperKey;

            return modifiers;
        }


        protected void OnLoaded()
        {
            Loaded?.Invoke(this, null);
        }

        protected void OnClosing()
        {
            Closing?.Invoke(this, null);
        }
        
        protected void OnClosed()
        {
            Closed?.Invoke(this, null);
        }

        protected void OnSizeChanged(float width, float height)
        {
            SizeChanged?.Invoke(this, new SizeChangeEventArgs(width, height));
        }


        protected void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            MouseDown?.Invoke(this, new MouseButtonEventArgs(ConvertMouseButton(e.Button)));
        }

        protected void OnMouseUp(System.Windows.Forms.MouseEventArgs e)
        {
            MouseUp?.Invoke(this, new MouseButtonEventArgs(ConvertMouseButton(e.Button)));
        }

        protected void OnMouseMove(System.Windows.Forms.MouseEventArgs e)
        {
            MouseMove?.Invoke(this, new MouseMoveEventArgs(e.X, e.Y));
        }

        protected void OnMouseWheel(System.Windows.Forms.MouseEventArgs e)
        {
            MouseWheel?.Invoke(this, new MouseWheelEventArgs(0, e.Delta));
        }

        private Ab4d.SharpEngine.Common.PointerButtons ConvertMouseButton(System.Windows.Forms.MouseButtons buttons)
        {
            var pressedButtons = Ab4d.SharpEngine.Common.PointerButtons.None;

            if ((buttons & MouseButtons.Left) != 0)
                pressedButtons |= Ab4d.SharpEngine.Common.PointerButtons.Left;

            if ((buttons & MouseButtons.Middle) != 0)
                pressedButtons |= Ab4d.SharpEngine.Common.PointerButtons.Middle;

            if ((buttons & MouseButtons.Right) != 0)
                pressedButtons |= Ab4d.SharpEngine.Common.PointerButtons.Right;

            if ((buttons & MouseButtons.XButton1) != 0)
                pressedButtons |= Ab4d.SharpEngine.Common.PointerButtons.XButton1;

            if ((buttons & MouseButtons.XButton2) != 0)
                pressedButtons |= Ab4d.SharpEngine.Common.PointerButtons.XButton2;

            return pressedButtons;
        }
        
        public void Dispose()
        {
            _renderForm?.Dispose();
            _renderForm = null;
        }
    }
}
