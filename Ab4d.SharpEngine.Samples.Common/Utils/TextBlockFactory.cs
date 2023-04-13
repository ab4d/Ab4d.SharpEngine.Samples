using System.Drawing;
using System.Numerics;
using System.Resources;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Cyotek.Drawing.BitmapFont;

namespace Ab4d.SharpEngine.Samples.Common.Utils;

public class TextBlockFactory
{
    private Scene _scene;
    private IBitmapIO _bitmapIO;

    private BitmapTextCreator? _bitmapTextCreator;

    public string? FontFileName { get; set; }

    public Color4 TextColor { get; set; } = Color4.Black;

    public float FontSize { get; set; } = 14;

    public bool IsSolidColorMaterial { get; set; } = true;

    public float BackgroundOffset { get; set; } = 0.05f; 

    public float BackgroundHorizontalPadding { get; set; } = 8;
    
    public float BackgroundVerticalPadding { get; set; } = 4;

    public Color4 BackgroundColor { get; set; } = Color4.Transparent;

    public float BorderThickness { get; set; } = 0;

    public Color4 BorderColor { get; set; } = Color4.Black;

    public Material BackgroundMaterial { get; set; } = StandardMaterials.Black;

    public TextBlockFactory(Scene scene, IBitmapIO bitmapIO)
    {
        _scene = scene;
        _bitmapIO = bitmapIO;
    }

    public SceneNode CreateTextBlock(Vector3 centerPosition, string text, float textAttitude = 0, float textHeading = 0)
    {
        return CreateTextBlock(centerPosition, PositionTypes.Center, text, textAttitude, textHeading);
    }

    public GroupNode CreateTextBlock(Vector3 position,
                                     PositionTypes positionType,
                                     string text,
                                     float textAttitude = 0,
                                     float textHeading = 0)
    {
        // dy default text is laying down on the XZ plane
        Vector3 textDirection = new Vector3(1, 0, 0);
        Vector3 upDirection = new Vector3(0, 0, -1);

        if (MathUtils.IsNotZero(textAttitude))
        {
            var attitudeTransform = new AxisAngleRotateTransform(new Vector3(1, 0, 0), textAttitude);
            upDirection = attitudeTransform.TransformNormal(upDirection);
        }

        if (MathUtils.IsNotZero(textHeading))
        {
            var headingTransform = new AxisAngleRotateTransform(new Vector3(0, 1, 0), textHeading);
            textDirection = headingTransform.TransformNormal(textDirection);
            upDirection = headingTransform.TransformNormal(upDirection);
        }

        return CreateTextBlock(position, positionType, text, textDirection, upDirection);
    }

    public GroupNode CreateTextBlock(Vector3 position,
                                     PositionTypes positionType, 
                                     string text, 
                                     Vector3 textDirection,
                                     Vector3 upDirection)
    {
        EnsureBitmapTextCreator();

        if (_bitmapTextCreator == null)
            throw new InvalidOperationException("Cannot initialize BitmapTextCreator");


        var groupNode = new GroupNode("TextBlockGroupNode");


        var textSize = _bitmapTextCreator!.GetTextSize(text, FontSize);
        var centerPosition = MathUtils.GetCenterPosition(position, positionType, textDirection, upDirection, textSize);

        var normalDirection = Vector3.Cross(textDirection, upDirection);
        normalDirection = Vector3.Normalize(normalDirection);

        var offsetVector = normalDirection * BackgroundOffset;


        if (BorderThickness > 0)
        {
            StandardMaterial borderMaterial;
            if (IsSolidColorMaterial)
                borderMaterial = new StandardMaterial() { EmissiveColor = BorderColor };
            else
                borderMaterial = new StandardMaterial(BorderColor); // use borderColor as DiffuseColor

            var borderPlaneModelNode = new PlaneModelNode()
            {
                Position = centerPosition - 2 * offsetVector,
                PositionType = PositionTypes.Center,
                Normal = normalDirection,
                HeightDirection = upDirection,
                Size = new Vector2(textSize.X + 2 * (BackgroundHorizontalPadding + BorderThickness), textSize.Y + 2 * (BackgroundVerticalPadding + BorderThickness)),
                Material = borderMaterial,
                BackMaterial = BackgroundMaterial,
                Name = "TextBlockBorderPlaneNode"
            };

            groupNode.Add(borderPlaneModelNode);
        }


        if (BackgroundColor != Color4.Transparent)
        {
            StandardMaterial backgroundMaterial;
            if (IsSolidColorMaterial)
                backgroundMaterial = new StandardMaterial() { EmissiveColor = BackgroundColor };
            else
                backgroundMaterial = new StandardMaterial(BackgroundColor); // use backgroundColor as DiffuseColor

            var backgroundPlaneModelNode = new PlaneModelNode()
            {
                Position = centerPosition - offsetVector,
                PositionType = PositionTypes.Center,
                Normal = normalDirection,
                HeightDirection = upDirection,
                Size = new Vector2(textSize.X + 2 * BackgroundHorizontalPadding, textSize.Y + 2 * BackgroundVerticalPadding),
                Material = backgroundMaterial,
                BackMaterial = BackgroundMaterial,
                Name = "TextBlockBackgroundPlaneNode"
            };

            groupNode.Add(backgroundPlaneModelNode);
        }


        if (!string.IsNullOrEmpty(text))
        {
            var textNode = _bitmapTextCreator.CreateTextNode(text,
                                                             position: position,
                                                             positionType: positionType,
                                                             textDirection: textDirection,
                                                             upDirection: upDirection,
                                                             fontSize: FontSize,
                                                             textColor: TextColor,
                                                             isSolidColorMaterial: IsSolidColorMaterial,
                                                             name: "TextBlockNode");

            groupNode.Add(textNode);
        }

        return groupNode;
    }
    
    //public SceneNode CreateTextBlock(Vector3 position, PositionTypes positionType, string text, Color4 textColor)
    //{
    //    return CreateTextBlock(position, positionType, text, textColor, textDirection: new Vector3(1, 0, 0), upDirection: new Vector3(0, 0, -1));
    //}

    //public SceneNode CreateTextBlock(Vector3 position, PositionTypes positionType, string text, Color4 textColor, Vector3 textDirection, Vector3 upDirection)
    //{
    //    EnsureBitmapTextCreator();

    //    if (_bitmapTextCreator == null)
    //        throw new InvalidOperationException("Cannot initialize BitmapTextCreator");

    //    var textNode = _bitmapTextCreator.CreateTextNode(text,
    //                                                     position: position,
    //                                                     positionType: positionType,
    //                                                     textDirection: textDirection,
    //                                                     upDirection: upDirection,
    //                                                     fontSize: FontSize,
    //                                                     textColor: textColor,
    //                                                     isSolidColorMaterial: IsSolidColorMaterial,
    //                                                     name: "TextBlockNode");

    //    return textNode;
    //}

    private void EnsureBitmapTextCreator()
    {
        if (_bitmapTextCreator != null)
            return;

        string fontFileName;

        if (FontFileName != null)
        {
            fontFileName = FontFileName;
            fontFileName = FileUtils.FixDirectorySeparator(fontFileName);

            if (!System.IO.File.Exists(fontFileName))
                throw new FileNotFoundException("The specified FontFileName does not exist: " + FontFileName, fontFileName);
        }
        else
        {
            var fontPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileUtils.FixDirectorySeparator(@"Resources\BitmapFonts\"));

            if (!System.IO.Directory.Exists(fontPath))
                throw new DirectoryNotFoundException("The common font directory does not exist: " + fontPath);

            var allFontFiles = System.IO.Directory.GetFiles(fontPath, "*.fnt", SearchOption.TopDirectoryOnly);

            if (allFontFiles == null || allFontFiles.Length == 0)
                throw new InvalidOperationException("Cannot find any fnt file in the BitmapFonts folder");


            string? foundFontFileName = null;
            foreach (var fontFile in allFontFiles)
            {
                if (fontFile.EndsWith("arial_128.fnt")) // first try to use arial font if exist
                {
                    foundFontFileName = fontFile;
                    break;
                }
                
                if (fontFile.EndsWith("roboto_128.fnt")) // if arial does not exist, use roboto
                {
                    foundFontFileName = fontFile;
                    break;
                }
            }

            if (foundFontFileName != null)
                fontFileName = foundFontFileName;
            else
                fontFileName = allFontFiles[0]; // just use the first available font
        }

        var bitmapFont = BitmapTextCreator.CreateBitmapFont(fontFileName, _bitmapIO);

        if (bitmapFont != null)
            _bitmapTextCreator = new BitmapTextCreator(_scene, bitmapFont, _bitmapIO);
    }

    public void Dispose()
    {
        if (_bitmapTextCreator != null)
        {
            _bitmapTextCreator.Dispose();
            _bitmapTextCreator = null;
        }
    }
}