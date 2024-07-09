using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
                // This requires Ab4d.SharpEngine.AvaloniaUI v2.0.8957-rc1 that depends on Avalonia v11.1.rc2
                // When an official version of Avalonia v11.1 will be released, then an official Ab4d.SharpEngine.AvaloniaUI will be also released.
#if VULKAN_BAKCEND
                .With(new Win32PlatformOptions
                {
                    RenderingMode = new[]
                    {
                        Win32RenderingMode.Vulkan
                    }
                })
                .With(new Avalonia.Vulkan.VulkanOptions()
                {
                    VulkanInstanceCreationOptions = new Avalonia.Vulkan.VulkanInstanceCreationOptions()
                    {
                        UseDebug = true
                    }
                })
#endif
                .LogToTrace();
    }
}
