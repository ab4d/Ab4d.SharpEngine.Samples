using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using SkiaSharp;
using Windows.Foundation;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.UnoPlatform;
using Microsoft.UI.Xaml.Shapes;
using Uno.WinUI.Graphics2DSK;
using DisplayInformation = Windows.Graphics.Display.DisplayInformation;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.UnoPlatform;

public class SharpEngineSceneView : SKCanvasElement, ISharpEngineSceneView, IComponentBase, IDisposable
{
    private static readonly string LogArea = typeof(SharpEngineSceneView).FullName!;

    private SKBitmap? _renderedSceneBitmap;
    private bool _isRenderedSceneBitmapDirty;
    private bool _isGpuDeviceCreatedHere;
    private bool _isFailedToCreateGpuDevice;
    private bool _stopRenderingWhenHidden = true;
    
    /// <inheritdoc />
    public long Id { get; }

    /// <inheritdoc />
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public bool IsDisposing { get; private set; }

    /// <inheritdoc />
    string IComponentBase.Name => Name;
    
    /// <summary>
    /// Gets a Boolean that specifies if this control is currently visible.
    /// </summary>
    public bool IsVisible => Visibility == Visibility.Visible;


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


    /// <summary>
    /// Gets or sets a Boolean that specifies if rendering is stopped when this control is hidden (IsVisible is false). 
    /// Default value is true.
    /// </summary>
    public bool StopRenderingWhenHidden
    {
        get => _stopRenderingWhenHidden;
        set
        {
            if (_stopRenderingWhenHidden == value)
                return;
            
            _stopRenderingWhenHidden = value;
            if (!value || !IsVisible)
                return;

            Refresh();
        }
    }

    /// <summary>
    /// Presentation type defines how the rendered 3D scene will be presented to the platform.
    /// In Uno platform, only <see cref="F:Ab4d.SharpEngine.Common.PresentationTypes.WriteableBitmap"/> is currently supported.
    /// Attempting to set any other presentation type will trigger a PresentationTypeChanged event.
    /// </summary>
    public PresentationTypes PresentationType 
    {
        get => PresentationTypes.WriteableBitmap;
        set 
        {
            if (value != PresentationTypes.WriteableBitmap)
                OnPresentationTypeChanged($"Only WriteableBitmap presentation type is supported on Uno platform. Requested type: {value}");
        }
    }

    /// <inheritdoc />
    public EngineCreateOptions CreateOptions { get; private set; } = new EngineCreateOptions();

    /// <inheritdoc />
    public VulkanDevice? GpuDevice { get; private set; }

    /// <inheritdoc />
    public Scene Scene { get; private set; }

    /// <inheritdoc />
    public SceneView SceneView { get; private set; }

    #region Events

    /// <inheritdoc />
    public event GpuDeviceCreatedEventHandler? GpuDeviceCreated;
    
    /// <inheritdoc />
    public event DeviceCreateFailedEventHandler? GpuDeviceCreationFailed;

    /// <inheritdoc />
    public event EventHandler? SceneViewInitialized;

    /// <inheritdoc />
    public event EventHandler? SceneUpdating;

    /// <inheritdoc />
    public event EventHandler? SceneRendered;

    /// <inheritdoc />
    public event ViewSizeChangedEventHandler? ViewSizeChanged;

    /// <inheritdoc />
    public event EventHandler<string?>? PresentationTypeChanged;

    /// <inheritdoc />
    public event EventHandler<bool>? Disposing;

    /// <inheritdoc />
    public event EventHandler<bool>? Disposed;
        
    #endregion
    
    public SharpEngineSceneView()
    {
        Id = ResourceTracker.GetNextId(this);
        
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        
        Log.Info?.Write(LogArea, Id, "Creating SharpEngineSceneView");
        
        Scene = new Scene(Name ?? "SharpEngineScene");
        SceneView = new SceneView(Scene, Name ?? nameof(SharpEngineSceneView))
        {
            Format = StandardBitmapFormats.Bgra
        };

        Loaded += OnLoaded;
        
        RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityChanged);
    }


    private void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (!IsVisible && StopRenderingWhenHidden)
        {
            Log.Info?.Write(LogArea, Id, "SharpEngineSceneView.IsVisible changed to false");
        }
        else if (IsVisible && SceneView.BackBuffersInitialized)
        {
            Log.Info?.Write(LogArea, Id, "SharpEngineSceneView.IsVisible changed to true");
            Refresh();
        }
    }

    /// <inheritdoc />
    public void RenderScene(bool forceRender = true, bool forceUpdate = false)
    {
        if (GpuDevice == null)
        {
            if (!(forceRender | forceUpdate))
                return;
            Log.Warn?.Write(LogArea, "Cannot render the scene because GpuDevice is null");
            return;
        }

        if (!SceneView.IsInitialized)
        {
            if (!(forceRender | forceUpdate))
                return;
            Log.Warn?.Write(LogArea, "Cannot render because SceneView is not initialized");
            return;
        }

        try
        {
            OnSceneUpdating();
        }
        catch (Exception ex)
        {
            Log.Error?.Write(LogArea, Id, "Exception in SceneUpdating event", ex);
        }

        if (!IsVisible && StopRenderingWhenHidden)
        {
            Log.Trace?.Write(LogArea, Id, "Skip rendering because IsVisible is false");
            return;
        }

        try
        {
            if (SceneView.Render(forceRender, forceUpdate))
            {
                _isRenderedSceneBitmapDirty = true;
                Invalidate();
                OnSceneRendered();
            }
        }
        catch (Exception ex)
        {
            Log.Error?.Write(LogArea, Id, "Unhandled exception in SharpEngineSceneView.RenderScene", ex);
        }
    }
    
    protected void OnSceneUpdating()
    {
        SceneUpdating?.Invoke(this, EventArgs.Empty);
    }

    protected void OnSceneRendered() 
    {
        SceneRendered?.Invoke(this, EventArgs.Empty);
    }

    protected void OnPresentationTypeChanged(string? reason)
    {
        PresentationTypeChanged?.Invoke(this, reason);
    }

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed || IsDisposing)
            return;

        IsDisposing = true;
        OnDisposing(disposing);

        if (disposing)
        {
            if (_renderedSceneBitmap != null)
            {
                _renderedSceneBitmap.Dispose();
                _renderedSceneBitmap = null;
            }

            SceneView.Dispose();
            Scene.Dispose();

            if (GpuDevice != null)
            {
                if (_isGpuDeviceCreatedHere && !GpuDevice.IsDisposed)
                    GpuDevice.Dispose();
                    
                GpuDevice = null;
            }
        }

        IsDisposed = true;
        IsDisposing = false;
        OnDisposed(disposing);
    }

    protected virtual void OnDisposing(bool disposing)
    {
        Disposing?.Invoke(this, disposing);
    }

    protected virtual void OnDisposed(bool disposing)
    {
        Disposed?.Invoke(this, disposing);
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (GpuDevice == null)
            InitializeInt(throwException: false); // Do not throw exception from Loaded event handler in case the VulkanDevice cannot be created
             
        Refresh();
    }

    public void Refresh()
    {
        if (_isRenderedSceneBitmapDirty || !SceneView.IsInitialized)
            return; // already waiting to be updated

        _isRenderedSceneBitmapDirty = true;

        Invalidate();
    }

    /// <inheritdoc />
    public VulkanDevice Initialize()
    {
        CheckIsGpuDeviceCreated(); // Throw exception if _gpuDevice was already created

        InitializeInt(throwException: true);

        return GpuDevice!; // _gpuDevice is not null here because in case when it cannot be created an exception is thrown
    }

    /// <inheritdoc />
    public VulkanDevice Initialize(EngineCreateOptions createOptions)
    {
        CheckIsGpuDeviceCreated(); // Throw exception if _gpuDevice was already created

        CreateOptions = createOptions;

        InitializeInt(throwException: true);

        return GpuDevice!; // _gpuDevice is not null here because in case when it cannot be created an exception is thrown
    }

    /// <inheritdoc />
    public VulkanDevice Initialize(Action<EngineCreateOptions>? configureAction)
    {
        CheckIsGpuDeviceCreated(); // Throw exception if _gpuDevice was already created

        configureAction?.Invoke(CreateOptions);
        
        InitializeInt(throwException: true);

        return GpuDevice!; // _gpuDevice is not null here because in case when it cannot be created an exception is thrown
    }

    /// <inheritdoc />
    public void Initialize(VulkanDevice gpuDevice)
    {
        if (gpuDevice == null)
            throw new ArgumentNullException(nameof(gpuDevice));

        CheckIsGpuDeviceCreated(); // Throw exception if _gpuDevice was already created

        GpuDevice = gpuDevice;
        CreateOptions = GpuDevice.CreateOptions;

        InitializeInt(throwException: false);
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
        
        var (width, height) = GetClientAreaSize();

        if (width == 0 || height == 0)
            return; // We do not have a valid size yet

        var displayInformation = DisplayInformation.GetForCurrentView();
        float dpiScale = (float)displayInformation.RawPixelsPerViewPixel;
        if (dpiScale == 0)
            dpiScale = 1;

        int pixelWidth  = (int)Math.Round(width * dpiScale);
        int pixelHeight = (int)Math.Round(height * dpiScale);

        if (pixelWidth > 0 && pixelHeight > 0)
        {
            try
            {
                int multisampleCount = _isMultisampleCountManuallyChanged ? _multisampleCount
                                                                          : GetDefaultMultiSampleCount(GpuDevice.PhysicalDeviceDetails);

                float supersamplingCount = _isSupersamplingCountManuallyChanged ? _supersamplingCount
                                                                                : GetDefaultSuperSamplingCount(GpuDevice.PhysicalDeviceDetails);

                SceneView.Initialize(pixelWidth, pixelHeight, dpiScale, dpiScale, multisampleCount, supersamplingCount);

                OnSceneViewInitialized();
            }
            catch (Exception ex)
            {
                Log.Error?.Write(LogArea, Id, "Error initializing SceneView: " + ex.Message, ex);

                PresentationType = PresentationTypes.None;
                return;
            }
        }
    }

    private (double, double) GetClientAreaSize()
    {
        double width, height;

        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (this.ActualWidth != 0 && !double.IsNaN(this.ActualWidth))
        {
            // Use Actual size if available (after this control has been measured)
            width  = this.ActualWidth;
            height = this.ActualHeight;
        }
        else if (this.Width != 0 && !double.IsNaN(this.Width) && this.Height != 0 && !double.IsNaN(this.Height))
        {
            // If this control has not been measured, then user may set the Width and Height
            width  = this.Width;
            height = this.Height;
        }
        else
        {
            // No size defined
            width = 0;
            height = 0;
        }
        // ReSharper restore CompareOfFloatsByEqualityOperator

        return (width, height);
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
    
    protected override void RenderOverride(SKCanvas canvas, Size area)
    {
        if (!SceneView.IsInitialized)
            return;

        float dpiScaleX = canvas.TotalMatrix.ScaleX;
        float dpiScaleY = canvas.TotalMatrix.ScaleY;
        int pixelWidth  = (int)Math.Round(area.Width * dpiScaleX);
        int pixelHeight = (int)Math.Round(area.Height * dpiScaleY);

        // The _renderedSceneBitmap is already scaled by dpiScale, so we need to reset the canvas.TotalMatrix to identity matrix. 
        // This prevents scaling the texture and copy the pixels from _renderedSceneBitmap to screen pixels without scaling.
        // Note that Skia provides the IgnorePixelScaling property, but this is not available with the SKCanvasElement.
        // The SKCanvasElement size is specified in device independent units.
        canvas.Scale(1 / dpiScaleX, 1 / dpiScaleY);

        if (SceneView.Width != pixelWidth || SceneView.Height != pixelHeight)
        {
            SceneView.Resize(pixelWidth, pixelHeight, renderNextFrameAfterResize: false);
        }

        if (_isRenderedSceneBitmapDirty ||
            _renderedSceneBitmap == null ||
            _renderedSceneBitmap.Width != SceneView.Width || 
            _renderedSceneBitmap.Height != SceneView.Height)
        {
            PaintSkCanvas();
        }

        canvas.DrawBitmap(_renderedSceneBitmap, 0, 0);
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
    
    public static bool IsPresentationTypePossible(PresentationTypes presentationType)
    {
        // For Uno, we currently only support WriteableBitmap
        return presentationType == PresentationTypes.WriteableBitmap;
    }

    private static bool CheckPresentationType(PresentationTypes presentationType, VulkanDevice device, out string? additionalInfo)
    {
        if (!IsPresentationTypePossible(presentationType))
        {
            additionalInfo = $"Presentation type {presentationType} is not supported by SharpEngine for Uno Platform. Only WriteableBitmap is supported.";
            return false;
        }
    
        additionalInfo = null;
        return true;
    }

    /// <inheritdoc />
    public bool IsPresentationTypeSupported(PresentationTypes presentationType)
    {
        if (GpuDevice == null)
            throw new InvalidOperationException("IsPresentationTypeSupported method without gpuDevice parameter can be called only after initialization");
        
        return IsPresentationTypeSupported(presentationType, GpuDevice, out _);
    }

    /// <inheritdoc />
    public bool IsPresentationTypeSupported(PresentationTypes presentationType, out string? additionalInfo)
    {
        if (GpuDevice == null)
            throw new InvalidOperationException("IsPresentationTypeSupported method without gpuDevice parameter can be called only after initialization");

        return IsPresentationTypeSupported(presentationType, GpuDevice, out additionalInfo);
    }

    /// <inheritdoc />
    public bool IsPresentationTypeSupported(PresentationTypes presentationType, VulkanDevice gpuDevice, out string? additionalInfo)
    {
        if (gpuDevice == null)
            throw new ArgumentNullException(nameof(gpuDevice));

        bool isSupported = CheckPresentationType(presentationType, gpuDevice, out additionalInfo);
    
        if (isSupported && presentationType == PresentationTypes.SharedTexture)
        {
            additionalInfo = "SharedTexture presentation type is not supported on Uno platform";
            return false;
        }

        return isSupported && additionalInfo == null;
    }
}
