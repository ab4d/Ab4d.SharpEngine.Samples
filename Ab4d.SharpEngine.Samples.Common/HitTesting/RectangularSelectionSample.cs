using Ab4d.SharpEngine.Common;
using System.Numerics;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Diagnostics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

// This sample must be derived from to subscribe to mouse events - this is platform specific and 
// needs to be done differently for WPF, Avalonia and WinUI


// This sample demonstrates two rectangular selection techniques:
// 1) Object bounds in 2D
// 2) Object ID bitmap
//
// 1) Object bounds in 2D:
// The simplest technique to do rectangular selection is to convert the object's 3D bounds
// (axis aligned bounding box) into a 2D rectangle that represents the bounds on the screen.
// Then we can simply call IntersectsWith method that checks if the two 2D rectangles intersect.
// 
// Advantages:
// - Very simple and fast when there is not a lot of 3D objects.
// - Also selects the objects that are behind the objects closer to the camera.
// 
// Disadvantages:
// - NOT ACCURATE - the bounding box of 3D objects and its bounding box in 2D world are bigger then 
//                  the actual 3D object - selection is done before the user actually touches the 3D object.
// - Slow when checking a lot of 3D objects.
// - Cannot be used to select 3D lines.
// 
//
// 2) Object ID bitmap:
// With Ab4d.SharpEngine it is possible to render objects to a bitmap in such a way that
// each object is rendered with a different color where the color represents the object's id. 
// When such a bitmap is rendered it is possible to get individual pixel colors and from that 
// get the original object that was used to render the pixel.
// See also HitTestingWithIdBitmapSample sample.
// 
// Advantages:
// - Pixel perfect accuracy.
// - Fast when rendering a lot of objects.
// - Can be used to select 3D lines.
// - Can be extended to support some other selection types and not only rectangular selection.
// 
// Disadvantages:
// - More complex (changing materials and rendering to bitmap) than using simple bounding boxes.
// - Slower when using a simple 3D scene.
// - Cannot select objects that are behind other objects (only the objects that are shown on a rendered scene can be detected).    


public abstract class RectangularSelectionSample : CommonSample
{
    public override string Title => "Rectangular selection";
    
    // After camera is changed, wait 250 ms (1/4 of a second) before rendering another ID bitmap
    // This significantly improves rendering performance, especially when the application is full screen (there it takes a long time to copy rendered ID bitmap to main memory).
    // Set this to 0, to render ID bitmap on each frame change
    private const double UpdateIdBitmapDelayMs = 250; 

    private static int _idToColorMultiplier = 1; // If you want to see the ID Bitmap with bigger color differences, change this value to 8 or similar

    private bool _useObjectIdBitmap = true;
    private bool _dumpObjectUnderPointer = true;

   
    private GroupNode? _objectsGroupNode;

    protected bool isLeftPointerButtonPressed; // This is set in a derived class

    private bool _isPointerSelectionStarted;
    private Vector2 _startPointerPosition;
        
    private HashSet<uint> _selectedIdsColors = new HashSet<uint>();
    private HashSet<SceneNode> _selectedObjects = new HashSet<SceneNode>();
    private Dictionary<ModelNode, Material> _savedMaterials = new Dictionary<ModelNode, Material>();
    private Dictionary<LineBaseNode, Color4> _savedLineColors = new Dictionary<LineBaseNode, Color4>();

    private StandardMaterial _selectedMaterial;
    private Color4 _selectedLineColor;

    private List<ModelNode> _modelsToRemove = new List<ModelNode>();
    private List<LineBaseNode> _linesToRemove = new List<LineBaseNode>();

    private bool _isScreenBoundingBoxesDirty = true;
    private List<(Vector2 minimumScreen, Vector2 maximumScreen)> _screenBoundingBoxes = new List<(Vector2 minimumScreen, Vector2 maximumScreen)>();

    private Dictionary<ModelNode, Material>? _sceneNodeOriginalMaterials;
    private Dictionary<ModelNode, Material>? _sceneNodeOriginalBackMaterials;
    private Dictionary<LineBaseNode, Color4>? _lineNodesOriginalLineColors;
    private List<SceneNode>? _idBitmapModelNodes;
    private List<Material>? _createdMaterials;

    private RawImageData? _rawRenderedBitmap;

    private bool _isIdBitmapDirty = true;

    private DateTime _lastCameraChangedTime;

    private SceneView? _bitmapIdSceneView;
    private TargetPositionCamera? _bitmapIdCamera;


    protected RectangularSelectionSample(ICommonSamplesContext context)
        : base(context)
    {
        RotateCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed;
        MoveCameraConditions = PointerAndKeyboardConditions.RightPointerButtonPressed | PointerAndKeyboardConditions.ControlKey;

        _selectedMaterial = StandardMaterials.Red;
        _selectedLineColor = Colors.Red;
    }

    protected abstract void SubscribeMouseEvents(ISharpEngineSceneView sharpEngineSceneView);

    protected abstract void CreateOverlaySelectionRectangle(ICommonSampleUIProvider ui);

    protected abstract void ShowSelectionRectangle(Vector2 startPosition, Vector2 endPosition);

    protected abstract void HideSelectionRectangle();


    #region Create test scene
    protected override void OnCreateScene(Scene scene)
    {
        var boxMesh    = Ab4d.SharpEngine.Meshes.MeshFactory.CreateBoxMesh(centerPosition: new Vector3(0, 0, 0), size: new Vector3(1, 1, 1), xSegments: 1, ySegments: 1, zSegments: 1);
        var sphereMesh = Ab4d.SharpEngine.Meshes.MeshFactory.CreateSphereMesh(centerPosition: new Vector3(0, 0, 0), radius: 0.7f, segments: 30);

        int modelsXCount = 10;
        int modelsYCount = 1;
        int modelsZCount = 10;

        _objectsGroupNode = new GroupNode();

        AddModels(_objectsGroupNode, boxMesh, new Vector3(0, 5, 0), new Vector3(500, modelsYCount * 10, 500), 10, modelsXCount, modelsYCount, modelsZCount, "Box", useBackMaterial: false);
        AddModels(_objectsGroupNode, sphereMesh, new Vector3(25, 5, 25), new Vector3(500, modelsYCount * 10, 500), 10, modelsXCount, modelsYCount, modelsZCount, "Sphere");
            
        scene.RootNode.Add(_objectsGroupNode);

        // It would be optimal to use WireGridNode to create a wire grid.
        // But because WireGridNode creates a MultiLineNode behind the scene, all the lines can have only a single color.
        // Therefore, we create multiple lines for this sample so we can easily change color of individual lines.
        // And what is more, this way the object id map can get us the hit 3D lines (otherwise the whole MultiLineNode would be hit)
        //var wireGridNode = new WireGridNode()
        //{
        //    CenterPosition = new Vector3(0, 0, 0),
        //    Size = new Vector2(500, 500),
        //    WidthCellsCount = 20,
        //    HeightCellsCount = 20,
        //    WidthDirection = new Vector3(1, 0, 0),
        //    HeightDirection = new Vector3(0, 0, 1),
        //    MajorLineColor = Colors.Gray,
        //    MajorLineThickness = 5
        //};

        var contentVisual3D = CreateWireGridLines(new Vector3(0, 0, 0), new Vector2(500, 500), 20, 20, new Vector3(1, 0, 0), new Vector3(0, 0, 1), Colors.Gray, 5);
        scene.RootNode.Add(contentVisual3D);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 250;

            targetPositionCamera.CameraChanged += (sender, args) =>
            {
                // On each camera change, we need to mark that the currently rendered ID Bitmap is not valid anymore
                // This is also called when SceneView is resized

                _isIdBitmapDirty = true;
                _isScreenBoundingBoxesDirty = true;
                _lastCameraChangedTime = DateTime.Now;
            };
        }
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (_bitmapIdSceneView != null)
        {
            _bitmapIdSceneView.Dispose();
            _bitmapIdSceneView = null;
        }

        base.OnDisposed();
    }

    public static void AddModels(GroupNode parentGroupNode, Mesh mesh, Vector3 center, Vector3 size, float modelScaleFactor, int xCount, int yCount, int zCount, string name, bool useBackMaterial = false)
    {
        float xStep = (float)(size.X / xCount);
        float yStep = (float)(size.Y / yCount);
        float zStep = (float)(size.Z / zCount);

        int i = 0;
        for (int z = 0; z < zCount; z++)
        {
            float zPos = (float)(center.Z - (size.Z / 2.0) + (z * zStep));

            for (int y = 0; y < yCount; y++)
            {
                float yPos = (float)(center.Y - (size.Y / 2.0) + (y * yStep));

                float yPercent = (float)y / (float)yCount;

                for (int x = 0; x < xCount; x++)
                {
                    float xPos = (float)(center.X - (size.X / 2.0) + (x * xStep));

                    var matrix = new Matrix4x4(modelScaleFactor, 0, 0, 0,
                                               0, modelScaleFactor, 0, 0,
                                               0, 0, modelScaleFactor, 0,
                                               xPos, yPos, zPos, 1);

                    var material = new StandardMaterial(new Color3((float)x / (float)xCount, 1, yPercent));

                    var model3D = new MeshModelNode(mesh, $"{name}_{z}_{y}_{x}");

                    if (useBackMaterial)
                        model3D.BackMaterial = material;
                    else
                        model3D.Material = material;

                    model3D.Transform = new MatrixTransform(matrix);

                    parentGroupNode.Add(model3D);

                    i++;
                }
            }
        }
    }

    private GroupNode CreateWireGridLines(Vector3 centerPosition,
                                          Vector2 size,
                                          int widthCellsCount,
                                          int heightCellsCount,
                                          Vector3 widthDirection,
                                          Vector3 heightDirection,
                                          Color4 linesColor,
                                          float linesThickness)
    {
        var wireGridGroupNode = new GroupNode("WireGridGroupNode");

        Vector3 onePosition;

        float width = size.X;
        var widthVector = new Vector3(width * widthDirection.X,
                                      width * widthDirection.Y,
                                      width * widthDirection.Z);

        float height = size.Y;
        var heightVector = new Vector3(height * heightDirection.X,
                                       height * heightDirection.Y,
                                       height * heightDirection.Z);

        var startPosition = centerPosition - (widthVector + heightVector) * 0.5f;


        float oneStepFactor = 1.0f / widthCellsCount;

        for (int x = 1; x < widthCellsCount; x++)
        {
            onePosition = startPosition + x * oneStepFactor * widthVector;

            var lineVisual3D = new LineNode($"Line_x_{x}")
            {
                StartPosition = onePosition,
                EndPosition = onePosition + heightVector,
                LineColor = linesColor,
                LineThickness = linesThickness,
            };

            wireGridGroupNode.Add(lineVisual3D);
        }


        oneStepFactor = 1.0f / heightCellsCount;

        for (int y = 1; y < heightCellsCount; y++)
        {
            onePosition = startPosition + y * oneStepFactor * heightVector;

            var lineVisual3D = new LineNode($"Line_y_{y}")
            {
                StartPosition = onePosition,
                EndPosition = onePosition + widthVector,
                LineColor = linesColor,
                LineThickness = linesThickness
            };

            wireGridGroupNode.Add(lineVisual3D);
        }

        return wireGridGroupNode;
    }
    #endregion

    #region process pointer events
    protected void ProcessLeftPointerButtonPressed(Vector2 pointerPosition)
    {
        _isPointerSelectionStarted = true;
        _startPointerPosition = pointerPosition;

        // It is important to restore original materials so that the ID bitmap will use the unselected objects
        RestoreOriginalMaterials();

        if (_useObjectIdBitmap)
            UpdateObjectIdBitmap();
    }

    protected void ProcessLeftPointerButtonReleased(Vector2 pointerPosition)
    {
        _isPointerSelectionStarted = false;

        HideSelectionRectangle();
    }

    protected void ProcessPointerMoved(Vector2 pointerPosition)
    {
        if (!_isPointerSelectionStarted)
            return;

        var selectionStartPos = new Vector2(MathF.Min(_startPointerPosition.X, pointerPosition.X), MathF.Min(_startPointerPosition.Y, pointerPosition.Y));
        var selectionEndPos   = new Vector2(MathF.Max(_startPointerPosition.X, pointerPosition.X), MathF.Max(_startPointerPosition.Y, pointerPosition.Y));

        ShowSelectionRectangle(selectionStartPos, selectionEndPos);

        _selectedObjects.Clear();

        if (_useObjectIdBitmap)
            UpdateSelectedObjectsWithObjectIdMap(selectionStartPos, selectionEndPos);
        else
            UpdateSelectedObjectsWithBoundsIn2D(selectionStartPos, selectionEndPos);


        if (_useObjectIdBitmap && _dumpObjectUnderPointer && SceneView != null && _rawRenderedBitmap != null && _idBitmapModelNodes != null)
        {
            int x = (int)(pointerPosition.X * SceneView.DpiScaleX);
            int y = (int)(pointerPosition.Y * SceneView.DpiScaleY);

            uint pixelColor = _rawRenderedBitmap.GetColor(x, y);

            if (pixelColor == 0)
            {
                System.Diagnostics.Debug.WriteLine($"({x}, {y}) = 0 (NO OBJECT HIT)");
            }
            else
            {
                int objectId = GetObjectIdFromColor(pixelColor, _rawRenderedBitmap.Format);

                if (objectId >= 0 && objectId < _idBitmapModelNodes.Count)
                {
                    var hitSceneNode = _idBitmapModelNodes[objectId];
                    System.Diagnostics.Debug.WriteLine($"({x}, {y}) = #{pixelColor:X8} => [{objectId}] = {hitSceneNode.Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"({x}, {y}) = #{pixelColor:X8} => [{objectId}] (OUT OF BOUNDS)");
                }
            }
        }

        UpdateSelectedObjects();
    }
    #endregion
    
    // NOTE:
    // When using object bounding box, then the selection is NOT precise because bounding box is usually bigger than the actual object.
    private void UpdateSelectedObjectsWithBoundsIn2D(Vector2 selectionStartPos, Vector2 selectionEndPos)
    {
        if (_objectsGroupNode == null || SceneView == null)
            return;


        int sceneNodesCount = _objectsGroupNode.Count;

        if (_isScreenBoundingBoxesDirty)
        {
            _screenBoundingBoxes.Clear();

            for (int i = 0; i < sceneNodesCount; i++)
            {
                var sceneNode = _objectsGroupNode[i];
                
                var boundingBox = sceneNode.WorldBoundingBox;
                
                // Convert 3D object bounds to 2D bounds on the screen
                // BoundingBox3DTo2D returns (Vector2 minimumScreen, Vector2 maximumScreen)
                var screenBoundingBox = SceneView.BoundingBox3DTo2D(boundingBox);

                _screenBoundingBoxes.Add(screenBoundingBox);
            }

            _isScreenBoundingBoxesDirty = false;
        }

        // when the selected is started we can convert the 3D bounds of all objects to 2D bounds.
        // Then in this method we would just call IntersectsWith without calling Rect3DTo2D.

        var minX = selectionStartPos.X * SceneView.DpiScaleX;
        var minY = selectionStartPos.Y * SceneView.DpiScaleY;
        var maxX = selectionEndPos.X * SceneView.DpiScaleX;
        var maxY = selectionEndPos.Y * SceneView.DpiScaleY;

        int count = _objectsGroupNode.Count;
        for (int i = 0; i < count; i++)
        {
            // Get already calculated screen bounding box
            var screenBoundingBox = _screenBoundingBoxes[i];
                
            // Check if our selection rectangle intersects with the screenBoundingBox of the object
            if (screenBoundingBox.minimumScreen.X <= maxX &&
                screenBoundingBox.maximumScreen.X >= minX &&
                screenBoundingBox.minimumScreen.Y <= maxY &&
                screenBoundingBox.maximumScreen.Y >= minY)
            {
                _selectedObjects.Add(_objectsGroupNode[i]);
            }
        }
    }

    private void UpdateObjectIdBitmap()
    {
        if (_isIdBitmapDirty)
        {
            var timeAfterLastCameraChanged = DateTime.Now - _lastCameraChangedTime;

            if (timeAfterLastCameraChanged.TotalMilliseconds > UpdateIdBitmapDelayMs)
                RenderIdBitmap();
        }
    }
    private void UpdateSelectedObjects()
    {
        // First remove selected objects that are not selected anymore:

        // reuse _modelsToRemove
        _modelsToRemove.Clear();

        foreach (var keyValuePair in _savedMaterials)
        {
            var modelNode = keyValuePair.Key;
            if (!_selectedObjects.Contains(modelNode))
            {
                // Restore saved material
                if (modelNode.BackMaterial != null)
                    modelNode.BackMaterial = keyValuePair.Value;
                else
                    modelNode.Material = keyValuePair.Value;

                _modelsToRemove.Add(modelNode); // And mark to be removed from _savedMaterials (we cannot do that inside foreach)
            }
        }

        foreach (var modelNode in _modelsToRemove)
            _savedMaterials.Remove(modelNode);


        // reuse _linesToRemove
        _linesToRemove.Clear();

        foreach (var keyValuePair in _savedLineColors)
        {
            var lineNode = keyValuePair.Key;
            if (!_selectedObjects.Contains(lineNode))
            {
                lineNode.LineColor = keyValuePair.Value; // Restore saved color
                _linesToRemove.Add(lineNode);            // And mark to be removed from _savedLineColors (we cannot do that inside foreach)
            }
        }

        foreach (var lineToRemove in _linesToRemove)
            _savedLineColors.Remove(lineToRemove);


        // Now add newly selected objects:
        foreach (var selectedObject in _selectedObjects)
        {
            if (selectedObject is ModelNode modelNode)
            {
                if (!_savedMaterials.ContainsKey(modelNode)) // Is this ModelNode already selected?
                {
                    // NO: Add it to the selection
                    if (modelNode.Material == null && modelNode.BackMaterial != null) // Do we need to change the BackMaterial (see CheckRenderingQueueObjectsCounts method for more info about that)
                    {
                        _savedMaterials.Add(modelNode, modelNode.BackMaterial);
                        modelNode.BackMaterial = _selectedMaterial;
                    }
                    else if (modelNode.Material != null)
                    {
                        _savedMaterials.Add(modelNode, modelNode.Material);
                        modelNode.Material = _selectedMaterial;
                    }
                }
            }
            else
            {
                if (selectedObject is LineBaseNode lineNode)
                {
                    if (!_savedLineColors.ContainsKey(lineNode)) // Is this geometryModel3D already selected?
                    {
                        // NO: Add it to selection ...
                        _savedLineColors.Add(lineNode, lineNode.LineColor);

                        // ... and change its material
                        lineNode.LineColor = _selectedLineColor;
                    }
                }
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var keyValuePair in _savedMaterials)
        {
            var geometryModel3D = keyValuePair.Key;

            if (geometryModel3D.BackMaterial != null)
                geometryModel3D.BackMaterial = keyValuePair.Value;
            else
                geometryModel3D.Material = keyValuePair.Value;
        }

        _savedMaterials.Clear();


        foreach (var savedLineColor in _savedLineColors)
        {
            var baseLineVisual3D = savedLineColor.Key;
            baseLineVisual3D.LineColor = savedLineColor.Value;
        }

        _savedLineColors.Clear();
    }

    private void UpdateSelectedObjectsWithObjectIdMap(Vector2 selectionStartPos, Vector2 selectionEndPos)
    {
        if (_rawRenderedBitmap == null || _idBitmapModelNodes == null || SceneView == null)
            return;

        int minX = (int)Math.Max(selectionStartPos.X * SceneView.DpiScaleX, 0);
        int maxX = (int)Math.Min(selectionEndPos.X * SceneView.DpiScaleX, _rawRenderedBitmap.Width);
        int width = (maxX - minX);

        int minY = (int)Math.Max(selectionStartPos.Y * SceneView.DpiScaleY, 0);
        int maxY = (int)Math.Min(selectionEndPos.Y * SceneView.DpiScaleY, _rawRenderedBitmap.Height);
        int height = maxY - minY;

        if (width == 0 || height == 0) 
            return;
        

        // Get all ids from the selected rectangle inside the bitmap byte array.
        _selectedIdsColors.Clear();
        uint lastPixelColor = 0;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                uint pixelColor = _rawRenderedBitmap.GetColor(x, y);

                if (pixelColor != 0 && pixelColor != lastPixelColor)
                {
                    _selectedIdsColors.Add(pixelColor);
                    lastPixelColor = pixelColor;
                }
            }
        }

        foreach (uint selectedIdColor in _selectedIdsColors)
        {
            int objectId = GetObjectIdFromColor(selectedIdColor, _rawRenderedBitmap.Format);

            if (objectId >= 0 && objectId < _idBitmapModelNodes.Count)
            {
                var hitSceneNode = _idBitmapModelNodes[objectId];
                _selectedObjects.Add(hitSceneNode);
            }
        }
    }

    #region Render bitmap ID
    // This code is the same as in HitTestingWithIdBitmapSample.cs

    private void RenderIdBitmap()
    {
        if (Scene == null || SceneView == null)
            return;

        // Change materials of all object to color that is created from object ID (index)
        UseObjectIdMaterials(Scene.RootNode);
        
        var savedBackground = SceneView.BackgroundColor;


        SceneView usedBitmapIdSceneView;

        // IMPORTANT:
        // When rendering ID bitmap, we need to disable multi-sampling (MSAA) and super-sampling (SSAA)
        // otherwise the aliasing smooth the colors from one to another object and this would produce invalid id values
        // when it is retrieved from the smoothed color.
        if (SceneView.MultisampleCount > 1 || SceneView.SupersamplingCount > 1)
        {
            // To disable MSAA and SSAA we create another SceneView without any multi-sampling and super-sampling.
            if (_bitmapIdSceneView == null)
            {
                _bitmapIdSceneView = new SceneView(Scene, "BitmapID-SceneView");
                _bitmapIdSceneView.Initialize(SceneView.Width, SceneView.Height, dpiScaleX: 1, dpiScaleY: 1, multisampleCount: 1, supersamplingCount: 1);
                _bitmapIdSceneView.BackgroundColor = Color4.TransparentBlack; // Set BackgroundColor to (0,0,0,0) so it will be different from actual objects that will have alpha set to 1.

                // Create a new TargetPositionCamera that will be used to render _bitmapIdSceneView.
                // This camera is sync with the main targetPositionCamera on each render pass (see code below).
                // Note that we cannot use one camera object on two different SceneView objects.
                _bitmapIdCamera = new TargetPositionCamera();
                _bitmapIdSceneView.Camera = _bitmapIdCamera;
            }
            else if (_bitmapIdSceneView.Width != SceneView.Width || _bitmapIdSceneView.Height != SceneView.Height)
            {
                _bitmapIdSceneView.Resize(SceneView.Width, SceneView.Height, renderNextFrameAfterResize: false);
            }

            // Sync the camera with the original TargetPositionCamera
            if (targetPositionCamera != null && _bitmapIdCamera != null)
            {
                _bitmapIdCamera.Heading = targetPositionCamera.Heading;
                _bitmapIdCamera.Attitude = targetPositionCamera.Attitude;
                _bitmapIdCamera.Bank = targetPositionCamera.Bank;
                _bitmapIdCamera.Distance = targetPositionCamera.Distance;
                _bitmapIdCamera.TargetPosition = targetPositionCamera.TargetPosition;
                _bitmapIdCamera.RotationCenterPosition = targetPositionCamera.RotationCenterPosition;
            }

            usedBitmapIdSceneView = _bitmapIdSceneView;
        }
        else
        {
            // When the SceneView does not use multi-sampling or super-sampling, 
            // then we can render bitmap id directly to this SceneView.
            usedBitmapIdSceneView = SceneView;

            // But we still need to make sure that the BackgroundColor is set to black (no object id)
            // Set BackgroundColor to (0,0,0,0) so it will be different from actual objects that will have alpha set to 1.
            SceneView.BackgroundColor = Color4.TransparentBlack;
        }


        // Recreate _rawRenderedBitmap when size is changed
        if (_rawRenderedBitmap != null && (_rawRenderedBitmap.Width != SceneView.Width || _rawRenderedBitmap.Height != SceneView.Height))
            _rawRenderedBitmap = null;

        // Render the updated scene to the RawImageData object
        if (_rawRenderedBitmap == null)
            _rawRenderedBitmap = usedBitmapIdSceneView.RenderToRawImageData(renderNewFrame: true, preserveGpuBuffer: true);
        else
            usedBitmapIdSceneView.RenderToRawImageData(_rawRenderedBitmap, renderNewFrame: true, preserveGpuBuffer: true);


        // Revert back BackgroundColor (when rendering to the current SceneView)
        if (_bitmapIdSceneView == null)
            SceneView.BackgroundColor = savedBackground;

        // Revert back materials
        ResetOriginalMaterials(Scene.RootNode);

        _isIdBitmapDirty = false; // Mark ID Bitmap as correct
    }

    private void SaveIdBitmap()
    {
        if (_rawRenderedBitmap == null)
            return;

        var pngBitmapIO = new PngBitmapIO();

        string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SharpEngineBitmapId.png");
        pngBitmapIO.SaveBitmap(_rawRenderedBitmap, fileName);

        System.Diagnostics.Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
    }

    private void UseObjectIdMaterials(GroupNode rootGroupNode)
    {
        // Create or clear helper Dictionaries and Lists
        
        // NOTE:
        // To slightly improve performance, it would be possible to preserve all the following Dictionaries and Lists
        // and update them only after the scene is changed (for example, object is added or removed or a material is changed).
        

        if (_sceneNodeOriginalMaterials == null)
            _sceneNodeOriginalMaterials ??= new Dictionary<ModelNode, Material>();
        else
            _sceneNodeOriginalMaterials.Clear();

        if (_sceneNodeOriginalBackMaterials == null)
            _sceneNodeOriginalBackMaterials = new Dictionary<ModelNode, Material>();
        else
            _sceneNodeOriginalBackMaterials.Clear();
        
        if (_lineNodesOriginalLineColors == null)
            _lineNodesOriginalLineColors = new Dictionary<LineBaseNode, Color4>();
        else
            _lineNodesOriginalLineColors.Clear();

        if (_idBitmapModelNodes == null)
            _idBitmapModelNodes = new List<SceneNode>();
        else
            _idBitmapModelNodes.Clear();

        if (_createdMaterials == null)
            _createdMaterials = new List<Material>();
        else
            _createdMaterials.Clear();


        rootGroupNode.ForEachChild<SceneNode>(childSceneNode =>
        {
            // Object ID is the same as the index of the SceneNode in the _idBitmapModelNodes
            int objectId = _idBitmapModelNodes.Count;
            Color4 colorId = GetColorFromObjectId(objectId);

            if (childSceneNode is ModelNode childModelNode)
            {
                // Create SolidColorMaterial so the color is not changed based on lights
                var colorIdMaterial = new SolidColorMaterial(colorId);
                _createdMaterials.Add(colorIdMaterial);


                var material = childModelNode.Material;
                if (material != null)
                    _sceneNodeOriginalMaterials.Add(childModelNode, material);

                childModelNode.Material = colorIdMaterial;


                var backMaterial = childModelNode.BackMaterial;
                if (backMaterial != null)
                {
                    _sceneNodeOriginalBackMaterials.Add(childModelNode, backMaterial);
                    childModelNode.BackMaterial = colorIdMaterial;
                }
                
                
                _idBitmapModelNodes.Add(childModelNode);
            }
            else if (childSceneNode is LineNode lineNode)
            {
                _lineNodesOriginalLineColors.Add(lineNode, lineNode.LineColor);

                lineNode.LineColor = colorId;

                _idBitmapModelNodes.Add(lineNode);
            }
            // Skip other scene nodes
        });
    }

    private void ResetOriginalMaterials(GroupNode rootGroupNode)
    {
        if (_sceneNodeOriginalMaterials == null || _sceneNodeOriginalBackMaterials == null || _lineNodesOriginalLineColors == null)
            return;

        rootGroupNode.ForEachChild<SceneNode>(childSceneNode =>
        {
            if (childSceneNode is ModelNode childModelNode)
            {
                if (_sceneNodeOriginalMaterials.TryGetValue(childModelNode, out var material))
                    childModelNode.Material = material;

                if (_sceneNodeOriginalBackMaterials.TryGetValue(childModelNode, out var backMaterial))
                    childModelNode.BackMaterial = backMaterial;
            }
            else if (childSceneNode is LineNode lineNode)
            {
                if (_lineNodesOriginalLineColors.TryGetValue(lineNode, out var lineColor))
                    lineNode.LineColor = lineColor;
            }
        });

        // _sceneNodeOriginalMaterials and _sceneNodeOriginalBackMaterials will not be needed anymore, so we can clear them
        _sceneNodeOriginalMaterials.Clear();
        _sceneNodeOriginalBackMaterials.Clear();
        _lineNodesOriginalLineColors.Clear();
    }

    public static Color4 GetColorFromObjectId(int objectIndex)
    {
#if DEBUG
        // This is not for production code but just for this sample,
        // where you can increase the IdToColorMultiplier value (for example to 8),
        // to see the ID Bitmap with bigger color differences.
        objectIndex *= _idToColorMultiplier;
#endif

        // Encode objectIndex into red, green and blue colors components (max written index is 16.777.215)
        // Set alpha to 1 to distinguish from alpha = 0: there is no object

        float red   = (float)((objectIndex >> 16) & 0xFF) / 255f;
        float green = (float)((objectIndex >> 8)  & 0xFF) / 255f;
        float blue  = (float)( objectIndex        & 0xFF) / 255f;

        return new Color4(red, green, blue, 1);
    }

    public static int GetObjectIdFromColor(uint idColor, Format idBitmapFormat)
    {
        if (idColor == 0)
            return -1; // no object

        int objectIndex; // use only red, green and blue

        if (idBitmapFormat == Format.R8G8B8A8Unorm)
        {
            objectIndex = (int)((idColor >> 8) & 0xFFFFFF);
        }
        else if (idBitmapFormat == Format.B8G8R8A8Unorm)
        {
            objectIndex = (int)((idColor >> 24) & 0xFF) +            // blue
                          (int)((idColor >> 16) & 0xFF) * 256 +      // green
                          (int)((idColor >> 8)  & 0xFF) * 256 * 256; // red
        }
        else
        {
            return -1; // unknown format
        }

#if DEBUG
        // See comment in GetObjectIdColor4
        objectIndex /= _idToColorMultiplier;
#endif

        return objectIndex;
    }

    #endregion


    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Selection technique:", isHeader: true);

        ui.CreateRadioButtons(new string[]
        {

@"Object bounds in 2D (?):The simplest technique to do rectangular selection is to convert the object's 3D bounds
(axis aligned bounding box) into a 2D rectangle that represents the bounds on the screen.
Then we can simply call IntersectsWith method that checks if the two 2D rectangles intersect.

Advantages:
- Very simple and fast when there is not a lot of 3D objects.
- Also selects the objects that are behind the objects closer to the camera.

Disadvantages:
- Not accurate - the bounding box of 3D objects and its bounding box in 2D world are bigger then 
                 the actual 3D object - selection is done before the user actually touches the 3D object.
- Slow when checking a lot of 3D objects.
- Cannot be used to select 3D lines.",

@"Object ID bitmap (?):With Ab4d.SharpEngine it is possible to render objects to a bitmap in such a way that
each object is rendered with a different color where the color represents the object's id. 
When such a bitmap is rendered it is possible to get individual pixel colors and from that 
get the original object that was used to render the pixel.

Advantages:
- Pixel perfect accuracy.
- Fast when rendering a lot of objects.
- Can be used to select 3D lines.
- Can be extended to support some other selection types and not only rectangular selection.

Disadvantages:
- More complex (changing materials and rendering to bitmap) than using simple bounding boxes.
- Slower when using a simple 3D scene.
- Cannot select objects that are behind some other objects that are closer to the camera."
        },
            (selectedIndex, selectedText) =>
            {
                RestoreOriginalMaterials();
                _useObjectIdBitmap = selectedIndex == 1;
            },
            selectedItemIndex: _useObjectIdBitmap ? 1 : 0);


        ui.AddSeparator();

        ui.CreateButton("Save ID bitmap", () =>
        {
            RenderIdBitmap();
            SaveIdBitmap();
        });

        ui.CreateCheckBox("Increase color difference (?):When checked the indexes in the ID bitmap are multiplied by 8 so that it is easier to see the different colors for different objects.",
            isInitiallyChecked: false,
            isChecked => _idToColorMultiplier = isChecked ? 8 : 1);

        ui.CreateCheckBox("Dump object under pointer (?):When checked then the object under pointer is displayed to the Output window (using Debug.WriteLine).",
            isInitiallyChecked: true,
            isChecked => _dumpObjectUnderPointer = isChecked);


        // Setup selection rectangle and pointer events in derived class (UI framework specific)
        CreateOverlaySelectionRectangle(ui);

        if (context.CurrentSharpEngineSceneView != null)
            SubscribeMouseEvents(context.CurrentSharpEngineSceneView);
    }
}