using System;
using System.Drawing;
using System.Numerics;
using System.Windows;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Cameras;

// NOTE:
// Please submit improvements and fixed to TwoDimensionalCamera to https://github.com/ab4d/Ab3d.DXEngine.Wpf.Samples


/// <summary>
/// TwoDimensionalCamera is a helper object that creates a TargetPositionCamera and MouseCameraController
/// and can be used to show 2D objects and 2D lines in a 3D space.
/// This is achieved with using an Orthographic camera type where the CameraWidth is based on the width of the view (visible area).
/// The 2D coordinates should be converted into 3D coordinates with reusing the X and Y coordinates and setting Z coordinates to 0 (or slightly bigger value to show the shape or line above other lines).
/// </summary>
public class TwoDimensionalCamera
{
    private float _dpiScale;

    private float _zoomFactor;

    /// <summary>
    /// TwoDimensionalCoordinateSystems defines possible 2D coordinate system and axis origin types.
    /// </summary>
    public enum TwoDimensionalCoordinateSystems
    {
        /// <summary>
        /// Coordinate system and axis origin (0, 0) start at the center of the view. Y axis points up.
        /// </summary>
        CenterOfViewOrigin = 0,

        // Currently unsupported:
        ///// <summary>
        ///// Coordinate system and axis origin (0, 0) start at the bottom left corner of the view. Y axis points up.
        ///// </summary>
        //LowerLeftCornerOrigin = 1
    }

    /// <summary>
    /// Gets a TargetPositionCamera that is used to show the scene. The UsedCamera is created in the constructor of the TwoDimensionalCamera.
    /// The CameraType is set to OrthographicCamera. Initial TargetPosition is set to (0, 0, 1).
    /// </summary>
    public TargetPositionCamera UsedCamera { get; private set; }

    /// <summary>
    /// Gets a MouseCameraController that is used to control the camera. The UsedMouseCameraController is created in the constructor of the TwoDimensionalCamera.
    /// By default, the camera rotation and quick zoom is disabled. Camera movement is assigned to left mouse button.
    /// Mouse wheel zoom is enabled and MouseWheelDistanceChangeFactor is set to 1.2 to slightly increase the zoom speed.
    /// ZoomMode is set to MousePosition.
    /// </summary>
    public ManualPointerCameraController UsedCameraController { get; private set; }

    /// <summary>
    /// Gets the SceneView that is shown by this TwoDimensionalCamera.
    /// </summary>
    public SceneView ParentSceneView { get; private set; }

    /// <summary>
    /// Gets a Boolean that was set when creating this TwoDimensionalCamera and specifies if screen space units are used.
    /// When false the device independent units are used - scaled by DPI scale (the same units are used by WPF).
    /// </summary>
    public bool UseScreenPixelUnits { get; private set; }

    /// <summary>
    /// Gets the CoordinateSystemType that was used to create this TwoDimensionalCamera.
    /// </summary>
    public TwoDimensionalCoordinateSystems CoordinateSystemType { get; private set; }


    /// <summary>
    /// Gets the size of screen pixel. This value can be used as LineThickness value that would create lines with 1 screen pixel thickness.
    /// This value is set in when the TwoDimensionalCamera is loaded and after the dpi scale is read (see <see cref="IsLoaded"/> property and <see cref="Loaded"/> event).
    /// </summary>
    public float ScreenPixelSize { get; private set; }

    /// <summary>
    /// Gets or sets the ShowCameraLight property from the <see cref="UsedCamera"/>.
    /// </summary>
    public ShowCameraLightType ShowCameraLight
    {
        get => UsedCamera.ShowCameraLight;
        set => UsedCamera.ShowCameraLight = value;
    }

    /// <summary>
    /// Gets or sets the MoveCameraConditions from the <see cref="UsedCameraController"/>.
    /// </summary>
    public PointerAndKeyboardConditions MoveCameraConditions
    {
        get => UsedCameraController.MoveCameraConditions;
        set => UsedCameraController.MoveCameraConditions = value;
    }

    /// <summary>
    /// Gets or sets the QuickZoomConditions from the <see cref="UsedCameraController"/>.
    /// </summary>
    public PointerAndKeyboardConditions QuickZoomConditions
    {
        get => UsedCameraController.QuickZoomConditions;
        set => UsedCameraController.QuickZoomConditions = value;
    }

    /// <summary>
    /// Gets or sets the IsPointerWheelZoomEnabled from the <see cref="UsedCameraController"/>.
    /// </summary>
    public bool IsWheelZoomEnabled
    {
        get => UsedCameraController.IsPointerWheelZoomEnabled;
        set => UsedCameraController.IsPointerWheelZoomEnabled = value;
    }
    
    /// <summary>
    /// Gets or sets a float value that specifies a value that used when zooming with pointer or mouse wheel.
    /// When zooming out the Camera's Distance or CameraWidth is multiplied with this value.
    /// When zooming in the Camera's Distance or CameraWidth is divided with this value.
    /// Default value is 1.2. Bigger value increases the speed of zooming with pointer or mouse wheel. The value should be bigger than 1.
    /// </summary>
    public float WheelDistanceChangeFactor
    {
        get => UsedCameraController.PointerWheelDistanceChangeFactor;
        set => UsedCameraController.PointerWheelDistanceChangeFactor = value;
    }

    /// <summary>
    /// Gets or sets the zoom factor.
    /// </summary>
    public float ZoomFactor
    {
        get => _zoomFactor;
        set
        {
            _zoomFactor = value;
            UsedCamera.ViewWidth = ViewSize.X / _zoomFactor;
        }
    }

    public Vector2 Offset
    {
        get
        {
            var cameraPosition = UsedCamera.GetCameraPosition();
            return new Vector2(cameraPosition.X, cameraPosition.Y);
        }

        set
        {
            UsedCamera.TargetPosition = new Vector3(value.X, value.Y, 1);
        }
    }

    /// <summary>
    /// Gets dpi scale factor.
    /// </summary>
    public float DpiScale
    {
        get { return _dpiScale; }
    }

    /// <summary>
    /// Gets the Size of the visible area when there is no zoom applied.
    /// To get the currently visible area based on the current zoom factor and offset call the <see cref="GetVisibleRect" /> method.
    /// </summary>
    public Vector2 ViewSize { get; private set; }

    /// <summary>
    /// IsLoaded is true when the TwoDimensionalCamera and its ParentDXViewportView is loaded and the dpi scale information is valid. 
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// CameraChanged event is triggered when the camera is changed (this can happen also when the size of the view is changed).
    /// </summary>
    public event EventHandler? CameraChanged;

    /// <summary>
    /// Loaded event is triggered when the parentDXViewportView is loaded and when the dpi scale information is valid. See also <see cref="IsLoaded"/> property.
    /// </summary>
    public event EventHandler? Loaded;


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="pointerCameraController">ManualPointerCameraController</param>
    /// <param name="useScreenPixelUnits">Boolean that specifies if screen space units are used. When false the device independent units are used - scaled by DPI scale (the same units are used by WPF).</param>
    /// <param name="coordinateSystemType">CoordinateSystemTypes</param>
    public TwoDimensionalCamera(ManualPointerCameraController pointerCameraController, bool useScreenPixelUnits, TwoDimensionalCoordinateSystems coordinateSystemType = TwoDimensionalCoordinateSystems.CenterOfViewOrigin)
    {
        if (pointerCameraController == null)
            throw new ArgumentNullException(nameof(pointerCameraController));

        if (pointerCameraController.SceneView == null)
            throw new ArgumentException("The SceneView property of the pointerCameraController is not set");

        UsedCameraController = pointerCameraController;
        ParentSceneView = pointerCameraController.SceneView;

        UseScreenPixelUnits = useScreenPixelUnits;
        CoordinateSystemType = coordinateSystemType;

        if (!ParentSceneView.IsInitialized)
            ParentSceneView.BackBuffersCreated += OnSceneViewInitialized;

        ParentSceneView.ViewResized += OnSceneViewSizeChanged;

        _zoomFactor = 1;
        UpdateDpiScale(); // Call this method even if DXScene is not yet initialized - in this case the _dpiScale and _singlePixelLineThickness will be set to 1 (and will not be 0 anymore)


        // Set camera initial values
        var targetPositionCamera = new TargetPositionCamera
        {
            ProjectionType = ProjectionTypes.Orthographic,
            TargetPosition = new Vector3(0, 0, 1), // Set to 0,0,1 so that we will never come after the lines
            Heading = 0,
            Attitude = 0,
            Bank = 0
        };


        UsedCamera = targetPositionCamera;

        ParentSceneView.Camera = targetPositionCamera;

        ProcessSizeChanged();

        targetPositionCamera.CameraChanged += (sender, args) =>
        {
            ProcessCameraChanged();
        };
        

        pointerCameraController.MoveCameraConditions             = PointerAndKeyboardConditions.LeftPointerButtonPressed;
        pointerCameraController.QuickZoomConditions              = PointerAndKeyboardConditions.Disabled;
        pointerCameraController.RotateCameraConditions           = PointerAndKeyboardConditions.Disabled;
        pointerCameraController.IsPointerWheelZoomEnabled        = true;
        pointerCameraController.PointerWheelDistanceChangeFactor = 1.2f; // Increase mouse wheel zooming speed by changing the factor from 1.05 to 1.2
        pointerCameraController.ZoomMode                         = CameraZoomMode.PointerPosition;
    }



    /// <summary>
    /// Returns the position and size of a rectangle that represent the visible area in the units of this camera.
    /// This takes <see cref="ZoomFactor"/> and <see cref="Offset"/> into account.
    /// To see the size of visible area when there is no zoom applied, see the <see cref="ViewSize"/> property.
    /// </summary>
    public (Vector2 position, Vector2 size) GetVisibleRect()
    {
        float width = ViewSize.X / ZoomFactor;
        float height = ViewSize.Y / ZoomFactor;

        var cameraPosition = UsedCamera.GetCameraPosition();

        var x1 = cameraPosition.X - width * 0.5f;
        var y1 = cameraPosition.Y - height * 0.5f;

        return (new Vector2(x1, y1), new Vector2(width, height));
    }


    //private void ParentSharpEngineSceneViewOnLoaded(object? sender, RoutedEventArgs e)
    //{
    //    IsLoaded = true;

    //    ParentSharpEngineSceneView.Loaded -= ParentSharpEngineSceneViewOnLoaded;

    //    UpdateDpiScale();
    //    OnLoaded();
    //}

    /// <summary>
    /// Update method manually updates the DpiScale and ViewSize.
    /// </summary>
    public void Update()
    {
        UpdateDpiScale();
        ProcessSizeChanged();
    }

    /// <summary>
    /// Reset method resets the camera to show the center of the axis and reset zoom factor to 1.
    /// </summary>
    public void Reset()
    {
        UsedCamera.TargetPosition = new Vector3(0, 0, 1);
        ZoomFactor = 1;
    }


    ///// <summary>
    ///// Returns a Vector3 in the 3D world coordinates from the 2D view coordinates (for example converts a mouse coordinate to coordinates in which the shown lines and shapes are defined).
    ///// See <see cref="ToWpfPosition(Point3D)"/> to get the opposite conversion.
    ///// </summary>
    ///// <param name="viewPosition">2D position in view coordinates</param>
    ///// <returns>Point3D in the view coordinates</returns>
    //public Vector3 ToWorldPosition3D(Vector2 viewPosition)
    //{
    //    var point2d = ToViewPosition(viewPosition);
    //    return new Vector3(point2d.X, point2d.Y, 0);
    //}

    ///// <summary>
    ///// Returns a Point in the view coordinates from the 2D wpfPosition (for example converts a mouse coordinate to coordinates in which the shown lines and shapes are defined).
    ///// See <see cref="ToWpfPosition(Point)"/> to get the opposite conversion.
    ///// </summary>
    ///// <param name="wpfPosition">WPF's 2D position</param>
    ///// <returns>Point in the view coordinates</returns>
    //public Point ToViewPosition(Point wpfPosition)
    //{
    //    float scale = _zoomFactor;
    //    if (UseScreenPixelUnits)
    //        scale *= _dpiScale;

    //    // adjust because we have center of screen in the middle (where the camera looks to)
    //    var offset = UsedCamera.Offset + UsedCamera.TargetPosition;

    //    float x = ( (wpfPosition.X - ViewSize.Width  * 0.5) / scale) + offset.X;
    //    float y = (-(wpfPosition.Y - ViewSize.Height * 0.5) / scale) + offset.Y;

    //    return new Point(x, y);
    //}

    ///// <summary>
    ///// Returns a Point in the WPF coordinates from the 2D view coordinates (for example converts a line or shape coordinate to WPF coordinates of an overlay Canvas).
    ///// See <see cref="ToViewPosition(Point)"/> to get the opposite conversion.
    ///// </summary>
    ///// <param name="viewPosition">2D view coordinates</param>
    ///// <returns>WPF's 2D position</returns>        
    //public Point ToWpfPosition(Point viewPosition)
    //{
    //    float scale = _zoomFactor;
    //    if (UseScreenPixelUnits)
    //        scale *= _dpiScale;

    //    // adjust because we have center of screen in the middle (where the camera looks to)
    //    var offset = UsedCamera.Offset + UsedCamera.TargetPosition;

    //    // For the formula in ToViewPosition
    //    // x2                      = ((x1 - ViewSize.Width * 0.5) / scale) + offset.X;
    //    // x2 - offset.X           = ((x1 - ViewSize.Width * 0.5) / scale);
    //    // (x2 - offset.X) * scale = x1 - ViewSize.Width * 0.5;
    //    // x1                      = ((x2 - offset.X) * scale) + ViewSize.Width * 0.5;

    //    float x = ( (viewPosition.X - offset.X) * scale) + ViewSize.Width * 0.5;
    //    float y = (-(viewPosition.Y - offset.Y) * scale) + ViewSize.Height * 0.5;

    //    return new Point(x, y);
    //}

    ///// <summary>
    ///// Returns a Point in the WPF coordinates from the view coordinates specified as as Point3D (for example converts a line or shape coordinate to WPF coordinates of an overlay Canvas).
    ///// See <see cref="ToWorldPosition3D"/> to get the opposite conversion.
    ///// </summary>
    ///// <param name="viewPosition3D">view coordinates as Point3D</param>
    ///// <returns>WPF's 2D position</returns>           
    //public Point ToWpfPosition(Point3D viewPosition3D)
    //{
    //    var point2d = ToWpfPosition(new Point(viewPosition3D.X, viewPosition3D.Y));
    //    return new Point(point2d.X, point2d.Y);
    //}


    ///// <summary>
    ///// Gets the size in the current view units and zoom level
    ///// (for example if you want to show a rectangle that will be shown as 10 WPF units wide rectangle, then use this method to get the required size in view units and for the current zoom factor).
    ///// </summary>
    ///// <param name="wpfSize">wpfSize</param>
    ///// <returns>size in the current view units and zoom factor</returns>
    //public float GetViewSizeFromWpfSize(float wpfSize)
    //{
    //    return wpfSize / ZoomFactor;
    //}

    ///// <summary>
    ///// Gets the size in WPF units form the view units.
    ///// This represents the size as visible by the user in WPF units based on the view units and current zoom factor.
    ///// </summary>
    ///// <param name="viewSize">size in view units</param>
    ///// <returns>size in WPF units</returns>        
    //public float GetWpfSizeFromViewSize(float viewSize)
    //{
    //    return viewSize * ZoomFactor;
    //}


    /// <summary>
    /// OnCameraChanged
    /// </summary>
    protected void OnCameraChanged()
    {
        if (CameraChanged != null)
            CameraChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// OnLoaded
    /// </summary>
    protected void OnLoaded()
    {
        if (Loaded != null)
            Loaded(this, EventArgs.Empty);
    }


    private void OnSceneViewInitialized(object? sender, EventArgs e)
    {
        ParentSceneView.BackBuffersCreated -= OnSceneViewInitialized;

        UpdateDpiScale();
        ProcessSizeChanged();
    }

    private void OnSceneViewSizeChanged(object? sender, ViewSizeChangedEventArgs e)
    {
        ProcessSizeChanged();

        //if (!this.IsLoaded)
        //    ParentSharpEngineSceneViewOnLoaded(sender, new Avalonia.Interactivity.RoutedEventArgs());
    }

    private void UpdateDpiScale()
    {
        _dpiScale = (ParentSceneView.DpiScaleX + ParentSceneView.DpiScaleY) * 0.5f;
        ScreenPixelSize = 1 / _dpiScale;
    }

    private void ProcessCameraChanged()
    {
        if (ViewSize.X > 0)
            _zoomFactor = ViewSize.X / UsedCamera.ViewWidth;

        OnCameraChanged();
    }

    private void ProcessSizeChanged()
    {
        var sceneView = UsedCameraController.SceneView;

        if (sceneView == null || !sceneView.IsInitialized)
            return;


        float viewWidth, viewHeight;

        if (this.UseScreenPixelUnits)
        {
            viewWidth = sceneView.Width;
            viewHeight = sceneView.Height;
        }
        else
        {
            viewWidth = sceneView.RenderWidth;
            viewHeight = sceneView.RenderHeight;
        }


        ViewSize = new Vector2(viewWidth, viewHeight);

        UsedCamera.ViewWidth = viewWidth / _zoomFactor;

        //OnCameraChanged(); // OnCameraChanged is called from ProcessCameraChanged that is called after we change the CameraWidth
    }
}