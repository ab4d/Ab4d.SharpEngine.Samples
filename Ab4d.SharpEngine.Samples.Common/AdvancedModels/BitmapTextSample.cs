using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
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

    private string _textToShow = "Demo bitmap text\nwith some special characters:\n{}@äöčšž";
    public override string Subtitle => "SharpEngine can render text by using BitmapTextCreator that can render bitmap fonts.";

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

    public BitmapTextSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        RecreateBitmapTextCreator();

        RecreateText();

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 50;
            targetPositionCamera.Attitude = -5;
            targetPositionCamera.Distance = 1200;
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
        if (Scene == null)
            return;

        if (_bitmapTextCreator == null)
        {
            RecreateBitmapTextCreator();
            if (_bitmapTextCreator == null)
                return;
        }

        Scene.RootNode.Clear();

        var textNode = _bitmapTextCreator.CreateTextNode(text: _textToShow,
                                                         position: new Vector3(0, 0, 0),
                                                         positionType: PositionTypes.Center,
                                                         textDirection: _textDirection,
                                                         upDirection: _upDirection,
                                                         fontSize: _fontSize,
                                                         textColor: Colors.Orange,
                                                         isSolidColorMaterial: _isSolidColorMaterial);

        Scene.RootNode.Add(textNode);


        // Gets the size of the text
        _textSize = _bitmapTextCreator.GetTextSize(text: _textToShow, fontSize: _fontSize, maxWidth: 0, fontStretch: 1);

        _textSizeLabel?.UpdateValue();
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
    
    [MemberNotNull(nameof(_fontFiles))]
    [MemberNotNull(nameof(_fontDescriptions))]
    private void CollectAvailableBitmapFonts()
    {
        string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\BitmapFonts\");

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

        var fontSizes = new int[] { 20, 50, 100, 200 };
        ui.CreateComboBox(fontSizes.Select(f => f.ToString()).ToArray(), 
            (selectedIndex, selectedText) =>
            {
                _fontSize = Int32.Parse(selectedText!);
                RecreateText();
            }, selectedItemIndex: 1, 
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

        _textSizeLabel = ui.CreateKeyValueLabel("Text size: ", () => $"{_textSize.X:F0} x {_textSize.Y:F0}");
        
        ui.AddSeparator();
        ui.CreateLabel("See comments in code for more info").SetStyle("italic");
    }
}