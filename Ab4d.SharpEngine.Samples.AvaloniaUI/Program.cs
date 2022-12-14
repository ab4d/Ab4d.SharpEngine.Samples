using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;

namespace AvaloniaTest
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.

        // !!! IMPORTANT !!!
        // When SharpEngine is used with Avalonia, then Avalonia needs to be initialized
        // by setting UseWgl to true in Win32PlatformOptions - this will try to use native OpenGL on Windows.
        // If this is not done, then shared texture cannot be used and then WritableBitmap will be used.
        // This is much slower because in this case the rendered image is copied from GPU to main memory 
        // into the WritableBitmap and then it is copied back to GPU to show the rendering.

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new Win32PlatformOptions { UseWgl = true }) // Use native OpenGL on Windows - this is required for using shared texture with SharpEngine
                .LogToTrace();
    }
}
