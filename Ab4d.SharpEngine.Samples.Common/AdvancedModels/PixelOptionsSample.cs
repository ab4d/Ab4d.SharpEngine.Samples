using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class PixelOptionsSample : CommonSample
{
    public override string Title => "PixelsNode with per-pixel colors and per-pixel size";
    
    public override string Subtitle => "The sample also shows how to render circular pixels.";

    private int _pixelsXCount = 16;
    private bool _hasTransparentPixelColors;
    
    private Vector3[]? _pixelPositions;
    
    private GpuImage? _treeGpuImage;
    private GpuImage? _whiteCircleGpuImage;


    public PixelOptionsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        //
        // Prepare the data
        //
        
        _pixelPositions = PixelsRenderingSample.CreatePositionsArray(new Vector3(0, 0, 0), new Vector3(80, 20, 60), _pixelsXCount, _pixelsXCount / 4, (int)(_pixelsXCount * 0.66));

        var pixelsBoundingBox = BoundingBox.FromPoints(_pixelPositions);
        
        
        int pixelsCount = _pixelPositions.Length;
        
        var pixelColors = GeneratePixelColors(pixelsCount, _pixelsXCount);


        var pixelSizes = new float[pixelsCount];
        for (int i = 0; i < pixelsCount; i++)
        {
            float percent = (float)i / (float)pixelsCount;
            pixelSizes[i] = (1f - percent) * 10f;
        }
        

        //
        // Add pixels nodes
        // 

        var solidColorSingleSizePixelsNode = new PixelsNode(_pixelPositions, pixelsBoundingBox, pixelColor: Colors.Orange, pixelSize: 5)
        {
            Transform = new TranslateTransform(0, 20, 300)
        };
        scene.RootNode.Add(solidColorSingleSizePixelsNode);
        

        var multiColorSingleSizePixelsNode = new PixelsNode(_pixelPositions, pixelsBoundingBox, pixelColors: pixelColors, pixelSize: 5, hasTransparentPixels: _hasTransparentPixelColors)
        {
            Transform = new TranslateTransform(0, 20, 200)
        };            
        scene.RootNode.Add(multiColorSingleSizePixelsNode);


        var multiColorWithColorMaskSingleSizePixelsNode = new PixelsNode(_pixelPositions, pixelsBoundingBox, pixelColors: pixelColors, pixelSize: 5, hasTransparentPixels: _hasTransparentPixelColors)
        {
            PixelColor = new Color4(0, 1, 0, 1), // Set the PixelColor property to use as a color mask; in our case the color mask is set to green
            Transform = new TranslateTransform(0, 20, 100)
        };            
        scene.RootNode.Add(multiColorWithColorMaskSingleSizePixelsNode);
        
        
        var solidColorMultiSizePixelsNode = new PixelsNode(_pixelPositions, pixelsBoundingBox, pixelColor: Colors.Orange, pixelSizes: pixelSizes)
        {
            Transform = new TranslateTransform(0, 20, 0)
        };            
        scene.RootNode.Add(solidColorMultiSizePixelsNode);
        
        
        var solidColorMultiSizeWithSizeFactorPixelsNode = new PixelsNode(_pixelPositions, pixelsBoundingBox, pixelColor: Colors.Orange, pixelSizes: pixelSizes)
        {
            PixelSize = 2, // Setting PixelSize to 2 will multiply all the pixel sizes in pixelSizes array by 2
            Transform = new TranslateTransform(0, 20, -100)
        };            
        scene.RootNode.Add(solidColorMultiSizeWithSizeFactorPixelsNode);
        
        
        var multiColorMultiSizePixelsNode = new PixelsNode(_pixelPositions, pixelsBoundingBox, pixelColors: pixelColors, pixelSizes: pixelSizes, hasTransparentPixels: _hasTransparentPixelColors)
        {
            Transform = new TranslateTransform(0, 20, -200)
        };            
        scene.RootNode.Add(multiColorMultiSizePixelsNode);
        

        //
        // Add helper scene objects
        // 

        var boxModel = new BoxModelNode(centerPosition: new Vector3(50, -10, 50), new Vector3(300, 10, 640), material: StandardMaterials.Silver);
        scene.RootNode.Add(boxModel);
        
        
        var textBlockFactory = context.GetTextBlockFactory();
        textBlockFactory.BackgroundColor = Colors.LightYellow;
        textBlockFactory.BorderThickness = 1;
        textBlockFactory.BorderColor = Colors.DimGray;
        textBlockFactory.FontSize = 10;
        
        var textNode1 = textBlockFactory.CreateTextBlock("Single color\nSingle size", new Vector3(120, 10, 300), textAttitude: 30);
        scene.RootNode.Add(textNode1);

        var textNode2 = textBlockFactory.CreateTextBlock("Per-pixel color\nSingle size", new Vector3(120, 10, 200), textAttitude: 30);
        scene.RootNode.Add(textNode2);
        
        var textNode3 = textBlockFactory.CreateTextBlock("Per-pixel color\nwith color mask\nSingle size", new Vector3(120, 10, 100), textAttitude: 30);
        scene.RootNode.Add(textNode3);
        
        var textNode4 = textBlockFactory.CreateTextBlock("Single color\nPer-pixel size", new Vector3(120, 10, 0), textAttitude: 30);
        scene.RootNode.Add(textNode4);
        
        var textNode5 = textBlockFactory.CreateTextBlock("Single color\nPer-pixel size\nwith scale factor: 2", new Vector3(120, 10, -100), textAttitude: 30);
        scene.RootNode.Add(textNode5);
        
        var textNode6 = textBlockFactory.CreateTextBlock("Per-pixel color\nPer-pixel size", new Vector3(120, 10, -200), textAttitude: 30);
        scene.RootNode.Add(textNode6);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = -20;
            targetPositionCamera.Attitude = -35;
            targetPositionCamera.Distance = 750;
            targetPositionCamera.TargetPosition = new Vector3(30, 0, 120);
        }

        ChangeTexture(selectedIndex: 1); // show circle texture by default
    }

    private Color4[] GeneratePixelColors(int pixelsCount, int pixelsXCount)
    {
        var pixelColors = new Color4[pixelsCount];

        int transparentPixelsStartIndex = _hasTransparentPixelColors ? int.MaxValue : 0; // make all pixels transparent; use the following to make only half of the pixels transparent: _hasTransparentPixelColors ? pixelsCount / 2 : 0;

        for (int i = 0; i < pixelsCount; i++)
        {
            float green = 1.0f - ((float)i / (float)pixelsCount);
            pixelColors[i] = new Color4(new Color3((float)(i % pixelsXCount) / (float)pixelsXCount, green, 1.0f - green), i < transparentPixelsStartIndex ? 0.2f : 1);
        }

        return pixelColors;
    }
    
    private void ChangeTexture(int selectedIndex)
    {
        if (Scene == null)
            return;
        
        GpuImage? texture;
        CommonSamplerTypes samplerType = CommonSamplerTypes.TransparentBorder;

        if (selectedIndex == 1)
        {
            // We also load a texture with white circle that can be used to render circular pixels.
            // Because the circle is white it can be multiplied by the pixel color to get the desired color.
            // NOTE:
            // We do not generate the mip-maps for this texture. We will also use a ClampNoInterpolation sampler.
            // This is needed to prevent darkening the at the edges of the circle that is produces by using semi-transparent pixels 
            // that are generated when producing mip-maps and when using the default anisotropic sampler.
            //
            _whiteCircleGpuImage ??= TextureLoader.CreateTexture(@"Resources\Textures\white-circle-64x64-noaa.png", Scene, generateMipMaps: false);

            // If you prefer using anti-aliased circles and do not mind the artifacts at the edges, then use the white-circle-64x64.png instead 
            // and do not specify the ClampNoInterpolation sampler.
            //_whiteCircleGpuImage ??= TextureLoader.CreateTexture(@"Resources\Textures\white-circle-64x64.png", scene, generateMipMaps: true); // generateMipMaps is true by default

            samplerType = CommonSamplerTypes.ClampNoInterpolation;
            
            texture = _whiteCircleGpuImage;
        }
        else if (selectedIndex == 2)
        {
            _treeGpuImage ??= TextureLoader.CreateTexture(@"Resources\Textures\TreeTexture-square.png", Scene);
            texture = _treeGpuImage;
        }
        else
        {
            texture = null; // No texture
        }
        
        
        Scene.RootNode.ForEachChild<PixelsNode>(pixelsNode =>
        {
            if (texture == null)
            {
                pixelsNode.RemoveTexture();
            }
            else
            {
                // To render texture for each pixel, we call the SetTexture method.
                // In this sample we also need to adjust the alphaClipThreshold from the default value of 0.5 to 0.1f,
                // because when using transparent colors, the alpha value of the pixels is set to 0.2f and the default alphaClipThreshold of 0.5 would not render any pixels.
                pixelsNode.SetTexture(texture, samplerType, alphaClipThreshold: 0.1f);
            }
        });        
    }

    private void UpdatePixelColors()
    {
        if (_pixelPositions == null || Scene == null)
            return;
        
        int pixelsCount = _pixelPositions.Length;
        
        var pixelColors = GeneratePixelColors(pixelsCount, _pixelsXCount);

        
        var pixelColor = Colors.Orange;
        if (_hasTransparentPixelColors)
            pixelColor.SetAlpha(0.2f);

        Scene.RootNode.ForEachChild<PixelsNode>(pixelsNode =>
        {
            if (pixelsNode.PixelColors != null) // do not change the pixels colors if a single color is used
            {
                pixelsNode.PixelColors = pixelColors;
                pixelsNode.HasTransparentPixelColors = _hasTransparentPixelColors; // update the HasTransparentPixelColors property so the pixels are rendered correctly
            }
            else
            {
                pixelsNode.PixelColor = pixelColor; // set the pixel color to orange with 50% transparency
            }
        });
    }
    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Use transparent pixel colors", false, isChecked =>
        {
            _hasTransparentPixelColors = isChecked;
            UpdatePixelColors();
        });
        
        ui.AddSeparator();

        ui.CreateRadioButtons(new string[] { "No texture", "Circle texture", "Tree texture" }, (selectedIndex, selectedText) => ChangeTexture(selectedIndex), selectedItemIndex: 1);
    }
}