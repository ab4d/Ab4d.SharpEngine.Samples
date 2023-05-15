using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System.Reflection;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;

namespace Ab4d.SharpEngine.Samples.WinUI.Common;

public class WinUiUtils
{
    public static AppWindow GetAppWindow(Window window)
    {
        // From: https://github.com/microsoft/microsoft-ui-xaml/issues/4056
        IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        return appWindow;
    }

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

    public static Windows.Graphics.SizeInt32 GetWindowSize(Window window)
    {
        var appWindow = GetAppWindow(window);
        return appWindow.Size;
    }

    public static void SetWindowSize(Window window, int width, int height)
    {
        var appWindow = GetAppWindow(window);
        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = width, Height = height });
    }
    
    public static void SetWindowClientSize(Window window, int width, int height)
    {
        var appWindow = GetAppWindow(window);

        int widthMargin = appWindow.Size.Width - appWindow.ClientSize.Width;
        int heightMargin = appWindow.Size.Height - appWindow.ClientSize.Height;

        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = width + widthMargin, Height = height + heightMargin });
    }
    
    public static void SetWindowClientHeight(Window window, int height)
    {
        var appWindow = GetAppWindow(window);

        int heightMargin = appWindow.Size.Height - appWindow.ClientSize.Height;

        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = appWindow.Size.Width, Height = height + heightMargin });
    }
    
    public static void SetWindowAlwaysOnTop(Window window)
    {
        var appWindow = GetAppWindow(window);
        appWindow.MoveInZOrderAtTop();
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

    public static async Task<ContentDialogResult> ShowMessageBox(XamlRoot xamlRoot, string message, string? title = null, string primaryButtonText = "OK", string? secondaryButtonText = null)
    {
        var contentDialog = new ContentDialog();

        contentDialog.Title = title;

        contentDialog.PrimaryButtonText = primaryButtonText;
        contentDialog.SecondaryButtonText = secondaryButtonText;

        contentDialog.XamlRoot = xamlRoot;

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

        var textBlock = new TextBlock()
        {
            Text = message,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        Grid.SetRow(textBlock, 0);
        grid.Children.Add(textBlock);

        contentDialog.Content = grid;

        var dialogResult = await contentDialog.ShowAsync();

        return dialogResult;
    }
}