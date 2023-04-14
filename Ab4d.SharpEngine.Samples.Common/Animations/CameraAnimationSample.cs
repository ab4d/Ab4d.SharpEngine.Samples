using System;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Animation;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class CameraAnimationSample : CommonSample
{
    public override string Title => "CameraAnimation sample";
    public override string? Subtitle => null;

    private CameraAnimation? _currentCameraAnimation;

    public CameraAnimationSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override void OnCreateScene(Scene scene)
    {
        var boxModelNode = new BoxModelNode()
        {
            Position = new Vector3(0, -5, 0),
            PositionType = PositionTypes.Center,
            Size = new Vector3(150, 10, 150),
            Material = StandardMaterials.Silver
        };

        scene.RootNode.Add(boxModelNode);


        var teapotFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources/Models/Teapot.obj");

        var readerObj = new ReaderObj();
        var teapotNode = readerObj.ReadSceneNodes(teapotFileName);

        Ab4d.SharpEngine.Utilities.ModelUtils.ChangeMaterial(teapotNode, StandardMaterials.Gold.SetSpecular(Colors.White, 32));
        Ab4d.SharpEngine.Utilities.ModelUtils.PositionAndScaleSceneNode(teapotNode, position: new Vector3(0, 0, 0), positionType: PositionTypes.Center | PositionTypes.Bottom, finalSize: new Vector3(100, 100, 100));

        scene.RootNode.Add(teapotNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -45;
            targetPositionCamera.Distance = 300;
        }
    }

    protected override void OnDisposed()
    {
        // Stop all animations (this will prevent automatically updating animations when the objects are removed from scene)
        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        base.OnDisposed();
    }

    private void AnimateCameraRotationTo(float targetHeading, float targetAttitude)
    {
        if (targetPositionCamera == null)
            return;

        // NOTE:
        // We could also just call RotateTo, but here we demonstrate how to use CameraAnimation
        //targetPositionCamera.RotateTo(targetHeading, targetAttitude, animationDurationInMilliseconds: 1000, EasingFunctions.QuadraticEaseInOutFunction);

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        var cameraAnimation = AnimationBuilder.CreateCameraAnimation(targetPositionCamera);
        cameraAnimation.SetDuration(TimeSpan.FromSeconds(1));
        cameraAnimation.EasingFunction = EasingFunctions.QuadraticEaseInOutFunction;

        if (!float.IsNaN(targetHeading))
            cameraAnimation.Set(CameraAnimatedProperties.Heading, targetHeading);

        if (!float.IsNaN(targetAttitude))
            cameraAnimation.Set(CameraAnimatedProperties.Attitude, targetAttitude);

        cameraAnimation.Start();
        _currentCameraAnimation = cameraAnimation;
    }

    private void AnimateCameraDistanceFor(float distanceFactor)
    {
        if (targetPositionCamera == null)
            return;

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        var cameraAnimation = AnimationBuilder.CreateCameraAnimation(targetPositionCamera);
        //cameraAnimation.SetDuration(500); // we will set duration with Set method
        cameraAnimation.EasingFunction = EasingFunctions.QuadraticEaseInOutFunction;

        float newDistance = targetPositionCamera.Distance * distanceFactor;
        cameraAnimation.Set(CameraAnimatedProperties.Distance, newDistance, duration: 500); // duration in ms

        cameraAnimation.Start();
        _currentCameraAnimation = cameraAnimation;
    }
    
    private void AnimateCameraDistanceTo(float newDistance)
    {
        if (targetPositionCamera == null)
            return;

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        var cameraAnimation = AnimationBuilder.CreateCameraAnimation(targetPositionCamera);
        //cameraAnimation.SetDuration(500); // we will set duration with Set method
        cameraAnimation.EasingFunction = EasingFunctions.QuadraticEaseInOutFunction;

        cameraAnimation.Set(CameraAnimatedProperties.Distance, newDistance, duration: 500); // duration in ms

        cameraAnimation.Start();
        _currentCameraAnimation = cameraAnimation;
    }

    private void PlayAnimation1()
    {
        if (targetPositionCamera == null)
            return;

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        var cameraAnimation = AnimationBuilder.CreateCameraAnimation(targetPositionCamera);

        // If animation camera's Heading is not 30 and Attitude is not -20 (with 0.5 degree of tolerance),
        // than start with animating to the start position: Heading = 30; Attitude = -20
        if (Math.Abs(targetPositionCamera.Heading - 30) > 0.5 ||
            Math.Abs(targetPositionCamera.Attitude - -20) > 0.5)
        {
            cameraAnimation.Set(CameraAnimatedProperties.Heading, 30, duration: 500);
            cameraAnimation.Set(CameraAnimatedProperties.Attitude, -20, duration: 500);
        }

        cameraAnimation.Set(CameraAnimatedProperties.Heading, 90, duration: 1000);
        cameraAnimation.Set(CameraAnimatedProperties.Attitude, 90, duration: 1000, delay: 1000);  // wait until Heading animates to 90, then start attitude animation from -20 to 90
        cameraAnimation.Set(CameraAnimatedProperties.Heading, 180, duration: 1000, delay: 1000);  // wait until Attitude animates to 90
        cameraAnimation.Set(CameraAnimatedProperties.Attitude, -20, duration: 1000);              // no delay, when Attitude is animated to 90, immediately start animation to -20
        cameraAnimation.Set(CameraAnimatedProperties.Heading, 30, duration: 2000);                // slowly animate to start position

        // Set easing function to all keyframe (we could also set that in each Set method call):
        cameraAnimation.SetEasingFunctionToAllKeyframes(EasingFunctions.QuadraticEaseInOutFunction);

        // We could also set keyframe to keyframes for individual animated properties:
        //cameraAnimation.SetEasingFunctionToAllKeyframes(CameraAnimatedProperties.Heading, EasingFunctions.QuadraticEaseInOutFunction);
        //cameraAnimation.SetEasingFunctionToAllKeyframes(CameraAnimatedProperties.Attitude, EasingFunctions.QuadraticEaseInOutFunction);
        
        cameraAnimation.Start();
        _currentCameraAnimation = cameraAnimation;
    }
    
    private void PlayAnimation2()
    {
        if (targetPositionCamera == null)
            return;

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        var cameraAnimation = AnimationBuilder.CreateCameraAnimation(targetPositionCamera);

        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Heading, time: 0,    targetPositionCamera.Heading);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Heading, time: 1000, 30);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Heading, time: 2000, 120);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Heading, time: 3000, 210);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Heading, time: 4000, 300);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Heading, time: 6000, 30);

        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Attitude, time: 0,    targetPositionCamera.Attitude);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Attitude, time: 1000, -30);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Attitude, time: 4000, -30);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Attitude, time: 5000, -20);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Attitude, time: 6000, 0);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Attitude, time: 7000, -20);

        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Distance, time: 0,    targetPositionCamera.Distance);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Distance, time: 1000, 150);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Distance, time: 2000, 150);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Distance, time: 2500, 400);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Distance, time: 3000, 150);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Distance, time: 5000, 150);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Distance, time: 6000, 40);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.Distance, time: 7000, 300);

        cameraAnimation.AddKeyframe(CameraAnimatedProperties.TargetPosition, time: 0,    targetPositionCamera.TargetPosition);
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.TargetPosition, time: 1000, new Vector3(-50, 50, 0));
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.TargetPosition, time: 2000, new Vector3(-50, 50, 0));
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.TargetPosition, time: 3000, new Vector3(50, 50, 0));
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.TargetPosition, time: 4000, new Vector3(50, 50, 0));
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.TargetPosition, time: 5000, new Vector3(0, 50, 0));
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.TargetPosition, time: 6000, new Vector3(0, 50, 0));
        cameraAnimation.AddKeyframe(CameraAnimatedProperties.TargetPosition, time: 7000, new Vector3(0, 0, 0));

        cameraAnimation.SetEasingFunctionToAllKeyframes(EasingFunctions.QuadraticEaseInOutFunction);

        cameraAnimation.Start();
        _currentCameraAnimation = cameraAnimation;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Right | PositionTypes.Bottom);

        ui.CreateButton("Rotate to top down view", () => AnimateCameraRotationTo(targetHeading: float.NaN, targetAttitude: -90));
        ui.CreateButton("Rotate to front view", ()    => AnimateCameraRotationTo(targetHeading: 0, targetAttitude: 0));
        ui.CreateButton("Rotate to left view", ()     => AnimateCameraRotationTo(targetHeading: 90, targetAttitude: 0));
        ui.CreateButton("Rotate to side view", ()     => AnimateCameraRotationTo(targetHeading: 30, targetAttitude: -20));

        ui.AddSeparator();
        ui.CreateButton("Zoom out", () => AnimateCameraDistanceFor(distanceFactor: 1.5f));
        ui.CreateButton("To standard distance", () => AnimateCameraDistanceTo(newDistance: 300));

        ui.AddSeparator();
        ui.CreateButton("Play animation 1", () => PlayAnimation1());
        ui.CreateButton("Play animation 2", () => PlayAnimation2());

        base.OnCreateUI(ui);
    }
}