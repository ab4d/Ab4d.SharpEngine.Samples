namespace Ab4d.SharpEngine.Samples.LinuxFramebuffer;

public abstract class OutputDevice
{
    /// <summary>
    /// Screen width, in pixels.
    /// </summary>
    public int ScreenWidth;

    /// <summary>
    /// Screen height, in pixels.
    /// </summary>
    public int ScreenHeight;

    /// <summary>
    /// If true, display mode is RGBA, otherwise it is BGRA.
    /// </summary>
    public bool RgbaMode;

    public abstract void Initialize(string deviceName, int requestedWidth, int requestedHeight);
    public abstract unsafe void DisplayImageData(byte *data);
    public abstract void Cleanup();
}
