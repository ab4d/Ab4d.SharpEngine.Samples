using System.Runtime.InteropServices;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
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


    private int _multisampleCount = 1;
    private bool _isMultisampleCountManuallyChanged;

    /// <summary>
    /// MultisampleCount defines the multi-sampling count (MSAA).
    /// See remarks for more info.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MultisampleCount defines the multi-sampling count (MSAA).
    /// </para>
    /// <para>
    /// When set before the SceneView is initialized it is used to initialize the SceneView.
    /// </para>
    /// <para>
    /// After initializing the SceneView, the MultisampleCount gets the actually used multi-sampling count.
    /// Default value before initialization is 1 (no MSAA).
    /// </para>
    /// <para>
    /// If this value is not manually set by the user, then during initialization the <see cref="GetDefaultMultiSampleCount"/> method is called to set the
    /// multi-sampling count for the used GPU device (by default MSAA is set to 4 for fast desktop devices, for other devices it is set to 1).
    /// </para>
    /// <para>
    /// Changing this value after the SceneView is initialized will call SceneView.Resize method with the new multi-sampling count.
    /// </para>
    /// </remarks>
    /// <seealso cref="SupersamplingCount"/>
    public int MultisampleCount
    {
        get => SceneView.IsInitialized ? SceneView.MultisampleCount : _multisampleCount;
        set
        {
            _isMultisampleCountManuallyChanged = true;
            
            if (_multisampleCount == value)
                return;

            _multisampleCount = value;

            if (SceneView.IsInitialized)
                SceneView.Resize(newMultisampleCount: value, renderNextFrameAfterResize: false);
        }
    }


    private float _supersamplingCount = 1;
    private bool _isSupersamplingCountManuallyChanged;

    /// <summary>
    /// SupersamplingCount defines the super-sampling count (SSAA) - how many more pixels are rendered for each final pixel.
    /// See remarks for more info.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SupersamplingCount defines the super-sampling count (SSAA) - how many more pixels are rendered for each final pixel.
    /// </para>
    /// <para>
    /// When SSAA is more than 1, then the rendering is done on a scaled texture that has SupersamplingCount-times more pixels that the final texture
    /// (width and height are scaled by the <see cref="SceneView.SupersamplingFactor"/> and set to <see cref="SharpEngine.SceneView.RenderWidth"/> and <see cref="SharpEngine.SceneView.RenderHeight"/>)
    /// At the end of the rendering this super-scaled texture is down-sampled into the texture with the final size (defined by <see cref="SceneView.Width"/> and <see cref="SceneView.Height"/>).
    /// </para>
    /// <para>
    /// Valid values are from 1 (no super-sampling) to 64 and are limited by the max texture size that is supported by the GPU device.
    /// </para>
    /// <para>
    /// Value 4 means that width and height are multiplied by 2 and this produces a texture with 4 times as many pixels.
    /// Value 2 means that width and height are multiplied by 1.41 (sqrt(2) = 1.41) and this produces a texture with 2 times as many pixels.
    /// </para>
    /// <para>
    /// After initializing the SceneView, the SupersamplingCount gets the actually used super-sampling count.
    /// </para>
    /// <para>
    /// If this value is not manually set by the user, then during initialization the <see cref="GetDefaultSuperSamplingCount"/> method is called to set the
    /// super-sampling count for the used GPU device (by default SSAA is set to 4 for dedicated desktop devices, for integrated desktop devices is 2, for mobile and low-end devices the default value is 1.
    /// </para>
    /// <para>
    /// Changing this value after the SceneView is initialized will call SceneView.Resize method with the new super-sampling count.
    /// </para>
    /// <para>
    /// Because super-sampling can significantly influence the rendering performance, it is highly recommended that the user of the application can manually adjust this setting.
    /// </para>
    /// </remarks>
    /// <seealso cref="MultisampleCount"/>
    public float SupersamplingCount
    {
        get => SceneView.IsInitialized ? SceneView.SupersamplingCount : _supersamplingCount;
        set
        {
            _isSupersamplingCountManuallyChanged = true;
            
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_supersamplingCount == value)
                return;

            _supersamplingCount = value;

            if (SceneView.IsInitialized)
                SceneView.Resize(newSupersamplingCount: value, renderNextFrameAfterResize: false);
        }
    }


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

        if (GpuDevice == null)
            return; // Try later

        // Set GpuDevice to Scene
        if (Scene.GpuDevice == null)
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

        if (width > 0 && height > 0)
        {
            try
            {
                int multisampleCount = _isMultisampleCountManuallyChanged ? _multisampleCount
                                                                          : GetDefaultMultiSampleCount(GpuDevice.PhysicalDeviceDetails);

                float supersamplingCount = _isSupersamplingCountManuallyChanged ? _supersamplingCount
                                                                                : GetDefaultSuperSamplingCount(GpuDevice.PhysicalDeviceDetails);

                SceneView.Initialize(width, height, 1, 1, multisampleCount, supersamplingCount);

                OnSceneViewInitialized();
            }
            catch (Exception ex)
            {
                Log.Error?.Write(LogArea, Id, "Error initializing SceneView: " + ex.Message, ex);
                return;
            }
        }
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

        /// <summary>
    /// Returns the default multi-sampling count for the specified GPU device.
    /// This method return 4 for DiscreteGpu and IntegratedGpu and 1 for others.
    /// The method can be overriden to provide custom multi-sampling count based on the used device.
    /// This method is called only when the multi-sampling is not set when calling Initialize method.
    /// </summary>
    /// <param name="physicalDeviceDetails">PhysicalDeviceDetails of the VulkanDevice</param>
    /// <returns>default multi-sample count</returns>
    public virtual int GetDefaultMultiSampleCount(PhysicalDeviceDetails physicalDeviceDetails)
    {
        var deviceType = physicalDeviceDetails.DeviceProperties.DeviceType;

        if (deviceType == PhysicalDeviceType.DiscreteGpu || deviceType == PhysicalDeviceType.IntegratedGpu)
            return 4;
        
        // Other: software, virtual gpu-s... => disable MSSA and SSAA
        return 1;
    }
    
    /// <summary>
    /// Returns the default super-sampling count for the specified GPU device.
    /// This method return 4 for DiscreteGpu, 2 for non-mobile IntegratedGpu and 1 for others also macOS and iOS.
    /// The method can be overriden to provide custom multi-sampling count based on the used device.
    /// This method is called only when the super-sampling is not set when calling Initialize method.
    /// </summary>
    /// <param name="physicalDeviceDetails">PhysicalDeviceDetails of the VulkanDevice</param>
    /// <returns>default super-sampling count</returns>
    public virtual float GetDefaultSuperSamplingCount(PhysicalDeviceDetails physicalDeviceDetails)
    {
        // Default SSAA values:
        // DiscreteGpu: 4
        // Non-mobile device with IntegratedGpu from Amd or Intel: 2
        // Mobile and IntegratedGpu from others: 1
        // Other (software, virtual GPU): 1

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
        {
            // MoltenVK that is used on macOS and iOS does not support geometry shader or thick lines,
            // so should not use SSAA as this would reduce visibility of 1px thick lines that are the only supported lines.
            return 1;
        }

        // We also check DpiScale. If it is very high, then we do not need super-sampling
        var deviceType = physicalDeviceDetails.DeviceProperties.DeviceType;
        

        if (deviceType == PhysicalDeviceType.DiscreteGpu)
            return 4;

        if (deviceType == PhysicalDeviceType.IntegratedGpu)
        {
            var vendorId = physicalDeviceDetails.DeviceProperties.VendorID;

            if (!physicalDeviceDetails.IsMobilePlatform && 
                (vendorId == VendorIds.Amd || vendorId == VendorIds.Intel)) // || vendorId == VendorIds.Apple)) // No SSAA for Apple because it does not support GeometryShader and wide-lines - only 1px line
            {
                return 2;
            }
        }

        // Other: Mobile devices and software renderer, virtual gpu-s... => disable MSSA and SSAA
        return 1;
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