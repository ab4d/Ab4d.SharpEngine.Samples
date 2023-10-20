using System.CommandLine;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Animation;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using Microsoft.Extensions.Logging;
using Logging = Ab4d.SharpEngine.Utilities.Log;

namespace Ab4d.SharpEngine.Samples.LinuxFramebuffer;

class Program
{
    static int Main(string[] args)
    {
        var returnCode = 0;

        /* Prevent attempts at running on unsupported OSes */
        if (!OperatingSystem.IsLinux())
        {
            Console.WriteLine("This sample works only on linux!");
            return -1;
        }

        /* Command-line parser */
        var outputDeviceOption = new Option<FileInfo?>(
            name: "--output-device",
            description: "Output framebuffer device (/dev/fbX or /dev/dri/cardX)."
        );
        var vulkanDeviceIndexOption = new Option<int>(
            name: "--vulkan-device-index",
            description: "Index of Vulkan device used for off-screen rendering."
        );
        var drawCursorOption = new Option<bool>(
            name: "--draw-cursor",
            description: "Draw mouse cursor.",
            getDefaultValue: () => true
        );
        var widthOption = new Option<int>(
            name: "--width",
            description: "Request specified horizontal resolution instead of preferred one."
        );
        var heightOption = new Option<int>(
            name: "--height",
            description: "Request specified vertical resolution instead of preferred one."
        );
        var switchConsoleModeOption = new Option<bool>(
            name: "--switch-console-mode",
            description: "Switch console to graphical mode and disable kernel keyboard processing."
        );

        var rootCommand = new RootCommand("Linux framebuffer demo with off-screen Vulkan renderer.");
        rootCommand.AddGlobalOption(outputDeviceOption);
        rootCommand.AddGlobalOption(vulkanDeviceIndexOption);
        rootCommand.AddGlobalOption(drawCursorOption);
        rootCommand.AddGlobalOption(widthOption);
        rootCommand.AddGlobalOption(heightOption);
        rootCommand.AddGlobalOption(switchConsoleModeOption);

        rootCommand.SetHandler((outputDevice, vulkanDeviceIndex, drawCursor, width, height, switchConsoleMode) =>
        {
            /* Set up logging */
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning)
                    .AddFilter("Ab4d.SharpEngine.Samples.LinuxFramebuffer", LogLevel.Information)
                    .AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                    });
            });
            var logger = loggerFactory.CreateLogger<Program>();

            /* Create program instance and run it */
            try {
                var program = new Program(
                    outputDevice?.FullName,
                    vulkanDeviceIndex,
                    drawCursor,
                    width,
                    height,
                    switchConsoleMode,
                    loggerFactory,
                    logger);
                program.ProgramMain();
                returnCode = 0;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during program execution!");
                returnCode = -1;
            }
        }, outputDeviceOption, vulkanDeviceIndexOption, drawCursorOption, widthOption, heightOption, switchConsoleModeOption);

        rootCommand.Invoke(args);
        return returnCode;
    }

    /* TTY/VT device setting */
    private readonly bool _switchConsoleMode;

    private int _vtFd = -1;
    private bool _kbModeSet;
    private uint _savedKbMode;
    private bool _ttyModeSet;

    /* Keyboard and mouse input */
    private InputDevice? _inputDevice;

    /* Output device */
    private string? _outputDeviceName;

    private OutputDevice? _outputDevice;

    /* SharpEngine renderer (off-screen) */
    private readonly int _vulkanDeviceIndex;

    private VulkanDevice? _vulkanDevice;
    private Scene? _scene;
    private SceneView? _sceneView;

    private TransformationAnimation? _animation;

    /* Mouse cursor */
    private readonly bool _drawCursor;

    private readonly int _requestedWidth;
    private readonly int _requestedHeight;

    /* Main loop */
    private bool _keepRunning = true;

    /* Logging */
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger? _logger;

    private Program(
        string? outputDeviceName = null,
        int vulkanDeviceIndex = 0,
        bool drawCursor = true,
        int requestedWidth = 0,
        int requestedHeight = 0,
        bool switchConsoleMode = false,
        ILoggerFactory? loggerFactory = null,
        ILogger? logger = null)
    {
        _outputDeviceName = outputDeviceName;
        _vulkanDeviceIndex = vulkanDeviceIndex;
        _drawCursor = drawCursor;
        _requestedWidth = requestedWidth;
        _requestedHeight = requestedHeight;
        _switchConsoleMode = switchConsoleMode;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    private void ProgramMain()
    {
        /* If output device is not explicitly given, try to find one */
        if (_outputDeviceName == null)
        {
            _outputDeviceName = DrmFramebufferOutput.FindFirstDevice() ?? FbDevOutput.FindFirstDevice();
            if (_outputDeviceName == null)
            {
                throw new Exception("Failed to auto-detect output device!");
            }
        }

        _logger?.LogInformation("Using output device: {name}", _outputDeviceName);

        /* Setup output device */
        SetupOutputDevice();

        /* Setup Vulkan device for off-screen rendering */
        SetupVulkanDevice();

        /* Setup test scene and view */
        SetupTestSceneAndView();

        /* Allow Ctrl+C to break the main loop - this is useful if program is started remotely via SSH */
        Console.CancelKeyPress += delegate(object? _, ConsoleCancelEventArgs e) {
            _logger?.LogInformation("Ctrl+C detected; stopping!");
            e.Cancel = true;
            _keepRunning = false;
        };

        /* Setup keyboard/mouse input */
        SetupInput();

        /* Put TTY in graphics mode, if necessary */
        if (_switchConsoleMode) {
            _logger?.LogInformation("Switching TTY/VT to graphical mode...");
            try
            {
                SetupVirtualTerminal();
                _logger?.LogInformation("TTY/VT switched to graphical mode!");
            } catch (Exception e)
            {
                _logger?.LogWarning(e, "Failed to switch VT/TTY to graphical mode!");
            }
        }

        /* Run main loop */
        try
        {
            MainLoop();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error during execution of the main loop!");
        }

        /* Clean-up */
        RestoreVirtualTerminal();
        _logger?.LogInformation("Restored TTY/VT to console mode...");

        _outputDevice?.Cleanup();
        _inputDevice?.Cleanup();
    }

    private void SetupInput()
    {
        Debug.Assert(_outputDevice != null, nameof(_outputDevice) + " != null");

        try
        {
            /* Create input handler */
            _inputDevice = new InputDevice(_outputDevice.ScreenWidth, _outputDevice.ScreenHeight);

            /* Register key-press event handler */
            _inputDevice.KeyPressEventHandler += (_, args) =>
            {
                var keyPressArgs = (InputDevice.KeyPressEventArgs)(args);
                if (keyPressArgs.KeyCode == 0x1B)
                {
                    _logger?.LogInformation("ESC key pressed. Exiting.");
                    _keepRunning = false;
                }
            };
        }
        catch (Exception e)
        {
            _logger?.LogWarning(e, "Failed to create input device!");
        }
    }

    private void SetupOutputDevice()
    {
        Debug.Assert(_outputDeviceName != null, nameof(_outputDeviceName) + " != null");

        if (_outputDeviceName.StartsWith("/dev/fb"))
        {
            _logger?.LogInformation("Creating FbDev output device: {name}", _outputDeviceName);
            _outputDevice = new FbDevOutput(_loggerFactory);
            _outputDevice.Initialize(_outputDeviceName, _requestedWidth, _requestedHeight);
        } else if (_outputDeviceName.StartsWith("/dev/dri/card")) {
            _logger?.LogInformation("Creating DRM output device: {name}", _outputDeviceName);
            _outputDevice = new DrmFramebufferOutput(_loggerFactory);
            _outputDevice.Initialize(_outputDeviceName, _requestedWidth, _requestedHeight);
        } else {
            throw new Exception($"Unsupported device name: {_outputDevice}");
        }
    }

    private void SetupVulkanDevice()
    {
        /* Create Vulkan instance */
        var engineCreateOptions = new EngineCreateOptions(
            applicationName: "LinuxFramebufferDemo",
            enableStandardValidation: false);
        var vulkanInstance = new VulkanInstance(engineCreateOptions);
        _logger?.LogInformation("Vulkan instance created. API version: {version}", vulkanInstance.ApiVersion);

        /* Select Vulkan device */
        if (vulkanInstance.AllPhysicalDeviceDetails.Length == 0)
        {
            throw new Exception("No Vulkan device available!");
        }

        _logger?.LogInformation("Available Vulkan devices ({count}):", vulkanInstance.AllPhysicalDeviceDetails.Length);
        for (var i = 0; i < vulkanInstance.AllPhysicalDeviceDetails.Length; i++)
        {
            var deviceDetails = vulkanInstance.AllPhysicalDeviceDetails[i];
            _logger?.LogInformation(" - Device #{number}: {name}", i, deviceDetails.DeviceName);
        }

        if (_vulkanDeviceIndex >= vulkanInstance.AllPhysicalDeviceDetails.Length)
        {
            throw new Exception($"Vulkan device index {_vulkanDeviceIndex} out of range!");
        }

        _logger?.LogInformation("Selected device #{index}: {name}", _vulkanDeviceIndex, vulkanInstance.AllPhysicalDeviceDetails[_vulkanDeviceIndex].DeviceName);

        _vulkanDevice = new VulkanDevice(
            vulkanInstance,
            vulkanInstance.AllPhysicalDeviceDetails[_vulkanDeviceIndex],
            SurfaceKHR.Null,
            vulkanInstance.CreateOptions);
    }

    private void SetupTestSceneAndView()
    {
        Debug.Assert(_vulkanDevice != null, nameof(_vulkanDevice) + " != null");
        Debug.Assert(_outputDevice != null, nameof(_outputDevice) + " != null");

        _scene = new Scene(_vulkanDevice);

        /* Create cube */
        var boxModelNode = new BoxModelNode(
            new Vector3(0, 0, 0),
            new Vector3(25, 25, 25),
            StandardMaterials.Yellow,
            "rotatingBox");

        _scene.RootNode.Add(boxModelNode);

        /* Create scene view with dimensions that match screen (framebuffer) resolution */
        _sceneView = new SceneView(_scene)
        {
            Format = _outputDevice.RgbaMode ? StandardBitmapFormats.Rgba : StandardBitmapFormats.Bgra
        };
        _sceneView.BackgroundColor = Color4.Black;
        _sceneView.Camera = new TargetPositionCamera()
        {
            TargetPosition = new Vector3(0, 0, 0),
            Heading = 0,
            Attitude = -50,
            Distance = 180,
        };

        Debug.Assert(_outputDevice.ScreenWidth != 0, "Invalid screen width!");
        Debug.Assert(_outputDevice.ScreenHeight != 0, "Invalid screen height!");
        _sceneView.Initialize(_outputDevice.ScreenWidth, height: _outputDevice.ScreenHeight);

        /* Animation */
        _animation = AnimationBuilder.CreateTransformationAnimation(_scene, "Test animation");
        _animation.AddTarget(boxModelNode);
        _animation.Set(TransformationAnimatedProperties.RotateZ, propertyValue: 180, 5000);
        _animation.Loop = true;
        _animation.LoopCount = 0; /* Loop forever */

        _animation.Start();
    }

    private unsafe void DisplayImage(GpuBuffer gpuBuffer, int width, int height)
    {
        Debug.Assert(_outputDevice != null, nameof(_outputDevice) + " != null");
        Debug.Assert(width == _outputDevice.ScreenWidth, $"Image width mismatch: {width} vs {_outputDevice.ScreenWidth}");
        Debug.Assert(height == _outputDevice.ScreenHeight, $"Image height mismatch: {height} vs {_outputDevice.ScreenHeight}");

        var bufferPtr = gpuBuffer.GetMappedMemoryPtr();

        try
        {
            /* Draw mouse cursor */
            if (_drawCursor && _inputDevice != null)
            {
                var (r, g, b) = _outputDevice.RgbaMode ? (255, 0, 0) : (0, 0, 255); /* Red */
                try
                {
                    DrawCursor(
                        (byte *)bufferPtr,
                        _outputDevice.ScreenWidth,
                        _outputDevice.ScreenHeight,
                        (int)_inputDevice.MousePointerX,
                        (int)_inputDevice.MousePointerY,
                        16,
                        1,
                        (byte)r,
                        (byte)g,
                        (byte)b);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to draw cursor", e);
                }
            }

            /* Display on the output device */
            try
            {
                _outputDevice.DisplayImageData((byte *)bufferPtr);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to write to output device", e);

            }
        }
        finally
        {
            gpuBuffer.UnmapMemory();
        }

    }
    private void MainLoop()
    {
        Debug.Assert(_sceneView != null, nameof(_sceneView) + " != null");
        Debug.Assert(_outputDevice != null, nameof(_outputDevice) + " != null");

        var timer = new Stopwatch();
        var frameCount = 0;

        timer.Start();
        while (_keepRunning)
        {
            /* Render scene - the callback passed to stagingGpuImageReady takes care of display */
            _sceneView.RenderToGpuBuffer(
                preserveGpuImage: true,
                stagingGpuImageReady: DisplayImage,
                renderNewFrame: true);

            /* FPS estimation */
            frameCount++;
            if (timer.ElapsedMilliseconds >= 5000)
            {
                var fps = (double)frameCount * 1000 / timer.ElapsedMilliseconds;
                _logger?.LogInformation("FPS estimate: {fps:F1}", fps);
                frameCount = 0;
                timer.Restart();
            }
        }
    }

    private static unsafe void DrawCursor(
        byte *buffer,
        int width,
        int height,
        int cx,
        int cy,
        int size = 32,
        int thickness = 1,
        byte r=255,
        byte g=255,
        byte b=255)
    {
        /* Draw horizontal lines */
        for (var y = cy - thickness; y <= cy + thickness; y++)
        {
            if (y < 0 || y >= height)
            {
                continue;
            }

            for (var x = cx - size; x <= cx + size; x++)
            {
                if (x < 0 || x >= width)
                {
                    continue;
                }

                int idx = (y * width + x) * 4;
                buffer[idx + 0] = r;
                buffer[idx + 1] = g;
                buffer[idx + 2] = b;
                buffer[idx + 3] = 255;
            }
        }

        /* Draw vertical lines */
        for (var x = cx - thickness; x <= cx + thickness; x++)
        {
            if (x < 0 || x >= width)
            {
                continue;
            }

            for (var y = cy - size; y <= cy + size; y++)
            {
                if (y < 0 || y >= height)
                {
                    continue;
                }

                int idx = (y * width + x) * 4;
                buffer[idx + 0] = r;
                buffer[idx + 1] = g;
                buffer[idx + 2] = b;
                buffer[idx + 3] = 255;
            }
        }

    }

    private unsafe void SetupVirtualTerminal()
    {
        var ttyDeviceName = Native.LibC.ttyname(Native.LibC.STDIN_FILENO);
        if (ttyDeviceName == null || !ttyDeviceName.StartsWith("/dev/tty"))
        {
            /* Try to get TTY from $TTYDEVICE environment variable */
            ttyDeviceName = Environment.GetEnvironmentVariable("TTYDEVICE");
            if (ttyDeviceName == null)
            {
                throw new Exception("Program is not launched from console and TTYDEVICE environment variable is not set!");
            }

            if (!ttyDeviceName.StartsWith("/dev/tty"))
            {
                throw new Exception("Device name given via TTYDEVICE environment variable does not start with /dev/tty!");
            }
        }
        _logger?.LogInformation("Using TTY: {name}", ttyDeviceName);

        _vtFd = Native.LibC.open(ttyDeviceName, Native.LibC.O_RDWR | Native.LibC.O_CLOEXEC | Native.LibC.O_NOCTTY, 0);
        if (_vtFd < 0)
        {
            throw new Exception($"Failed to open TTY device {ttyDeviceName}: {Marshal.GetLastWin32Error()}");
        }

        /* Completely disable kernel keyboard processing: this prevents us from being killed on Ctrl-C. */
        uint mode;
        if (Native.LibC.ioctl(_vtFd, Native.LibC.KDGKBMODE, &mode) < 0)
        {
            throw new Exception($"Failed to query keyboard processing mode: {Marshal.GetLastWin32Error()}");
        }
        _savedKbMode = mode;

        if (Native.LibC.ioctl(_vtFd, Native.LibC.KDSKBMODE, (void *)(IntPtr)Native.LibC.K_OFF) != 0)
        {
            throw new Exception($"Failed to set keyboard processing mode: {Marshal.GetLastWin32Error()}");
        }
        _kbModeSet = true;

        /* Change the VT into graphics mode, so the kernel no longer prints text out on top of us. */
        if (Native.LibC.ioctl(_vtFd, Native.LibC.KDSETMODE, (void *)(IntPtr)Native.LibC.KD_GRAPHICS) != 0) {
            throw new Exception($"Failed to switch TTY to graphics mode: {Marshal.GetLastWin32Error()}");
        }
        _ttyModeSet = true;
    }

    private unsafe void RestoreVirtualTerminal()
    {
        if (_vtFd < 0)
        {
            return;
        }

        /* Restore keyboard processing mode */
        if (_kbModeSet)
        {
            Native.LibC.ioctl(_vtFd, Native.LibC.KDSKBMODE, (void*)_savedKbMode);
        }

        /* Change the VT into text mode */
        if (_ttyModeSet)
        {
            Native.LibC.ioctl(_vtFd, Native.LibC.KDSETMODE, (void*)Native.LibC.KD_TEXT);
        }

        Native.LibC.close(_vtFd);
        _vtFd = -1;
    }
}
