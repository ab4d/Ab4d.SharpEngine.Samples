using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System.Reflection;
using System;
using System.IO;
using Microsoft.UI.Windowing;

namespace Ab4d.SharpEngine.Samples.WinUI.Common;

public class WinUiUtils
{
    public static void SetWindowIcon(Window window, string fileName)
    {
        string fullFileNamer = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        if (!System.IO.File.Exists(fullFileNamer))
            throw new FileNotFoundException("Icon file not found: " + fileName, fullFileNamer);

        // From: https://github.com/microsoft/microsoft-ui-xaml/issues/4056
        IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.SetIcon(fullFileNamer);
    }

    // Based on https://github.com/microsoft/WindowsAppSDK/discussions/1816
    public static void ChangeCursor(FrameworkElement frameworkElement, InputCursor cursor)
    {
        // Changing cursor before the control is loaded will crash the app (https://github.com/MicrosoftDocs/winui-api/issues/112)
        if (!frameworkElement.IsLoaded)
        {
            frameworkElement.Loaded += (sender, args) => ChangeCursor(frameworkElement, cursor);
            return;
        }

        Type type = typeof(UIElement);
        var propertyInfo = type.GetProperty("ProtectedCursor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance);

        if (propertyInfo != null)
            propertyInfo.SetValue(frameworkElement, cursor);
    }
}