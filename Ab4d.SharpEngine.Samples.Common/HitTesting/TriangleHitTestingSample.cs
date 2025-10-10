using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Diagnostics;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Meshes;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

public class TriangleHitTestingSample : CommonSample
{
    public override string Title => "Triangle hit testing with PrimitiveIdMaterial";
    public override string? Subtitle => "PrimitiveIdMaterial can be used to render the triangles of the mesh so that each triangle has a color that is calculated from the triangle index.\nThis can be used as ID bitmap for very fast triangle hit testing on a very complex mesh.\n\nLeft mouse button: rotate the camera\nMouse wheel: zoom in / out";


    // When we render more than 16 million triangles, then we need to use all 4 color bytes to store the triangle index.
    // But in this sample this is not needed so we can use only red, green and blue color channels to store the triangle index.
    // This preserves the alpha value at 1 so we can see the color in the ID bitmap (otherwise the alpha would be 0 and the rendered bitmap id would appear black).
    private static readonly bool UseAlphaColorForPrimitiveId = false; 
    
    
    private StandardMesh _torusMesh;
    
    private RawImageData? _rawRenderedBitmap;

    private bool _isIdBitmapDirty;

    private Vector2 _lastMousePosition;
    private uint _lastPixelColor;
    private int _lastTriangleIndex;
    private double? _lastRenderTime;

    private Stopwatch _renderStopwatch = new Stopwatch();

    private Scene? _idBitmapScene;
    private SceneView? _idBitmapSceneView;
    private TargetPositionCamera? _idBitmapCamera;

    private Vector3[] _selectedTriangleLinePositions;
    private MultiLineNode? _selectedTriangleLinesNode;
    
    private ICommonSampleUIElement? _mousePositionLabel;
    private ICommonSampleUIElement? _pixelColorLabel;
    private ICommonSampleUIElement? _triangleIndexLabel;
    private ICommonSampleUIElement? _renderTimeLabel;
    
    
    public TriangleHitTestingSample(ICommonSamplesContext context)
        : base(context)
    {
        ZoomMode = CameraZoomMode.PointerPosition;

        // Create a complex torus mesh with 200,000 triangles
        _torusMesh = MeshFactory.CreateTorusKnotMesh(centerPosition: new Vector3(0, 0, 0), p: 5, q: 3, radius1: 40, radius2: 20, radius3: 7, uSegmentsCount: 1000, vSegmentsCount: 100);

        // Create _selectedTriangleLinePositions here so it is not nullable         
        _selectedTriangleLinePositions = new Vector3[4]; 
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Show torus with silver specular material
        var material = StandardMaterials.Silver.SetSpecular(32);
        
        var torusKnotModelNode = new MeshModelNode(_torusMesh, material);
        scene.RootNode.Add(torusKnotModelNode);


        // Add MultiLineNode that will show the selected triangle
        _selectedTriangleLinesNode = new MultiLineNode(_selectedTriangleLinePositions, isLineStrip: true)
        {
            LineColor = Colors.Red,
            LineThickness = 2,
            DepthBias = 0.001f, 
            Visibility = SceneNodeVisibility.Hidden // Initially hide the lines
        };
        scene.RootNode.Add(_selectedTriangleLinesNode);

        
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Distance = 330;

            // On each camera change, we need to render the ID bitmap again
            targetPositionCamera.CameraChanged += OnTargetPositionCameraChanged;
        }
    }

    /// <inheritdoc />
    protected override void OnSceneViewInitialized(SceneView sceneView)
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
        
        sceneView.ViewResized += OnSceneViewResized;

        // Add torus mesh to the _idBitmapScene and use PrimitiveIdMaterial
        // This will render the triangles with colors that are calculated from the triangle index.

        // The PrimitiveIdMaterial defines AddedColor property that can be used to add a color to the calculated primitive id color.
        // This can be used when other objects are rendered to the same ID bitmap to distinguish the object with this material from other objects.
        // 
        // In this sample we use AddedColor to set alpha to 1 so when saving the ID bitmap to local disk, the triangles will be visible.
        // This is used when UseAlphaColorForPrimitiveId is false (by default).
        // When UseAlphaColorForPrimitiveId is true, then alpha color will be also used for triangle index calculation.
        Color4 addedColor = UseAlphaColorForPrimitiveId ? Color4.TransparentBlack : Color4.Black; // TransparentBlack: 0x00000000; Black: 0x000000FF (only Alpha is 0xFF)

        var material = new PrimitiveIdMaterial
        {
            IsTwoSided = false, // We can set that to true if we want to also hit-test the back side of the triangles
            AddedColor = addedColor
        };

        var torusKnotModelNode = new MeshModelNode(_torusMesh, material);
        _idBitmapScene.RootNode.Add(torusKnotModelNode);
        
        
        base.OnSceneViewInitialized(sceneView);
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
    

    // This code is very similar to the code in HitTestingWithIdBitmapSample.cs and RectangularSelectionSample.cs

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
                _lastTriangleIndex = GetTriangleIdFromColor(_lastPixelColor, _rawRenderedBitmap.Format);
            }
        }
        else
        {
            _lastPixelColor = 0;
        }

        if (_lastPixelColor == 0)
            _lastTriangleIndex = -1;

        
        // Show selected triangle
        if (_selectedTriangleLinesNode != null)
        {
            if (_lastTriangleIndex >= 0)
            {
                int startIndex = _lastTriangleIndex * 3; // Each triangle has 3 indices

                int i0 = _torusMesh.TriangleIndices![startIndex];
                int i1 = _torusMesh.TriangleIndices[startIndex + 1];
                int i2 = _torusMesh.TriangleIndices[startIndex + 2];
                
                _selectedTriangleLinePositions[0] = _torusMesh.Vertices![i0].Position;
                _selectedTriangleLinePositions[1] = _torusMesh.Vertices[i1].Position;
                _selectedTriangleLinePositions[2] = _torusMesh.Vertices[i2].Position;
                _selectedTriangleLinePositions[3] = _selectedTriangleLinePositions[0]; // close the lines
                
                _selectedTriangleLinesNode.UpdatePositions();
                _selectedTriangleLinesNode.Visibility = SceneNodeVisibility.Visible;
            }
            else
            {
                _selectedTriangleLinesNode.Visibility = SceneNodeVisibility.Hidden; // no triangle selected
            }
        }

        _pixelColorLabel?.UpdateValue();
        _triangleIndexLabel?.UpdateValue();
    }
    
    public static int GetTriangleIdFromColor(uint idColor, Format idBitmapFormat)
    {
        if (idColor == 0)
            return -1; // no triangle hit

        int objectIndex; // use only red, green and blue

        if (idBitmapFormat == Format.R8G8B8A8Unorm)
        {
            if (UseAlphaColorForPrimitiveId)
                objectIndex = (int)idColor;
            else
                objectIndex = (int)((idColor >> 8) & 0xFFFFFF);
        }
        else if (idBitmapFormat == Format.B8G8R8A8Unorm)
        {
            if (UseAlphaColorForPrimitiveId)
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
        
        _renderStopwatch.Restart();
        
        // Recreate _rawRenderedBitmap when size is changed
        if (_rawRenderedBitmap != null && (_rawRenderedBitmap.Width != _idBitmapSceneView.Width || _rawRenderedBitmap.Height != _idBitmapSceneView.Height))
            _rawRenderedBitmap = null; 

        
        // Render the updated scene to the RawImageData object
        if (_rawRenderedBitmap == null)
            _rawRenderedBitmap = _idBitmapSceneView.RenderToRawImageData(renderNewFrame: true, preserveGpuBuffer: true);
        else
            _idBitmapSceneView.RenderToRawImageData(_rawRenderedBitmap, renderNewFrame: true, preserveGpuBuffer: true);
        
        
        _isIdBitmapDirty = false; // Mark ID Bitmap as correct

        _renderStopwatch.Stop();
        _lastRenderTime = _renderStopwatch.Elapsed.TotalMilliseconds;

        _renderTimeLabel?.UpdateValue();
    }

    private void SaveIdBitmap()
    {
        if (_rawRenderedBitmap == null)
            return;

        string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SharpEngineBitmapId.png");
        Scene!.GpuDevice!.DefaultBitmapIO.SaveBitmap(_rawRenderedBitmap, fileName);

        System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
    }    
    
    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        _mousePositionLabel = ui.CreateKeyValueLabel("Mouse position: ", () => $"{_lastMousePosition.X:F0} {_lastMousePosition.Y:F0}", keyTextWidth: 100);
        _pixelColorLabel = ui.CreateKeyValueLabel("ID Bitmap color: ", () => $"0x{_lastPixelColor:X8}", keyTextWidth: 100);
        
        _triangleIndexLabel = ui.CreateKeyValueLabel("Triangle Index: ", () => $"{(_lastTriangleIndex > 0 ? _lastTriangleIndex.ToString() : "")}", keyTextWidth: 100);

        ui.AddSeparator();
        
        _renderTimeLabel = ui.CreateKeyValueLabel("Render time:", () => $"{_lastRenderTime:F2} ms");

        ui.AddSeparator();

        ui.CreateButton("Save ID bitmap", () =>
        {
            RenderIdBitmap();
            SaveIdBitmap();
        }, width: 190);

        // Subscribe to mouse (pointer) moved
        ui.RegisterPointerMoved(ProcessPointerMove);
    }    
}