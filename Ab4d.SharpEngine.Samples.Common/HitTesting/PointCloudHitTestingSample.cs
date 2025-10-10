using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using System.Diagnostics;
using System.Numerics;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

// This sample shows how to do hit testing on point clouds by using ID bitmap that is generated
// by using the pixel color that is generated from pixel index.
// Then we can convert the pixel color back to pixel index (see GetPixelIndexFromColor method).
//
// There are two ways to use pixel index color:
//
// 1) Call PixelsNode.UsePixelIndexColor method. This will render the pixels with colors based on pixel index.
//    This can be used when we want to some pixels are pixel index colors and some with normal colors.
//    Set the ShowPixelIndexColors to true to see that in action. The ShowPixelIndexColors is used in the LoadPointCloud method.
//
// 2) Set PixelEffect.UsePixelIndexColor to true.
//    Usually we create a new instance of PixelEffect (so the original PixelEffect still renders the pixels normally).
//    This is usually used when we render the pixels with normal colors on the screen, but behind the scenes we have a second SceneView
//    this is used to render pixels with pixel index colors to produce the ID bitmap.
//    This ID bitmap can then be used for hit testing. This is also shown in this sample (see CreateIdBitmapSceneView, SetupIdBitmapScene and RenderIdBitmap).

public class PointCloudHitTestingSample : CommonSample
{
    public override string Title => "Point-cloud hit testing with ID bitmap generated from pixel indexes";
    
    // When we render more than 16 million pixels, then we need to use all 4 color bytes to store the pixel index.
    // But in this sample this is not needed so we can use only red, green and blue color channels to store the triangle index.
    // This preserves the alpha value at 1 so we can see the color in the ID bitmap (otherwise the alpha would be 0 and the rendered bitmap id would appear black).
    private static readonly bool UseAlphaColorForIdBitmap = false; 

    private static bool ShowPixelIndexColors = false; // Set to true to see the 3D scene rendered with pixel index colors

    private readonly string _pointCloudFileName = @"Resources\PointClouds\14 Ladybrook Road 10 - cropped.ply";
    
    private float _pixelSize = 2;
    
    private Color4[]? _pointCloudPositionColors;
    private Vector3[]? _pointCloudPositions;

    private float _boundsDiagonalLength;
    private PixelsNode? _pixelsNode;
    private WireCrossNode? _hitPositionWireCross;

    private bool _isIdBitmapDirty;
    private RawImageData? _rawRenderedBitmap;

    private Vector2 _lastMousePosition;
    private uint _lastPixelColor;

    private Scene? _idBitmapScene;
    private SceneView? _idBitmapSceneView;
    private TargetPositionCamera? _idBitmapCamera;
    private MeshModelNode? _pixelIndexColorMesh;

    private int _lastPixelIndex = -1;
    private uint _lastHitPixelColor;
    private Vector3 _lastHitPixelPosition;

    private ICommonSampleUIElement? _mousePositionLabel;
    private ICommonSampleUIElement? _pixelIndexColorLabel;
    private ICommonSampleUIElement? _pixelIndexLabel;
    private ICommonSampleUIElement? _pixelColorLabel;
    private ICommonSampleUIElement? _pixelPositionLabel;


    public PointCloudHitTestingSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        LoadPointCloud();

        _hitPositionWireCross = new WireCrossNode(position: new Vector3(0, 0, 0), lineColor: Colors.Red, lineLength: 0.2f, lineThickness: 2)
        {
            Visibility = SceneNodeVisibility.Hidden
        };

        scene.RootNode.Add(_hitPositionWireCross);


        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged += OnTargetPositionCameraChanged; // On each camera change, we need to render the ID bitmap again

        ShowCameraAxisPanel = true;
    }

        /// <inheritdoc />
    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        CreateIdBitmapSceneView(sceneView);
        
        base.OnSceneViewInitialized(sceneView);
    }

    private void CreateIdBitmapSceneView(SceneView sceneView)
    {
        var scene = sceneView.Scene;

        if (scene.GpuDevice == null)
            return; // GpuDevice should be set here, but just add this if to make the compiler happy


        // To render the ID bitmap with PrimitiveIdMaterial, we will create a separate Scene and SceneView.
        // The Scene will have the torus mesh but instead of using the StandardMaterial, we will use PrimitiveIdMaterial.
        //
        // IMPORTANT:
        // To reuse the same torus mesh (and its vertex and index buffers), we need to create the Scene with the same GpuDevice as the initial Scene.
        
        _idBitmapScene = new Scene(scene.GpuDevice, "BitmapIdScene");

        // The SceneView is created with the same size as the original SceneView bit without any multi-sampling or supersampling.
        // Using multi-sampling or supersampling would create smooth pixels that would blend id colors and produce invalid triangle indices.
        _idBitmapSceneView = new SceneView(_idBitmapScene, "BitmapIdSceneView");
        
        _idBitmapSceneView.Initialize(sceneView.Width, sceneView.Height, dpiScaleX: 1, dpiScaleY: 1, multisampleCount: 1, supersamplingCount: 1);
        _idBitmapSceneView.BackgroundColor = Color4.TransparentBlack; // Set BackgroundColor to (0,0,0,0) so it will be different from actual objects that will have alpha set to 1.

        // It is not allowed to use the same camera object on more than one SceneView
        // Therefore, we need to create a new TargetPositionCamera that will be synced with the original TargetPositionCamera (before rendering the ID bitmap).
        _idBitmapCamera = new TargetPositionCamera("BitmapIdCamera");
        _idBitmapSceneView.Camera = _idBitmapCamera;

        _isIdBitmapDirty = true;

        sceneView.ViewResized += OnSceneViewResized;


        if (_pixelsNode != null && _pixelsNode.IsInitialized)
            SetupIdBitmapScene();
    }

    private void SetupIdBitmapScene()
    { 
        if (Scene == null || _idBitmapScene == null || _pixelsNode == null || !_pixelsNode.IsInitialized)
            return;

        // To prevent recreating the vertex buffer for the pixel positions,
        // we can get the mesh from the existing PixelsNode.
        var positionsMesh = _pixelsNode.GetMesh();

        if (positionsMesh == null)
            return;


        // To render pixels with colors based on pixel index, we need to set UsePixelIndexColor in PixelEffect to true.
        // Create a new PixelEffect that will have UsePixelIndexColor set to true.
        var pixelEffect = Scene.EffectsManager.GetOrCreate<PixelEffect>("PixelIndexColorEffect");
        pixelEffect.UsePixelIndexColor = true;

        var pixelMaterial = new PixelMaterial("PixelIndexColorMaterial")
        {
            PixelSize = _pixelSize
        };
        
        // When pixelEffect.UsePixelIndexColor is true, then pixelMaterial.PixelColor is added to the calculated color from pixel index.
        // This can be used when other objects are rendered to the same ID bitmap to distinguish the object with this material from other objects.
        // 
        // In this sample we use PixelColor to set alpha to 1 so when saving the ID bitmap to local disk, the pixels will be visible.
        // This is used when UseAlphaColorForIdBitmap is false (by default).
        // When UseAlphaColorForIdBitmap is true, then alpha color will be also used for pixel index color calculation.
        pixelMaterial.PixelColor = UseAlphaColorForIdBitmap ? Color4.TransparentBlack : Color4.Black; // TransparentBlack: 0x00000000; Black: 0x000000FF (only Alpha is 0xFF)

        // Manually set the Effect. This will prevent assigning the default PixelEffect (without UsePixelIndexColor) to the pixelMaterial
        pixelMaterial.Effect = pixelEffect;

        // Create a new MeshModelNode that will use the same mesh as the PixelsNode but will use the pixelMaterial with UsePixelIndexColor
        _pixelIndexColorMesh = new MeshModelNode(positionsMesh, pixelMaterial);

        _idBitmapScene.RootNode.Clear();
        _idBitmapScene.RootNode.Add(_pixelIndexColorMesh);
    }
    
    private void RenderIdBitmap()
    {
        if (_idBitmapSceneView == null)
            return;


        // Sync the camera with the original TargetPositionCamera
        if (targetPositionCamera != null && _idBitmapCamera != null)
        {
            _idBitmapCamera.Heading                = targetPositionCamera.Heading;
            _idBitmapCamera.Attitude               = targetPositionCamera.Attitude;
            _idBitmapCamera.Bank                   = targetPositionCamera.Bank;
            _idBitmapCamera.Distance               = targetPositionCamera.Distance;
            _idBitmapCamera.TargetPosition         = targetPositionCamera.TargetPosition;
            _idBitmapCamera.RotationCenterPosition = targetPositionCamera.RotationCenterPosition;
            _idBitmapCamera.ViewWidth              = targetPositionCamera.ViewWidth;
            _idBitmapCamera.ProjectionType         = targetPositionCamera.ProjectionType;
        }
        
        // Recreate _rawRenderedBitmap when size is changed
        if (_rawRenderedBitmap != null && (_rawRenderedBitmap.Width != _idBitmapSceneView.Width || _rawRenderedBitmap.Height != _idBitmapSceneView.Height))
            _rawRenderedBitmap = null; 

        
        // Render the updated scene to the RawImageData object
        if (_rawRenderedBitmap == null)
            _rawRenderedBitmap = _idBitmapSceneView.RenderToRawImageData(renderNewFrame: true, preserveGpuBuffer: true);
        else
            _idBitmapSceneView.RenderToRawImageData(_rawRenderedBitmap, renderNewFrame: true, preserveGpuBuffer: true);
        
        
        _isIdBitmapDirty = false; // Mark ID Bitmap as correct
    }

    private void OnTargetPositionCameraChanged(object? sender, EventArgs e)
    {
        _isIdBitmapDirty = true;
        ProcessPointerMove(_lastMousePosition); // This will render the ID bitmap
    }
    
    private void OnSceneViewResized(object sender, ViewSizeChangedEventArgs e)
    {
        if (_idBitmapSceneView == null || this.SceneView == null)
            return;

        _idBitmapSceneView.Resize(e.ViewPixelSize.Width, e.ViewPixelSize.Height, renderNextFrameAfterResize: false);
        _isIdBitmapDirty = true;
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (targetPositionCamera != null)
            targetPositionCamera.CameraChanged -= OnTargetPositionCameraChanged;
        
        if (SceneView != null)
            SceneView.ViewResized -= OnSceneViewResized;
        
        if (_idBitmapSceneView != null)
        {
            _idBitmapSceneView.Dispose();
            _idBitmapSceneView = null;
        }
        
        if (_idBitmapScene != null)
        {
            _idBitmapScene.Dispose();
            _idBitmapScene = null;
        }

        base.OnDisposed();
    }   

    private void LoadPointCloud()
    {
        if (Scene == null)
            return;

        Scene.RootNode.Clear();

        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _pointCloudFileName);
        
        try
        {
            // See Importers/PointCloudImporterSample.cs for more details about loading point clouds
            // That sample also includes code for loading .xyz files.

            var plyPointCloudReader = new PlyPointCloudReader()
            {
                SwapYZCoordinates = true
            };

            _pointCloudPositions = plyPointCloudReader.ReadPointCloud(fileName);
            _pointCloudPositionColors = plyPointCloudReader.PixelColors;
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error importing file:\n" + ex.Message);
            return;
        }


        //_pointCloudPositions = CreatePositionsArray(new Vector3(0, 0, 0), new Vector3(300, 0, 300), 100, 1, 100);
        //_pointCloudPositionColors = null;

        var positionsBounds = BoundingBox.FromPoints(_pointCloudPositions);
        _boundsDiagonalLength = positionsBounds.GetDiagonalLength();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = positionsBounds.GetCenterPosition();
            targetPositionCamera.Distance = _boundsDiagonalLength * 1.8f;
        }
        
       
        // Create PixelsNode that will show the positions.
        // We can also pass the positionBounds that define the BoundingBox of the positions.
        // If this is not done, the BoundingBox is calculated by the SharpEngine by checking all the positions.

        // When using PixelColors, PixelColor is used as a mask (multiplied with each color in PixelColors)
        Color4 pixelColor = _pointCloudPositionColors != null ? Colors.White : Colors.Black;

        _pixelsNode = new PixelsNode(_pointCloudPositions, positionsBounds, pixelColor, _pixelSize, "PixelsNode");


        // One way to show pixel index colors is to call UsePixelIndexColor on the PixelsNode.
        // See comment at the beginning of this file for more details.
        if (ShowPixelIndexColors)
        {
            if (UseAlphaColorForIdBitmap)
                _pixelsNode.UsePixelIndexColor();
            else
                _pixelsNode.UsePixelIndexColor(addedColor: Color4.Black);
        }

        if (_pointCloudPositionColors != null)
            _pixelsNode.PixelColors = _pointCloudPositionColors;

        Scene.RootNode.Add(_pixelsNode);
    }

    private void SaveIdBitmap()
    {
        if (_rawRenderedBitmap == null)
            return;

        string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SharpEngineBitmapId.png");
        Scene!.GpuDevice!.DefaultBitmapIO.SaveBitmap(_rawRenderedBitmap, fileName);

        System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
    }  

    private void ProcessPointerMove(Vector2 pointerPosition)
    {
        if (SceneView == null)
            return;

        _lastMousePosition = pointerPosition;

        _mousePositionLabel?.UpdateValue();

        if (_isIdBitmapDirty)
            RenderIdBitmap();

        if (_rawRenderedBitmap != null && !_isIdBitmapDirty)
        {
            // Adjust for DPI scale (rendered image is bigger than max mouse coordinate)
            int idBitmapX = (int)(pointerPosition.X * SceneView.DpiScaleX);
            int idBitmapY = (int)(pointerPosition.Y * SceneView.DpiScaleY);

            if (idBitmapX < 0 || idBitmapX >= _rawRenderedBitmap.Width ||
                idBitmapY < 0 || idBitmapY >= _rawRenderedBitmap.Height)
            {
                _lastPixelColor = 0; // no object hit
            }
            else
            {
                // Get color from ID bitmap
                _lastPixelColor = _rawRenderedBitmap.GetColor(idBitmapX, idBitmapY);

                //Covert color to triangle index
                _lastPixelIndex = GetPixelIndexFromColor(_lastPixelColor, _rawRenderedBitmap.Format);
            }
        }
        else
        {
            _lastPixelColor = 0;
        }

        if (_lastPixelColor == 0)
            _lastPixelIndex = -1;


        if (_pointCloudPositionColors != null && _lastPixelIndex >= 0 && _lastPixelIndex < _pointCloudPositionColors.Length)
        {
            var hotColor = _pointCloudPositionColors[_lastPixelIndex];
            _pixelColorLabel?.SetColor(hotColor);
            _lastHitPixelColor = hotColor.ToBgra();
        }
        else
        {
            _lastHitPixelColor = 0;    
            _pixelColorLabel?.SetColor(Color3.White); // hide
        }

        if (_pointCloudPositions != null && _lastPixelIndex >= 0 && _lastPixelIndex < _pointCloudPositions.Length)
        {
            _lastHitPixelPosition = _pointCloudPositions[_lastPixelIndex];

            if (_hitPositionWireCross != null)
            {
                _hitPositionWireCross.Position = _lastHitPixelPosition;
                _hitPositionWireCross.Visibility = SceneNodeVisibility.Visible;
            }
        }
        else
        {
            _lastHitPixelPosition = new Vector3(float.NaN, float.NaN, float.NaN); // This will hide the value for hit position

            if (_hitPositionWireCross != null)
                _hitPositionWireCross.Visibility = SceneNodeVisibility.Hidden;
        }

        _pixelIndexLabel?.UpdateValue();
        _pixelIndexColorLabel?.UpdateValue();
        _pixelPositionLabel?.UpdateValue();

        _pixelColorLabel?.UpdateValue();
    }

    private static int GetPixelIndexFromColor(uint idColor, Format idBitmapFormat)
    {
        if (idColor == 0)
            return -1; // no pixel hit

        int objectIndex; // use only red, green and blue

        if (idBitmapFormat == Format.R8G8B8A8Unorm)
        {
            if (UseAlphaColorForIdBitmap)
                objectIndex = (int)idColor;
            else
                objectIndex = (int)((idColor >> 8) & 0xFFFFFF);
        }
        else if (idBitmapFormat == Format.B8G8R8A8Unorm)
        {
            if (UseAlphaColorForIdBitmap)
            {
                objectIndex = (int)((idColor >> 24) & 0xFF) +            // blue
                              (int)((idColor >> 16) & 0xFF) * 256 +      // green
                              (int)((idColor >> 8) & 0xFF) * 256 * 256 + // red
                              (int)(idColor & 0xFF) * 256 * 256 * 256;   // alpha
            }
            else
            {
                objectIndex = (int)((idColor >> 24) & 0xFF) +           // blue
                              (int)((idColor >> 16) & 0xFF) * 256 +     // green
                              (int)((idColor >> 8) & 0xFF) * 256 * 256; // red
            }
        }
        else
        {
            return 0; // unknown format (-1 will be returned after 1 is decremented)
        }
        
        // Decrement the index to avoid 0 value (0 is reserved for no object hit; so the first triangle has idColor set to 1)
        return objectIndex - 1;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, isVertical: true);

        _mousePositionLabel = ui.CreateKeyValueLabel("Mouse position: ", () => $"{_lastMousePosition.X:F0} {_lastMousePosition.Y:F0}", keyTextWidth: 110);
        _pixelIndexColorLabel = ui.CreateKeyValueLabel("ID Bitmap color: ", () => $"0x{_lastPixelColor:X8}", keyTextWidth: 110);
        
        ui.AddSeparator();
        _pixelIndexLabel = ui.CreateKeyValueLabel("Pixel index: ", () => $"{_lastPixelIndex}", keyTextWidth: 110);
        _pixelPositionLabel = ui.CreateKeyValueLabel("Hit pixel position: ", () => float.IsNaN(_lastHitPixelPosition.X) ? "" : $"{_lastHitPixelPosition.X:F1} {_lastHitPixelPosition.Y:F1} {_lastHitPixelPosition.Z:F1}", keyTextWidth: 110);
        _pixelColorLabel = ui.CreateKeyValueLabel("Hit pixel color: ", () => $"0x{_lastHitPixelColor:X8}", keyTextWidth: 110);

        ui.AddSeparator();
        ui.AddSeparator();

        ui.CreateButton("Save ID bitmap", () =>
        {
            RenderIdBitmap();
            SaveIdBitmap();
        }, width: 210);

        // Subscribe to mouse (pointer) moved
        ui.RegisterPointerMoved(ProcessPointerMove);
    }
}
