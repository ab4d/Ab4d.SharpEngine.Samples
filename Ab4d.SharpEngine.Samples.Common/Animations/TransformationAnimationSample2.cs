using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine.Animation;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class TransformationAnimationSample2 : CommonSample
{
    public override string Title => "Transformation animation sample";
    public override string? Subtitle => null;

    private ICommonSampleUIProvider? _uiProvider;

    private TargetPositionCamera? _targetPositionCamera;

    private IAnimation? _animation;

    private GroupNode? _animatedObjectsGroup;
    private PlaneModelNode? _planeModelNode;
    private DirectionalLight? _shadowDirectionalLight;
    private TextBlockFactory? _textBlockFactory;
    private PlanarShadowNode? _planarShadowNode;

    public TransformationAnimationSample2(ICommonSamplesContext context)
        : base(context)
    {
        ShowCameraAxisPanel = true;
    }

    private void CreateAnimationTests(Scene scene)
    {
        if (_animatedObjectsGroup == null)
            return;

        var boxModelNode = new BoxModelNode(name: "AnimatedBox")
        {
            Position = new Vector3(-150, 0, -50),
            PositionType = PositionTypes.Bottom | PositionTypes.Center,
            Size = new Vector3(60, 20, 40),
            Material = StandardMaterials.Orange
        };

        _animatedObjectsGroup.Add(boxModelNode);
        

        var description =
@"Animation steps:
TranslateY, ScaleY
TranslateZ, RotateY
TranslateY, ScaleY,
TranslateZ, RotateY";

        var textPosition = new Vector3(100, 0, -50);
        AddTextWithBorder(scene, textPosition, description, Colors.Black, fontSize: 20);


        var animation = AnimationBuilder.CreateTransformationAnimation(scene, "Animation");
        animation.AddTarget(boxModelNode);
        //animation.AddTargets("Box*"); // we can also add multiple targets and also use pattern filter to get targets by name


        animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 100, duration: 1000);
        animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 0,   duration: 1000, delay: 1000);

        animation.Set(TransformationAnimatedProperties.TranslateZ, propertyValue: 200, duration: 1000, delay: 1000);
        animation.Set(TransformationAnimatedProperties.TranslateZ, propertyValue: -50, duration: 1000, delay: 1000);

        animation.Set(TransformationAnimatedProperties.ScaleY, propertyValue: 2f,   duration: 500);
        animation.Set(TransformationAnimatedProperties.ScaleY, propertyValue: 1,    duration: 500);
        animation.Set(TransformationAnimatedProperties.ScaleY, propertyValue: 1.3f, duration: 500, delay: 1000);
        animation.Set(TransformationAnimatedProperties.ScaleY, propertyValue: 1,    duration: 500);

        animation.Set(TransformationAnimatedProperties.RotateY, propertyValue: 180,  duration: 1000, delay: 1000);
        animation.Set(TransformationAnimatedProperties.RotateY, propertyValue: -180, duration: 1000, delay: 1000);


        animation.SetDuration(5000); // 5 seconds

        animation.Updated += delegate(object? sender, EventArgs args)
        {
            UpdatePlanarShadow();
        };

        //animation.Start(); // This will be called from start button click

        _animation = animation;
    }

    private void AddTextWithBorder(Scene scene, Vector3 topCenterPosition, string text, Color4 textColor, float fontSize)
    {
        if (_textBlockFactory == null)
            return;

        _textBlockFactory.TextColor = textColor;
        _textBlockFactory.FontSize = fontSize;
        _textBlockFactory.TextColor = textColor;

        var textNode = _textBlockFactory.CreateTextBlock(position: topCenterPosition,
                                                         positionType: PositionTypes.Top | PositionTypes.Center,
                                                         text,
                                                         textDirection: new Vector3(1, 0, 0),
                                                         upDirection: new Vector3(0, 0, -1));

        scene.RootNode.Add(textNode);
    }

    private void SetupPlanarShadow(Scene scene)
    {
        if (_animatedObjectsGroup == null || _planeModelNode == null || _shadowDirectionalLight == null)
            return;

        _planarShadowNode = new PlanarShadowNode(_animatedObjectsGroup);
        _planarShadowNode.SetPlane(_planeModelNode, offset: 0.05f);

        _planarShadowNode.ApplyDirectionalLight(_shadowDirectionalLight.Direction);

        scene.RootNode.Add(_planarShadowNode);
    }

    private void UpdatePlanarShadow()
    {
        if (_animation != null && _animation.IsRunning && _planarShadowNode != null && _shadowDirectionalLight != null)
            _planarShadowNode.ApplyDirectionalLight(_shadowDirectionalLight.Direction, updateTransformations: true);
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        _textBlockFactory = await context.GetTextBlockFactoryAsync();
        _textBlockFactory.FontSize = 14;
        _textBlockFactory.IsSolidColorMaterial = true;

        _planeModelNode = new PlaneModelNode("BasePlane")
        {
            Position = new Vector3(0, -5f, 0),
            PositionType = PositionTypes.Center,
            Normal = new Vector3(0, 1, 0),
            HeightDirection = new Vector3(0, 0, 1),
            Size = new Vector2(600, 400),
            Material = StandardMaterials.Gray,
            BackMaterial = StandardMaterials.Black
        };

        scene.RootNode.Add(_planeModelNode);


        _animatedObjectsGroup = new GroupNode("AnimatedObjectsGroup");
        scene.RootNode.Add(_animatedObjectsGroup);


        CreateAnimationTests(scene);
        SetupPlanarShadow(scene);
    }

    protected override Camera OnCreateCamera()
    {
        _targetPositionCamera = new TargetPositionCamera()
        {
            Heading = -15,
            Attitude = -30,
            Distance = 850,
            TargetPosition = new Vector3(0, 40, 0),
            ShowCameraLight = ShowCameraLightType.Never
        };

        return _targetPositionCamera;
    }

    protected override void OnCreateLights(Scene scene)
    {
        // Add ambient light and one directions lights that will be also used for planar shadow
        scene.SetAmbientLight(intensity: 0.3f);

        _shadowDirectionalLight = new DirectionalLight(new Vector3(-0.4f, -1, -0.2f));
        scene.Lights.Add(_shadowDirectionalLight);

        SetupPlanarShadow(scene);

        //base.OnCreateLights(scene);
    }


    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        _uiProvider = ui;

        ui.CreateStackPanel(alignment: PositionTypes.BottomRight);

        ui.CreateButton("Start animation", () =>
        {
            if (_animation == null)
                return;

            _animation.Stop();
            _animation.Rewind();
            _animation.Start();
        });

        ui.CreateButton("Dump animation to VS Output", () =>
        {
            if (_animation == null)
                return;

            // Before we can get information about the animation, we need to initialize it (but not if the animation is already running)
            // (this is automatically done when the animation is started)
            if (!_animation.IsRunning)
                _animation.Initialize(); 

            var infoText = _animation.GetInfoText();
            System.Diagnostics.Debug.WriteLine(infoText);
        });
    }


    protected override void OnDisposed()
    {
        // Stop all animations (this will prevent automatically updating animations when the objects are removed from scene)
        if (_animation != null)
            _animation.Stop();

        base.OnDisposed();
    }
}