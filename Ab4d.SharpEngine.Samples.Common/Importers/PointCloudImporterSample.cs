using System.Globalization;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

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

    private string? _importedFileName;
    private int _importedPixelsCount;

    private bool _isZAxisUp = true;
    private float _pixelsSize = 1;

    private float _boundsDiagonalLength;
    private PixelsNode? _pixelsNode;


    public PointCloudImporterSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        string fileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _initialFileName);
        LoadPointCloud(fileName);

        ShowCameraAxisPanel = true;
    }   

    protected void LoadPointCloud(string? fileName)
    {
        if (Scene == null || fileName == null)
            return;

        Scene.RootNode.Clear();

        _importedFileName = null;
        _importedPixelsCount = 0;


        Color4[]? positionColors;
        Vector3[]? positions;

        try
        {
            positions = LoadPositions(fileName, out positionColors);
        }
        catch (Exception ex)
        {
            _pixelsCountLabel?.UpdateValue();
            ShowErrorMessage("Error importing file:\n" + ex.Message);
            return;
        }

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
        
       
        // Create PixelsNode that will show the positions.
        // We can also pass the positionBounds that define the BoundingBox of the positions.
        // If this is not done, the BoundingBox is calculated by the SharpEngine by checking all the positions.

        // When using PixelColors, PixelColor is used as a mask (multiplied with each color in PixelColors)
        Color4 pixelColor = positionColors != null ? Colors.White : Colors.Black;

        _pixelsNode = new PixelsNode(positions, positionsBounds, pixelColor, _pixelsSize, "PixelsNode");

        if (positionColors != null)
            _pixelsNode.PixelColors = positionColors;

        Scene.RootNode.Add(_pixelsNode);


        _importedPixelsCount = positions.Length;
        _importedFileName = fileName;

        _pixelsCountLabel?.UpdateValue();
    }

    private Vector3[]? LoadPositions(string fileName, out Color4[]? positionColors)
    {
        Vector3[]? positions;
        
        fileName = FileUtils.FixDirectorySeparator(fileName);
        
        var fileExtension = System.IO.Path.GetExtension(fileName);
        bool swapYZCoordinates = _isZAxisUp;

        // Use PlyPointCloudReader to read .ply files
        // PlyPointCloudReader is available with full source code in the Ab4d.SharpEngine.Samples.Common project
        // in the Utils folder so you can change it to your needs.
        if (fileExtension.Equals(".ply", StringComparison.OrdinalIgnoreCase))
        {
            var plyPointCloudReader = new PlyPointCloudReader()
            {
                SwapYZCoordinates = swapYZCoordinates
            };

            positions = plyPointCloudReader.ReadPointCloud(fileName);

            positionColors = plyPointCloudReader.PixelColors;
        }
        else if (fileExtension.Equals(".xyz", StringComparison.OrdinalIgnoreCase))
        {
            positions = LoadXyzFile(fileName, out positionColors);
        }
        else
        {
            ShowErrorMessage($"Unsupported file extension '{fileExtension}'. Only .ply and .xyz file extensions are currently supported.");
            positions = null;
            positionColors = null;
        }

        return positions;
    }

    // Load xyz files that have x,y and z value separated by tab ('\t')
    // It can also read position colors that are written after z value: color is written as red, green and blue separated by tab ('\t').
    // Note that the code below is from the performance and memory usage not optimal because it uses File.ReadAllLines.
    // This requires reading the whole file and storing it into memory. It would be better to use stream reader and read line by line.
    private Vector3[] LoadXyzFile(string fileName, out Color4[]? positionColors)
    {
        var fileLines = System.IO.File.ReadAllLines(fileName);

        int count = fileLines.Length;

        var positions = new Vector3[count];

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

        return positions;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right, isVertical: true);

        _pixelsCountLabel = ui.CreateKeyValueLabel("Pixels count:", () => $"{_importedPixelsCount:#,##0}");

        ui.AddSeparator();

        

        // If GpuDevice is already defined, then get the minimum pixel size that is supported by the current GPU.
        // For example, for NVIDIA this is usually 1, for Intel 0.125.
        // Note that the PointSizeRange[1] defines the max pixel size (this is much usually much bigger than in our sample, for example for NVIDIA: 2047.9375, for Intel: 255.875)
        float minPixelSize;
        if (GpuDevice != null)
            minPixelSize = GpuDevice.PhysicalDeviceDetails.PhysicalDeviceLimitsEx.PointSizeRange[0];
        else
            minPixelSize = 1;

        ui.CreateSlider(minPixelSize, maxValue: 5f, 
            getValueFunc: () => _pixelsSize, 
            setValueAction: newValue =>
            {
                _pixelsSize = newValue;
                if (_pixelsNode != null)
                    _pixelsNode.PixelSize = newValue;
            },
            width: 100,
            keyText: "PixelSize:",
            formatShownValueFunc: sliderValue => sliderValue.ToString("F1"));

        ui.AddSeparator();

        ui.CreateCheckBox("Is Z axis up", _isZAxisUp, isChecked =>
        {
            _isZAxisUp = isChecked;

            if (_importedFileName != null)
                LoadPointCloud(_importedFileName); // Load again
        });


        // Try to register for file drag-and-drop
        bool isDragAndDropSupported = ui.RegisterFileDropped(".*", LoadPointCloud);

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
                CameraAxisPanel.Position = new Vector2(10, 80); // CameraAxisPanel is aligned to BottomLeft, so we only need to increase the y position from 10 to 80
        }
    }
}
