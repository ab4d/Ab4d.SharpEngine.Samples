using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class SpritesSample : CommonSample
{
    public override string Title => "Sprites";
    public override string Subtitle => "SpriteBatch can be used to render sprites (textures), rectangles and 2D text.";

    private GpuImage? _uvCheckerTexture;
    private GpuImage? _treeTexture;

    private bool _isAnimatingSprite = true;
    private SpriteBatch? _animatedSpriteBatch;
    
    private SpriteBatch? _sceneViewSpriteBatch;
    private GpuImage? _gradientTexture;

    public SpritesSample(ICommonSamplesContext context)
        : base(context)
    {

    }

    protected override void OnCreateScene(Scene scene)
    {
        if (scene.GpuDevice == null)
            return;

        _uvCheckerTexture = base.GetCommonTexture("uvchecker.png", scene);
        _treeTexture      = base.GetCommonTexture("TreeTexture.png", scene);
        
        // Create SpriteBatch on the Scene object (note that there we CANNOT use absolute coordinates and can use only relative coordinates, that are in range from 0 to 1)
        // It is also possible to create SpiteBatch on SceneView (see OnSceneViewInitialized below). In this case absolute coordinates can be used.
        var sceneSpriteBatch = scene.CreateOverlaySpriteBatch("SceneOverlaySpriteBatch");

        sceneSpriteBatch.Begin();

        sceneSpriteBatch.SetSpriteTexture(_uvCheckerTexture);

        // Draw 2 sprites using relative coordinates (form 0 to 1).
        // Note that the spriteSize has the same width and height, but it is rendered with bigger width than height because view's Width is bigger then Height 
        sceneSpriteBatch.DrawSprite(topLeftPosition: new Vector2(0.08f, 0.2f), spriteSize: new Vector2(0.15f, 0.15f)); 

        // When using scaleX and scaleY instead of spriteSize, the aspect ratio of the sprite is preserved
        sceneSpriteBatch.DrawSprite(topLeftPosition: new Vector2(0.25f, 0.2f), scaleX: 0.1f, scaleY: 0.1f);

        // It is also possible to render 2D text
        sceneSpriteBatch.DrawBitmapText("Relative coordinates:", new Vector2(0.08f, 0.175f), fontSize: 20, Color4.Black);
        
        sceneSpriteBatch.End();
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        if (_uvCheckerTexture == null || _treeTexture == null)
            return;

        // Create SpriteBatch on the SceneView object (note that there we CAN use absolute coordinates - set when calling Begin method)
        _sceneViewSpriteBatch = sceneView.CreateOverlaySpriteBatch("SceneViewOverlaySpriteBatch");

        _sceneViewSpriteBatch.Begin(useAbsoluteCoordinates: true); // use absolute coordinates for all following Draw calls

        // To switch to using relative coordinates, set IsUsingAbsoluteCoordinates to false
        // All following draw calls will use relative coordinates.
        //sceneViewSpriteBatch.IsUsingAbsoluteCoordinates = false;


        _sceneViewSpriteBatch.DrawBitmapText("Absolute coordinates\nwith color mask:", new Vector2(100, 350), fontSize: 20, Color4.Black);

        _sceneViewSpriteBatch.SetSpriteTexture(_uvCheckerTexture);

        // Use color mask
        _sceneViewSpriteBatch.DrawSprite(new Vector2(100, 400), new Vector2(50, 50), colorMask: Colors.Red);
        _sceneViewSpriteBatch.DrawSprite(new Vector2(160, 400), new Vector2(50, 50), colorMask: Colors.Green);
        _sceneViewSpriteBatch.DrawSprite(new Vector2(220, 400), new Vector2(50, 50), colorMask: Colors.Blue);


        // Draw rectangles
        _sceneViewSpriteBatch.DrawBitmapText("DrawRectangle:", new Vector2(100, 490), fontSize: 20, Color4.Black);

        _sceneViewSpriteBatch.DrawRectangle(new Vector2(100, 520), new Vector2(40, 40), Colors.Red);
        _sceneViewSpriteBatch.DrawRectangle(new Vector2(160, 520), new Vector2(40, 40), Colors.Green, rotationAngleDegrees: 33);
        _sceneViewSpriteBatch.DrawRectangle(new Vector2(220, 520), new Vector2(40, 40), Colors.Blue, rotationAngleDegrees: 66);


        // Advanced draw text with background and margin
        _sceneViewSpriteBatch.DrawBitmapText("Rotated text", new Vector2(700, 200), fontSize: 20, textColor: Color4.Black, rotationAngleDegrees: 90);
        _sceneViewSpriteBatch.DrawBitmapText("Text with background", new Vector2(500, 150), fontSize: 20, textColor: Color4.Black, backgroundColor: Colors.LightGreen);
        _sceneViewSpriteBatch.DrawBitmapText("Text with\nbackground\nand margin", new Vector2(500, 200), fontSize: 20, textColor: Color4.Black, backgroundColor: Colors.LightGreen,
                                            marginLeft: 10, marginRight: 10, marginTop: 10, marginBottom: 10);

        if (sceneView.GpuDevice != null)
        {
            // Use TextureFactory.CreateGradientTexture to create a gradient texture that can be shown as a spite.
            // Note that CreateGradientTexture creates a horizontal texture with height 1 and width set to textureWidth.
            // To show such texture a vertical, we need to rotate the spite by 90 degrees (using rotationAngleDegrees)
            var gradientStops = new GradientStop[]
            {
                new GradientStop(Colors.Aqua,       offset: 0.25f),
                new GradientStop(Colors.LightGreen, offset: 0.5f),
                new GradientStop(Colors.Yellow,     offset: 0.75f),
                new GradientStop(Colors.Red,        offset: 1.0f),
            };

            _gradientTexture = TextureFactory.CreateGradientTexture(sceneView.GpuDevice, gradientStops, textureSize: 256);

            // If we only need from two colors, we can also use startColor and endColor instead of gradientStops:
            //var gradientTexture = TextureFactory.CreateGradientTexture(sceneView.GpuDevice, startColor: Colors.DeepSkyBlue, endColor: Colors.Yellow, textureWidth: 256);

            _sceneViewSpriteBatch.SetSpriteTexture(_gradientTexture);
            _sceneViewSpriteBatch.DrawSprite(new Vector2(870, 90), new Vector2(100, 200), rotationAngleDegrees: 90);

            _sceneViewSpriteBatch.DrawBitmapText("Sprite with\ngradient", new Vector2(850, 170), fontSize: 20, textColor: Color4.Black);
        }


        // Change texture
        _sceneViewSpriteBatch.SetSpriteTexture(_treeTexture);

        // Use SetCoordinateCenter to align to bottom or right corners
        // When center position is horizontally set to Left or Center, then horizontal axis is pointing to the right.
        // When Right is used, then horizontal axis is pointing to the left (to define the distance from the right border).
        // When center position is vertically set to Top, then vertical axis is pointing down (distance from the top).
        // When Center or Bottom is used, then vertical axis is pointing Up (distance from the bottom or center).
        _sceneViewSpriteBatch.SetCoordinateCenter(PositionTypes.BottomRight);
        _sceneViewSpriteBatch.DrawSprite(new Vector2(60, 100), new Vector2(50, 90), rotationAngleDegrees: 0);
        _sceneViewSpriteBatch.DrawBitmapText("Coordinate\ncenter set to\nBottomRight", new Vector2(125, 180), fontSize: 20, textColor: Color4.Black);

        _sceneViewSpriteBatch.SetCoordinateCenter(PositionTypes.TopRight);
        _sceneViewSpriteBatch.DrawSprite(new Vector2(60, 10), new Vector2(50, 90), rotationAngleDegrees: 0);
        _sceneViewSpriteBatch.DrawBitmapText("Coordinate\ncenter set to\nTopRight", new Vector2(125, 110), fontSize: 20, textColor: Color4.Black);

        _sceneViewSpriteBatch.SetCoordinateCenter(PositionTypes.BottomLeft);
        _sceneViewSpriteBatch.DrawSprite(new Vector2(10, 100), new Vector2(50, 90), rotationAngleDegrees: 0);
        _sceneViewSpriteBatch.DrawBitmapText("Coordinate\ncenter set to\nBottomLeft", new Vector2(10, 180), fontSize: 20, textColor: Color4.Black);

        // Hide top-left sprite so that the title of the sample is visible
        // sceneViewSpriteBatch.SetCoordinateCenter(PositionTypes.TopLeft);
        // sceneViewSpriteBatch.Draw(new Vector2(10, 10), new Vector2(50, 90), rotationAngleDegrees: 0);

        _sceneViewSpriteBatch.End();


        // Create a new SpriteBatch that will use dpi scaled coordinates and sizes (dpi scale is get for window's dpi scale setting).
        var dpiAwareSpriteBatch = sceneView.CreateOverlaySpriteBatch("DpiAwareSpriteBatch");
        dpiAwareSpriteBatch.IsUsingDpiScale = true;

        dpiAwareSpriteBatch.Begin(useAbsoluteCoordinates: true);

        dpiAwareSpriteBatch.SetSpriteTexture(_uvCheckerTexture);

        // Note that when there is some dpi scale, then this box will be bigger and not in line with other 3 sprites
        dpiAwareSpriteBatch.DrawSprite(new Vector2(280, 400), new Vector2(50, 50));
        dpiAwareSpriteBatch.DrawBitmapText("Dpi scaled:", new Vector2(250, 380), fontSize: 20, textColor: Color4.Black);

        dpiAwareSpriteBatch.End();


        // Create a new SpriteBatch that will be animated.
        // It is highly recommended to put static sprites and texts into a on SpriteBatch,
        // and animated sprites and text that change often into another SpriteBatch.
        // This way the RenderingItems for the static SpriteBatch are generated only once
        // and only the animated SpriteBatch needs to recreate its RenderingItems.
        _animatedSpriteBatch = sceneView.CreateOverlaySpriteBatch("AnimatedSpriteBatch");

        
        // Usually the custom animation is done in the SceneUpdating event handler, that is subscribed by the following code:
        //sceneView.SceneUpdating += OnSceneViewOnSceneUpdating;
        //
        // But in this samples project we use call to CommonSample.SubscribeSceneUpdating method to subscribe to the SceneUpdating event.
        // This allows automatic unsubscribing when the sample is unloaded and automatic UI testing
        // (prevented starting animation and using CallSceneUpdating with providing custom elapsedSeconds value).
        base.SubscribeSceneUpdating(UpdateAnimatedSpriteBatch);

        // Set the initial position of the animated sprite batch
        UpdateAnimatedSpriteBatch(elapsedSeconds: 0);
    }

    private void UpdateAnimatedSpriteBatch(float elapsedSeconds)
    {
        if (!_isAnimatingSprite || _animatedSpriteBatch == null || _uvCheckerTexture == null || SceneView == null)
            return;

        var animatedAngle = (elapsedSeconds * 90f) % 360f;
        var scale = MathF.Sin(elapsedSeconds * 2) * 0.1f + 0.12f; // from 0.02 to 0.22
        var imageSize = new Vector2(_uvCheckerTexture.Width * scale, _uvCheckerTexture.Height * scale);

        // Set sprite position so that the center will be at the center of the view
        var spritePosition = new Vector2((SceneView.Width - imageSize.X) * 0.5f, (SceneView.Height - imageSize.Y) * 0.5f);


        _animatedSpriteBatch.Begin(useAbsoluteCoordinates: true);

        _animatedSpriteBatch.SetSpriteTexture(_uvCheckerTexture);
        
        _animatedSpriteBatch.DrawSprite(spritePosition, scaleX: scale, scaleY: scale, rotationAngleDegrees: animatedAngle);

        var textPosition = new Vector2((SceneView.Width - 100) * 0.5f, (SceneView.Height + 200) * 0.5f);
        _animatedSpriteBatch.DrawBitmapText($"Angle: {animatedAngle:F0}°\nScale: {scale:F2}", textPosition, fontSize: 20, textColor: Color4.Black);

        _animatedSpriteBatch.End();
    }

    protected override void OnDisposed()
    {
        Scene?.RemoveAllSpriteBatches();
        SceneView?.RemoveAllSpriteBatches();

        base.OnDisposed();
    }
    
    private void ChangeGradientTexture()
    {
        if (_gradientTexture == null || _sceneViewSpriteBatch == null)
            return;

        // Create a new random gradient texture data
        var rawImageData = TextureFactory.CreateGradientRawImageData(GetRandomHsvColor3(), GetRandomHsvColor3(), _gradientTexture.Width);

        // Copy the new data to the existing gradient texture
        _gradientTexture.CopyDataToImage(rawImageData.Data, transitionImageToShaderReadOnlyOptimalLayout: true);

        // Instead of recreating the sprite batch by calling Begin and End methods,
        // we can just notify the SceneView that the texture has changed.
        // This renders a new frame without recreating the command buffers.
        _sceneViewSpriteBatch.NotifyTextureChange();
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateCheckBox("Animate sprite", _isAnimatingSprite, isChecked =>
        {
            _isAnimatingSprite = isChecked;
        });
        
        ui.AddSeparator();

        ui.CreateButton(
            "Change sprite texture (?):When we only change the texture of the sprite,\nthen SharpEngine does not need to record a new command buffer,\nbut can only render a new frame.\n\nTo test this, open the Diagnostics window (note that when the animation\nis running a new command buffer is recorder on each frame),\nstop the animation and click on this button - a new frame\nwill be rendered but the previous command buffer will be reused.",
            () => ChangeGradientTexture());
    }
}