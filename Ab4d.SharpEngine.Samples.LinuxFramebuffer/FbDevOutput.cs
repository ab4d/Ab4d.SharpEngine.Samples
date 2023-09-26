using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Ab4d.SharpEngine.Samples.LinuxFramebuffer;

public class FbDevOutput : OutputDevice
{
    private int _deviceFd = -1;

    private int _framebufferSize;
    private IntPtr _framebufferAddress = new(-1);

    /* Logging */
    private readonly ILogger? _logger;

    public static string? FindFirstDevice()
    {
        var files = Directory.GetFiles("/dev");
        Array.Sort(files);
        foreach(var file in files)
        {
            var match = Regex.Match(file, "fb[0-9]+");
            if (!match.Success)
            {
                continue;
            }

            var fd = Native.LibC.open(file, Native.LibC.O_RDWR | Native.LibC.O_CLOEXEC, 0);
            if (fd == -1)
            {
                continue;
            }

            Native.LibC.close(fd);
            return file;
        }

        return null;
    }

    public FbDevOutput(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<FbDevOutput>();
    }

    public override unsafe void Initialize(string deviceName, int requestedWidth, int requestedHeight)
    {
        /* This backend does not support requested width/height */
        if (requestedWidth != 0 || requestedHeight != 0)
        {
            throw new Exception("FdDev output does not support screen size request.");
        }

        /* Open framebuffer device */
        _deviceFd = Native.LibC.open(deviceName, Native.LibC.O_RDWR | Native.LibC.O_CLOEXEC, 0);
        if (_deviceFd <= 0)
        {
            Cleanup();
            throw new Exception($"Failed to open framebuffer device {deviceName}: {Marshal.GetLastWin32Error()}");
        }

        /* Query the screen information */
        var screenInfo = new Native.FbDev.fb_var_screeninfo();
        if (Native.LibC.ioctl(_deviceFd, Native.FbDev.FBIOGET_VSCREENINFO, &screenInfo) < 0)
        {
            Cleanup();
            throw new Exception($"Failed to query screen info: {Marshal.GetLastWin32Error()}");
        }

        /* Validate framebuffer mode */
        _logger?.LogInformation("Framebuffer depth: {depth} bits per pixel", screenInfo.bits_per_pixel);
        if (screenInfo.bits_per_pixel != 32)
        {
            Cleanup();
            throw new Exception($"Unsupported bit depth: {screenInfo.bits_per_pixel}! Only 32 BPP is supported!");
        }

        if (screenInfo.red.length == 8 && screenInfo.green.length == 8 && screenInfo.blue.length == 8 &&
            (screenInfo.transp.length == 8 || screenInfo.transp.length == 0))
        {
            if (screenInfo.red.offset == 0 && screenInfo.green.offset == 8 && screenInfo.blue.offset == 16 &&
                (screenInfo.transp.offset == 24 || screenInfo.transp.offset == 0))
            {
                _logger?.LogInformation("Framebuffer color mode: RGBA8888");
                RgbaMode = true;
            }
            else if (screenInfo.red.offset == 16 && screenInfo.green.offset == 8 && screenInfo.blue.offset == 0 &&
                     (screenInfo.transp.offset == 24 || screenInfo.transp.offset == 0))
            {
                _logger?.LogInformation("Framebuffer color mode: BGRA8888");
                RgbaMode = false;
            }
            else
            {
                Cleanup();
                throw new Exception(
                    $"Unsupported color mode! Only RGBA8888 and BGRA8888 are supported! Offsets: " +
                    $"R={screenInfo.red.offset} G={screenInfo.green.offset} B={screenInfo.blue.offset} A={screenInfo.transp.offset}");
            }
        }
        else
        {
            Cleanup();
            throw new Exception(
                $"Unsupported bit depth: only RGBA8888 and BGRA8888 are supported! Depths: " +
                $"R={screenInfo.red.length} G={screenInfo.green.length} B={screenInfo.blue.length} A={screenInfo.transp.length}");
        }

        /* Map the framebuffer */
        ScreenWidth = (int)screenInfo.xres;
        ScreenHeight = (int)screenInfo.yres;
        _logger?.LogInformation("Screen dimensions: {width} x {height}", ScreenWidth, ScreenHeight);

        var bytesPerRow = screenInfo.xres * screenInfo.bits_per_pixel / 8;
        _framebufferSize = (int)(screenInfo.yres * bytesPerRow);

        _framebufferAddress = Native.LibC.mmap(
            IntPtr.Zero,
            new IntPtr(_framebufferSize),
            Native.LibC.PROT_READ | Native.LibC.PROT_WRITE,
            Native.LibC.MAP_SHARED,
            _deviceFd,
            IntPtr.Zero);
        if (_framebufferAddress == new IntPtr(-1))
        {
            throw new Exception($"Failed to mmap framebuffer: {Marshal.GetLastWin32Error()}");
        }
    }

    public override unsafe void DisplayImageData(byte *data)
    {
        /* Copy on vsync */
        Native.LibC.ioctl(_deviceFd, Native.FbDev.FBIO_WAITFORVSYNC, null);
        Unsafe.CopyBlock((byte *)_framebufferAddress, data, (uint)_framebufferSize); /* in lieu of memcpy() */
    }

    public override void Cleanup()
    {
        /* Unmap framebuffer */
        if (_framebufferAddress != new IntPtr(-1))
        {
            if (Native.LibC.munmap(_framebufferAddress, new IntPtr(_framebufferSize)) < 0)
            {
                throw new Exception($"Failed to munmap framebuffer: {Marshal.GetLastWin32Error()}");
            }
            _framebufferAddress = new IntPtr(-1);
        }

        /* Close framebuffer device */
        if (_deviceFd >= 0)
        {
            if (Native.LibC.close(_deviceFd) < 0)
            {
                throw new Exception($"$Failed to close framebuffer file descriptor: {Marshal.GetLastWin32Error()}");
            }
            _deviceFd = -1;
        }
    }
}