using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Text;

public class InstancedTextNodePerformanceSample : CommonSample
{
    public override string Title => "InstancedTextNode - amazing performance";

    public override string Subtitle => "InstancedTextNode is using mesh instancing and can render millions of characters very efficiently";
    
    // Start with 8,000 individual texts (each shows its coordinates)
    private int _xCount = 10;
    private int _yCount = 20;
    private int _zCount = 40;
    private float _fontSize = 20;
    private bool _hasBackSize = true;
    
    private BitmapFont? _bitmapFont;
    private InstancedTextNode? _instancedTextNode;
    private ICommonSampleUIElement? _charsCountLabel;
    private ICommonSampleUIElement? _alphaClipThresholdLabel;

    public InstancedTextNodePerformanceSample(ICommonSamplesContext context)
        : base(context)
    {
        ShowCameraAxisPanel = false;
    }

    protected override void OnCreateScene(Scene scene)
    {
        // The following commented code create a BitmapFont from a ttf font file.
        //string fontPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\BitmapFonts\");
        //fontPath = FileUtils.FixDirectorySeparator(fontPath);

        //string fontFileName = fontPath + "roboto_128.fnt"; // use roboto font (Google's Open Font) that is rendered with font size 128 pixels

        //var bitmapFont = new BitmapFont(fontFileName);

        // But in this sample we use default BitmapFont that is included in the Ab4d.SharpEngine assembly - it uses Roboto font with size 64 pixels.
        _bitmapFont = BitmapTextCreator.GetDefaultBitmapFont();

        RecreateInstancedTextNode(scene);

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading        = -19.4347916f;
            targetPositionCamera.Attitude       = -1.80629194f;
            targetPositionCamera.Distance       = 5389.14355f;
            targetPositionCamera.TargetPosition = new Vector3(-549.80176f, 763.0017f, 354.4638f);
            
            //targetPositionCamera.Heading        = -4.159573f;
            //targetPositionCamera.Attitude       = 4.96790266f;
            //targetPositionCamera.Distance       = 5389.14355f;
            //targetPositionCamera.TargetPosition = new Vector3(67.7f, 14.1f, 1.2f);
        }
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (_instancedTextNode != null)
        {
            _instancedTextNode.Dispose();
            _instancedTextNode = null;
        }
        
        base.OnDisposed();
    }

    private void RecreateInstancedTextNode(Scene scene)
    {
        if (_bitmapFont == null)
            return;

        if (_instancedTextNode != null)
        {
            scene.RootNode.Remove(_instancedTextNode);
            _instancedTextNode.Dispose(); // Dispose the old InstancedTextNode - this also releases the memory used by the GPU buffers.
        }
        
        _instancedTextNode = new InstancedTextNode(_bitmapFont, 
                                                   isSolidColorMaterial: true,          // Use solid color material so the text will not be shaded by lights
                                                   textDirection: new Vector3(1, 0, 0), // Set initial text direction (those are also the default values, but are here for clarity)
                                                   upDirection: new Vector3(0, 1, 0));  // This is also the default up direction

        // We can also set the text direction by calling SetTextDirection method
        //_instancedTextNode.SetTextDirection(textDirection: new Vector3(1, 0, 0), upDirection: new Vector3(0, 1, 0));
        
        CreateInstanceText(_instancedTextNode, 
                           centerPosition: new Vector3(0, 0, 0), 
                           size: new Vector3(2000, 2000, 10000), 
                           xCount: _xCount, yCount: _yCount, zCount: _zCount, 
                           textColor: Colors.Black, 
                           _fontSize);
        
        scene.RootNode.Add(_instancedTextNode);

        UpdateTotalCharsCount();
    }
    
    private void CreateInstanceText(InstancedTextNode instancedTextNode, Vector3 centerPosition, Vector3 size, int xCount, int yCount, int zCount, Color4 textColor, float fontSize)
    {
        float xStep = xCount <= 1 ? 0 : (float)(size.X / (xCount - 1));
        float yStep = yCount <= 1 ? 0 : (float)(size.Y / (yCount - 1));
        float zStep = zCount <= 1 ? 0 : (float)(size.Z / (zCount - 1));

        for (int z = 0; z < zCount; z++)
        {
            float zPos = (float)(centerPosition.Z - (size.Z / 2.0) + (z * zStep));

            for (int y = 0; y < yCount; y++)
            {
                float yPos = (float)(centerPosition.Y - (size.Y / 2.0) + (y * yStep));

                for (int x = 0; x < xCount; x++)
                {
                    float xPos = (float)(centerPosition.X - (size.X / 2.0) + (x * xStep));

                    string infoText = $"({xPos:0} {yPos:0} {zPos:0})";
                    instancedTextNode.AddText(infoText, textColor, new Vector3(xPos, yPos, zPos), fontSize, hasBackSide: _hasBackSize);
                }
            }
        }
    }       
        
    private void UpdateTotalCharsCount()
    {
        if (_charsCountLabel == null || _instancedTextNode == null)
            return;
        
        _charsCountLabel.SetText($"Total chars count: {_instancedTextNode.CharactersCount:N0}");
    }
    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Select texts count:", isHeader: true);

        ui.CreateRadioButtons(new string[] { "125  (5 x 5 x 5)", "8,000  (10 x 20 x 40)", "200,000  (20 x 100 x 100)", "1,000,000  (500 x 100 x 100)" },
            (selectedIndex, selectedText) =>
            {
                switch (selectedIndex)
                {
                    case 0: // "125":
                        _xCount = 5;
                        _yCount = 5;
                        _zCount = 5;
                        _fontSize = 50;
                        break;

                    case 1: //  "8,000":
                        _xCount = 10;
                        _yCount = 20;
                        _zCount = 40;
                        _fontSize = 20;
                        break;
                    
                    case 2: // "200,000":
                        _xCount = 20;
                        _yCount = 100;
                        _zCount = 100;
                        _fontSize = 10;
                        break;
                    
                    case 3: // "1,000,000":
                        _xCount = 20;
                        _yCount = 100;
                        _zCount = 500;
                        _fontSize = 10;
                        break;
                }
                
                RecreateInstancedTextNode(this.Scene!);
            },
            selectedItemIndex: 1);
        
        ui.AddSeparator();

        ui.CreateLabel("Each text shows its X, Y and Z coordinate in brackets.", width: 200).SetStyle("italic");
        
        ui.AddSeparator();
        
        _charsCountLabel = ui.CreateLabel("Total chars count: ").SetStyle("bold");
        UpdateTotalCharsCount();
        
        ui.AddSeparator();
        ui.AddSeparator();

        ui.CreateCheckBox("Render BackSide (?):By default the front and back side of the text are rendered.\nIt is possible to improve performance, by rendering\nonly the front side (reducing the number of drawn triangles by half).", _hasBackSize, isChecked =>
        {
            _hasBackSize = isChecked;
            RecreateInstancedTextNode(this.Scene!);
        });
        
        ui.AddSeparator();
        
        _alphaClipThresholdLabel = ui.CreateKeyValueLabel("AlphaClipThreshold: (?):AlphaClipThreshold specifies at which alpha value the pixels will be clipped (not rendered).\nDefault value is 0.5.", () => _instancedTextNode?.AlphaClipThreshold.ToString("F2") ?? "0");
        ui.CreateSlider(0, 1, () => _instancedTextNode?.AlphaClipThreshold ?? 0, newValue =>
        {
            if (_instancedTextNode != null)
            {
                _instancedTextNode.AlphaClipThreshold = newValue;
                _alphaClipThresholdLabel.UpdateValue();
            }
        });
        
        ui.AddSeparator();

        ui.CreateButton("Show report in VS Output", () =>
        {
            if (_instancedTextNode != null)
                System.Diagnostics.Debug.WriteLine(_instancedTextNode.GetReport());
        });
    }    
}