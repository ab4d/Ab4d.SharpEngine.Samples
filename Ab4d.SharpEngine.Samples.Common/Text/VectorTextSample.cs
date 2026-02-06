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

namespace Ab4d.SharpEngine.Samples.Common.Text;

public class VectorTextSample : CommonSample
{
    public override string Title => "Vector text from TrueType files (.ttf)";

    private string _subtitle = "Create text meshes and outlines from font glyphs.";
    public override string? Subtitle => _subtitle;

    //public override string Subtitle => "SharpEngine can render text by using BitmapTextCreator that can render bitmap fonts.";

    private string _selectedFontFileName;

    private string _textToShow;
    
    private string[]? _allFontFiles;
    
    
    private bool _showSolidTextMesh = true;
    private bool _showTextOutlines = false;
    private bool _showIndividualCharacterMeshes = false;
    private bool _isSolidColorMaterial = true;
    private bool _showBoundingRectangle = true;

    private Vector3 _textPosition = new Vector3(0, 0, 0);
    private TextPositionTypes _selectedPositionType = TextPositionTypes.Baseline;
    private Vector3 _textDirection = new Vector3(1, 0, 0);
    private Vector3 _upDirection = new Vector3(0, 1, 0);
    private TextAlignment _textAlignment = TextAlignment.Left;
    private float _lineHeight = 1.0f;
    private float _charSpacing = 0f;
    private float _fontStretch = 1.0f;

#if WEB_GL
    private int _bezierCurveSegmentsCount = 3; // reduce the number of font segments for bezier curvers to improve performance of triangulation in the browser
#else
    private int _bezierCurveSegmentsCount = 8;
#endif

    private float _fontSize = 50;
    
    private string? _textInfoString;

    private ICommonSampleUIElement? _infoLabel;
    private ICommonSampleUIElement? _textBoxElement;

    private GroupNode? _rootTextNode;
    private RectangleNode? _textBoundingRectangleNode;
    private WireCrossNode? _textPositionCrossNode;

    private VectorFontFactory? _currentVectorFontFactory;

    private Dictionary<string, VectorFontFactory> _allVectorFontFactories = new ();
    

    public VectorTextSample(ICommonSamplesContext context)
        : base(context)
    {
        var allFontFiles = CollectAvailableFonts();
        _selectedFontFileName = allFontFiles[0];

        _textToShow = CreateAllCharsText(from: 33, to: 126, lineLength: 16); // ASCII chars from 33 - 126

        ShowCameraAxisPanel = true;
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        await LoadFont(_selectedFontFileName);

        _rootTextNode = new GroupNode("RootTextNode");
        scene.RootNode.Add(_rootTextNode);

        RecreateText();

        
        _textPositionCrossNode = new WireCrossNode(_textPosition, lineColor: Colors.Red, lineThickness: 3, lineLength: 30);
        scene.RootNode.Add(_textPositionCrossNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(300, -140, 0);
            targetPositionCamera.Heading = 50;
            targetPositionCamera.Attitude = -5;
            targetPositionCamera.Distance = 1200;
        }

        ShowCameraAxisPanel = true;
    }
    
    private async Task LoadFont(string fontFileName, bool recreateText = false)
    {
        if (Scene == null || Scene.GpuDevice == null)
            return;

        var fontName = Path.GetFileNameWithoutExtension(fontFileName); // remove ".ttf"
        
        if (_allVectorFontFactories.TryGetValue(fontName, out _currentVectorFontFactory))
        {
            if (recreateText)
                RecreateText();

            return;
        }

#if VULKAN
        if (!Path.IsPathRooted(fontFileName))
            fontFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/TrueTypeFonts/", fontFileName);
#endif


        // Load the font file
        // This method can be called multiple times when the same fontName and fontFilePath is used.
        // You can also check if font is loaded by calling:
        // bool isLoaded = TrueTypeFontLoader.Instance.IsFontLoaded(fontName);

        try
        {
#if VULKAN
            await TrueTypeFontLoader.Instance.LoadFontFileAsync(fontFileName, fontName);

            // You can also use the non-async version of LoadFontFile method that read the font file in the main thread:
            TrueTypeFontLoader.Instance.LoadFontFile(fontFileName, fontName);

#elif WEB_GL
            await TrueTypeFontLoader.Instance.LoadFontFileAsync(fontFileName, fontName, Scene.GpuDevice.CanvasInterop);
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error loading ttf font file: " + ex.Message);
            return;
        }


        ClearErrorMessage();

        // After font is loaded, we can create an instance of VectorFontFactory by passing the fontName to constructor
        _currentVectorFontFactory = new VectorFontFactory(fontName);

        _allVectorFontFactories.Add(fontName, _currentVectorFontFactory);

        if (recreateText)
            RecreateText();
    }


    private void RecreateText()
    {
        if (_rootTextNode == null || _currentVectorFontFactory == null)
            return;

        _rootTextNode.Clear();

        var textPosition = new Vector3(0, 0, 0);

        _currentVectorFontFactory.LineHeight = _lineHeight;
        _currentVectorFontFactory.CharSpacing = _charSpacing;
        _currentVectorFontFactory.FontStretch = _fontStretch;

        // BezierCurveSegmentsCount specified how many segments (individual straight lines) each bezier curve from the character glyph is converted.
        // Smaller values produce smaller meshes or less outline positions (are faster to render),
        // bigger values produce more accurate fonts but are slower to render.
        _currentVectorFontFactory.BezierCurveSegmentsCount = _bezierCurveSegmentsCount;

        // You can also set SpaceSize and TabSize (set to default values in the commented code below):
        //_currentVectorFontFactory.SpaceSize = 0.333f;
        //_currentVectorFontFactory.TabSize = 4;


        _textInfoString = "";

        if (_showSolidTextMesh)
        {
            if (!_showIndividualCharacterMeshes)
            {
                // Create a single text mesh for the whole text
                // This is the most common use case for showing text
                var textMesh = _currentVectorFontFactory.CreateTextMesh(_textToShow,
                                                                        textPosition,
                                                                        _selectedPositionType, // NOTE that this takes TextPositionTypes that also defines the Baseline value
                                                                        textDirection: _textDirection,
                                                                        upDirection: _upDirection,
                                                                        fontSize: _fontSize,
                                                                        textAlignment: _textAlignment);

                if (textMesh != null)
                {
                    Material usedMaterial = _isSolidColorMaterial ? new SolidColorMaterial(Colors.Orange) : StandardMaterials.Orange;

                    var textMeshModelNode = new MeshModelNode(textMesh, usedMaterial)
                    {
                        BackMaterial = usedMaterial // Make text visible from both sides
                    };

                    _rootTextNode.Add(textMeshModelNode);

                    _textInfoString = $"Text mesh: {textMesh.TrianglesCount:#,##0} triangles";
                }
            }
            else
            {
                // Generate individual meshes and MeshModelNode for each character
                List<(int, char, StandardMesh)> individualMeshes = _currentVectorFontFactory.CreateIndividualTextMeshes(_textToShow,
                                                                                                                        textPosition,
                                                                                                                        _selectedPositionType, // NOTE that this takes TextPositionTypes that also defines the Baseline value
                                                                                                                        textDirection: _textDirection,
                                                                                                                        upDirection: _upDirection,
                                                                                                                        fontSize: _fontSize,
                                                                                                                        textAlignment: _textAlignment);

                var colorHue = 0;
                int trianglesCount = 0;

                foreach (var (charIndex, character, mesh) in individualMeshes)
                {
                    var color = Color3.FromHsl(colorHue);
                    colorHue += 33;

                    Material usedMaterial = _isSolidColorMaterial ? new SolidColorMaterial(color) : new StandardMaterial(color);

                    var textMeshModelNode = new MeshModelNode(mesh, usedMaterial, name: $"Mesh_{character}")
                    {
                        BackMaterial = usedMaterial // Make text visible from both sides
                    };

                    _rootTextNode.Add(textMeshModelNode);

                    trianglesCount += mesh.TrianglesCount;
                }

                _textInfoString = $"{individualMeshes.Count} text meshes: {trianglesCount:#,##0} triangles";
            }
        }
        else
        {
            _textInfoString = "Text mesh: /";
        }


        if (_showTextOutlines)
        {
            var textOutlines = _currentVectorFontFactory.CreateTextOutlinePositions(_textToShow,
                                                                                    textPosition,
                                                                                    _selectedPositionType, // NOTE that this takes TextPositionTypes that also defines the Baseline value
                                                                                    textDirection: _textDirection,
                                                                                    upDirection: _upDirection,
                                                                                    fontSize: _fontSize,
                                                                                    textAlignment: _textAlignment);

            // To get original character outline 2D positions, use the following code:
            //List<(int, char, Vector2[])> individualTextOutlinePositions = _currentVectorFontFactory.CreateIndividualTextOutlinePositions(_textToShow, _fontSize);

            var textOutlinesGroupNode = new GroupNode("TextOutlinesGroup");

            int totalPositionsCount = 0;

            for (var i = 0; i < textOutlines.Length; i++)
            {
                Vector3[] outlinePositions = textOutlines[i];
                var multiLineNode = new MultiLineNode(outlinePositions, isLineStrip: true)
                {
                    LineColor = Colors.Black,
                    LineThickness = 1
                };

                textOutlinesGroupNode.Add(multiLineNode);

                totalPositionsCount += outlinePositions.Length;
            }

            _rootTextNode.Add(textOutlinesGroupNode);

            _textInfoString += $"\nOutline positions: {totalPositionsCount:#,##0}";
        }
        else
        {
            _textInfoString += "\nOutline positions: /";
        }


        // Update the text info label
        _infoLabel?.UpdateValue();


        // Get the bounding rectangle of the text
        (Vector3 topLeftPosition, Vector2 textSize) = _currentVectorFontFactory.GetBoundingRectangle(_textToShow, textPosition, _selectedPositionType, _textDirection, _upDirection, _fontSize);

        // We can also get only the text size:
        //var textSize = _currentVectorFontFactory.GetTextSize(_textToShow, _fontSize);

        _textBoundingRectangleNode = new RectangleNode(topLeftPosition, PositionTypes.TopLeft, textSize, _textDirection, _upDirection, Colors.Black, lineThickness: 1, "TextBoundingRectangle");
        _textBoundingRectangleNode.Visibility = _showBoundingRectangle ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        _rootTextNode.Add(_textBoundingRectangleNode);
    }
    
    private string CreateAllCharsText(int from = 32, int to = 128, int lineLength = 16)
    {
        string text = "";
        for (int i = from; i <= to; i++)
        {
            text += (char)i;
            if (i > from && i % lineLength == 0)
                text += "\r\n";
        }

        return text;
    }

    private string[] CollectAvailableFonts()
    {
        if (_allFontFiles == null)
        {
#if WEB_GL
            // On the browser we have a fixed set of font files
            _allFontFiles = new string[] { "fonts/Roboto-Regular.ttf", "fonts/Roboto-Bold.ttf" };
#else
            var localFontFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/TrueTypeFonts/");
            var fontsFiles = Directory.GetFiles(localFontFolder, "*.ttf", SearchOption.TopDirectoryOnly).ToList();

            // Commented reading system fonts because on Windows all font files have denied access
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
#endif
        }

        return _allFontFiles;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        // Try to register for file drag-and-drop
#if VULKAN
        bool isDragAndDropSupported = ui.RegisterFileDropped(".ttf", (fileName) => LoadFont(fileName, recreateText: true));
#else
        bool isDragAndDropSupported = false;
#endif

        var allFontFiles = CollectAvailableFonts();
        var fontNames = allFontFiles.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();

        ui.CreateRadioButtons(
            fontNames,
            (selectedIndex, selectedText) =>
            {
                _selectedFontFileName = allFontFiles[selectedIndex];
                _ = LoadFont(_selectedFontFileName, recreateText: true);
            },
            selectedItemIndex: 0);

        if (isDragAndDropSupported)
            ui.CreateLabel("or drag & drop .ttf file to load it").SetStyle("italic");


        ui.AddSeparator();

        ui.CreateTextBox(width: 240, height: 88, 
            initialText: _textToShow,
            textChangedAction: (newText) =>
            {
                _textToShow = newText;
                RecreateText();
            });


        ui.AddSeparator();


        ui.CreateCheckBox(text: "Show solid text mesh", _showSolidTextMesh, isChecked =>
        {
            _showSolidTextMesh = isChecked;
            RecreateText();
        });
        
        ui.CreateCheckBox(text: "Show text outlines", _showTextOutlines, isChecked =>
        {
            _showTextOutlines = isChecked;
            RecreateText();
        });

        ui.CreateCheckBox("Show bounding rectangle", 
            _showBoundingRectangle, 
            isChecked =>
            {
                if (_textBoundingRectangleNode != null)
                    _textBoundingRectangleNode.Visibility = isChecked ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;

                _showBoundingRectangle = isChecked;
            });

        ui.CreateCheckBox(text: "Create individual characters (?):When unchecked, then a single StandardMesh is created from the whole text.\nWhen checked, then StandardMeshes are created for each character.\nThis way each character can have its own MeshModelNode and material.", _showIndividualCharacterMeshes, isChecked =>
        {
            _showIndividualCharacterMeshes = isChecked;
            RecreateText();
        });
                
        ui.CreateCheckBox("IsSolidColorMaterial (?):When checked (by default) then text is rendered with a solid color regardless of lights;\nwhen unchecked then rotate tha camera so the text is at steep angle and\nsee that text is shaded based on the angle to the CameraLight.", 
            _isSolidColorMaterial, 
            isChecked =>
            {
                _isSolidColorMaterial = isChecked;
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
            0, width: 130, "PositionType:", keyTextWidth: 100);

        
        var textAlignments = Enum.GetNames(typeof(TextAlignment));
        ui.CreateComboBox(textAlignments, 
            (selectedIndex, selectedText) =>
            {
                _textAlignment = (TextAlignment)selectedIndex;
                RecreateText();
            }, selectedItemIndex: (int)_textAlignment, 
            width: 130, 
            keyText: "Text alignment:", keyTextWidth: 100);

        
        ui.AddSeparator();

        ui.CreateLabel("TextDirection: (1, 0, 0)");
        ui.CreateLabel("UpDirection: (0, 1, 0)");

        var fontSizes = new int[] { 8, 10, 20, 30, 40, 50, 100, 200 };
        ui.CreateComboBox(fontSizes.Select(f => f.ToString()).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                _fontSize = int.Parse(selectedText!);
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
            keyTextWidth: 80,
            formatShownValueFunc: lineHeight => $"{lineHeight:F1} em");
        
        ui.CreateSlider(-0.2f, 0.2f, () => _charSpacing, 
            newValue =>
            {
                _charSpacing = newValue;
                RecreateText();
            }, width: 90, 
            keyText: "CharSpacing:", 
            keyTextWidth: 80,
            formatShownValueFunc: charSpacing => $"{charSpacing:F2} em");
        
        ui.CreateSlider(0.5f, 2f, () => _fontStretch, 
            newValue =>
            {
                _fontStretch = newValue;
                RecreateText();
            }, width: 90, 
            keyText: "FontStretch:", 
            keyTextWidth: 80,
            formatShownValueFunc: fontStretch => $"{fontStretch*100:F0}%");

        ui.CreateLabel("BezierCurveSegmentsCount: (?):Specifies into how many segments (individual straight lines)\neach bezier curve from the character glyph is converted.\nSmaller values produce smaller meshes or less outline positions (are faster to render),\nbigger values produce more accurate fonts but are slower to render.\nSee 'Text mesh info' below to see the number of triangles used to define the text.\nZoom the camera to be very close to the text to see the difference on the curved parts.");
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

        _infoLabel = ui.CreateKeyValueLabel("", () => _textInfoString ?? "");

#if VULKAN
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
                _ = LoadFont(_textBoxElement.GetText() ?? "", recreateText: true);
            });

            // When File name TextBox is shown in the bottom left corner, then we need to lift the CameraAxisPanel above it
            if (CameraAxisPanel != null)
                CameraAxisPanel.Position = new Vector2(10, 80); // CameraAxisPanel is aligned to BottomLeft, so we only need to increase the y position from 10 to 80
        }
#endif
    }
}