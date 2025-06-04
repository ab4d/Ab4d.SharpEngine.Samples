using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.Text;

public class InstancedTextNodeSample : CommonSample
{
    public override string Title => "InstancedTextNode - updating shown text";

    public override string Subtitle => "This sample shows how to change the text that is rendered by InstancedTextNode";
    
    private BitmapFont? _defaultBitmapFont;
    private BitmapFont? _additionalBitmapFont;
    private InstancedTextNode? _instancedTextNode1;
    private InstancedTextNode? _instancedTextNode2;
    private InstancedText? _instancedText;
    private ICommonSampleUIElement? _showHideButton;
    private int _rotationsCount;
    private List<InstancedText> _addedTexts = new ();
    
    public InstancedTextNodeSample(ICommonSamplesContext context)
        : base(context)
    {
        ShowCameraAxisPanel = true;
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Get the _defaultBitmapFont that is created from the BitmapFont that is included in the Ab4d.SharpEngine assembly - it uses Roboto font with size 64 pixels.
        _defaultBitmapFont = BitmapTextCreator.GetDefaultBitmapFont();
        
        // Create one additional BitmapFont
        // Get font file with outlines from the BitmapFonts folder
        string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\BitmapFonts\");
        fontPath = FileUtils.FixDirectorySeparator(fontPath);

        var allFontFiles = System.IO.Directory.GetFiles(fontPath);
        var fontFile = allFontFiles.FirstOrDefault(f => f.EndsWith("with_outline_128.fnt", StringComparison.OrdinalIgnoreCase));

        if (fontFile == null)
            throw new FileNotFoundException("Could not find font file with outlines in the BitmapFonts folder.");

        _additionalBitmapFont = new BitmapFont(fontFile);


        // Create two InstancedTextNodes that will render text with different BitmapFonts
        
        _instancedTextNode1 = new InstancedTextNode(_additionalBitmapFont); // by default the isSolidColorMaterial is true, textDirection is (1, 0, 0) and upDirection is (0, 1, 0)
        
        // We can also adjust character and line spacing
        _instancedTextNode1.AdditionalCharacterSpace = -5;
        _instancedTextNode1.AdditionalLineSpace = -10;

        // AddText method returns the InstancedText object that can be used to change the text later
        _instancedText = _instancedTextNode1.AddText(_additionalBitmapFont.FamilyName + "\nwith outline", Colors.Orange, position: new Vector3(-400, 100, 0), fontSize: 60, hasBackSide: true);
        
        _instancedTextNode1.SetTextDirection(textDirection: new Vector3(0, 0, -1), upDirection: new Vector3(0, 1, 0));

        _instancedTextNode1.AddText("in multiple directions", Colors.Orange, position: new Vector3(-400, -120, 0), fontSize: 80, hasBackSide: true);
        
        scene.RootNode.Add(_instancedTextNode1);

        
                
        _instancedTextNode2 = new InstancedTextNode(_defaultBitmapFont,                                                    
                                                    isSolidColorMaterial: false,         // Use StandardMaterial (and not SolidColorMaterial) so the text will be shaded by lights
                                                    textDirection: new Vector3(0, 1, 0), // Set initial text direction so that the text will be rendered vertically
                                                    upDirection: new Vector3(-1, 0, 0));

        _instancedTextNode2.AddText("Vertical text", Colors.Green, position: new Vector3(200, -100, 0), fontSize: 50, hasBackSide: true);
        
        scene.RootNode.Add(_instancedTextNode2);    

        

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -30;
            targetPositionCamera.Attitude = -7;
            targetPositionCamera.Distance = 1100;
        }
    }
    
    private void ChangeText()
    {
        if (_instancedText == null)
            return;

        string currentText = _instancedText.Text;

        char newEndChar;
        char lastChar = _instancedText.Text[^1];

        switch (lastChar)
        {
            case '/':
                newEndChar = '-';
                break;
                
            case '-':
                newEndChar = '\\';
                break;
                
            case '\\':
                newEndChar = '|';
                break;
                
            case '|':
                newEndChar = '/';
                break;
                
            default:
                newEndChar = '/';
                currentText += "  "; // No animated char yet - add 2 spaces so that last space will be replaced by the animated char
                break;
        }

        string newText = currentText.Substring(0, currentText.Length - 1) + newEndChar;

        _instancedText.SetText(newText);
    }
    
    private void AddNewText()
    {
        if (_instancedTextNode2 == null)
            return;

        var textIndex = _addedTexts.Count + 1;
        var newInstancedText = _instancedTextNode2.AddText($"Added text {textIndex}", Colors.Green, position: new Vector3(200, -100, -textIndex * 100), fontSize: 50, hasBackSide: true);
        _addedTexts.Add(newInstancedText);
    }
    
    private void RemoveText()
    {
        if (_instancedTextNode2 == null || _addedTexts.Count == 0)
            return;

        var lastIndex = _addedTexts.Count - 1;
        _instancedTextNode2.RemoveText(_addedTexts[lastIndex]);
        _addedTexts.RemoveAt(lastIndex);
    }
    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateButton("Change text", ChangeText);
        
        ui.CreateButton("Change color", () =>
        {
            _instancedText?.SetColor(GetRandomHsvColor4());
        });
        
        ui.CreateButton("Change position", () =>
        {
            _instancedText?.Move(new Vector3(0, 20, 0));
            
            // We could also call SetPosition
            //_instancedText?.SetPosition(_instancedText.Position + new Vector3(0, 30, 0));
        });
        
        ui.CreateButton("Change orientation", () =>
        {
            _rotationsCount++;
            
            var angleInDegrees = _rotationsCount * 20;
            var rotationMatrix = Matrix4x4.CreateRotationY(MathUtils.DegreesToRadians(angleInDegrees));

            var initialDirection = new Vector3(1, 0, 0);
            var textDirection = Vector3.Transform(initialDirection, rotationMatrix);

            _instancedText?.SetOrientation(textDirection, new Vector3(0, 1, 0));
        });

        _showHideButton = ui.CreateButton("Hide", () =>
        {
            if (_instancedText == null)
                return;
            
            if (_instancedText.IsVisible)
            {
                _instancedText.Hide();
                _showHideButton?.SetText("Show");
            }
            else
            {
                _instancedText.Show();
                _showHideButton?.SetText("Hide");
            }
        });
        
        ui.AddSeparator();

        ui.CreateButton("Add new text", AddNewText);
        ui.CreateButton("Remove text", RemoveText);
    }        
}