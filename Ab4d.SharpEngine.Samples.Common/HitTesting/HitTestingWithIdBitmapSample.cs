using System.Diagnostics;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.HitTesting;

// This sample shows how to render the 3D scene to a bitmap where the colors of the objects are 
// set from the object IDs. This way it is possible to get the object from the pixel's color.
//
// This can be used for rectangular selection or some other more complex selection types.
//
// ADVANTAGES of this technique compared to standard hit-testing:
// - after ID bitmap is generated it is you can get hit object with almost no CPU cost
// - can be used for rectangular and other complicated selection
// - can be used to select objects or lines by moving the mouse close to it
//
// DISADVANTAGES:
// - cannot be used for instanced object (for now)
// - cannot be used to get all selected objects but only gets the object closest to the camera
// - slow to change all the materials, render the scene and transfer the ID bitmap from GPU to CPU memory (change window to full scree to see the slow transfer rate)
// - when camera is rotated, then new ID bitmap must be generated for each frame.


public class HitTestingWithIdBitmapSample : CommonSample
{
    public override string Title => "Hit-testing with rendering to ID bitmap";
    public override string? Subtitle => "Rotate the camera with left mouse button to render new ID bitmap.";

    // After camera is changed, wait 250 ms (1/4 of a second) before rendering another ID bitmap
    // This significantly improves rendering performance, especially when the application is full screen (there it takes a long time to copy rendered ID bitmap to main memory).
    // Set this to 0, to render ID bitmap on each frame change
    private const double UpdateIdBitmapDelayMs = 250; 

    private static int _idToColorMultiplier = 1; // If you want to see the ID Bitmap with bigger color differences, change this value to 8 or similar


    private Dictionary<ModelNode, Material>? _sceneNodeOriginalMaterials;
    private Dictionary<ModelNode, Material>? _sceneNodeOriginalBackMaterials;
    private Dictionary<LineBaseNode, Color4>? _lineNodesOriginalLineColors;
    private List<SceneNode>? _idBitmapModelNodes;
    private List<Material>? _createdMaterials;

    private ICommonSampleUIElement? _mousePositionLabel;
    private ICommonSampleUIElement? _pixelColorLabel;
    private ICommonSampleUIElement? _objectIdLabel;
    private ICommonSampleUIElement? _objectNameLabel;
    private ICommonSampleUIElement? _renderTimeLabel;

    private RawImageData? _rawRenderedBitmap;
    private PngBitmapIO? _pngBitmapIO;

    private bool _isIdBitmapDirty;

    private Vector2 _lastMousePosition;
    private uint _lastPixelColor;
    private int _lastObjectId;
    private string? _lastObjectName;
    private double? _lastRenderTime;

    private DateTime _lastCameraChangedTime;
    private Stopwatch _renderStopwatch = new Stopwatch();


    public HitTestingWithIdBitmapSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Read model from obj file
        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Models\\robotarm.obj");

        var readerObj = new ReaderObj();
        var importedModel = readerObj.ReadSceneNodes(fileName);

        // Position the model so its center is at (0, 0, 0) and scale it to (100, 100, 100)
        Ab4d.SharpEngine.Utilities.ModelUtils.PositionAndScaleSceneNode(importedModel, 
                                                                        position: new Vector3(0, 0, 0), 
                                                                        positionType: PositionTypes.Center, 
                                                                        finalSize: new Vector3(100, 100, 100),
                                                                        preserveAspectRatio: true);

        scene.RootNode.Add(importedModel);


        // Add 40 vertical lines to demonstrate that it is also possible to get hit 3D lines
        for (int i = 0; i <= 40; i++)
        {
            var startPosition = new Vector3(-50 + i * 2.5f, -25, -40);
            var endPosition = startPosition + new Vector3(0, 50, 0);
            var lineNode = new LineNode(startPosition, endPosition, lineThickness: 2, lineColor: Colors.Gray, name: $"Line_{i}");
            scene.RootNode.Add(lineNode);
        }


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 25;
            targetPositionCamera.Attitude = -15;
            targetPositionCamera.Distance = 200;

            targetPositionCamera.CameraChanged += (sender, args) =>
            {
                // On each camera change, we need to mark that the currently rendered ID Bitmap is not valid anymore
                // This is also called when SceneView is resized

                _isIdBitmapDirty = true; 

                // Data from ID bitmap are not valid anymore
                _lastPixelColor = 0;
                _lastObjectId = -1;
                _lastObjectName = "";

                _pixelColorLabel?.UpdateValue();
                _objectIdLabel?.UpdateValue();
                _objectNameLabel?.UpdateValue();

                _lastCameraChangedTime = DateTime.Now;
            };
        }
    }

    protected void ProcessPointerMove(Vector2 pointerPosition)
    {
        if (SceneView == null)
            return;

        _lastMousePosition = pointerPosition;

        _mousePositionLabel?.UpdateValue();

        if (_isIdBitmapDirty)
        {
            var timeAfterLastCameraChanged = DateTime.Now - _lastCameraChangedTime;

            if (timeAfterLastCameraChanged.TotalMilliseconds > UpdateIdBitmapDelayMs)
                RenderIdBitmap();
        }

        if (_rawRenderedBitmap != null && !_isIdBitmapDirty)
        {
            // Adjust for DPI scale (rendered image is bigger than max mouse coordinate)
            int idBitmapX = (int)(pointerPosition.X * SceneView.DpiScaleX);
            int idBitmapY = (int)(pointerPosition.Y * SceneView.DpiScaleY);

            if (idBitmapX < 0 || idBitmapX > _rawRenderedBitmap.Width ||
                idBitmapY < 0 || idBitmapY > _rawRenderedBitmap.Height)
            {
                _lastPixelColor = 0; // no object hit
            }
            else
            {
                // Get color from ID bitmap
                _lastPixelColor = _rawRenderedBitmap.GetColor(idBitmapX, idBitmapY);

                // Covert color to Object ID (index in the _idBitmapModelNodes)
                _lastObjectId = GetObjectIdFromColor(_lastPixelColor, _rawRenderedBitmap.Format);

                if (_idBitmapModelNodes != null && _lastObjectId >= 0 && _lastObjectId < _idBitmapModelNodes.Count)
                {
                    var hitObject = _idBitmapModelNodes[_lastObjectId];
                    _lastObjectName = hitObject?.Name ?? "";
                }
                else
                {
                    _lastObjectName = "";
                }
            }
        }
        else
        {
            _lastPixelColor = 0;
        }

        if (_lastPixelColor == 0)
        {
            _lastObjectId = -1;
            _lastObjectName = "";
        }

        _pixelColorLabel?.UpdateValue();
        _objectIdLabel?.UpdateValue();
        _objectNameLabel?.UpdateValue();
    }

    #region Render bitmap ID
    // This code is the same as in RectangularSelectionSample.cs

    private void RenderIdBitmap()
    {
        if (Scene == null || SceneView == null)
            return;


        _renderStopwatch.Restart();

        // Change materials of all object to color that is created from object ID (index)
        UseObjectIdMaterials(Scene.RootNode);
        
        // Set BackgroundColor to (0,0,0,0) so it will be different from actual objects that will have alpha set to 1.
        var savedBackground = SceneView.BackgroundColor;
        SceneView.BackgroundColor = new Color4(0, 0, 0, 0);


        // Recreate _rawRenderedBitmap when size is changed
        if (_rawRenderedBitmap != null && (_rawRenderedBitmap.Width != SceneView.Width || _rawRenderedBitmap.Height != SceneView.Height))
            _rawRenderedBitmap = null; 

        // Render the updated scene to RawImageData object
        if (_rawRenderedBitmap == null)
            _rawRenderedBitmap = SceneView.RenderToRawImageData(renderNewFrame: true, preserveGpuBuffer: true);
        else
            SceneView.RenderToRawImageData(_rawRenderedBitmap, renderNewFrame: true, preserveGpuBuffer: true);


        // Revert back BackgroundColor and materials

        SceneView.BackgroundColor = savedBackground;

        ResetOriginalMaterials(Scene.RootNode);

        _isIdBitmapDirty = false; // Mark ID Bitmap as correct

        _renderStopwatch.Stop();
        _lastRenderTime = _renderStopwatch.Elapsed.TotalMilliseconds;

        _renderTimeLabel?.UpdateValue();
    }

    private void SaveIdBitmap()
    {
        if (_rawRenderedBitmap == null)
            return;

        _pngBitmapIO ??= new PngBitmapIO();

        string fileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SharpEngineBitmapId.png");
        _pngBitmapIO.SaveBitmap(_rawRenderedBitmap, fileName);

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

            if (childSceneNode is ModelNode childModelNode)
            {
                Color4 colorId = GetColorFromObjectId(objectId);

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

                Color4 colorId = GetColorFromObjectId(objectId);
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
            objectIndex = (int)((idColor >> 24) & 0xFF) +         // blue
                          (int)((idColor >> 16) & 0xFF) * 0xFF +  // green
                          (int)((idColor >> 8)  & 0xFF) * 0xFFFF; // red
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

        _mousePositionLabel = ui.CreateKeyValueLabel("Mouse position: ", () => $"{_lastMousePosition.X:F0} {_lastMousePosition.Y:F0}", keyTextWidth: 100);
        _pixelColorLabel = ui.CreateKeyValueLabel("ID Bitmap color: ", () => $"0x{_lastPixelColor:X8}", keyTextWidth: 100);
        _objectIdLabel = ui.CreateKeyValueLabel("Object ID: ", () => $"{_lastObjectId}", keyTextWidth: 100);

        ui.AddSeparator();
        ui.CreateLabel("Object Name:");
        _objectNameLabel = ui.CreateKeyValueLabel("", () => $"{_lastObjectName}");

        ui.AddSeparator();
        
        _renderTimeLabel = ui.CreateKeyValueLabel("Render time:", () => $"{_lastRenderTime:F2} ms");

        ui.AddSeparator();

        ui.CreateButton("Save ID bitmap", () =>
        {
            RenderIdBitmap();
            SaveIdBitmap();
        }, width: 190);

        ui.CreateCheckBox("Increase color difference (?):When checked the indexes in the ID bitmap are multiplied by 8 so that it is easier to see the different colors for different objects.",
            isInitiallyChecked: false,
            isChecked => _idToColorMultiplier = isChecked ? 8 : 1);
        
        // Subscribe to mouse (pointer) moved
        ui.RegisterPointerMoved(ProcessPointerMove);
    }
}