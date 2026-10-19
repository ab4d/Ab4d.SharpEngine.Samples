using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Globalization;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Importers;

// The PlyPointCloudReader and PlyPointCloudWriter classes are defined with full source code in the Utils folder.
// LoadXyzFile method is defined below.

public class PointCloudImporterSample : CommonSample
{
    public override string Title => "Point-cloud importer from .ply and .xyz files";
    
    private string _subtitle = ""; // empty by default; can be set to "Drag and drop .ply or .xyz file here to open it."
    public override string? Subtitle => _subtitle;

    private readonly string _initialFileName = @"Resources\PointClouds\14 Ladybrook Road 10 - cropped.ply";

    private ICommonSampleUIElement? _textBoxElement;
    private ICommonSampleUIElement? _pixelsCountLabel;
    private ICommonSampleUIElement? _loadingLabel;

    private string? _importedFileName;
    private int _importedPixelsCount;

    private bool _isWorldSize = false;
    private bool _showCircularPixels = false;
    private bool _isZAxisUp = true;
    private float _pixelsSize = 2;
    private float _pixelSizeFactor = 1;

    private Vector2? _savedAxisPanelPosition;

    private float _boundsDiagonalLength;
    private PixelsNode? _pixelsNode;
    private GpuImage? _whiteCircleGpuImage;
    
    public PointCloudImporterSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
#if VULKAN
        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _initialFileName);
#else
        string fileName = _initialFileName;
#endif

        ShowCameraAxisPanel = true;

        await LoadPointCloud(fileName);

        // We also load a texture with white circle that can be used to render circular pixels.
        // Because the circle is white it can be multiplied by the pixel color to get the desired color.
        // NOTE:
        // We do not generate the mip-maps for this texture. We will also use a ClampNoInterpolation sampler.
        // This is needed to prevent darkening the at the edges of the circle that is produces by using semi-transparent pixels 
        // that are generated when producing mip-maps and when using the default anisotropic sampler.
        var textureFileName = base.GetCommonTexturePath("white-circle-64x64-noaa.png");
        _whiteCircleGpuImage ??= await TextureLoader.CreateTextureAsync(textureFileName, scene, generateMipMaps: false);
    }

#if VULKAN // CameraAxisPanel is currently not supported in WebGL
    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (CameraAxisPanel != null)
        {
            if (_savedAxisPanelPosition != null)
                CameraAxisPanel.Position = _savedAxisPanelPosition.Value;

            if (_isZAxisUp)
            {
                // Reset to Y up axis
                CameraAxisPanel.CustomizeAxes(xAxisVector: new Vector3(1, 0, 0), xAxisColor: Colors.Red,
                                              yAxisVector: new Vector3(0, 1, 0), yAxisColor: Colors.Green,
                                              zAxisVector: new Vector3(0, 0, 1), zAxisColor: Colors.Blue);
            }
        }

        base.OnDisposed();
    }
#endif

    protected async Task LoadPointCloud(string? fileName)
    {
        if (Scene == null || fileName == null || this.IsDisposed)
            return;

        Scene.RootNode.Clear();

        _importedFileName = null;
        _importedPixelsCount = 0;


        Color4[]? positionColors;
        Vector3[]? positions;

        try
        {
            (positions, positionColors) = await LoadPositions(fileName);
        }
        catch (Exception ex)
        {
            _pixelsCountLabel?.UpdateValue();
            ShowErrorMessage("Error importing file:\n" + ex.Message);
            return;
        }
        
        if (this.IsDisposed) // Exit when user changes the sample before async operation was completed
            return;

        if (positions == null)
        {
            _pixelsCountLabel?.UpdateValue();
            return;
        }

        var positionsBounds = BoundingBox.FromPoints(positions);
        _boundsDiagonalLength = positionsBounds.GetDiagonalLength();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = positionsBounds.GetCenterPosition();
            targetPositionCamera.Distance = _boundsDiagonalLength * 1.8f;
        }
        
        _pixelSizeFactor = _boundsDiagonalLength / 1000f; // When using WorldSize pixel size, then set the Maximum size based on the size of the scene (max value is 1/200 of the _boundsDiagonalLength)
        var usedPixelSize = _isWorldSize ? _pixelsSize * _pixelSizeFactor : _pixelsSize; // When using WorldSize, then scale pixel size based on the size of loaded point cloud

        // Create PixelsNode that will show the positions.
        // We can also pass the positionBounds that define the BoundingBox of the positions.
        // If this is not done, the BoundingBox is calculated by the SharpEngine by checking all the positions.

        // When using PixelColors, PixelColor is used as a mask (multiplied with each color in PixelColors)
        Color4 pixelColor = positionColors != null ? Colors.White : Colors.Black;

        _pixelsNode = new PixelsNode(positions, positionsBounds, pixelColor, usedPixelSize, "PixelsNode")
        {
            IsWorldSize = _isWorldSize
        };

        if (positionColors != null)
            _pixelsNode.PixelColors = positionColors;

        Scene.RootNode.Add(_pixelsNode);


        _importedPixelsCount = positions.Length;
        _importedFileName = fileName;

        _pixelsCountLabel?.UpdateValue();

        _loadingLabel?.SetIsVisible(false);

#if VULKAN
        if (CameraAxisPanel != null)
        {
            if (_isZAxisUp)
            {
                // Show Z up axis
                CameraAxisPanel.CustomizeAxes(xAxisVector: new Vector3(1, 0, 0), xAxisColor: Colors.Red,
                                              yAxisVector: new Vector3(0, 0, 1), yAxisColor: Colors.Green,
                                              zAxisVector: new Vector3(0, 1, 0), zAxisColor: Colors.Blue);
            }
            else
            {
                // Reset to Y up axis
                CameraAxisPanel.CustomizeAxes(xAxisVector: new Vector3(1, 0, 0), xAxisColor: Colors.Red,
                                              yAxisVector: new Vector3(0, 1, 0), yAxisColor: Colors.Green,
                                              zAxisVector: new Vector3(0, 0, 1), zAxisColor: Colors.Blue);
            }
        }
#endif
    }

    private async Task<(Vector3[]? positions, Color4[]? positionColors)> LoadPositions(string fileName)
    {
        Vector3[]? positions;
        Color4[]? positionColors;
        
        fileName = FileUtils.FixDirectorySeparator(fileName);
        
        var fileExtension = System.IO.Path.GetExtension(fileName);


        // Use PlyPointCloudReader to read .ply files
        // PlyPointCloudReader is available with full source code in the Ab4d.SharpEngine.Samples.Common project
        // in the Utils folder so you can change it to your needs.
        if (fileExtension.Equals(".ply", StringComparison.OrdinalIgnoreCase))
        {
            var plyPointCloudReader = new PlyPointCloudReader()
            {
                SwapYZCoordinates = _isZAxisUp
            };

#if WEB_GL
            if (GpuDevice == null)
                return (null, null);

            byte[] fileBytes = await GpuDevice.CanvasInterop.LoadBinaryFileAsync(fileName);
            var memoryStream = new MemoryStream(fileBytes);

            positions = plyPointCloudReader.ReadPointCloud(memoryStream);
#else
            positions = await plyPointCloudReader.ReadPointCloudAsync(fileName);
#endif

            positionColors = plyPointCloudReader.PixelColors;
        }
        else if (fileExtension.Equals(".xyz", StringComparison.OrdinalIgnoreCase))
        {
            (positions, positionColors) = await LoadXyzFileAsync(fileName);
        }
        else
        {
            ShowErrorMessage($"Unsupported file extension '{fileExtension}'. Only .ply and .xyz file extensions are currently supported.");
            positions = null;
            positionColors = null;
        }

        return (positions, positionColors);
    }

    // Load xyz files that have x,y and z value separated by tab ('\t')
    // It can also read position colors that are written after z value: color is written as red, green and blue separated by tab ('\t').
    // Note that the code below is from the performance and memory usage not optimal because it uses File.ReadAllLines.
    // This requires reading the whole file and storing it into memory. It would be better to use stream reader and read line by line.
    private async Task<(Vector3[] positions, Color4[]? positionColors)> LoadXyzFileAsync(string fileName)
    {
        var fileLines = await System.IO.File.ReadAllLinesAsync(fileName);

        int count = fileLines.Length;

        var positions = new Vector3[count];
        Color4[]? positionColors;

        var oneLine = fileLines[0];
        var oneLineParts = oneLine.Split('\t');

        if (oneLineParts.Length == 6)
        {
            // we also have color data
            positionColors = new Color4[count];
        }
        else
        {
            positionColors = null;
        }

        for (int i = 0; i < count; i++)
        {
            oneLine = fileLines[i];
            oneLineParts = oneLine.Split('\t');

            float x = float.Parse(oneLineParts[0], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
            float y = float.Parse(oneLineParts[1], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
            float z = float.Parse(oneLineParts[2], NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

            // swap z and y; we assume that SwapYZCoordinates is true (if not, we fix that later - see code below)
            positions[i] = new Vector3(x, z, y); 

            if (positionColors != null)
            {
                int red   = int.Parse(oneLineParts[3]);
                int green = int.Parse(oneLineParts[4]);
                int blue  = int.Parse(oneLineParts[5]);

                positionColors[i] = new Color4((float)red / 255f, (float)green / 255f, (float)blue / 255f, 1);
            }
        }

        return (positions, positionColors);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        if (_pixelsNode == null) // if point cloud is not yet loaded show "Loading point cloud ..."
        {
            ui.CreateStackPanel(PositionTypes.Center, addBorder: false);
            _loadingLabel = ui.CreateLabel("Loading point cloud ...");
        }


        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, isVertical: true);

        _pixelsCountLabel = ui.CreateKeyValueLabel("Pixels count:", () => $"{_importedPixelsCount:#,##0}");

        ui.AddSeparator();


        ui.CreateSlider(minValue: 0.1f, maxValue: 5f, 
            getValueFunc: () => _pixelsSize, 
            setValueAction: newValue =>
            {
                _pixelsSize = newValue;
                var usedPixelSize = _isWorldSize ? _pixelsSize * _pixelSizeFactor : _pixelsSize; // When using WorldSize, then scale pixel size based on the size of loaded point cloud
                if (_pixelsNode != null)
                    _pixelsNode.PixelSize = usedPixelSize;
            },
            width: 100,
            keyText: "PixelSize:",
            formatShownValueFunc: sliderValue => sliderValue.ToString("F1"));

        ui.CreateCheckBox("IsWorldSize (?):When checked then the PixelSize is specified in 3D world coordinates\n(when using perspective camera, the size of the pixel is smaller when it is farther away from the camera).\n\nWhen unchecked, then the size is defined in screen coordinates\n(the size of the pixel is always the same regardless of the distance from the camera).", 
            _isWorldSize, (isChecked) =>
            {
                _isWorldSize = isChecked;
                var usedPixelSize = _isWorldSize ? _pixelsSize * _pixelSizeFactor : _pixelsSize; // When using WorldSize, then scale pixel size based on the size of loaded point cloud

                if (_pixelsNode != null)
                {
                    _pixelsNode.IsWorldSize = _isWorldSize;
                    _pixelsNode.PixelSize = usedPixelSize;
                }
            });
        
        
        ui.AddSeparator();

        
        ui.CreateCheckBox("Is circular pixel", 
            _showCircularPixels, (isChecked) =>
            {
                _showCircularPixels = isChecked;

                if (_showCircularPixels)
                {
                    if (_whiteCircleGpuImage != null)
                    {
                        // To render texture for each pixel, we call the SetTexture method.
                        // In this sample we also need to adjust the alphaClipThreshold from the default value of 0.5 to 0.1f,
                        // because when using transparent colors, the alpha value of the pixels is set to 0.2f and the default alphaClipThreshold of 0.5 would not render any pixels.
                        _pixelsNode?.SetTexture(_whiteCircleGpuImage, CommonSamplerTypes.ClampNoInterpolation, alphaClipThreshold: 0.1f);
                    }
                }
                else
                {
                    _pixelsNode?.RemoveTexture();
                }
            });

        ui.AddSeparator();


#if VULKAN
        ui.CreateCheckBox("Is Z axis up", _isZAxisUp, isChecked =>
        {
            _isZAxisUp = isChecked;

            if (_importedFileName != null)
                _ = LoadPointCloud(_importedFileName); // Load again
        });


        // Try to register for file drag-and-drop
        bool isDragAndDropSupported = ui.RegisterFileDropped(".*", (fileName) => _ = LoadPointCloud(fileName));

        if (isDragAndDropSupported)
        {
            _subtitle += "Drag and drop .ply or .xyz file here to open it.";
        }
        else
        {
            // If drag and drop is not supported, then show TextBox so user can enter file name to import

            ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Left, isVertical: false);

            ui.CreateLabel("FileName:");
            _textBoxElement = ui.CreateTextBox(width: 400, initialText: FileUtils.FixDirectorySeparator(_initialFileName));

            ui.CreateButton("Load", () =>
            {
                LoadPointCloud(_textBoxElement.GetText());
            });

            // When File name TextBox is shown in the bottom left corner, then we need to lift the CameraAxisPanel above it
            if (CameraAxisPanel != null)
            {
                _savedAxisPanelPosition = CameraAxisPanel.Position;
                CameraAxisPanel.Position = new Vector2(10, 80); // CameraAxisPanel is aligned to BottomLeft, so we only need to increase the y position from 10 to 80
            }
        }
#endif
    }
}
