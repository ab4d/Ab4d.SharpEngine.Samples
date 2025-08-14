using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Core;
using Ab4d.SharpEngine.Effects;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Materials;

public class AnimatedTexturesSample : CommonSample
{
    public override string Title => "Animated textures";
    public override string Subtitle => "Using CircleModelNode and animating texture with gradient. This can be used for object selection.";

    private GpuImage? _animatedGradientTexture1;
    private GpuImage? _animatedGradientTexture2;
    
    private GradientStop[]? _animatedGradientStops;
    
    private SolidColorMaterial? _gradientMaterial1;
    private SolidColorMaterial? _gradientMaterial2;
    
    
    public AnimatedTexturesSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        // Use SolidColorMaterial instead of StandardMaterial so the colors do not get shaded when the camera is changed (and the camera's light angle is changed)
        _gradientMaterial1 = new SolidColorMaterial("AnimatedGradientMaterial1");
        _gradientMaterial2 = new SolidColorMaterial("AnimatedGradientMaterial2");

        
        
        var circleModelNode1 = new CircleModelNode("CircleModelNode1")
        {
            CenterPosition = new Vector3(-250, 0, 0),
            Normal = new Vector3(0, 1, 0),
            UpDirection = new Vector3(0, 0, -1),
            Radius = 100,
            Segments = 60, // default value is 30
            TextureMappingType = CircleTextureMappingTypes.RadialFromCenter,
            Material = _gradientMaterial1,
            BackMaterial = _gradientMaterial1,
        };
        
        scene.RootNode.Add(circleModelNode1);
        
        
        var sphereModelNode = new SphereModelNode("GreenSphere")
        {
            CenterPosition = new Vector3(-250, 45, -250),
            Radius = 40,
            Material = StandardMaterials.Green
        };
        
        scene.RootNode.Add(sphereModelNode);
        
        
        var circleModelNode2 = new CircleModelNode("CircleModelNode2")
        {
            CenterPosition = new Vector3(-250, 0, -250),
            Normal = new Vector3(0, 1, 0),
            UpDirection = new Vector3(0, 0, -1),
            Radius = 40,
            Segments = 60, // default value is 30
            TextureMappingType = CircleTextureMappingTypes.RadialFromCenter,
            Material = _gradientMaterial1,
            BackMaterial = _gradientMaterial1,
        };
        
        scene.RootNode.Add(circleModelNode2);
        
        
        
        // To show animated gradient on a rectangle, use CircleModelNode and set Segments to 4 (and adjust StartAngle - see comment below)
        var circleModelNode3 = new CircleModelNode("CircleModelNode1")
        {
            CenterPosition = new Vector3(0, 0, 0),
            Normal = new Vector3(0, 1, 0),
            UpDirection = new Vector3(0, 0, -1),
            Radius = 100,
            Segments = 4,
            StartAngle = 45, // Rotate the rectangle by 45 degrees to its first corner does not start at the top but at 45 degrees so the sides are aligned with the axes
            TextureMappingType = CircleTextureMappingTypes.RadialFromCenter,
            Material = _gradientMaterial1,
            BackMaterial = _gradientMaterial1,
        };
        
        scene.RootNode.Add(circleModelNode3);
        

        var boxModelNode = new BoxModelNode("GreenBox")
        {
            Position = new Vector3(0, 30, -250),
            Size = new Vector3(80, 60, 120),
            Material = StandardMaterials.Green
        };
        
        scene.RootNode.Add(boxModelNode);

        var circleModelNode4 = new CircleModelNode("AnimatedRectangle")
        {
            TextureMappingType = CircleTextureMappingTypes.RadialFromInnerRadius,
            Material = _gradientMaterial1,
            BackMaterial = _gradientMaterial1,
        };
        
        // CreateRectangle sets the properties of this CircleModelNode so that it renders a rectangle (Segments is set to 4 and StartAngle to 45)
        circleModelNode4.CreateRectangle(position: new Vector3(0, 0, -250), 
                                          positionType: PositionTypes.Center, 
                                          size: new Vector2(100, 150), 
                                          innerSizeFactor: 0.8f,  // this sets innerSize to 0.8 * size = Vector2(80, 120)
                                          widthDirection: new Vector3(1, 0, 0), 
                                          heightDirection: new Vector3(0, 0, -1));
        
        scene.RootNode.Add(circleModelNode4);


        
        var circleModelNode5 = new CircleModelNode("CircleModelNode5")
        {
            CenterPosition = new Vector3(250, 0, 0),
            Normal = new Vector3(0, 1, 0),
            UpDirection = new Vector3(0, 0, -1),
            Radius = 100,
            Segments = 60, // default value is 30
            TextureMappingType = CircleTextureMappingTypes.RadialFromCenter,
            Material = _gradientMaterial2,
            BackMaterial = _gradientMaterial2,
        };
        
        scene.RootNode.Add(circleModelNode5);
        
        
        var sphereModelNode2 = new SphereModelNode("GreenSphere")
        {
            CenterPosition = new Vector3(250, 45, -250),
            Radius = 40,
            Material = StandardMaterials.Green
        };
        
        scene.RootNode.Add(sphereModelNode2);
        
        
        var circleModelNode6 = new CircleModelNode("CircleModelNode2")
        {
            CenterPosition = new Vector3(250, 0, -250),
            Normal = new Vector3(0, 1, 0),
            UpDirection = new Vector3(0, 0, -1),
            Radius = 40,
            Segments = 60, // default value is 30
            TextureMappingType = CircleTextureMappingTypes.RadialFromCenter,
            Material = _gradientMaterial2,
            BackMaterial = _gradientMaterial2,
        };
        
        scene.RootNode.Add(circleModelNode6);
        
        
        var circleModelNode7 = new CircleModelNode("AnimatedRectangle")
        {
            TextureMappingType = CircleTextureMappingTypes.RadialFromInnerRadius,
            Material = _gradientMaterial2,
            BackMaterial = _gradientMaterial2,
        };
        
        circleModelNode7.CreateRectangle(position: new Vector3(500, 0, 0), 
                                         positionType: PositionTypes.Center, 
                                         size: new Vector2(200, 200), 
                                         innerSizeFactor: 0f, // no inner hole
                                         widthDirection: new Vector3(1, 0, 0), 
                                         heightDirection: new Vector3(0, 0, -1));
        
        scene.RootNode.Add(circleModelNode7);
        
        
        
        var boxModelNode2 = new BoxModelNode("GreenBox")
        {
            Position = new Vector3(500, 30, -250),
            Size = new Vector3(80, 60, 120),
            Material = StandardMaterials.Green
        };
        
        scene.RootNode.Add(boxModelNode2);
        
        
        var circleModelNode8 = new CircleModelNode("AnimatedRectangle")
        {
            TextureMappingType = CircleTextureMappingTypes.RadialFromInnerRadius,
            Material = _gradientMaterial2,
            BackMaterial = _gradientMaterial2,
        };
        
        circleModelNode8.CreateRectangle(position: new Vector3(500, 0, -250), 
                                         positionType: PositionTypes.Center, 
                                         size: new Vector2(100, 150), 
                                         innerSizeFactor: 0.8f,  // this sets innerSize to 0.8 * size = Vector2(80, 120)
                                         widthDirection: new Vector3(1, 0, 0), 
                                         heightDirection: new Vector3(0, 0, -1));
        
        scene.RootNode.Add(circleModelNode8);
        
        
        // Usually the custom animation is done in the SceneUpdating event handler, that is subscribed by the following code:
        //sceneView.SceneUpdating += OnSceneViewOnSceneUpdating;
        //
        // But in this samples project we use call to CommonSample.SubscribeSceneUpdating method to subscribe to the SceneUpdating event.
        // This allows automatic unsubscribing when the sample is unloaded and automatic UI testing
        // (prevented starting animation and using CallSceneUpdating with providing custom elapsedSeconds value).
        base.SubscribeSceneUpdating(UpdateAnimatedGradient);
        
        UpdateAnimatedGradient(elapsedSeconds: 0);
        

        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 25;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 1400;
            targetPositionCamera.TargetPosition = new Vector3(170, 0, -125);
        }
    }
    
    /// <inheritdoc />
    protected override void OnDisposed()
    {
        _gradientMaterial1?.DisposeWithTexture();
        _gradientMaterial2?.DisposeWithTexture();
        base.OnDisposed();
    }

    // ReSharper disable once MemberCanBePrivate.Global - this is public because it is used in automated tests
    public void UpdateAnimatedGradient(float elapsedSeconds)
    {
        if (GpuDevice == null)
            return;
        
        // gradientProgress defines the center of the red circle
        // the red circle thickness is 0.2 and there is also a transition from transparent to red for 0.1
        // go from 0.2 to 0.8 over 1 second
        var gradientProgress = 0.2f + (float)(elapsedSeconds % 1) * 0.6f; 

        _animatedGradientStops ??= new GradientStop[4];

        _animatedGradientStops[0] = new GradientStop(Colors.Transparent, gradientProgress - 0.2f);
        _animatedGradientStops[1] = new GradientStop(Colors.Red,         gradientProgress - 0.1f);
        _animatedGradientStops[2] = new GradientStop(Colors.Red,         gradientProgress + 0.1f);
        _animatedGradientStops[3] = new GradientStop(Colors.Transparent, gradientProgress + 0.2f);

        // Dispose the previous GpuImage.
        // This can be easily done many times because the memory was allocated from a bigger memory block
        // and will be reused with new _animatedGradientTexture objects.
        if (_animatedGradientTexture1 != null)
            _animatedGradientTexture1.Dispose();

        _animatedGradientTexture1 = TextureFactory.CreateGradientTexture(GpuDevice, _animatedGradientStops, isHorizontal: false, name: $"AnimatedGradientTexture_{elapsedSeconds*100:F0}");
        
        if (_gradientMaterial1 != null)
        {
            _gradientMaterial1.DiffuseTextureSamplerType = CommonSamplerTypes.Clamp; // Change sampler from default Mirror to Clamp so that the  border color is used for texture coordinates outside the texture
            _gradientMaterial1.DiffuseTexture = _animatedGradientTexture1;
        }        
        
        
        
        if (_animatedGradientTexture2 != null)
            _animatedGradientTexture2.Dispose();

        var progress = (float)(elapsedSeconds % 1);
        var startColor = new Color4(progress, 0, 0, progress); // semi-transparent red based on progress

        _animatedGradientTexture2 = TextureFactory.CreateGradientTexture(GpuDevice, startColor, endColor: Color4.Transparent, isHorizontal: false, name: $"AnimatedGradientTexture2_{elapsedSeconds*100:F0}");
        
        if (_gradientMaterial2 != null)
        {
            _gradientMaterial2.DiffuseTextureSamplerType = CommonSamplerTypes.Clamp; // Change sampler from default Mirror to Clamp so that the  border color is used for texture coordinates outside the texture 
            _gradientMaterial2.DiffuseTexture = _animatedGradientTexture2;
        }
    }    
}