using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.AdvancedModels;

public class SpritesSample : CommonSample
{
    public override string Title => "Sprites";
    public override string Subtitle => "Absolutely and relatively positioned sprites (resize window to see the difference)";

    private GpuImage? _uvCheckerTexture;
    private GpuImage? _treeTexture;

    private SpriteBatch? _animatedSpriteBatch;
    private DateTime _animationStartTime;

    public SpritesSample(ICommonSamplesContext context)
        : base(context)
    {

    }

    protected override void OnCreateScene(Scene scene)
    {
        if (scene.GpuDevice == null)
            return;

        _uvCheckerTexture = TextureLoader.CreateTexture(@"Resources/Textures/uvchecker.png", BitmapIO, scene.GpuDevice);
        _treeTexture = TextureLoader.CreateTexture(@"Resources/Textures/TreeTexture.png", BitmapIO, scene.GpuDevice);


        // Create SpriteBatch on the Scene object (note that there we CANNOT use absolute coordinates)
        // It is also possible to create SpiteBatch on SceneView (see OnSceneViewInitialized below). In this case absolute coordinates can be used.
        var sceneSpriteBatch = scene.CreateOverlaySpriteBatch("SceneOverlaySpriteBatch");

        sceneSpriteBatch.Begin();

        sceneSpriteBatch.SetSpriteTexture(_uvCheckerTexture);

        // relative coordinates are from 0 to 1
        sceneSpriteBatch.DrawSprite(new Vector2(0.1f, 0.2f), new Vector2(0.2f, 0.2f));
        sceneSpriteBatch.DrawSprite(new Vector2(0.3f, 0.4f), scaleX: 0.1f, scaleY: 0.1f);

        sceneSpriteBatch.End();
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        if (_uvCheckerTexture == null || _treeTexture == null)
            return;

        // Create SpriteBatch on the SceneView object (note that there we CAN use absolute coordinates - set when calling Begin method)
        var sceneViewSpriteBatch = sceneView.CreateOverlaySpriteBatch("SceneViewOverlaySpriteBatch");
        
        sceneViewSpriteBatch.Begin(useAbsoluteCoordinates: true); // use absolute coordinates for all following Draw calls

        // To switch to using relative coordinates, set IsUsingAbsoluteCoordinates to false
        // All following draw calls will use relative coordinates.
        //sceneViewSpriteBatch.IsUsingAbsoluteCoordinates = false;

        sceneViewSpriteBatch.SetSpriteTexture(_uvCheckerTexture);

        // Use color mask
        sceneViewSpriteBatch.DrawSprite(new Vector2(150, 400), new Vector2(50, 50), colorMask: Colors.Red);
        sceneViewSpriteBatch.DrawSprite(new Vector2(150, 460), new Vector2(50, 50), colorMask: Colors.Green);
        sceneViewSpriteBatch.DrawSprite(new Vector2(150, 520), new Vector2(50, 50), colorMask: Colors.Blue);


        // Change texture
        sceneViewSpriteBatch.SetSpriteTexture(_treeTexture);

        sceneViewSpriteBatch.DrawSprite(new Vector2(410, 520), new Vector2(50, 90));
        sceneViewSpriteBatch.DrawSprite(new Vector2(405, 540), new Vector2(50, 90));
        sceneViewSpriteBatch.DrawSprite(new Vector2(400, 560), new Vector2(50, 90));
        sceneViewSpriteBatch.DrawSprite(new Vector2(380, 560), new Vector2(50, 90));
        sceneViewSpriteBatch.DrawSprite(new Vector2(360, 560), new Vector2(50, 90));


        // Use SetCoordinateCenter to align to bottom or right corners
        // When center position is horizontally set to Left or Center, then horizontal axis is pointing to the right.
        // When Right is used, then horizontal axis is pointing to the left (to define the distance from the right border).
        // When center position is vertically set to Top, then vertical axis is pointing down (distance from the top).
        // When Center or Bottom is used, then vertical axis is pointing Up (distance from the bottom or center).
        sceneViewSpriteBatch.SetCoordinateCenter(PositionTypes.BottomRight);
        sceneViewSpriteBatch.DrawSprite(new Vector2(60, 100), new Vector2(50, 90), rotationAngleDegrees: 0);

        sceneViewSpriteBatch.SetCoordinateCenter(PositionTypes.TopRight);
        sceneViewSpriteBatch.DrawSprite(new Vector2(60, 10), new Vector2(50, 90), rotationAngleDegrees: 0);

        sceneViewSpriteBatch.SetCoordinateCenter(PositionTypes.BottomLeft);
        sceneViewSpriteBatch.DrawSprite(new Vector2(10, 100), new Vector2(50, 90), rotationAngleDegrees: 0);

        // Hide top-left sprite so that the title of the sample is visible
        //sceneViewSpriteBatch.SetCoordinateCenter(PositionTypes.TopLeft);
        //sceneViewSpriteBatch.Draw(new Vector2(10, 10), new Vector2(50, 90), rotationAngleDegrees: 0);

        sceneViewSpriteBatch.End();


        var dpiAwareSpriteBatch = sceneView.CreateOverlaySpriteBatch("DpiAwareSpriteBatch");
        dpiAwareSpriteBatch.IsUsingDpiScale = true;

        dpiAwareSpriteBatch.Begin(useAbsoluteCoordinates: true);

        dpiAwareSpriteBatch.SetSpriteTexture(_uvCheckerTexture);

        dpiAwareSpriteBatch.DrawSprite(new Vector2(150, 520), new Vector2(50, 50));

        dpiAwareSpriteBatch.End();


        _animatedSpriteBatch = sceneView.CreateOverlaySpriteBatch("AnimatedSpriteBatch");

        UpdateAnimatedSpriteBatch();

        sceneView.SceneUpdating += delegate (object? sender, EventArgs args)
        {
           UpdateAnimatedSpriteBatch();
        };
    }

    
    private void UpdateAnimatedSpriteBatch()
    {
        if (_animatedSpriteBatch == null || _uvCheckerTexture == null || SceneView == null)
            return;

        var now = DateTime.Now;

        if (_animationStartTime == DateTime.MinValue)
            _animationStartTime = now;

        var elapsedTime = now - _animationStartTime;
        var animatedAngle = (float)elapsedTime.TotalSeconds * 90f;
        var scale = MathF.Sin((float)elapsedTime.TotalSeconds * 2) * 0.1f + 0.12f; // from 0.02 to 0.22
        var imageSize = new Vector2(_uvCheckerTexture.Width * scale, _uvCheckerTexture.Height * scale);

        // Set sprite position so that the center will be at the center of the view
        var spritePosition = new Vector2((SceneView.Width - imageSize.X) * 0.5f, (SceneView.Height - imageSize.Y) * 0.5f);


        _animatedSpriteBatch.Begin(useAbsoluteCoordinates: true);

        _animatedSpriteBatch.SetSpriteTexture(_uvCheckerTexture);
        
        _animatedSpriteBatch.DrawSprite(spritePosition, scaleX: scale, scaleY: scale, rotationAngleDegrees: animatedAngle);

        _animatedSpriteBatch.End();
    }

    protected override void OnDisposed()
    {
        Scene?.RemoveAllSpriteBatches();
        SceneView?.RemoveAllSpriteBatches();

        if (_uvCheckerTexture != null)
        {
            _uvCheckerTexture.Dispose();
            _uvCheckerTexture = null;
        }
        
        if (_treeTexture != null)
        {
            _treeTexture.Dispose();
            _treeTexture = null;
        }

        base.OnDisposed();
    }
}