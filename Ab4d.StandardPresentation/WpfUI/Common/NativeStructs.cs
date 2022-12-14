using System;
using System.Runtime.InteropServices;

namespace WpfUI.Common
{
    // These structs are taken from the DirectX SDK
    // In the SDK unsigned int (uint in c#) is used, however, we're only going
    // to use normal ints so we are CLR compliant.
    internal static class NativeStructs
    {
        internal const int
            WS_CHILD = 0x40000000,
            WS_VISIBLE = 0x10000000,
            LBS_NOTIFY = 0x00000001,
            HOST_ID = 0x00000002,
            LISTBOX_ID = 0x00000001,
            WS_VSCROLL = 0x00200000,
            WS_BORDER = 0x00800000,
            WM_SIZE = 0x0005,
            WM_PAINT = 0x000F,
            WM_ERASEBKND = 0x0014;

        [StructLayout(LayoutKind.Sequential)]
        public sealed class D3DPRESENT_PARAMETERS
        {
            public int BackBufferWidth;
            public int BackBufferHeight;
            public int BackBufferFormat;
            public int BackBufferCount;
            public int MultiSampleType;
            public int MultiSampleQuality;
            public int SwapEffect;
            public IntPtr hDeviceWindow;
            public int Windowed;
            public int EnableAutoDepthStencil;
            public int AutoDepthStencilFormat;
            public int Flags;
            public int FullScreen_RefreshRateInHz;
            public int PresentationInterval;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
    }
}
