using System;
using Ab4d.SharpEngine.Common;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AvaloniaTest
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // The following is a sample global exception handler that can be used 
            // to get system info (with details about graphics card and drivers)
            // in case of exception in SharpEngine.
            // You can use similar code to improve your error reporting data.
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
            {
                if (e.ExceptionObject is SharpEngineException)
                {
                    // Here we just show a MessageBox with some exception info.
                    // In a real application it is recommended to report or store full exception and system info (fullSystemInfo)
                    System.Diagnostics.Debug.WriteLine($"Unhandled {e.ExceptionObject.GetType().Name} occurred while running the sample:\r\n{((Exception)e.ExceptionObject).Message}\r\n\r\nIf this is not expected, please report that to support@ab4d.com.");
                }
            };

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new MainWindow();

            base.OnFrameworkInitializationCompleted();
        }
    }
}
