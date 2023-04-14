using System;
using System.Collections.Generic;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Animation;
using Ab4d.SharpEngine.Samples.Common.Utils;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class AnimationEasingSample : CommonSample
{
    public override string Title => "Animation easing sample";
    public override string? Subtitle => null;

    private TextBlockFactory? _textBlockFactory;

    private List<IAnimation> _allAnimations = new();

    public AnimationEasingSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var boxModelNode = new BoxModelNode(centerPosition: new Vector3(0, -5, 20), size: new Vector3(440, 10, 250), material: StandardMaterials.Silver, name: "BaseBox");
        scene.RootNode.Add(boxModelNode);

        var sphere1 = new SphereModelNode(centerPosition: new Vector3(-100, 10, -50), radius: 10, material: StandardMaterials.Orange, name: "Sphere1");
        var sphere2 = new SphereModelNode(centerPosition: new Vector3(-100, 10, 0), radius: 10, material: StandardMaterials.Green, name: "Sphere2");
        var sphere3 = new SphereModelNode(centerPosition: new Vector3(-100, 10, 50), radius: 10, material: StandardMaterials.Blue, name: "Sphere3");
        var sphere4 = new SphereModelNode(centerPosition: new Vector3(-100, 10, 100), radius: 10, material: StandardMaterials.Yellow, name: "Sphere4");

        scene.RootNode.Add(sphere1);
        scene.RootNode.Add(sphere2);
        scene.RootNode.Add(sphere3);
        scene.RootNode.Add(sphere4);


        AddTextWithBorder(scene, position: new Vector3(-210, 1, -50), text: "No easing", Color4.Black, 14);
        var animation1 = CreateAnimation(scene, sphere1);

        AddTextWithBorder(scene, position: new Vector3(-210, 1, 0), text: "QuadraticEaseInOut\non Animation", Color4.Black, 10);
        var animation2 = CreateAnimation(scene, sphere2);
        animation2.EasingFunction = EasingFunctions.QuadraticEaseInOutFunction;

        AddTextWithBorder(scene, position: new Vector3(-210, 1, 50), text: "QuadraticEaseInOut\non all keyframes", Color4.Black, 10);
        var animation3 = CreateAnimation(scene, sphere3);
        animation3.SetEasingFunctionToAllKeyframes(EasingFunctions.QuadraticEaseInOutFunction);

        AddTextWithBorder(scene, position: new Vector3(-210, 1, 100), text: "QuadraticEaseOut\non all keyframes", Color4.Black, 10);
        var animation4 = CreateAnimation(scene, sphere4);
        animation4.SetEasingFunctionToAllKeyframes(EasingFunctions.QuadraticEaseOutFunction);


        _allAnimations.Add(animation1);
        _allAnimations.Add(animation2);
        _allAnimations.Add(animation3);
        _allAnimations.Add(animation4);


        AddAnimationProgressTicks(scene, animation1, sphere1);
        AddAnimationProgressTicks(scene, animation2, sphere2);
        AddAnimationProgressTicks(scene, animation3, sphere3);
        AddAnimationProgressTicks(scene, animation4, sphere4);

        animation1.Start();
        animation2.Start();
        animation3.Start();
        animation4.Start();


        if (targetPositionCamera != null)
        {
            targetPositionCamera.TargetPosition = new Vector3(0, 0, 60);
            targetPositionCamera.Heading  = 10;
            targetPositionCamera.Attitude = -40;
            targetPositionCamera.Distance = 550;
        }
    }

    protected override void OnDisposed()
    {
        // Stop all animations (this will prevent automatically updating animations when the objects are removed from scene)
        foreach (var animation in _allAnimations)
        {
            if (animation.IsRunning)
                animation.Stop();
        }

        base.OnDisposed();
    }

    private TransformationAnimation CreateAnimation(Scene scene, SceneNode sceneNode)
    {
        var transformationAnimation = AnimationBuilder.CreateTransformationAnimation(scene);
        transformationAnimation.AddTarget(sceneNode);
        transformationAnimation.Loop = true;

        // Advanced X by 100 each second
        transformationAnimation.AddKeyframe(TransformationAnimatedProperties.TranslateX, 0,    0);
        transformationAnimation.AddKeyframe(TransformationAnimatedProperties.TranslateX, 1000, 100);
        transformationAnimation.AddKeyframe(TransformationAnimatedProperties.TranslateX, 2000, 200);
        transformationAnimation.AddKeyframe(TransformationAnimatedProperties.TranslateX, 3000, 300);

        return transformationAnimation;
    }

    private void AddAnimationProgressTicks(Scene scene, TransformationAnimation animation, SphereModelNode animatedSphere, float tickTimeInterval = 100)
    {
        animation.Initialize();

        float time = 0;

        Vector3 startPosition = Vector3.Zero;
        Vector3 tickPosition = Vector3.Zero;

        while (time <= animation.Duration)
        {
            animation.Seek(time); // Move animation to the specified time

            if (animatedSphere.Transform is StandardTransform standardTransform)
            {
                tickPosition = new Vector3(animatedSphere.CenterPosition.X + standardTransform.TranslateX, animatedSphere.CenterPosition.Y, animatedSphere.CenterPosition.Z);

                var wireCrossNode = new WireCrossNode(position: tickPosition);

                if (time % 1000 == 0)
                {
                    // major line tick:
                    wireCrossNode.LineThickness = 2;
                    wireCrossNode.LinesLength = 8;
                    wireCrossNode.LineColor = Colors.Black;
                }
                else
                {
                    // minor line tick:
                    wireCrossNode.LineThickness = 1;
                    wireCrossNode.LinesLength = 6;
                    wireCrossNode.LineColor = Colors.Red;
                }

                scene.RootNode.Add(wireCrossNode);

                if (time == 0)
                    startPosition = tickPosition;
            }

            time += tickTimeInterval;
        }

        var endPosition = tickPosition;

        // Add line under the whole path of the sphere
        var lineNode = new LineNode(startPosition, endPosition, lineColor: Colors.Black, lineThickness: 2);
        scene.RootNode.Add(lineNode);

        // Reset animation time
        animation.Seek(0);
    }

    private void AddTextWithBorder(Scene scene, Vector3 position, string text, Color4 textColor, float fontSize)
    {
        EnsureTextBlockFactory(scene);

        if (_textBlockFactory == null)
            return;

        _textBlockFactory.FontSize = fontSize;
        _textBlockFactory.TextColor = textColor;
        _textBlockFactory.IsSolidColorMaterial = true;

        var textNode = _textBlockFactory.CreateTextBlock(position: position,
            positionType: PositionTypes.Center | PositionTypes.Left,
            text,
            textDirection: new Vector3(1, 0, 0),
            upDirection: new Vector3(0, 0, -1));

        scene.RootNode.Add(textNode);
    }

    private void EnsureTextBlockFactory(Scene scene)
    {
        if (_textBlockFactory != null)
            return;

        _textBlockFactory = new TextBlockFactory(scene, context.BitmapIO);
    }
}