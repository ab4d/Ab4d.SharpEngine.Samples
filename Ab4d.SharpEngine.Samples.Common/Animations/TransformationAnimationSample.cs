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
    public override string Title => "TransformationAnimation sample";
    public override string? Subtitle => null;

    private ICommonSampleUIProvider? _uiProvider;

    private TargetPositionCamera? _targetPositionCamera;

    private TextBlockFactory? _textBlockFactory;

    private List<IAnimation> _animations;

    private GroupNode? _animatedObjectsGroup;
    private PlaneModelNode? _planeModelNode;
    private DirectionalLight? _shadowDirectionalLight;
    private PlanarShadowMeshCreator? _planarShadowMeshCreator;
    private MeshModelNode? _shadowModel;

    public TransformationAnimationSample(ICommonSamplesContext context)
        : base(context)
    {
        _animations = new List<IAnimation>();
    }

    private void CreateAnimationTests(Scene scene)
    {
        AddAnimationSample(scene, new Vector3(-200, 0, -100), "TranslateY", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 50);
        });

        AddAnimationSample(scene, new Vector3(0, 0, -100), "TranslateY\r\nTranslateX", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 50);
            animation.Set(TransformationAnimatedProperties.TranslateX, propertyValue: 50);
        });

        AddAnimationSample(scene, new Vector3(200, 0, -100), "Scale", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.Scale, propertyValue: 2);
        });


        AddAnimationSample(scene, new Vector3(-200, 0, 100), "ScaleY\r\nTranslateY", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.ScaleY, propertyValue: 2);
            animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 50);
        });

        AddAnimationSample(scene, new Vector3(0, 0, 100), "RotateY", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.RotateY, propertyValue: 180);
        });

        AddAnimationSample(scene, new Vector3(200, 0, 100), "RotateY\r\nScale\r\nTranslateY", delegate (TransformationAnimation animation)
        {
            animation.Set(TransformationAnimatedProperties.RotateY, propertyValue: 90);
            animation.Set(TransformationAnimatedProperties.ScaleY, propertyValue: 0.5f);
            animation.Set(TransformationAnimatedProperties.ScaleX, propertyValue: 1.4f); // when y is reduced to 50%, we need to scale x and z by 1.4 to preserve the volume
            animation.Set(TransformationAnimatedProperties.ScaleZ, propertyValue: 1.4f);
            animation.Set(TransformationAnimatedProperties.TranslateY, propertyValue: 50);
        });


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

    protected override void OnCreateScene(Scene scene)
    {
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


        // Show Axes (red = x, green = y, blue = z)
        var axisLineNode = new AxisLineNode();
        scene.RootNode.Add(axisLineNode);

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
        scene.Lights.Add(new AmbientLight(0.3f));

        _shadowDirectionalLight = new DirectionalLight(new Vector3(-0.4f, -1, -0.2f));
        scene.Lights.Add(_shadowDirectionalLight);

        SetupPlanarShadow(scene);

        //base.OnCreateLights(scene);
    }


    private void AddTextWithBorder(Scene scene, Vector3 topCenterPosition, string text, Color4 textColor)
    {
        EnsureTextBlockFactory(scene);

        if (_textBlockFactory == null)
            return;

        var textNode = _textBlockFactory.CreateTextBlock(position: topCenterPosition,
                                                         positionType: PositionTypes.Top | PositionTypes.Center,
                                                         text,
                                                         textDirection: new Vector3(1, 0, 0),
                                                         upDirection: new Vector3(0, 0, -1));
        
        _textBlockFactory.FontSize = 14;
        _textBlockFactory.TextColor = textColor;
        _textBlockFactory.IsSolidColorMaterial = true;

        scene.RootNode.Add(textNode);
    }

    private void EnsureTextBlockFactory(Scene scene)
    {
        if (_textBlockFactory != null)
            return;

        _textBlockFactory = new TextBlockFactory(scene, context.BitmapIO);
        //_textBlockFactory.BackgroundColor = Colors.LightYellow;
        //_textBlockFactory.BorderThickness = 1;
        //_textBlockFactory.BorderColor = Colors.DimGray;
    }

    private void SetupPlanarShadow(Scene scene)
    {
        if (_animatedObjectsGroup == null || _planeModelNode == null || _shadowDirectionalLight == null)
            return;

        // Create PlanarShadowMeshCreator
        _planarShadowMeshCreator = new PlanarShadowMeshCreator(_animatedObjectsGroup);
        _planarShadowMeshCreator.SetPlane(_planeModelNode.GetCenterPosition(), _planeModelNode.Normal, _planeModelNode.HeightDirection, _planeModelNode.Size);
        _planarShadowMeshCreator.ClipToPlane = false; // No need to clip shadow to plane because plane is big enough (when having smaller plane, turn this on - this creates a lot of additional objects on GC)

        _planarShadowMeshCreator.ApplyDirectionalLight(_shadowDirectionalLight.Direction);

        if (_planarShadowMeshCreator.ShadowMesh != null)
        {
            _shadowModel = new MeshModelNode(_planarShadowMeshCreator.ShadowMesh, StandardMaterials.DimGray, "PlanarShadowModel");
            _shadowModel.Transform = new Ab4d.SharpEngine.Transformations.TranslateTransform(0, 0.05f, 0); // Lift the shadow 3D model slightly above the ground

            scene.RootNode.Add(_shadowModel);
        }
    }

    private void UpdatePlanarShadow()
    {
        if (_planarShadowMeshCreator != null && _shadowModel != null && _shadowDirectionalLight != null)
        {
            _planarShadowMeshCreator.UpdateGroupNode();
            _planarShadowMeshCreator.ApplyDirectionalLight(_shadowDirectionalLight.Direction);

            _shadowModel.Mesh = _planarShadowMeshCreator.ShadowMesh;
        }
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
        if (_textBlockFactory != null)
        {
            _textBlockFactory.Dispose();
            _textBlockFactory = null;
        }

        if (SceneView != null)
            SceneView.SceneUpdating -= OnSceneViewOnSceneUpdating;

        base.OnDisposed();
    }
}