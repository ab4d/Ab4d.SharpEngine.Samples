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

public class TransformationAnimationSample : CommonSample
{
    public override string Title => "Transformation animation sample";
    public override string? Subtitle => null;

    private ICommonSampleUIProvider? _uiProvider;

    private TargetPositionCamera? _targetPositionCamera;

    private List<IAnimation> _animations;

    private GroupNode? _animatedObjectsGroup;
    private PlaneModelNode? _planeModelNode;
    private DirectionalLight? _shadowDirectionalLight;
    private MeshModelNode? _shadowModel;
    private TextBlockFactory? _textBlockFactory;
    private PlanarShadowNode? _planarShadowNode;

    public TransformationAnimationSample(ICommonSamplesContext context)
        : base(context)
    {
        _animations = new List<IAnimation>();

        ShowCameraAxisPanel = true;
    }

    private void CreateAnimationTests(Scene scene)
    {
        AddAnimationSample(scene, new Vector3(-200, 0, -100), "TranslateY", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 50);
        });

        AddAnimationSample(scene, new Vector3(0, 0, -100), "TranslateY\nTranslateX", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 50);
            animation.Set(TransformationAnimatedProperties.TranslateX, propertyValue: 50);
        });

        AddAnimationSample(scene, new Vector3(200, 0, -100), "Scale", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.Scale, propertyValue: 2);
        });


        AddAnimationSample(scene, new Vector3(-200, 0, 100), "ScaleY\nTranslateY", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.ScaleY, propertyValue: 2);
            animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 50);
        });

        AddAnimationSample(scene, new Vector3(0, 0, 100), "RotateY", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.RotateY, propertyValue: 180);
        });

        AddAnimationSample(scene, new Vector3(200, 0, 100), "Quaternion\nrotation", delegate (TransformationAnimation animation)
        {
            animation.SetRotate(new Quaternion(0, 0.707f, 0, 0.707f)); // rotate by 90 degrees around the y-axis
            animation.SetRotate(Quaternion.Identity);                  // then animate to no rotation
            animation.SetRotate(new Quaternion(0.707f, 0, 0, 0.707f)); // and then 90 degrees around the x-axis
            animation.SetRotate(Quaternion.Identity);                  // and back to no rotation

            // Instead of calling animation.SetRotate, we could also set individual properties:
            //animation.Set(TransformationAnimatedProperties.QuaternionY, propertyValue: 0.707f);
            //animation.Set(TransformationAnimatedProperties.QuaternionW, propertyValue: 0.707f);
        });
        

        // Another sample with multiple animations (there is not place in 4x2 samples to show that):
        //AddAnimationSample(scene, new Vector3(200, 0, 100), "RotateY\r\nScale\r\nTranslateY", delegate (TransformationAnimation animation)
        //{
        //    animation.Set(TransformationAnimatedProperties.RotateY, propertyValue: 90);
        //    animation.Set(TransformationAnimatedProperties.ScaleY, propertyValue: 0.5f);
        //    animation.Set(TransformationAnimatedProperties.ScaleX, propertyValue: 1.4f); // when y is reduced to 50%, we need to scale x and z by 1.4 to preserve the volume
        //    animation.Set(TransformationAnimatedProperties.ScaleZ, propertyValue: 1.4f);
        //    animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 50);
        //});


        // Start all animations
        foreach (var animation in _animations)
        {
            animation.SetDuration(5000); // 5 seconds

            // Temporarily set IsAutomaticallyUpdating to false
            // This will not be needed in the future when the animation will be able to automatically subscribe to Scene or SceneView Updating event
            animation.IsAutomaticallyUpdating = false;

            //animation.Start();
        }
    }

    private void AddAnimationSample(Scene scene, Vector3 position, string description, Action<TransformationAnimation> setupAnimation)
    {
        if (_animatedObjectsGroup == null)
            return;

        int index = _animations.Count;

        var boxModelNode = new BoxModelNode(name: $"Box_{index}")
        {
            Position = position,
            PositionType = PositionTypes.Bottom | PositionTypes.Center,
            Size = new Vector3(60, 20, 40),
            Material = StandardMaterials.Orange
        };

        _animatedObjectsGroup.Add(boxModelNode);

        var textPosition = position + new Vector3(0, 0, 40);
        AddTextWithBorder(scene, textPosition, description, Colors.Black);


        var animation = AnimationBuilder.CreateTransformationAnimation(scene, $"Animation_{index}");
        animation.AddTarget(boxModelNode);
        animation.Direction = AnimationDirections.Alternate;
        //animation.AddTargets("Box*"); // we can also add multiple targets and also use pattern filter to get targets by name

        // We will manually update all animations by calling Update method from SceneUpdating.
        // This is required because after all animations are updated, we need to manually update the planar shadow (see OnSceneViewInitialized method)
        animation.IsAutomaticallyUpdating = false;

        setupAnimation(animation);

        _animations.Add(animation);
    }

    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        // In this sample we manually update the animation by calling Update method from SceneUpdating event handler
        sceneView.SceneUpdating += OnSceneViewOnSceneUpdating;
    }

    private void OnSceneViewOnSceneUpdating(object? sender, EventArgs args)
    {
        foreach (var animation in _animations)
            animation.Update();

        UpdatePlanarShadow();
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


    private void AddTextWithBorder(Scene scene, Vector3 topCenterPosition, string text, Color4 textColor)
    {
        if (_textBlockFactory == null)
            return;

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
        bool isAnyAnimationRunning = false;
        foreach (var animation in _animations)
            isAnyAnimationRunning |= animation.IsRunning;

        if (isAnyAnimationRunning && _planarShadowNode != null && _shadowDirectionalLight != null)
            _planarShadowNode.ApplyDirectionalLight(_shadowDirectionalLight.Direction, updateTransformations: true);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        _uiProvider = ui;

        ui.CreateStackPanel(alignment: PositionTypes.BottomRight);

        ui.CreateButton("Start animation", () =>
        {
            foreach (var animation in _animations)
            {
                animation.Stop();
                animation.Rewind();
                animation.Start();
            }
        });
    }
    
    protected override void OnDisposed()
    {
        if (SceneView != null)
            SceneView.SceneUpdating -= OnSceneViewOnSceneUpdating;

        base.OnDisposed();
    }
}