using Ab4d.SharpEngine.Animation;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using System.Numerics;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class CustomAnimationSample : CommonSample
{
    public override string Title => "Custom animation sample";
    public override string? Subtitle => "Custom animation can be creating by subscribing to the Updating event.\nThis sample animates the text, hue color and rotation angle.";

    private const float AnimationDuration = 3; // in seconds
    
    private DateTime _startTime;

    private GroupNode? _textNode1;
    private GroupNode? _textNode2;
    private StandardMaterial _animatedMaterial1;
    private StandardMaterial _animatedMaterial2;
    private StandardTransform? _rotationTransform1;
    private StandardTransform? _rotationTransform2;
    private TextBlockFactory? _textBlockFactory;
    
    private ICommonSampleUIElement? _startAnimationButton;

    public CustomAnimationSample(ICommonSamplesContext context)
        : base(context)
    {
        var color = Color3.FromHsv(hue: 0);
        _animatedMaterial1 = new StandardMaterial(color, "AnimatedMaterial1");
        _animatedMaterial2 = new StandardMaterial(color, "AnimatedMaterial2");
    }

    protected override void OnDisposed()
    {
        StopAnimation();
        base.OnDisposed();
    }

    private void StartStopAnimation()
    {
        if (_startTime == DateTime.MinValue)
            StartAnimation();
        else
            StopAnimation();
    }

    private void StartAnimation()
    {
        if (SceneView == null || _startTime != DateTime.MinValue)
            return;

        // Custom animation is performed in the SceneUpdating event handler
        _startTime = DateTime.Now;
        SceneView.SceneUpdating += OnSceneUpdating;

        _startAnimationButton?.SetText("Stop animation");
    }
    
    private void StopAnimation()
    {
        if (SceneView == null || _startTime == DateTime.MinValue)
            return;

        SceneView.SceneUpdating -= OnSceneUpdating;
        _startTime = DateTime.MinValue;

        _startAnimationButton?.SetText("Start animation");
    }

    private void OnSceneUpdating(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var elapsed = (now - _startTime).TotalSeconds;

        // time is in range from 0 ... 1 (1 is after AnimationDuration - 4 seconds by default)
        var time = (float)elapsed / AnimationDuration; 

        if (time > 1)
        {
            time = 1;
            StopAnimation();
        }

        // Demonstrate the manual usage of easing function. The function expects a time value in range from 0 to 1.
        var easedTime = EasingFunctions.CubicEaseInOutFunction(time);

        // Calculate angle so that we change for 90 degrees per second
        var angleNoEasing   = time * 90f;
        var angleWithEasing = easedTime * 90f;
        

        // Now that we have the new values, we can update the animated properties

        _animatedMaterial1.DiffuseColor = Color3.FromHsv(hue: angleNoEasing);
        _animatedMaterial2.DiffuseColor = Color3.FromHsv(hue: angleWithEasing);
        
        if (_rotationTransform1 != null)
            _rotationTransform1.RotateY = angleNoEasing;
        
        if (_rotationTransform2 != null)
            _rotationTransform2.RotateY = angleWithEasing;

        UpdateHueTextNode(Scene, hueValue1: angleNoEasing, hueValue2: angleWithEasing);
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        var boxCenterPosition1 = new Vector3(100, 0, -100);
        _rotationTransform1 = new StandardTransform()
        {
            PivotPoint = boxCenterPosition1
        };

        var boxModelNode1 = new BoxModelNode(boxCenterPosition1, size: new Vector3(100, 10, 100), _animatedMaterial1)
        {
            Transform = _rotationTransform1
        };

        scene.RootNode.Add(boxModelNode1);
        
        
        var boxCenterPosition2 = new Vector3(100, 0, 100);
        _rotationTransform2 = new StandardTransform()
        {
            PivotPoint = boxCenterPosition2
        };

        var boxModelNode2 = new BoxModelNode(boxCenterPosition2, size: new Vector3(100, 10, 100), _animatedMaterial2)
        {
            Transform = _rotationTransform2
        };

        scene.RootNode.Add(boxModelNode2);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 0;
            targetPositionCamera.Attitude = -40;
            targetPositionCamera.Distance = 600;
        }


        _textBlockFactory = await context.GetTextBlockFactoryAsync();
        _textBlockFactory.FontSize = 20;

        UpdateHueTextNode(scene, hueValue1: 0, hueValue2: 0);
    }

    private void UpdateHueTextNode(Scene? scene, float hueValue1, float hueValue2)
    {
        if (scene == null || _textBlockFactory == null)
            return;

        if (_textNode1 != null)
        {
            scene.RootNode.Remove(_textNode1);
            _textNode1.Dispose();
        }
        
        if (_textNode2 != null)
        {
            scene.RootNode.Remove(_textNode2);
            _textNode2.Dispose();
        }

        _textNode1 = _textBlockFactory.CreateTextBlock(position: new Vector3(-130, 0, -100),
                                                       positionType: PositionTypes.Left,
                                                       $"Hue / angle: {hueValue1:F0}\n(no easing)",
                                                       textDirection: new Vector3(1, 0, 0),
                                                       upDirection: new Vector3(0, 0, -1));

        scene.RootNode.Add(_textNode1);
        
        
        _textNode2 = _textBlockFactory.CreateTextBlock(position: new Vector3(-130, 0, 100),
                                                       positionType: PositionTypes.Left,
                                                       $"Hue / angle: {hueValue2:F0}\n(with easing)",
                                                       textDirection: new Vector3(1, 0, 0),
                                                       upDirection: new Vector3(0, 0, -1));

        scene.RootNode.Add(_textNode2);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(alignment: PositionTypes.BottomRight);

        _startAnimationButton = ui.CreateButton("Start animation", StartStopAnimation);
    }
}