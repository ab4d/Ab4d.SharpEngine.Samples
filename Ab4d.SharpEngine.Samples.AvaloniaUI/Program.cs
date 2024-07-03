using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Vulkan;
using System;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI
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

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                // With Avalonia v11.1+ it is possible to use Vulkan backend - in this case uncomment the following code
                //.With(new Win32PlatformOptions
                //{
                //    RenderingMode = new[]
                //    {
                //        Win32RenderingMode.Vulkan
                //    }
                //})
                //.With(new VulkanOptions()
                //{
                //    VulkanInstanceCreationOptions = new VulkanInstanceCreationOptions()
                //    {
                //        UseDebug = true
                //    }
                //})
                .LogToTrace();
    }
}
