using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class VectorTextSample : CommonSample
{
    public override string Title => "Vector text from TrueType files (.ttf)";

    private string _subtitle = "Create text meshes and outlines from font glyphs.";
    public override string? Subtitle => _subtitle;

    //public override string Subtitle => "SharpEngine can render text by using BitmapTextCreator that can render bitmap fonts.";

    private string _selectedFontFileName;

    private string _textToShow;
    
    private string[]? _allFontFiles;

    private Vector3 _textPosition = new Vector3(0, 0, 0);
    private TextPositionTypes _selectedPositionType = TextPositionTypes.Baseline;
    private Vector3 _textDirection = new Vector3(1, 0, 0);
    private Vector3 _upDirection = new Vector3(0, 1, 0);
    private float _lineHeight = 1.0f;
    private int _bezierCurveSegmentsCount = 8;

    private float _fontSize = 50;
    
    private Vector2 _textSize;

    private Material? _textMaterial;
    private StandardMesh? _textMesh;
    private MeshModelNode? _textMeshModelNode;

    private ICommonSampleUIElement? _textSizeLabel;
    private ICommonSampleUIElement? _meshInfoLabel;
    private ICommonSampleUIElement? _textBoxElement;

    private GroupNode? _rootTextNode;
    private RectangleNode? _textBoundingRectangleNode;
    private WireCrossNode? _textPositionCrossNode;

    private VectorFontFactory? _currentVectorFontFactory;

    private Dictionary<string, VectorFontFactory> _allVectorFontFactories = new ();
    

    public VectorTextSample(ICommonSamplesContext context)
        : base(context)
    {
        _selectedFontFileName = "Roboto-Black.ttf";
        _textToShow = CreateAllCharsText(from: 33, to: 127, lineLength: 16); // ASCII chars from 33 - 127

        ShowCameraAxisPanel = true;
    }

    private void LoadFont(string fontFileName, bool recreateText = false)
    {
        var fontName = Path.GetFileNameWithoutExtension(fontFileName); // remove ".ttf"
        
        if (_allVectorFontFactories.TryGetValue(fontName, out _currentVectorFontFactory))
        {
            if (recreateText)
                RecreateText();

            return;
        }


        if (!System.IO.Path.IsPathRooted(fontFileName))
            fontFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/TrueTypeFonts/", fontFileName);


        // Load the font file
        // This method can be called multiple times when the same fontName and fontFilePath is used.
        // You can also check if font is loaded by calling:
        // bool isLoaded = TrueTypeFontLoader.Instance.IsFontLoaded(fontName);

        try
        {
            TrueTypeFontLoader.Instance.LoadFontFile(fontFileName, fontName);
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error loading font:\n" + ex.Message);
            return;
        }

        ClearErrorMessage();

        // After font is loaded, we can create an instance of VectorFontFactory by passing the fontName to constructor
        _currentVectorFontFactory = new VectorFontFactory(fontName);

        _allVectorFontFactories.Add(fontName, _currentVectorFontFactory);

        if (recreateText)
            RecreateText();
    }

    protected override void OnCreateScene(Scene scene)
    {
        LoadFont(_selectedFontFileName);

        _rootTextNode = new GroupNode("RootTextNode");
        scene.RootNode.Add(_rootTextNode);

        _textMaterial = new SolidColorMaterial(Colors.Orange);

        RecreateText();

        
        _textPositionCrossNode = new WireCrossNode(_textPosition, lineColor: Colors.Red, lineThickness: 3, lineLength: 30);
        scene.RootNode.Add(_textPositionCrossNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(250, -140, 0);
            targetPositionCamera.Heading = 50;
            targetPositionCamera.Attitude = -5;
            targetPositionCamera.Distance = 1200;
        }

        ShowCameraAxisPanel = true;
    }

    private void RecreateText()
    {
        if (_rootTextNode == null || _currentVectorFontFactory == null)
            return;

        _rootTextNode.Clear();

        var textPosition = new Vector3(0, 0, 0);

        _currentVectorFontFactory.LineHeight = _lineHeight;

        // BezierCurveSegmentsCount specified how many segments (individual straight lines) each bezier curve from the character glyph is converted.
        // Smaller values produce smaller meshes or less outline positions (are faster to render),
        // bigger values produce more accurate fonts but are slower to render.
        _currentVectorFontFactory.BezierCurveSegmentsCount = _bezierCurveSegmentsCount;

        // You can also set SpaceSize and TabSize (set to default values in the commented code below):
        //_currentVectorFontFactory.SpaceSize = 0.333f;
        //_currentVectorFontFactory.TabSize = 4;


        _textMesh = _currentVectorFontFactory.CreateTextMesh(_textToShow,
                                                             textPosition,
                                                             _selectedPositionType, // NOTE that this takes TextPositionTypes that also defines the Baseline value
                                                             textDirection: _textDirection,
                                                             upDirection: _upDirection,
                                                             fontSize: _fontSize);

        if (_textMesh != null)
        {
            _textMeshModelNode = new MeshModelNode(_textMesh, _textMaterial)
            {
                BackMaterial = _textMaterial
            };

            _rootTextNode.Add(_textMeshModelNode);
        }

        // Get the size of the text
        _textSize = _currentVectorFontFactory.GetTextSize(_textToShow, _fontSize);
        
        _textSizeLabel?.UpdateValue();
        _meshInfoLabel?.UpdateValue(); 

        // Get the bounding rectangle of the text
        (Vector3 topLeftPosition, Vector2 textSize) = _currentVectorFontFactory.GetBoundingRectangle(_textToShow, textPosition, _selectedPositionType, _textDirection, _upDirection, _fontSize);

        _textBoundingRectangleNode = new RectangleNode(topLeftPosition, PositionTypes.TopLeft, textSize, _textDirection, _upDirection, Colors.Black, lineThickness: 1, "TextBoundingRectangle");
        _rootTextNode.Add(_textBoundingRectangleNode);
    }
    
    private string CreateAllCharsText(int from = 32, int to = 128, int lineLength = 16)
    {
        string text = "";
        for (int i = from; i <= to; i++)
        {
            text += (char)i;
            if (i > from && (i % lineLength) == 0)
                text += "\r\n";
        }

        return text;
    }

    private string[] CollectAvailableFonts()
    {
        if (_allFontFiles == null)
        {
            var localFontFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/TrueTypeFonts/");
            var fontsFiles = Directory.GetFiles(localFontFolder, "*.ttf", SearchOption.TopDirectoryOnly).ToList();

            // Commented reading system fonts because on Windows all font files have access denied
            //if (OperatingSystem.IsWindows())
            //{
            //    var systemFontsFolder = @"C:\Windows\Fonts\";
            //    if (System.IO.Directory.Exists(systemFontsFolder))
            //    {
            //        var allSystemFontFiles = Directory.GetFiles(systemFontsFolder, "*.ttf", SearchOption.TopDirectoryOnly);
            //        fontsFiles.AddRange(allSystemFontFiles);
            //    }
            //}

            _allFontFiles = fontsFiles.ToArray();
        }

        return _allFontFiles;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        // Try to register for file drag-and-drop
        bool isDragAndDropSupported = ui.RegisterFileDropped(".ttf", (fileName) => LoadFont(fileName, recreateText: true));


        //ui.CreateLabel("Font:", isHeader: true);

        var allFontFiles = CollectAvailableFonts();
        var fontNames = allFontFiles.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();

        ui.CreateRadioButtons(
            fontNames,
            (selectedIndex, selectedText) =>
            {
                _selectedFontFileName = allFontFiles[selectedIndex];
                LoadFont(_selectedFontFileName, recreateText: true);
            },
            selectedItemIndex: 0);

        if (isDragAndDropSupported)
            ui.CreateLabel("or drag & drop .ttf file to load it").SetStyle("italic");


        ui.AddSeparator();

        ui.CreateTextBox(width: 220, height: 115, 
            initialText: _textToShow,
            textChangedAction: (newText) =>
            {
                _textToShow = newText;
                RecreateText();
            });


        ui.AddSeparator();

        ui.CreateCheckBox("Position: (0, 0, 0)", 
            true, 
            isChecked =>
            {
                if (_textPositionCrossNode != null)
                    _textPositionCrossNode.Visibility = isChecked ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
            }).SetColor(Colors.Red);


        var allPositionTypesInSample = new TextPositionTypes[]
        {
            TextPositionTypes.Baseline, // TextPositionTypes adds Baseline to the position types defined in the PositionTypes enum

            TextPositionTypes.TopLeft,
            TextPositionTypes.Top,
            TextPositionTypes.TopRight,
            
            TextPositionTypes.Left,
            TextPositionTypes.Center,
            TextPositionTypes.Right,
            
            TextPositionTypes.BottomLeft,
            TextPositionTypes.Bottom,
            TextPositionTypes.BottomRight
        };

        ui.CreateComboBox(allPositionTypesInSample.Select(
            p => p.ToString()).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                _selectedPositionType = allPositionTypesInSample[selectedIndex];
                RecreateText();
            }, 
            0, 130, "PositionType:", 0);

        ui.CreateLabel("TextDirection: (1, 0, 0)");
        ui.CreateLabel("UpDirection: (0, 1, 0)");

        var fontSizes = new int[] { 8, 10, 20, 30, 40, 50, 100, 200 };
        ui.CreateComboBox(fontSizes.Select(f => f.ToString()).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                _fontSize = Int32.Parse(selectedText!);
                RecreateText();
            }, selectedItemIndex: Array.IndexOf(fontSizes, (int)_fontSize), 
            width: 80, 
            keyText: "Font size:");


        ui.AddSeparator();

        ui.CreateSlider(0.8f, 2, () => _lineHeight, 
            newValue =>
            {
                _lineHeight = newValue;
                RecreateText();
            }, width: 90, 
            keyText: "LineHeight:", 
            formatShownValueFunc: lineHeight => $"{lineHeight:F1} em");

        ui.CreateLabel("BezierCurveSegmentsCount: (?):Specifies into how many segments (individual straight lines)\neach bezier curve from the character glyph is converted.\nSmaller values produce smaller meshes or less outline positions (are faster to render),\nbigger values produce more accurate fonts but are slower to render.\nSee 'Text mesh info' below to see the number of triangles used to define the text.");
        ui.CreateSlider(1, 12, () => _bezierCurveSegmentsCount, 
            newValue =>
            {
                if (_bezierCurveSegmentsCount != (int)newValue)
                {
                    _bezierCurveSegmentsCount = (int)newValue;
                    RecreateText();
                }
            }, width: 200, 
            formatShownValueFunc: newValue => $"{newValue:F0}");


        ui.AddSeparator();

        
        _meshInfoLabel = ui.CreateKeyValueLabel("Text mesh info: ", () =>
        {
            if (_textMesh == null)
                return "";

            return $"{(_textMesh.IndexCount / 3):#,##0} triangles";
        });

        _textSizeLabel = ui.CreateKeyValueLabel("Text world size: ", () => $"{_textSize.X:F0} x {_textSize.Y:F0}");
                
        ui.CreateCheckBox("Show bounding rectangle", 
            true, 
            isChecked =>
            {
                if (_textBoundingRectangleNode != null)
                    _textBoundingRectangleNode.Visibility = isChecked ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
            });


        ui.AddSeparator();

        ui.CreateCheckBox("IsSolidColorMaterial (?):When checked (by default) then text is rendered with a solid color regardless of lights;\nwhen unchecked then rotate tha camera so the text is at steep angle and\nsee that text is shaded based on the angle to the CameraLight.", 
            true, 
            isChecked =>
            {
                if (isChecked)
                    _textMaterial = new SolidColorMaterial(Colors.Orange);
                else
                    _textMaterial = StandardMaterials.Orange;

                if (_textMeshModelNode != null)
                {
                    _textMeshModelNode.Material = _textMaterial;
                    _textMeshModelNode.BackMaterial = _textMaterial;
                }
                RecreateText();
            });

        ui.AddSeparator();


        if (isDragAndDropSupported)
        {
            _subtitle += "\nDrag and drop .ttf file here to open it.";
        }
        else
        {
            // If drag and drop is not supported, then show TextBox so user can enter file name to import
            ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Left, isVertical: false);

            ui.CreateLabel("Font file path (.ttf):");
            _textBoxElement = ui.CreateTextBox(width: 500, initialText: "");

            ui.CreateButton("Load", () =>
            {
                LoadFont(_textBoxElement.GetText() ?? "", recreateText: true);
            });

            // When File name TextBox is shown in the bottom left corner, then we need to lift the CameraAxisPanel above it
            if (CameraAxisPanel != null)
                CameraAxisPanel.Position = new Vector2(10, 80); // CameraAxisPanel is aligned to BottomLeft, so we only need to increase the y position from 10 to 80
        }
    }
}