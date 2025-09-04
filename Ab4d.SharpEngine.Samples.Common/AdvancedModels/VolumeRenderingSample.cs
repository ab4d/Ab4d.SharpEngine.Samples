using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Vulkan;
using Ab4d.Vulkan;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

// Volume rendering can show a 3D model from 2D slice images, such as CT or MRI scans.
// The implementation in Ab4d.SharpEngine is based on "GPU GEMS: Chapter 39. Volume Rendering Techniques"
// https://developer.nvidia.com/gpugems/gpugems/part-vi-beyond-triangles/chapter-39-volume-rendering-techniques

public class VolumeRenderingSample : CommonSample
{
    public override string Title => "Volume Rendering";
    public override string Subtitle => "Volume rendering can show a 3D model from 2D slice images, such as CT or MRI scans.";

    private const int ScanImageSpriteSize = 150;
        
    bool _showSliceLines = false;
    bool _showSliceMesh = false;
    bool _showVolumeMaterial = true;
    bool _disableUpdatingSlices = false;
        
    private int _totalSlicesCount = 250; // Number of slices to create. This also defines the distance between the slices.
    private float _shownSlicesPercent = 1;
    private float _startSliceOffset = 0;
    
    private Vector3 _volumeOffset = new Vector3(0, 0, 0);
    private Vector3 _volumeSize = new Vector3(100, 100, 100); // default value
        
    private float _valueClipThreshold = 0.2f;
    private float _softTissueOpacity = 0.1f;
    private float _hardTissueOpacity = 0.7f;
    private float _gradientFactor = 0.6f;
    
    private GpuImage? _slicesGpuImage3D;
    private GpuImage? _transferFunctionTexture;
    
    private GpuImage?[]? _individualScanGpuImages;

    private VolumeMaterial? _volumeMaterial;

    private Transform? _volumeMeshTransform;

    private GradientStop[]? _gradientStops;
    private Action<float, float>? _generateTransferFunctionGradientStops;

    private int _sliceWidth;
    private int _sliceHeight;
    private int _slicesCount;
    private int _oneSlicesLineStride;
    private int _oneSlicesDataStride;
    private Format _sliceImageFormat;
    private byte[]? _allSlicesData;
    
    private Color4[]? _lineColorsGradient;

    private int _currentScanImageIndex;
    
    private SpriteBatch? _scanImageSpriteBatch;
    private GroupNode? _linesGroupNode;
    private GroupNode? _slicesGroupNode;
    private WireBoxNode? _wireBoxNode;
    
    private MeshModelNode? _meshModelNode;
    private Mesh? _slicesTriangleMesh;

    private CubeSlicer _cubeSlicer = new ();
    
    private ICommonSampleUIElement? _scanImageIndexLabel;
    private ICommonSampleUIElement? _scanImageSlider;
    private ICommonSampleUIElement? _softTissueOpacitySlider;
    private ICommonSampleUIElement? _hardTissueOpacitySlider;
    private ICommonSampleUIElement? _gradientFactorSlider;
    private ICommonSampleUIElement? _valueClipThresholdSlider;

    
    public VolumeRenderingSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        _wireBoxNode = new WireBoxNode(_volumeOffset, _volumeSize, Colors.Black, lineThickness: 2, "WireBox");
        scene.RootNode.Add(_wireBoxNode);


        _linesGroupNode = new GroupNode("LinesNode");
        scene.RootNode.Add(_linesGroupNode);

        _slicesGroupNode = new GroupNode("SlicesNode");
        scene.RootNode.Add(_slicesGroupNode);
        
        
        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -30;
            targetPositionCamera.Attitude = 0;
            targetPositionCamera.Distance = 300;
            
            // Set up off-center camera rendering so that the volume models is moved to the left to prevent obscuring the UI elements.
            // This is achieved by setting the TargetPosition and then setting the RotationCenterPosition to (0, 0, 0).
            // This will make the camera rotate around (0, 0, 0) but look at (50, 0, 0).
            // NOTES:
            // - PointerCameraController.RotateAroundPointerPosition must be false.
            // - RotationCenterPosition is supported only by TargetPositionCamera and by FreeCamera.
            targetPositionCamera.TargetPosition = new Vector3(50, 10, 0);
            targetPositionCamera.RotationCenterPosition = new Vector3(0, 0, 0);

            targetPositionCamera.CameraChanged += (sender, args) => UpdateCubeSlices();
            
            targetPositionCamera.StartRotation(-30);
        }
    }

    /// <inheritdoc />
    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        // We will show the current scan image in a SpriteBatch that is rendered on top of the Scene
        _scanImageSpriteBatch = sceneView.CreateOverlaySpriteBatch("OneSliceImageOverlaySpriteBatch");
        _scanImageSpriteBatch.IsUsingDpiScale = true; // To make this sprite batch the same size as the UI slider, we need to scale it by DPI scale

        LoadScanData(scanIndex: 0);
        
        base.OnSceneViewInitialized(sceneView);
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        DisposeScanResources(disposeSlicesGpuImage3D: true);
        base.OnDisposed();
    }
    
    
    private void LoadScanData(int scanIndex)
    {
        var gpuDevice = Scene?.GpuDevice;
        if (gpuDevice == null)
            return;

        DisposeScanResources(disposeSlicesGpuImage3D: true);
        
        if (scanIndex == 0)
            LoadRawScanImages(gpuDevice, "Head_256_256_225.scan");
        else
            LoadRawScanImages(gpuDevice, "Bonsai_512_512_189.scan");
        

        if (_slicesCount == 0 || _allSlicesData == null)
            return;
                
                
        _softTissueOpacitySlider?.UpdateValue();
        _hardTissueOpacitySlider?.UpdateValue();
        _gradientFactorSlider?.UpdateValue();
        _valueClipThresholdSlider?.UpdateValue();
        
        CreateTransferFunctionTexture(gpuDevice);
        
                                
        _slicesGpuImage3D = new GpuImage(gpuDevice, _sliceWidth, _sliceHeight, _sliceImageFormat,
            usage: ImageUsageFlags.Sampled | ImageUsageFlags.TransferDst,
            imageDepth: _slicesCount,
            name: "SlicesImage3D");
        
        _slicesGpuImage3D.CopyDataToImage(_allSlicesData, transitionImageToShaderReadOnlyOptimalLayout: true);


        if (_wireBoxNode != null)
            _wireBoxNode.Size = _volumeSize;

        UpdateCubeSlices();     
     
        
        int index = _slicesCount / 2;
        ShowScanImage(index);

        _scanImageSlider?.SetProperty("Maximum", (_slicesCount - 1).ToString());
        _scanImageSlider?.SetValue(index);

        _scanImageIndexLabel?.UpdateValue();
    }
    
    private void LoadRawScanImages(VulkanDevice gpuDevice, string fileName)
    {
        var fullFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ScannedImages", fileName);
        
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var fileNameParts = fileNameWithoutExtension.Split('_');

        _sliceWidth  = int.Parse(fileNameParts[1]);
        _sliceHeight = int.Parse(fileNameParts[2]);
        _slicesCount = int.Parse(fileNameParts[3]);

        _sliceImageFormat = Format.R8Unorm;
        _oneSlicesLineStride = _sliceWidth;
        _oneSlicesDataStride = _oneSlicesLineStride * _sliceHeight;

        // Read 8-bit unorm data to an array with all the data for all the slices
        // unorm data values are written as byte values from 0 to 255 and are interpreted as float values from 0 to 1.
        _allSlicesData = System.IO.File.ReadAllBytes(fullFileName);
        
        _slicesGpuImage3D = new GpuImage(gpuDevice, _sliceWidth, _sliceHeight, Format.R8Unorm,
                                         usage: ImageUsageFlags.Sampled | ImageUsageFlags.TransferDst,
                                         imageDepth: _slicesCount, // setting imageDepth makes this a 3D image
                                         name: "SlicesImage3D");
        
        _slicesGpuImage3D.CopyDataToImage(_allSlicesData, transitionImageToShaderReadOnlyOptimalLayout: true);
        

        _volumeOffset = new Vector3(0, 0, 0);
        _volumeSize = new Vector3(100, 100, 100);
        
        if (fileName.StartsWith("Head"))
        {
            // Set a different transfer function texture for head scan
            _generateTransferFunctionGradientStops = (softTissueOpacity, hardTissueOpacity) =>
            {
                if (_gradientStops == null || _gradientStops.Length != 7)
                    _gradientStops = new GradientStop[7];

                var skinColor = Color4.FromByteRgba(241, 194, 125, 255);

                _gradientStops[0] = new GradientStop(Color4.TransparentBlack, 0.0f);
                _gradientStops[1] = new GradientStop(Color4.TransparentBlack, 0.3f);
                _gradientStops[2] = new GradientStop(skinColor.SetAlpha(softTissueOpacity * 0.5f), 0.30001f);
                _gradientStops[3] = new GradientStop(skinColor.SetAlpha(softTissueOpacity), 0.4f);
                _gradientStops[4] = new GradientStop(Colors.Gray.SetAlpha(hardTissueOpacity * 0.5f), 0.45f);
                _gradientStops[5] = new GradientStop(Colors.Gray.SetAlpha(hardTissueOpacity), 0.70f);
                _gradientStops[6] = new GradientStop(Colors.Gray.SetAlpha(hardTissueOpacity), 1);
            };

            // Rotate the volume mesh so that the head is upright
            _volumeMeshTransform = new AxisAngleRotateTransform(new Vector3(1, 0, 0), -90); 
            
            _valueClipThreshold = 0.2f;
            _softTissueOpacity = 0.04f;
            _hardTissueOpacity = 0.7f;
            _gradientFactor = 0.6f;
        }   
        else if (fileName.StartsWith("Bonsai"))
        {
            _generateTransferFunctionGradientStops = (softTissueOpacity, hardTissueOpacity) =>
            {
                if (_gradientStops == null || _gradientStops.Length != 6)
                    _gradientStops = new GradientStop[6];

                _gradientStops[0] = new GradientStop(Color4.TransparentBlack, 0);
                _gradientStops[1] = new GradientStop(Color4.TransparentBlack, 0.12f);
                _gradientStops[2] = new GradientStop(Colors.Green.SetAlpha(softTissueOpacity), 0.12f);
                _gradientStops[3] = new GradientStop(Colors.Green.SetAlpha(softTissueOpacity), 0.18f);
                _gradientStops[4] = new GradientStop(Colors.SandyBrown.SetAlpha(hardTissueOpacity), 0.181f);
                _gradientStops[5] = new GradientStop(Colors.Black.SetAlpha(hardTissueOpacity), 1f);
            };
            
            _volumeMeshTransform = null; 

            _valueClipThreshold = 0.1f;
            _softTissueOpacity = 0.2f;
            _hardTissueOpacity = 0.7f;
            _gradientFactor = 1.0f;
        }
        else
        {
            // Generic transfer function with gradient from blue to red
            _generateTransferFunctionGradientStops = (softTissueOpacity, hardTissueOpacity) =>
            {
                if (_gradientStops == null || _gradientStops.Length != 7)
                    _gradientStops = new GradientStop[2];

                _gradientStops[0] = new GradientStop(Colors.Blue.SetAlpha(softTissueOpacity), 0f);
                _gradientStops[1] = new GradientStop(Colors.Red.SetAlpha(hardTissueOpacity), 1f);
            };
        }
    }    
    
    private void UpdateTransferFunctionTexture()
    {
        if (GpuDevice == null)
            return;
        
        CreateTransferFunctionTexture(GpuDevice);
        
        if (_volumeMaterial != null)
            _volumeMaterial.TransferFunctionTexture = _transferFunctionTexture;
    }
    
    private void CreateTransferFunctionTexture(VulkanDevice gpuDevice)
    {
        if (_transferFunctionTexture != null)
        {
            if (_volumeMaterial != null && _volumeMaterial.TransferFunctionTexture == _transferFunctionTexture)
                _volumeMaterial.TransferFunctionTexture = null;
            
            _transferFunctionTexture.Dispose();
        }

        if (_generateTransferFunctionGradientStops != null)
            _generateTransferFunctionGradientStops(_softTissueOpacity, _hardTissueOpacity);
        
        if (_gradientStops == null)
            return;

        // Transfer function texture represents a texture that is used to convert the slice values from the _slicesGpuImage3D to colors (Color4) values.
        // Here we create a one-dimensional (horizontal) texture where the x position (u) represents the slice value.
        // 
        // It is also possible to create a two-dimensional texture.
        // In this case the y position (v) represent the gradient value (when GradientFactor > 0) or slice index (when GradientFactor == 0).
        // This can be used for finer control and more sophisticated visualization (see GPU GEMS article in the link at the begging of this file).
        _transferFunctionTexture = TextureFactory.CreateGradientTexture(gpuDevice, _gradientStops, textureSize: 512, isColorAlphaPremultiplied: false, isHorizontal: true);
    }

    private void ShowScanImage(int imageIndex)
    {
        var gpuDevice = Scene?.GpuDevice;

        if (_slicesCount == 0 || _allSlicesData == null || _scanImageSpriteBatch == null || gpuDevice == null || _currentScanImageIndex == imageIndex)
            return;

        if (_individualScanGpuImages == null || _individualScanGpuImages.Length != _slicesCount)
            _individualScanGpuImages = new GpuImage[_slicesCount];

        var gpuImage = _individualScanGpuImages[imageIndex];

        if (gpuImage == null)
        {
            var oneImageData = new byte[_oneSlicesDataStride];
            Array.Copy(_allSlicesData, _oneSlicesDataStride * imageIndex, oneImageData, 0, _oneSlicesDataStride);
            var rawImageData = new RawImageData(_sliceWidth, _sliceHeight, _oneSlicesLineStride, _sliceImageFormat, oneImageData, checkTransparency: false);

            gpuImage = new GpuImage(gpuDevice, rawImageData, generateMipMaps: false, $"Scan_{imageIndex}");
            _individualScanGpuImages[imageIndex] = gpuImage;
        }

        _scanImageSpriteBatch.Begin(useAbsoluteCoordinates: true);
        _scanImageSpriteBatch.SetCoordinateCenter(PositionTypes.TopRight);
        
        _scanImageSpriteBatch.SetSpriteTexture(gpuImage);
        _scanImageSpriteBatch.DrawSprite(topLeftPosition: new Vector2(ScanImageSpriteSize + 10, 50), spriteSize: new Vector2(ScanImageSpriteSize, ScanImageSpriteSize));

        _scanImageSpriteBatch.End();

        _currentScanImageIndex = imageIndex;

        _scanImageIndexLabel?.UpdateValue();
    }        

    private void UpdateCubeSlices()
    {
        if (_slicesCount == 0 || _slicesGpuImage3D == null || targetPositionCamera == null || _disableUpdatingSlices)
            return;

        
        Matrix4x4 viewMatrix = targetPositionCamera.GetViewMatrix();
        int shownSlicesCount = (int)(_totalSlicesCount * _shownSlicesPercent);
        
        
        if (_showSliceLines)
        {
            // Create a PolyLineNode with different colors (from blue to red) for each cube slice.
            _linesGroupNode?.Clear();
            
            Vector3[][]? intersectionPoints = _cubeSlicer.GenerateIntersectionPoints(in viewMatrix, _totalSlicesCount, shownSlicesCount, _startSliceOffset, _volumeSize, _volumeOffset, _volumeMeshTransform);
            
            if (intersectionPoints == null)
                return;
            
            if (_lineColorsGradient == null)
                _lineColorsGradient = TextureFactory.CreateGradientColors(startColor: Colors.Blue, endColor: Colors.Red, 128);

            for (int i = 0; i < intersectionPoints.Length; i++)
            {
                int colorIndex = (int)Math.Round(((_lineColorsGradient.Length - 1) * (float)i) / (float)shownSlicesCount);
                var color = _lineColorsGradient[colorIndex];
                
                var polyLineNode = new PolyLineNode(intersectionPoints[i], lineColor: color, lineThickness: 1) { IsClosed = true };
                _linesGroupNode?.Add(polyLineNode);  
            }
            
            return;
        }


        // Show slices mesh or volume material

        (PositionTexture3DVertex[]? vertices, int[]? triangleIndices) = _cubeSlicer.GenerateVerticesAndTriangleIndices(in viewMatrix, _totalSlicesCount, shownSlicesCount, _startSliceOffset, _volumeSize, _volumeOffset, _volumeMeshTransform);

        if (vertices == null || triangleIndices == null)
            return;
        
        
        if (_slicesTriangleMesh != null)
            _slicesTriangleMesh.Dispose();

        Mesh newSlicesMesh;
        if (_showVolumeMaterial)
        {
            newSlicesMesh = new TriangleMesh<PositionTexture3DVertex>(vertices, triangleIndices, name: "SlicesMesh-PositionTexture3D");
        }
        else if (_showSliceMesh)
        {
            // When we are using SolidColorMaterial we need to use Vector3 as vertex type and not PositionTexture3DVertex because it is not supported by vertex shader in SolidColorMaterial
            newSlicesMesh = new TriangleMesh<Vector3>(vertices.Select(v => v.Position).ToArray(), triangleIndices, name: "SlicesMesh-Vector3");
        }
        else
        {
            return;
        }

        Material? frontMaterial, backMaterial;

        if (_showVolumeMaterial)
        {
            if (_transferFunctionTexture == null && GpuDevice != null)
                CreateTransferFunctionTexture(GpuDevice);
            
            if (_volumeMaterial == null)
            {
                _volumeMaterial = new VolumeMaterial()
                {
                    SlicesTexture3D = _slicesGpuImage3D,
                    TransferFunctionTexture = _transferFunctionTexture
                };
            }

            _volumeMaterial.ValueClipThreshold = _valueClipThreshold;
            _volumeMaterial.GradientFactor     = _gradientFactor;
        
            frontMaterial = _volumeMaterial;
            backMaterial = null;
        }
        else
        {
            frontMaterial = new SolidColorMaterial(Colors.Green);
            backMaterial = new SolidColorMaterial(Colors.Red);
        }

        if (_meshModelNode == null)
        {
            _meshModelNode = new MeshModelNode(newSlicesMesh, "SlicesModelNode");
            _slicesGroupNode?.Add(_meshModelNode);
        }
        else
        {
            _meshModelNode.Mesh = newSlicesMesh;
        }

        _meshModelNode.Material = frontMaterial;
        _meshModelNode.BackMaterial = backMaterial;
        
        _slicesTriangleMesh = newSlicesMesh;
    }    
    
    private void DisposeScanResources(bool disposeSlicesGpuImage3D)
    {
        _linesGroupNode?.DisposeAllChildren(disposeMeshes: true, disposeMaterials: false, disposeTextures: false, runSceneCleanup: false);
        _slicesGroupNode?.Clear();
        
        if (_meshModelNode != null)
        {
            _meshModelNode.DisposeWithMeshAndMaterial();
            _meshModelNode = null;
            _volumeMaterial = null;
            _slicesTriangleMesh = null;
        }
        
        if (_transferFunctionTexture != null)
        {
            _transferFunctionTexture.Dispose();
            _transferFunctionTexture = null;
        }
        
        if (disposeSlicesGpuImage3D && _slicesGpuImage3D != null)
        {
            _slicesGpuImage3D.Dispose();
            _slicesGpuImage3D = null;
        }
    }
    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Top | PositionTypes.Right, addBorder: false);

        _scanImageIndexLabel = ui.CreateKeyValueLabel("Scan image index: ", () => _currentScanImageIndex.ToString());
        
        var sliderMax = _slicesCount > 0 ? (_slicesCount - 1): 100;
        _scanImageSlider = ui.CreateSlider(0, sliderMax, () => _currentScanImageIndex, newValue =>
            {
                ShowScanImage((int)newValue);
            }, 
            width: 150);


        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateRadioButtons(
            new string[] { "Head", "Bonsai" }, 
            (selectedIndex, selectedText) => LoadScanData(selectedIndex), 
            selectedItemIndex: 0);
        
        
        ui.AddSeparator();
        
        ui.CreateRadioButtons(new string[]
            {
                "Show slice lines (?):To better see the individual slices,\nreduce the 'Total slices count' to only a few slices and\nthen change the camera angle and the 'Start slice offset'.", 
                "Show slices mesh (?):To better see the individual slices,\nreduce the 'Total slices count' to only a few slices and\nthen change the camera angle and the 'Start slice offset'.", 
                "Show volume material"
            }, 
            (selectedIndex, selectedText) =>
            {
                _showSliceLines     = selectedIndex == 0;
                _showSliceMesh      = selectedIndex == 1;
                _showVolumeMaterial = selectedIndex == 2;

                // Dispose and clear all existing SceneNodes and Meshes and then generate them again
                DisposeScanResources(disposeSlicesGpuImage3D: false); // preserve loaded scan data
                UpdateCubeSlices();
            }, 
            selectedItemIndex: 2);
        
        
        ui.AddSeparator();

        var sliderWidth = 120;
        var keyTextWidth = 135;
        
        _softTissueOpacitySlider = ui.CreateSlider(0, 1, () => _softTissueOpacity, newValue =>
            {
                _softTissueOpacity = newValue;
                UpdateTransferFunctionTexture();
            }, 
            width: sliderWidth, keyText: "Soft tissue opacity:", keyTextWidth: keyTextWidth, formatShownValueFunc: sliderValue => sliderValue.ToString("F2")); 
        
        _hardTissueOpacitySlider = ui.CreateSlider(0, 1, () => _hardTissueOpacity, newValue =>
            {
                _hardTissueOpacity = newValue;
                UpdateTransferFunctionTexture();
            }, 
            width: sliderWidth, keyText: "Hard tissue opacity:", keyTextWidth: keyTextWidth, formatShownValueFunc: sliderValue => sliderValue.ToString("F2")); 
        
        _gradientFactorSlider = ui.CreateSlider(0, 1, () => _gradientFactor, newValue =>
            {
                _gradientFactor = newValue;
                if (_volumeMaterial != null)
                    _volumeMaterial.GradientFactor = newValue;
            }, 
            width: sliderWidth, keyText: "Gradient factor: (?):When bigger than 0, then for each pixel the shader computes the gradient of how\nthe scan value is changing compared to the value from the neighbouring pixels.\nThis slider defines how much the gradient influences the final color of the pixel.", 
            keyTextWidth: keyTextWidth, formatShownValueFunc: sliderValue => sliderValue.ToString("F2"));
        
        _valueClipThresholdSlider = ui.CreateSlider(0, 1, () => _valueClipThreshold, newValue =>
            {
                _valueClipThreshold = newValue;
                if (_volumeMaterial != null)
                    _volumeMaterial.ValueClipThreshold = newValue;
            }, 
            width: sliderWidth, keyText: "ValueClipThreshold: (?):Pixels where the value from the 3D slice (scan value) is below this value will be clipped (not shown)", 
            keyTextWidth: keyTextWidth, formatShownValueFunc: sliderValue => sliderValue.ToString("F2"));
        
        
        ui.AddSeparator();
        
        ui.CreateSlider(1, 500, () => _totalSlicesCount, newValue =>
            {
                _totalSlicesCount = (int)newValue;
                UpdateCubeSlices();
            }, 
            width: sliderWidth, keyText: "Total slices count:", keyTextWidth: keyTextWidth, formatShownValueFunc: sliderValue => sliderValue.ToString("F0"));
        
        ui.CreateSlider(0, 1, () => _shownSlicesPercent, newValue =>
            {
                _shownSlicesPercent = newValue;
                UpdateCubeSlices();
            }, 
            width: sliderWidth, keyText: "Shown slices percent:", keyTextWidth: keyTextWidth, formatShownValueFunc: sliderValue => (sliderValue * 100).ToString("F0") + "%");
        
        ui.CreateSlider(-1, 1, () => _startSliceOffset, newValue =>
            {
                _startSliceOffset = newValue;
                UpdateCubeSlices();
            }, 
            width: sliderWidth, keyText: "Start slice offset:", keyTextWidth: keyTextWidth, formatShownValueFunc: sliderValue => sliderValue.ToString("F2"));
        
        
        ui.AddSeparator();

        ui.CreateCheckBox("Rotate camera", true, isChecked =>
        {
            if (isChecked)
                targetPositionCamera?.StartRotation(-30);
            else
                targetPositionCamera?.StopRotation();
        });
        
        ui.CreateCheckBox("Disable updating slices", _disableUpdatingSlices, isChecked =>
        {
            _disableUpdatingSlices = isChecked;
            if (!isChecked)
                UpdateCubeSlices();
        });
    }    
}