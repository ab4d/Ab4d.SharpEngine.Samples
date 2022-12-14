using System;
using Ab4d.SharpEngine.Common;

namespace Ab4d
{
    public interface IPresentationControl : IDisposable
    {
        float Left { get; }
        float Top { get; }
        float Width { get; }
        float Height { get; }
        float DpiScaleX { get; }
        float DpiScaleY { get; }
        string Title { get; set; }
        bool IsMinimized { get; } // Note that when this value is changed, the SizeChanged event is called
        IntPtr WindowHandle { get; }
        event EventHandler Loaded;
        event EventHandler Closing;
        event EventHandler Closed;
        event SizeChangeEventHandler SizeChanged;
        event MouseButtonEventHandler MouseDown;
        event MouseButtonEventHandler MouseUp;
        event MouseMoveEventHandler MouseMove;
        event MouseWheelEventHandler MouseWheel;
        void SetSize(int width, int height);
        void Show();
        void Close();
        void StartRenderLoop(Action renderCallback);
        void GetMouseState(out float x, out float y, out MouseButtons pressedButtons);
        ulong CreateVulkanSurface(IntPtr vulkanInstance); // When non zero value is returned then the returned ulong surfaceHandle is used to create the surface (new VkSurfaceKHR(surfaceHandle))

        bool IsMouseCaptureSupported { get; }
        void CaptureMouse();
        void ReleaseMouseCapture();

        bool IsKeyPressed(string keyName);
        KeyboardModifiers GetKeyboardModifiers();
    }
}