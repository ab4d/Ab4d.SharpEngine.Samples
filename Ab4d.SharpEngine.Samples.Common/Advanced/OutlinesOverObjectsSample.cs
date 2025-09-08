using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.Vulkan;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.PostProcessing;
using Ab4d.SharpEngine.RenderingLayers;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class OutlinesOverObjectsSample: CommonSample
{
    public override string Title => "Object selection by rendering outlines over all objects";

    private List<SceneNode> _selectedObjects = new();
    
    private SceneView? _outlineObjectsSceneView;
    private RenderingLayer? _filteredRenderingLayer1;
    private RenderingLayer? _filteredRenderingLayer2;

    private SobelEdgeDetectionPostProcess? _sobelEdgeDetectionPostProcess;
    private ExpandPostProcess? _expandPostProcess1;
    private ExpandPostProcess? _expandPostProcess2;
    
    private GpuImage? _overlayPanelGpuImage;
    private SpriteBatch? _spriteBatch;
    private TargetPositionCamera? _outlineObjectsCamera;

    public OutlinesOverObjectsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var dragonMesh = TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Dragon, 
                                                position: new Vector3(0, 0, 0), 
                                                positionType: PositionTypes.Center, 
                                                finalSize: new Vector3(30, 30, 30));
        
        for (int i = 0; i < 4; i++)
        {
            var dragonModelNode = new MeshModelNode(dragonMesh, StandardMaterials.Silver, $"DragonModel_{i + 1}")
            {
                Transform = new TranslateTransform(i * 50 - 50, 0, -50)
            };
            
            scene.RootNode.Add(dragonModelNode);
        }
        
        for (int i = 0; i < 4; i++)
        {
            var boxModel = new BoxModelNode($"Box_{i + 1}")
            {
                Position = new Vector3(i * 50 - 50, 0, -10),
                Size = new Vector3(20, 20, 20),
                Material = StandardMaterials.Silver
            };

            scene.RootNode.Add(boxModel);
        }
        
        for (int i = 0; i < 4; i++)
        {
            var sphereModel = new SphereModelNode($"Sphere_{i+1}")
            {
                CenterPosition = new Vector3(i * 50 - 50, 0, 30),
                Radius         = 10,
                Material       = StandardMaterials.Silver
            };

            scene.RootNode.Add(sphereModel);
        }
        

        // Initially select 6 objects
        
        if (scene.RootNode.Count > 6) // Just in case if used changes the above if (reduce the number of added objects), prevent infinite loop 
        {
            while (_selectedObjects.Count < 6)
            {
                int selectedIndex = GetRandomInt(scene.RootNode.Count);
                var selectedSceneNode = scene.RootNode[selectedIndex];

                if (!_selectedObjects.Contains(selectedSceneNode))
                    _selectedObjects.Add(selectedSceneNode);
            }
        }

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 70;
            targetPositionCamera.Attitude = -10;
            targetPositionCamera.Distance = 230;
        }
    }

    /// <inheritdoc />
    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        SetupOutlinesOverObjects(sceneView);
        
        base.OnSceneViewInitialized(sceneView);
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (_outlineObjectsSceneView != null)
        {
            _outlineObjectsSceneView.PostProcesses.Clear();
            
            // Most of the post processes do not create any resources, but still it is a good practice to dispose them (maybe in the future they will require some resources).
            _sobelEdgeDetectionPostProcess?.Dispose();
            _expandPostProcess1?.Dispose();
            _expandPostProcess2?.Dispose();
            
            _overlayPanelGpuImage?.Dispose();
            _spriteBatch?.Dispose();

            _outlineObjectsSceneView.Dispose();
        }
        
        if (SceneView != null)
            SceneView.ViewResized += SceneViewOnViewResized;
        
        base.OnDisposed();
    }

    private void SetupOutlinesOverObjects(SceneView parentSceneView)
    {
        // We will render the object's outlines to a separate SceneView.
        _outlineObjectsSceneView = new SceneView(parentSceneView.Scene, "OutlineObjectsSceneView");

        // Add filters to render only selected objects.
        //
        // To improve performance (reduce the number of calls to FilterObjectsFunction), 
        // it is recommended to first filter by RenderingLayers.
        // 
        // In this sample the selected objects are put into the following two RenderingLayer:
        // - StandardGeometryRenderingLayer (box and sphere objects)
        // - ComplexGeometryRenderingLayer (dragon object that have many vertices and triangles)
        //
        // To check what RenderingLayers are used, you can call Scene.DumpRenderingLayers() in the Visual Studio Immediate window
        // or use "Dump Rendering Layers" button in the Diagnostics window.
        //
        // After filtering by RenderingLayers, the FilterObjectsFunction is called to also filter which RenderingItems are rendered.

        var renderObjectsRenderingStep = _outlineObjectsSceneView.DefaultRenderObjectsRenderingStep;
        if (renderObjectsRenderingStep != null)
        {
            // Filter rendering layers to render only objects in the _filteredRenderingLayer (set to Scene.StandardGeometryRenderingLayer)
            _filteredRenderingLayer1 = Scene!.StandardGeometryRenderingLayer;
            _filteredRenderingLayer2 = Scene!.ComplexGeometryRenderingLayer;
            renderObjectsRenderingStep.FilterRenderingLayersFunction += (renderingLayer) => renderingLayer == _filteredRenderingLayer1 || renderingLayer == _filteredRenderingLayer2;
            
            // Then filter rendering RenderingItems with ParentSceneNode from the _selectedObjects list
            renderObjectsRenderingStep.FilterObjectsFunction += (renderingItem) => renderingItem.ParentSceneNode != null && _selectedObjects.Contains(renderingItem.ParentSceneNode);
        }

        // After the selected objects are rendered, we apply a SobelEdgeDetectionPostProcess to find the edges of the rendered objects.
        _sobelEdgeDetectionPostProcess = new SobelEdgeDetectionPostProcess()
        {
            EdgeThreshold = 0.05f,                  // This value can be increased to slightly reduce the size of the edge
            ColorFactors = new Vector4(0, 0, 0, 1), // Detect edge only on alpha channel so we do not get inner-object edges but only outer-object edges
            ClearColor = Color4.Transparent,        // Use transparent color for background
            AddEdgeToCurrentColor = false,          // do not multiply the edge color with the current color
            EdgeColor = Colors.Red,
            NonEdgeColor = Color4.TransparentBlack  // all color components set to 0
        };

        _outlineObjectsSceneView.PostProcesses.Add(_sobelEdgeDetectionPostProcess);
        
        
        // ExpandPostProcess requires two passes: horizontal and vertical
        _expandPostProcess1 = new ExpandPostProcess(isVerticalRenderingPass: false, expansionWidth: 3, backgroundColor: Color4.Transparent);
        _expandPostProcess2 = new ExpandPostProcess(isVerticalRenderingPass: true, expansionWidth: 3, backgroundColor: Color4.Transparent);

        // Expand is disabled by default and is added to PostProcesses when user enabled it by the ComboBox that is defined below.
        //_outlineObjectsSceneView.PostProcesses.Remove(_expandPostProcess1);
        //_outlineObjectsSceneView.PostProcesses.Remove(_expandPostProcess2);


        // Initialize the _outlineObjectsSceneView with the same size as the parentSceneView.
        if (parentSceneView.Width > 0 && parentSceneView.Height > 0)
            _outlineObjectsSceneView.Initialize(parentSceneView.Width, parentSceneView.Height, parentSceneView.DpiScaleX, parentSceneView.DpiScaleX, multisampleCount: parentSceneView.MultisampleCount, supersamplingCount: 1);
        
        
        // When parentSceneView is resized, we also need to resize the _outlineObjectsSceneView
        parentSceneView.ViewResized += SceneViewOnViewResized;
        
        // It is not allowed to use the same camera object on more than one SceneView
        // Therefore, we need to create a new TargetPositionCamera that will be synced with the original TargetPositionCamera (before rendering the ID bitmap).
        if (targetPositionCamera != null)
        {
            _outlineObjectsCamera = new TargetPositionCamera("OutlineObjectsCamera");
            _outlineObjectsSceneView.Camera = _outlineObjectsCamera;
            
            targetPositionCamera.CameraChanged += (sender, args) => UpdateOutlines();
        }
        
        // Create SpriteBatch that will render the outlines.
        // See also the CustomOverlayPanelSample for more info 
        _spriteBatch = parentSceneView.CreateOverlaySpriteBatch("OverlaySceneViewSpriteBatch");
        
        // When _outlineObjectsSceneView is rendered, then we copy the rendered image to the _overlayPanelGpuImage and render it as a sprite
        _outlineObjectsSceneView.SceneRendered += OutlineObjectsSceneViewOnSceneRendered;


        // Force initial render of outlines
        UpdateOutlines();
    }

    private void UpdateOutlines()
    {
        // Sync the camera with the original TargetPositionCamera
        if (targetPositionCamera != null && _outlineObjectsCamera != null)
        {
            _outlineObjectsCamera.Heading                = targetPositionCamera.Heading;
            _outlineObjectsCamera.Attitude               = targetPositionCamera.Attitude;
            _outlineObjectsCamera.Bank                   = targetPositionCamera.Bank;
            _outlineObjectsCamera.Distance               = targetPositionCamera.Distance;
            _outlineObjectsCamera.TargetPosition         = targetPositionCamera.TargetPosition;
            _outlineObjectsCamera.RotationCenterPosition = targetPositionCamera.RotationCenterPosition;
        }
        
        // When the result of an object filter function (assigned to renderObjectsRenderingStep.FilterObjectsFunction) 
        // is changed, then we need to call NotifyChange with SceneViewDirtyFlags.ObjectsFilterChanged to recreate the rendering commands.
        _outlineObjectsSceneView?.NotifyChange(SceneViewDirtyFlags.ObjectsFilterChanged);
        _outlineObjectsSceneView?.Render();
    }
    
    private void SceneViewOnViewResized(object sender, ViewSizeChangedEventArgs e)
    {
        _outlineObjectsSceneView?.Resize(e.ViewPixelSize.Width, e.ViewPixelSize.Height, renderNextFrameAfterResize: true);
    }

    private void OutlineObjectsSceneViewOnSceneRendered(object? sender, EventArgs e)
    {
        if (_outlineObjectsSceneView == null || GpuDevice == null || _outlineObjectsSceneView.SwapChain == null || _outlineObjectsSceneView.RenderingContext == null || _spriteBatch == null)
            return;

        // We copy the _outlineObjectsSceneView to the _overlayPanelGpuImage
        // that is than render to the main SceneView by using sprite rendering.
        // See also the CustomOverlayPanelSample for more info

        // Update the _overlayPanelGpuImage if its size is no longer the same as the _outlineObjectsSceneView size.

        if (_overlayPanelGpuImage == null)
        {
            _overlayPanelGpuImage = new GpuImage(GpuDevice,
                                                 _outlineObjectsSceneView.Width, _outlineObjectsSceneView.Height, _outlineObjectsSceneView.SwapChainFormat,
                                                 usage: ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled,
                                                 name: "OverlayPanelGpuImage")
            {
                IsPreMultipliedAlpha = true,
                HasTransparentPixels = true,
            };
        }
        else
        {
            if (_overlayPanelGpuImage.Width != _outlineObjectsSceneView.Width || _overlayPanelGpuImage.Height != _outlineObjectsSceneView.Height)
                _overlayPanelGpuImage.Resize(_outlineObjectsSceneView.Width, _outlineObjectsSceneView.Height);
        }


        // Copy the last rendered scene to _overlayPanelGpuImage
        // This preserves the GpuImage on the GPU memory and is much faster than the option below where we render to RawImageData
        _outlineObjectsSceneView.SwapChain.CopyToGpuImage(_outlineObjectsSceneView.RenderingContext.CurrentSwapChainImageIndex,
                                                          targetGpuImage: _overlayPanelGpuImage, targetImageFinalLayout: ImageLayout.ShaderReadOnlyOptimal);
        
        
        // Render _overlayPanelGpuImage as Sprite over the while image

        _spriteBatch.Begin(useAbsoluteCoordinates: false);
        _spriteBatch.SetSpriteTexture(_overlayPanelGpuImage);
        _spriteBatch.DrawSprite(topLeftPosition: new Vector2(0, 0), spriteSize: new Vector2(1, 1));
    
        _spriteBatch.End();        
    }
    

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        if (Scene == null)
            return;
        
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, isVertical: true);

        
        ui.CreateLabel("Selected objects:", isHeader: true);
        
        foreach (var sceneNode in Scene.RootNode)
        {
            string objectName = sceneNode.Name;
            bool isSelected = _selectedObjects.Contains(sceneNode);

            ui.CreateCheckBox(objectName, isSelected, isChecked =>
            {
                if (isChecked)
                    _selectedObjects.Add(sceneNode);
                else
                    _selectedObjects.Remove(sceneNode);

                UpdateOutlines();
            });
        }


        ui.CreateLabel("Selection settings:", isHeader: true);
        
        ui.CreateComboBox(new string[] { "Black", "Red", "Green", "Blue", "#55000055" /* semi-transparent red */ },
            (selectedIndex, selectedText) =>
            {
                if (_sobelEdgeDetectionPostProcess != null && Color4.TryParse(selectedText, out var edgeColor))
                    _sobelEdgeDetectionPostProcess.EdgeColor = edgeColor;
                
                UpdateOutlines();
            },
            selectedItemIndex: 1,
            width: 100,
            keyText: "Edge Color:",
            keyTextWidth: 100);
        
        
        var edgeThresholds = new float[] { 0.01f, 0.05f, 0.1f, 0.3f, 0.9f}; 
        var edgeThresholdTexts = edgeThresholds.Select(v => v.ToString()).ToArray(); 
        ui.CreateComboBox(edgeThresholdTexts,
            (selectedIndex, selectedText) =>
            {
                if (_sobelEdgeDetectionPostProcess != null)
                    _sobelEdgeDetectionPostProcess.EdgeThreshold = edgeThresholds[selectedIndex];

                UpdateOutlines();
            },
            selectedItemIndex: 1,
            width: 100,
            keyText: "Edge Threshold:",
            keyTextWidth: 100);        
        
        ui.CreateComboBox(new string[] { "Disabled", "1", "2", "3" },
            (selectedIndex, selectedText) =>
            {
                if (_outlineObjectsSceneView == null || _expandPostProcess1 == null || _expandPostProcess2 == null)
                    return;
                
                if (selectedIndex == 0)
                {
                    _outlineObjectsSceneView.PostProcesses.Remove(_expandPostProcess1);
                    _outlineObjectsSceneView.PostProcesses.Remove(_expandPostProcess2);
                }
                else
                {
                    _expandPostProcess1.ExpansionWidth = selectedIndex;
                    _expandPostProcess2.ExpansionWidth = selectedIndex;

                    if (!_outlineObjectsSceneView.PostProcesses.Contains(_expandPostProcess1))
                    {
                        _outlineObjectsSceneView.PostProcesses.Add(_expandPostProcess1);
                        _outlineObjectsSceneView.PostProcesses.Add(_expandPostProcess2);
                    }
                }

                UpdateOutlines();
            },
            selectedItemIndex: 0,
            width: 100,
            keyText: "Expand edge: (?):When enabled, then the ExpandPostProcess is used to expand the outline",
            keyTextWidth: 100);
    }    
}