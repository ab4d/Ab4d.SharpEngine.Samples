using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
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

    private Vector3 _textDirection = new Vector3(1, 0, 0);
    private Vector3 _upDirection = new Vector3(0, 1, 0);

    private float _fontSize = 50;
    private bool _isSolidColorMaterial = true;

    private Vector2 _textSize;

    private ICommonSampleUIElement? _textSizeLabel;
    private ICommonSampleUIElement? _textBoxElement;

    private SceneNode? _textNode;
    private GroupNode? _rootTextNode;
    
    private bool _alignWithCamera;
    private bool _fixScreenSize;
    
    private VectorFontFactory? _currentVectorFontFactory;

    private Dictionary<string, VectorFontFactory> _allVectorFontFactories = new ();

    public VectorTextSample(ICommonSamplesContext context)
        : base(context)
    {
        _selectedFontFileName = "Roboto-Black.ttf";

        ShowCameraAxisPanel = true;
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        // Reset position of CameraAxisPanel, if it was changed in the OnCreateUI
        if (CameraAxisPanel != null)
            CameraAxisPanel.Position = new Vector2(10, 10);

        base.OnDisposed();
    }

    private void LoadFont(string fontFileName, bool recreateText = false)
    {
        var fontName = Path.GetFileNameWithoutExtension(fontFileName); // remove ".ttf"
        
        if (_allVectorFontFactories.TryGetValue(fontName, out _currentVectorFontFactory))
            return;


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


        
        _textToShow = GetSelectedText(0); // ASCII chars (1)

        RecreateText();

        
        // Add WireGridNode so we can see the effect when text is aligned to camera or when fixed screen size is used
        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(0, -150, 0),
            Size = new Vector2(800, 400),
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 0, 1),
            WidthCellsCount = 8,
            HeightCellsCount = 4,
            IsClosed = true,
            MajorLineColor = Colors.Gray,
            MinorLineColor = Colors.Gray,
            MajorLineThickness = 1
        };

        scene.RootNode.Add(wireGridNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 50;
            targetPositionCamera.Attitude = -5;
            targetPositionCamera.Distance = 1200;

            targetPositionCamera.CameraChanged += (sender, args) =>
            {
                UpdateBitmapTextTransformation();
            };
        }

        ShowCameraAxisPanel = true;
    }

    private void RecreateText()
    {
        if (_rootTextNode == null || _currentVectorFontFactory == null)
            return;

        _rootTextNode.Clear();

        var textMesh = _currentVectorFontFactory.CreateTextMesh(text: _textToShow,
                                                                position: new Vector3(0, 0, 0),
                                                                positionType: PositionTypes.Center,
                                                                textDirection: _textDirection,
                                                                upDirection: _upDirection,
                                                                fontSize: _fontSize,
                                                                textColor: Colors.Orange,
                                                                isSolidColorMaterial: _isSolidColorMaterial);

        if (textMesh != null)
        {
            var meshModelNode = new MeshModelNode(textMesh, StandardMaterials.Orange)
            {
                BackMaterial = StandardMaterials.Red
            };

            _rootTextNode.Add(meshModelNode);
        }

        //// Gets the size of the text
        //_textSize = _bitmapTextCreator.GetTextSize(text: _textToShow, fontSize: _fontSize, maxWidth: 0, fontStretch: 1);

        //_textSizeLabel?.UpdateValue();

        //UpdateBitmapTextTransformation();
    }
    
    private void UpdateBitmapTextTransformation()
    {
        if (targetPositionCamera == null || SceneView == null || _textNode == null)
            return;

        if (!_fixScreenSize && !_alignWithCamera)
        {
            _textNode.Transform = null;
            return;
        }


        var desiredScreenSize = new Vector2(300, 77); // preserve aspect ratio of original world size: 685 x 177

        // If we want to specify the screen size in device independent units, then we need to scale by DPI scale.
        // If we want to set the size in pixels, the comment the following line.
        desiredScreenSize *= new Vector2(SceneView.DpiScaleX, SceneView.DpiScaleY);

        
        float scaleX, scaleY;

        if (_fixScreenSize)
        {
            if (targetPositionCamera.ProjectionType == ProjectionTypes.Orthographic)
            {
                scaleX = (desiredScreenSize.X / SceneView.Width) / _textSize.X;
                scaleY = (desiredScreenSize.Y / SceneView.Height) / _textSize.Y;
            }
            else
            {
                // Get lookDirectionDistance
                // If we look directly at the text, then we could use: lookDirectionDistance = textPosition - cameraPosition,
                // but when we look at some other direction, then we need to use the following code that
                // gets the distance to the text in the look direction:
                var textPosition = _textNode.WorldBoundingBox.GetCenterPosition();
                var cameraPosition = targetPositionCamera.GetCameraPosition();

                var distanceVector = textPosition - cameraPosition;

                var lookDirection = Vector3.Normalize(targetPositionCamera.GetLookDirection());

                // To get look direction distance we project the distanceVector to the look direction vector
                var lookDirectionDistance = Vector3.Dot(distanceVector, lookDirection);

                var worldSize = Utilities.CameraUtils.GetPerspectiveWorldSize(desiredScreenSize, lookDirectionDistance, targetPositionCamera.FieldOfView, new Vector2(SceneView.Width, SceneView.Height));


                scaleX = worldSize.X / _textSize.X;
                scaleY = worldSize.Y / _textSize.Y;
            }

            if (!_alignWithCamera)
            {
                if (_textNode.Transform is not ScaleTransform scaleTransform)
                {
                    scaleTransform = new ScaleTransform();
                    _textNode.Transform = scaleTransform;
                }

                scaleTransform.ScaleX = scaleX;
                scaleTransform.ScaleY = scaleY;

                return;
            }
            // else - this will be handled below
        }
        else
        {
            scaleX = 1;
            scaleY = 1;
        }


        if (_alignWithCamera)
        {
            // To align the text with camera, we first need to generate the text
            // so that its textDirection is set to (1, 0, 0) and upDirection is set to (0, 1, 0).
            // This will orient the text with the camera when Heading is 0 and Attitude is 0.
            // After that, we can align the text with the camera by simply negating the camera's 
            // rotation that is defined by view matrix.

            if (_textDirection != new Vector3(1, 0, 0) || _upDirection != new Vector3(0, 1, 0))
            {
                _textDirection = new Vector3(1, 0, 0);
                _upDirection = new Vector3(0, 1, 0);
                RecreateText();
            }


            var invertedView = targetPositionCamera.GetInvertedViewMatrix();
            
            // Remove offset so we get only camera rotation
            invertedView.M41 = 0;
            invertedView.M42 = 0;
            invertedView.M43 = 0;

            if (_textNode.Transform is not MatrixTransform matrixTransform)
            {
                matrixTransform = new MatrixTransform();
                _textNode.Transform = matrixTransform;
            }

            if (_fixScreenSize)
            {
                matrixTransform.SetMatrix(Matrix4x4.CreateScale(scaleX, scaleY, 1) * invertedView);
            }
            else // only _fixScreenSize
            {
                matrixTransform.SetMatrix(invertedView);
            }
        }
    }

    private string GetSelectedText(int selectedIndex)
    {
        string selectedText;

        switch (selectedIndex)
        {
            case 0:  // ASCII chars from 32 - 128
                selectedText = CreateAllCharsText(from: 32, to: 128, lineLength: 16);
                break;
                
            case 1:  // ASCII chars from 128 - 255
                selectedText = CreateAllCharsText(from: 128, to: 255, lineLength: 16);
                break;
                
            case 2:  // AB4D
                selectedText = "AB4D";
                break;
                
            case 3:  // Symbol chars
                selectedText = "⇦⇧⇨⇩↯";
                break;
                
            case 4:  // Chinese chars
                selectedText = "太極拳";
                break;

            default:
                selectedText = "";
                break;
        }

        return selectedText;
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
    
    private void CollectAvailableFonts()
    {
        if (_allFontFiles != null)
            return;

        var localFontFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/TrueTypeFonts/");
        var fontsFiles = Directory.GetFiles(localFontFolder, "*.ttf", SearchOption.TopDirectoryOnly).ToList();

        string? systemFontsFolder = null;
        string[]? systemFontNames = null;

        if (OperatingSystem.IsWindows())
        {
            systemFontsFolder = @"C:\Windows\Fonts\";

            // We need to define a list of only selected fonts because if we would try to select from all
            // fonts, then for many font files we would get access denied error
            systemFontNames = new string[] { "arial", "arialuni", "cour", "OpenSans-Regular", "times", "webdings" };
        }

        if (systemFontsFolder != null && 
            systemFontNames != null &&
            System.IO.Directory.Exists(systemFontsFolder))
        {
            var allSystemFontFiles = Directory.GetFiles(systemFontsFolder, "*.ttf", SearchOption.TopDirectoryOnly);

            foreach (var oneFontFile in allSystemFontFiles)
            {
                var name = Path.GetFileNameWithoutExtension(oneFontFile).ToLower();
                if (systemFontNames.Contains(name))
                    fontsFiles.Add(oneFontFile);
            }
        }

        _allFontFiles = fontsFiles.ToArray();

        _allFontFiles = Directory.GetFiles(@"C:\FontsCopy", "*.ttf", SearchOption.TopDirectoryOnly); 
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        CollectAvailableFonts();

        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Font:", isHeader: true);


        var fontNames = _allFontFiles.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();

        ui.CreateComboBox(
            fontNames,
            (selectedIndex, selectedText) =>
            {
                _selectedFontFileName = _allFontFiles[selectedIndex];
                LoadFont(_selectedFontFileName, recreateText: true);
            },
            selectedItemIndex: 0);

        ui.AddSeparator();

        ui.CreateComboBox(new string[]
            {
                "ASCII chars (1)", 
                "ASCII chars (2)", 
                "AB4D", 
                "Symbol chars", 
                "Chinese chars"
            },
            (selectedIndex, selectedText) =>
            {
                _textToShow = GetSelectedText(selectedIndex);
                RecreateText();
            },
            selectedItemIndex: 0,
            width: 150,
            keyText: "Text: (?):Symbols and Chines chars are supported only with some fonts");

        ui.AddSeparator();


        var fontSizes = new int[] { 8, 10, 20, 30, 40, 50, 100, 200 };
        ui.CreateComboBox(fontSizes.Select(f => f.ToString()).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                _fontSize = Int32.Parse(selectedText!);
                RecreateText();
            }, selectedItemIndex: Array.IndexOf(fontSizes, (int)_fontSize), 
            width: 80, 
            keyText: "Font size:");

        ui.CreateCheckBox("IsSolidColorMaterial (?):When checked (by default) then text is rendered with a solid color regardless of lights; when unchecked then rotate tha camera so the text is at steep angle and see that text is shaded based on the angle to the CameraLight.", 
            _isSolidColorMaterial, 
            isChecked =>
            {
                _isSolidColorMaterial = isChecked;
                RecreateText();
            });

        ui.AddSeparator();

        ui.CreateCheckBox("Align with camera", 
            _alignWithCamera, 
            isChecked =>
            {
                _alignWithCamera = isChecked;
                UpdateBitmapTextTransformation();
            });
        
        ui.CreateCheckBox("Fix to screen size 300 x 77", 
            _fixScreenSize, 
            isChecked =>
            {
                _fixScreenSize = isChecked;
                UpdateBitmapTextTransformation();
            });

        ui.AddSeparator();

        _textSizeLabel = ui.CreateKeyValueLabel("Text world size: ", () => $"{_textSize.X:F0} x {_textSize.Y:F0}");
        
        ui.AddSeparator();
        ui.CreateLabel("See comments in code for more info").SetStyle("italic");


        // Try to register for file drag-and-drop
        bool isDragAndDropSupported = ui.RegisterFileDropped(".ttf", (fileName) => LoadFont(fileName, recreateText: true));

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