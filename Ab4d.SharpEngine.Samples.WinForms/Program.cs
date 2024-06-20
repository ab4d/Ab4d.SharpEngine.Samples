namespace Ab4d.SharpEngine.PrivateSamples.WinForms
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); // Similar to PerMonitor, but enables child window DPI change notification, improved scaling of comctl32 controls, and dialog scaling.
            
            Application.Run(new SamplesForm());

            // Uncomment to run RenderFormSample:
            //using (var game = new RenderFormSample())
            //    game.Run();
        }
    }
}