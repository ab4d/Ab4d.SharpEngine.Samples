using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

// Ab4d.SharpEngine can show text by rendering bitmap font.
// Bitmap font is defined by one or more textures with rendered characters and font data that define where on the texture the character is.
//
// Ab4d.SharpEngine has a build-in default font that is based on the Roboto font (Google's Open Font). The font bitmap is created with 64 pixels font size.
// To use that font call the GetDefaultBitmapTextCreator(Scene) method.
// 
// The samples project also comes with some already prepared fonts:
// Arial, ArialBlack, ArialBlack with outline, Roboto, RobotoBlack, RobotoBlack with outline.
// See the Ab4d.SharpEngine.Samples.Common/Resources/BitmapFonts folder.
// The following are the file extensions:
// .fnt      - binary bitmap font definition file
// .text.fnt - text bitmap font definition file (has the same data as binary fnt file; note that SharpEngine can read ONLY binary fnt file)
// .png      - texture with rendered characters
// .bmfc     - bitmap font definition file for "Bitmap Font Generator" (see link below) - this file can be used to see what settings were used for the .fnt and .png files.
//
// To use other font, you will need to create the font data file (.fnt file) and rendered texture (in png file format).
// This can be created by a third party "Bitmap Font Generator" (for example from https://www.angelcode.com/products/bmfont/).
// The following are tips for using the Bitmap Font Generator:
// - In "Export options" use "White text with alpha" preset and use 32 bit depth
// - For better quality when scaling it is very good that the font texture has mip-maps (see internet for more into about mip-maps). Because mip-maps are scaled down versions of the original image, the font must be rendered with padding to prevent that pixels from sibling characters would affect the scaled down mip-maps. It is not needed to provide all mip-map levels (to 1x1 bitmap). But it is recommended to provide mip-maps until the mip-map where each character is 8x8 pixels big. Each mip-map requires to double the padding, for example:
// - for texture with character size 64 pixels: if we provide 8px padding, we can use 3 mip-maps: 32x32, 16x16, 8x8
// - for texture with character size 128 pixels: if we provide 16px padding, we can use 4 mip-maps: 64x64, 32x32, 16x16, 8x8
//
// BitmapTextCreator will read the padding and create the mip maps based on the padding value.
// - Note that when creating 8 bit png bitmap with "Bitmap font generator" does not correctly create an alpha-transparent bitmap.
// Therefore, it is better to store into 32 bit bitmap and then use some third-party imaging tool to convert to 8-bit png with proper transparency.

public class BitmapTextSample : CommonSample
{
    public override string Title => "Bitmap Text";
    public override string Subtitle => "SharpEngine can render text by using BitmapTextCreator that can render bitmap fonts.";
    
    private string _textToShow = "Demo bitmap text\nwith some special characters:\n{}@äöčšž";

    private int _bitmapTextCreatorIndex = 0;
    
    private List<string>? _fontFiles;
    private List<string>? _fontDescriptions;

    private Vector3 _textDirection = new Vector3(1, 0, 0);
    private Vector3 _upDirection = new Vector3(0, 1, 0);

    private float _fontSize = 50;
    private bool _isSolidColorMaterial = true;

    private Vector2 _textSize;

    private BitmapTextCreator? _bitmapTextCreator;
    private ICommonSampleUIElement? _textSizeLabel;
    private SceneNode? _textNode;
    private GroupNode? _rootTextNode;

    private bool _alignWithCamera;
    private bool _fixScreenSize;

    public BitmapTextSample(ICommonSamplesContext context)
        : base(context)
    {
        
    }

    protected override void OnCreateScene(Scene scene)
    {
        _rootTextNode = new GroupNode("RootTextNode");
        scene.RootNode.Add(_rootTextNode);

        RecreateBitmapTextCreator();

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

    protected override void OnDisposed()
    {
        if (_bitmapTextCreatorIndex > 0 && _bitmapTextCreator != null)
        {
            _bitmapTextCreator.Dispose();
            _bitmapTextCreator = null;
        }

        base.OnDisposed();
    }

    private void RecreateText()
    {
        if (_rootTextNode == null)
            return;

        if (_bitmapTextCreator == null)
        {
            RecreateBitmapTextCreator();
            if (_bitmapTextCreator == null)
                return;
        }

        _rootTextNode.Clear();

        _textNode = _bitmapTextCreator.CreateTextNode(text: _textToShow,
                                                      position: new Vector3(0, 0, 0),
                                                      positionType: PositionTypes.Center,
                                                      textDirection: _textDirection,
                                                      upDirection: _upDirection,
                                                      fontSize: _fontSize,
                                                      textColor: Colors.Orange,
                                                      isSolidColorMaterial: _isSolidColorMaterial);

        _rootTextNode.Add(_textNode);


        // Gets the size of the text
        _textSize = _bitmapTextCreator.GetTextSize(text: _textToShow, fontSize: _fontSize, maxWidth: 0, fontStretch: 1);

        _textSizeLabel?.UpdateValue();

        UpdateBitmapTextTransformation();
    }

    private void RecreateBitmapTextCreator()
    {
        if (Scene == null || Scene.GpuDevice == null)
            return;

        if (_fontFiles == null)
            CollectAvailableBitmapFonts();

        if (_bitmapTextCreator != null)
            _bitmapTextCreator.Dispose();

        if (_bitmapTextCreatorIndex == 0)
        {
            _bitmapTextCreator = BitmapTextCreator.GetDefaultBitmapTextCreator(Scene);
        }
        else
        {
            string bitmapFontFileName = _fontFiles[_bitmapTextCreatorIndex];

            var bitmapFont = new BitmapFont(bitmapFontFileName);
            _bitmapTextCreator = new BitmapTextCreator(Scene, bitmapFont, BitmapIO);
        }
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

    [MemberNotNull(nameof(_fontFiles))]
    [MemberNotNull(nameof(_fontDescriptions))]
    private void CollectAvailableBitmapFonts()
    {
        string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\BitmapFonts\");
        fontPath = FileUtils.FixDirectorySeparator(fontPath);
        
        var allFontFiles = System.IO.Directory.GetFiles(fontPath, "*.fnt");

        _fontFiles = new List<string>(allFontFiles.Length + 1);
        _fontDescriptions = new List<string>(allFontFiles.Length + 1);

        // First add the default font file
        _fontFiles.Add("");
        _fontDescriptions.Add("Roboto (default; size: 64) (?):The Robot font is build into the SharpEngine and can be used by calling BitmapTextCreator.GetDefaultBitmapTextCreator method.\nFiles for other fonts in this sample are defined in the Resources/BitmapFonts folder.");

        foreach (var fontFile in allFontFiles)
        {
            // Remove all text font files because only binary fnt files are supported
            if (fontFile.Contains(".text."))
                continue;

            var fontFileName = System.IO.Path.GetFileName(fontFile);
            var fontFileParts = fontFileName.Split('_');

            var fontName = fontFileParts[0]; // Start with font name
            var fontDescription = char.ToUpper(fontName[0]) + fontName[1..]; // Make first char uppercase

            if (fontFileParts.Contains("bold"))
                fontDescription += " bold";
            
            if (fontFileParts.Contains("black"))
                fontDescription += " black";
            
            if (fontFileParts.Contains("outline"))
                fontDescription += " with outline";
            
            if (fontFileName.Contains("_64"))
                fontDescription += " (size: 64)";
            
            if (fontFileName.Contains("_128"))
                fontDescription += " (size: 128)";

            _fontFiles.Add(fontFile);
            _fontDescriptions.Add(fontDescription);
        }
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        if (_fontFiles == null || _fontDescriptions == null)
            CollectAvailableBitmapFonts();

        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Bitmap font:", isHeader: true);

        ui.CreateRadioButtons(_fontDescriptions.ToArray(), (selectedIndex, selectedText) =>
            {
                _bitmapTextCreatorIndex = selectedIndex;
                RecreateBitmapTextCreator();
                RecreateText();
            },
            selectedItemIndex: 0);


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

        ui.CreateCheckBox("IsSolidColorMaterial (?):When checked (by default) then text is rendered with a solid color regardless of lights;\nwhen unchecked then rotate tha camera so the text is at steep angle and\nsee that text is shaded based on the angle to the CameraLight.", 
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
    }
}