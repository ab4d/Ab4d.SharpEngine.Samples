using System.Runtime.InteropServices;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Ab4d.SharpEngine.Samples.Maui;

public class SharpEngineSceneView : SKCanvasView
{
    private static readonly string LogArea = typeof(SharpEngineSceneView).FullName!;

    private SKBitmap? _renderedSceneBitmap;
    private bool _isRenderedSceneBitmapDirty;

    public VulkanDevice? GpuDevice { get; private set; }
    private bool _isGpuDeviceCreatedHere;
    private bool _isFailedToCreateGpuDevice;

    public EngineCreateOptions CreateOptions { get; private set; } = new EngineCreateOptions();

    public Scene Scene { get; private set; }
    public SceneView SceneView { get; private set; }


    #region Events

    /// <summary>
    /// Called after the <see cref="GpuDevice"/> object was created.
    /// If device creation has failed, then <see cref="GpuDeviceCreationFailed"/> event is triggered.
    /// </summary>
    public event GpuDeviceCreatedEventHandler? GpuDeviceCreated;

    /// <summary>
    /// Called when the device creation has failed. User can set the IsHandled property to true to prevent showing error text that is shown by SharpEngineSceneView.
    /// </summary>
    public event DeviceCreateFailedEventHandler? GpuDeviceCreationFailed;

    /// <summary>
    /// Called after the <see cref="SceneView"/> object have been initialized (have a valid view size and the back buffers were created).
    /// </summary>
    public event EventHandler? SceneViewInitialized;

    #endregion

    
    public SharpEngineSceneView()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions   = LayoutOptions.Fill;

        Scene     = new Scene();
        SceneView = new SceneView(Scene);

        this.PaintSurface += CanvasViewOnPaintSurface;

        this.Loaded += delegate (object? sender, EventArgs args)
        {
            if (GpuDevice == null)
                InitializeInt(throwException: false); // Do not thorw exception from Loaded event handler in case the VulkanDevice cannot be created

            Refresh();
        };
    }

    public void Refresh()
    {
        if (_isRenderedSceneBitmapDirty || !SceneView.IsInitialized)
            return; // already waiting to be updated

        _isRenderedSceneBitmapDirty = true;
        this.InvalidateSurface();
    }

    public VulkanDevice Initialize()
    {
        CheckIsGpuDeviceCreated(); // Throw exception if _gpuDevice was already created

        InitializeInt(throwException: true);

        return GpuDevice!; // _gpuDevice is not null here because in case when it cannot be created an exception is thrown
    }

    public VulkanDevice Initialize(EngineCreateOptions createOptions)
    {
        CheckIsGpuDeviceCreated(); // Throw exception if _gpuDevice was already created

        CreateOptions = createOptions;

        InitializeInt(throwException: true);

        return GpuDevice!; // _gpuDevice is not null here because in case when it cannot be created an exception is thrown
    }

    public VulkanDevice Initialize(Action<EngineCreateOptions>? configureAction)
    {
        CheckIsGpuDeviceCreated(); // Throw exception if _gpuDevice was already created

        configureAction?.Invoke(CreateOptions);
        InitializeInt(throwException: true);

        return GpuDevice!; // _gpuDevice is not null here because in case when it cannot be created an exception is thrown
    }

    public void Initialize(VulkanDevice gpuDevice)
    {
        if (gpuDevice == null)
            throw new ArgumentNullException(nameof(gpuDevice));

        CheckIsGpuDeviceCreated(); // Throw exception if _gpuDevice was already created

        GpuDevice     = gpuDevice;
        CreateOptions = GpuDevice.CreateOptions;

        InitializeInt(throwException: false);
    }


    private void InitializeInt(bool throwException)
    {
        bool isGpuDeviceCreated = false;

        if (GpuDevice == null)
        {
            if (Scene.GpuDevice != null)
            {
                Log.Info?.Write(LogArea, "Scene.GpuDevice was already set by the used");
                GpuDevice = Scene.GpuDevice;
            }
            else
            {
                isGpuDeviceCreated = CreateGpuDevice(vulkanSurfaceProvider: null, throwException);
                if (!isGpuDeviceCreated)
                    return;
            }
        }

        // Set GpuDevice to Scene
        if (GpuDevice != null && Scene.GpuDevice == null)
        {
            try
            {
                Scene.Initialize(GpuDevice);
            }
            catch (Exception ex)
            {
                string errorMessage = "Failed to initialize Scene: " + ex.Message;
                Log.Error?.Write(LogArea, errorMessage, ex);

                if (throwException)
                    throw new SharpEngineException(errorMessage, ex);
            }
        }

        // We waited until we also initialize Scene before calling OnGpuDeviceCreated
        if (isGpuDeviceCreated && GpuDevice != null)
            OnGpuDeviceCreated(GpuDevice);


        int width, height;
        if (this.Width <= 0 || this.Height <= 0)
        {
            width = 256;
            height = 256;
        }
        else
        {
            width  = (int)this.Width;
            height = (int)this.Height;
        }

        SceneView.Initialize(width, height);

        OnSceneViewInitialized();
    }

    // when throwException is true, then exception is thrown when VulkanDevice, Scene or SceneView cannot be created
    private bool CreateGpuDevice(VulkanSurfaceProvider? vulkanSurfaceProvider, bool throwException)
    {
        if (_isFailedToCreateGpuDevice) // It is useless to try to create GpuDevice multiple times
            return false;

        Log.Info?.Write(LogArea, "SharpEngineSceneView.CreateGpuDevice()");

        _isGpuDeviceCreatedHere = false;

        try
        {
            Log.LastVulkanValidationMessage = null; // reset LastVulkanValidationMessage (this is used in ShowVulkanDeviceCreationError)

            // Save VulkanDevice because we will also need to dispose it
            GpuDevice = VulkanDevice.Create(vulkanSurfaceProvider, CreateOptions);
            _isGpuDeviceCreatedHere = true;

            // Wait until we also initialize Scene before calling OnGpuDeviceCreated
            //OnGpuDeviceCreated(GpuDevice);
        }
        catch (Exception ex)
        {
            _isFailedToCreateGpuDevice = true;

            string errorMessage = "Failed to create VulkanDevice: " + ex.Message;

            if (Log.LastVulkanValidationMessage != null && !ex.Message.Contains(Log.LastVulkanValidationMessage))
                errorMessage += Environment.NewLine + Log.LastVulkanValidationMessage; // Add additional info

            Log.Error?.Write(LogArea, errorMessage);

            // Call DeviceCreateFailed event. If user sets IsHandled to true, then we will not call ShowVulkanDeviceCreationError
            OnGpuDeviceCreationFailed(ex);

            if (throwException)
            {
                if (ex is SharpEngineException)
                    throw;

                throw new SharpEngineException(errorMessage, ex);
            }
        }

        return _isGpuDeviceCreatedHere;
    }

    private void CheckIsGpuDeviceCreated()
    {
        if (GpuDevice != null)
            throw new InvalidOperationException("Cannot initialize SharpEngineSceneView again. Initialize method was probably automatically called after the SharpEngineSceneView was loaded or resized. To manually initialize SharpEngineSceneView call Initialize method before the SharpEngineSceneView is added to the parent control.");
    }

    private void CanvasViewOnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (!SceneView.IsInitialized)
            return;
        
        SKImageInfo info = e.Info;

        if (SceneView.Width != info.Rect.Width || SceneView.Height != info.Rect.Height)
            SceneView.Resize(info.Rect.Width, info.Rect.Height, renderNextFrameAfterResize: false);

        if (_isRenderedSceneBitmapDirty ||
            _renderedSceneBitmap == null ||
            _renderedSceneBitmap.Width != SceneView.Width || _renderedSceneBitmap.Height != SceneView.Height)
        {
            PaintSkCanvas();
        }

        e.Surface.Canvas.DrawBitmap(_renderedSceneBitmap, info.Rect);
    }

    private void PaintSkCanvas()
    {
        if (!SceneView.IsInitialized)
            return;

        // Use RenderToGpuBuffer to render the 3D scene to a staging GpuBuffer.
        // We can then map the staging buffer and get its address that can be used to copy 
        // the bitmap to some other source, in our case to the _renderedSceneBitmap.
        SceneView.RenderToGpuBuffer(
            preserveGpuImage: true,
            stagingGpuImageReady: (GpuBuffer stagingGpuBuffer, int width, int height) =>
            {
                if (_renderedSceneBitmap == null || _renderedSceneBitmap.Width != width || _renderedSceneBitmap.Height != height)
                {
                    if (_renderedSceneBitmap != null)
                        _renderedSceneBitmap.Dispose();

                    _renderedSceneBitmap = new SKBitmap();
                }


                var mappedMemoryPtr = stagingGpuBuffer.GetMappedMemoryPtr();

                // Copy the rendered bitmap to _renderedSceneBitmap
                var skImageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
                _renderedSceneBitmap.InstallPixels(skImageInfo, mappedMemoryPtr);

                stagingGpuBuffer.UnmapMemory();
            });

        // Mark that the _renderedSceneBitmap has been updated
        _isRenderedSceneBitmapDirty = false;
    }


    /// <summary>
    /// OnSceneViewInitialized
    /// </summary>
    protected void OnSceneViewInitialized()
    {
        if (SceneViewInitialized == null)
            return;

        try
        {
            SceneViewInitialized(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Log.Error?.Write(LogArea, "Unhandled exception in SceneViewInitialized event handler: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// OnGpuDeviceCreated
    /// </summary>
    /// <param name="gpuDevice">VulkanDevice</param>
    protected void OnGpuDeviceCreated(VulkanDevice gpuDevice)
    {
        if (GpuDeviceCreated == null)
            return;

        var gpuDeviceCreatedEventArgs = new GpuDeviceCreatedEventArgs(gpuDevice);

        try
        {
            GpuDeviceCreated(this, gpuDeviceCreatedEventArgs);
        }
        catch (Exception ex)
        {
            Log.Error?.Write(LogArea, "Unhandled exception in GpuDeviceCreated event handler: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// OnGpuDeviceCreationFailed
    /// </summary>
    /// <param name="exception">Exception</param>
    protected bool OnGpuDeviceCreationFailed(Exception exception)
    {
        if (GpuDeviceCreationFailed == null)
            return false;

        var deviceCreateFailedEventArgs = new DeviceCreateFailedEventArgs(exception);

        try
        {
            GpuDeviceCreationFailed(this, deviceCreateFailedEventArgs);
        }
        catch (Exception ex)
        {
            Log.Error?.Write(LogArea, "Unhandled exception in GpuDeviceCreationFailed event handler: " + ex.Message, ex);
        }

        return deviceCreateFailedEventArgs.IsHandled;
    }
}