using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using Avalonia.Vulkan;

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
                // With Avalonia v11.1+ it is possible to use Vulkan backend - in this case the whole application uses Vulkan (also to render Avalonia UI).
#if VULKAN_BACKEND
                .With(new Win32PlatformOptions
                {
                    RenderingMode = new[]
                    {
                        Win32RenderingMode.Vulkan
                    }
                })
                .With(new X11PlatformOptions
                {
                    RenderingMode = new[]
                    {
                        X11RenderingMode.Vulkan
                    }
                })
                .With(new Avalonia.Vulkan.VulkanOptions()
                {
                    VulkanDeviceCreationOptions = new VulkanDeviceCreationOptions()
                    {
                        // When the following option is set, then on a laptop with multiple GPUs
                        // Avalonia and SharpEngine will use a discrete GPU even if the "High performance"
                        // is not selected for this app in the Windows Graphics Settings.
                        //
                        // It is still recommended to use "High performance" to prevent potential
                        // copying of the window's content to the primary graphics card.
                        //
                        // Comment this setting if you want to use integrated GPU and improve battery life.
                        PreferDiscreteGpu = true
                    },
                    //VulkanInstanceCreationOptions = new Avalonia.Vulkan.VulkanInstanceCreationOptions()
                    //{
                    //    UseDebug = true // Use Vulkan debug layers for Avalonia UI operations
                    //}
                })
#endif
                .LogToTrace();
    }
}
