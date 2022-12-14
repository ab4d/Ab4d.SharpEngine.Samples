using System;

namespace Ab4d.SharpEngine.Samples.CrossPlatform
{
    class Program
    {
#if WPF
        [STAThread]
#endif
        static void Main(string[] args)
        {
            var sharpEngineCrossPlatformSamplesRunner = new SharpEngineCrossPlatformSamplesRunner();
            sharpEngineCrossPlatformSamplesRunner.Run();
        }
    }
}