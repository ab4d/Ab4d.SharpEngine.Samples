using System;
using System.Runtime.InteropServices;
using System.Security;

namespace WpfUI.Common
{
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        [System.Security.SuppressUnmanagedCodeSecurity] // This makes this call much faster
        internal static extern IntPtr CreateWindowEx(int dwExStyle,
                                                      string lpszClassName,
                                                      string lpszWindowName,
                                                      int style,
                                                      int x, int y,
                                                      uint width, uint height,
                                                      IntPtr hwndParent,
                                                      IntPtr hMenu,
                                                      IntPtr hInst,
                                                      IntPtr pvParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        internal static extern bool DestroyWindow(IntPtr hwnd);

        //[DllImport("d3d9.dll")]
        //[System.Security.SuppressUnmanagedCodeSecurity]
        //public static extern int Direct3DCreate9Ex(int sdkVersion, out ComInterface.IDirect3D9Ex directX);

        [DllImport("user32.dll", SetLastError = false)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = false)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern int GetUpdateRect(IntPtr hwnd, IntPtr lpRect, int erase);

        [DllImport("user32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern IntPtr BeginPaint(IntPtr hwnd, out NativeStructs.PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern bool EndPaint(IntPtr hWnd, [In] ref NativeStructs.PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static extern bool GetClientRect(IntPtr hWnd, out NativeStructs.RECT lpRect);
    }
}