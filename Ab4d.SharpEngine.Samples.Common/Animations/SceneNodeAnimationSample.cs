using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Common.Utils;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Animation;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class SceneNodeAnimationSample : CommonSample
{
    public override string Title => "SceneNode animation sample";
    public override string? Subtitle => "Animated property is specified as string (for example \"Radius\") and then the value is animated by using Reflection";

    private ICommonSampleUIProvider? _uiProvider;

    private TargetPositionCamera? _targetPositionCamera;

    private List<IAnimation> _animations;

    private GroupNode? _animatedObjectsGroup;
    private PlaneModelNode? _planeModelNode;
    private DirectionalLight? _shadowDirectionalLight;
    private PlanarShadowMeshCreator? _planarShadowMeshCreator;
    private MeshModelNode? _shadowModel;

    public SceneNodeAnimationSample(ICommonSamplesContext context)
        : base(context)
    {
        _animations = new List<IAnimation>();
    }

    private void CreateAnimationTests(Scene scene)
    {
        AddSphereAnimationSample(scene, new Vector3(-200, 40, -100), 20, "Radius", delegate (SceneNodeAnimation animation)
        {
            animation.Set("Radius", propertyValue: 50);
        });
        
        AddSphereAnimationSample(scene, new Vector3(0, 40, -100), 20, "CenterPosition", delegate (SceneNodeAnimation animation)
        {
            animation.Set("CenterPosition", propertyValue: new Vector3(40, 100, 0));
        });
        
        AddBoxAnimationSample(scene, new Vector3(200, 0, -100), "Size", delegate (SceneNodeAnimation animation)
        {
            animation.Set("Size", propertyValue: new Vector3(40, 60, 30)); // From (60, 20, 40)
        });

        
        AddWireGridAnimationSample(scene, new Vector3(-200, 5, 100), "HeightDirection", delegate (SceneNodeAnimation animation)
        {
            animation.Set("HeightDirection", propertyValue: new Vector3(0, 1, 0)); // HeightDirection will transition from (0, 0, -1) to (0, 1, 0) - it will be normalized in the WireGrid
        });
        
        AddWireGridAnimationSample(scene, new Vector3(0, 5, 100), "Size", delegate (SceneNodeAnimation animation)
        {
            animation.Set("Size", propertyValue: new Vector3(80, 60, 0)); // WireGridNode.Size is Vector2 - so only X and Y from the Vector3 that is specified here will be used
        });
        
        AddWireGridAnimationSample(scene, new Vector3(200, 5, 100), "WidthCellsCount\nHeightCellsCount", delegate (SceneNodeAnimation animation)
        {
            // WireGridNode.WidthCellsCount and HeightCellsCount are int type - we can specify float as animated values - the value will be converted to int (not rounded)
            animation.Set("WidthCellsCount", propertyValue: 20f); 
            animation.Set("HeightCellsCount", propertyValue: 20f); 
        });


        // Start all animations
        foreach (var animation in _animations)
        {
            animation.SetDuration(3000); // 5 seconds

            // Temporarily set IsAutomaticallyUpdating to false
            // This will not be needed in the future when the animation will be able to automatically subscribe to Scene or SceneView Updating event
            animation.IsAutomaticallyUpdating = false;

            //animation.Start();
        }
    }

    private void AddSphereAnimationSample(Scene scene, Vector3 centerPosition, float radius, string description, Action<SceneNodeAnimation> setupAnimation)
    {
        if (_animatedObjectsGroup == null)
            return;

        int index = _animations.Count;

        var sphereModelNode = new SphereModelNode(name: $"Sphere_{index}")
        {
            CenterPosition = centerPosition,
            Radius = radius,
            Material = StandardMaterials.Orange
        };

        _animatedObjectsGroup.Add(sphereModelNode);

        var textPosition = new Vector3(centerPosition.X, 5, centerPosition.Z + 50);
        AddTextWithBorder(scene, textPosition, description, Colors.Black);


        AddSceneNodeAnimationSample(scene, sphereModelNode, setupAnimation);
    }
    
    private void AddBoxAnimationSample(Scene scene, Vector3 position, string description, Action<SceneNodeAnimation> setupAnimation)
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
        
        AddSceneNodeAnimationSample(scene, boxModelNode, setupAnimation);
    }
    
    private void AddWireGridAnimationSample(Scene scene, Vector3 position, string description, Action<SceneNodeAnimation> setupAnimation)
    {
        if (_animatedObjectsGroup == null)
            return;

        int index = _animations.Count;

        var wireGridModelNode = new WireGridNode(name: $"WireGrid_{index}")
        {
            CenterPosition = position,
            Size = new Vector2(60, 40),
            WidthDirection = new Vector3(1, 0, 0),
            HeightDirection = new Vector3(0, 0, -1),
            WidthCellsCount = 5,
            HeightCellsCount = 5,
            MajorLineColor = Colors.Orange,
            MinorLineColor = Colors.Orange,
        };

        _animatedObjectsGroup.Add(wireGridModelNode);

        var textPosition = position + new Vector3(0, 0, 40);
        AddTextWithBorder(scene, textPosition, description, Colors.Black);
        
        AddSceneNodeAnimationSample(scene, wireGridModelNode, setupAnimation);
    }
    
    private void AddSceneNodeAnimationSample(Scene scene, SceneNode sceneNode, Action<SceneNodeAnimation> setupAnimation)
    {
        var animation = AnimationBuilder.CreateSceneNodeAnimation(scene, $"Animation_{sceneNode.Name}");
        animation.AddTarget(sceneNode);
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
        sceneView.SceneUpdating += SceneViewOnSceneUpdating;
    }

    private void SceneViewOnSceneUpdating(object? sender, EventArgs e)
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


        //// Show Axes (red = x, green = y, blue = z)
        //var axisLineNode = new AxisLineNode();
        //scene.RootNode.Add(axisLineNode);

        CreateAnimationTests(scene);
        SetupPlanarShadow(scene);
    }

    protected override Camera OnCreateCamera()
    {
        _targetPositionCamera = new TargetPositionCamera()
        {
            Heading = -15,
            Attitude = -30,
            Distance = 900,
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
        var textBlockFactory = context.GetTextBlockFactory();

        textBlockFactory.FontSize = 14;
        textBlockFactory.TextColor = textColor;
        textBlockFactory.IsSolidColorMaterial = true;

        var textNode = textBlockFactory.CreateTextBlock(position: topCenterPosition,
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
        bool isAnyAnimationRunning = false;
        foreach (var animation in _animations)
            isAnyAnimationRunning |= animation.IsRunning;

        if (isAnyAnimationRunning && _planarShadowMeshCreator != null && _shadowModel != null && _shadowDirectionalLight != null)
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
        if (SceneView != null)
            SceneView.SceneUpdating -= SceneViewOnSceneUpdating;

        base.OnDisposed();
    }
}