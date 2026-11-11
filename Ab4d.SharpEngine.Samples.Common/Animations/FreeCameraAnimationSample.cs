using System;
using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SharpEngine.Animation;
using Ab4d.SharpEngine.Cameras;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class FreeCameraAnimationSample : CommonSample
{
    public override string Title => "FreeCamera animation sample";
    public override string? Subtitle => "This sample shows how to animate FreeCamera by animating CameraPosition, TargetPosition and UpDirection properties.";

    private FreeCamera? _freeCamera;
    private bool _isSphericalInterpolation = true;
    private CameraAnimation? _currentCameraAnimation;

    public FreeCameraAnimationSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    protected override Camera OnCreateCamera()
    {
        _freeCamera = new FreeCamera()
        {
            CameraPosition = new Vector3(-106, 212, 184),
            TargetPosition = new Vector3(0, 0, 0),
            UpDirection    = new Vector3(0.35f, 0.71f, -0.61f)
        };

        return _freeCamera;
    }

    protected override async Task OnCreateSceneAsync(Scene scene)
    {
        var boxModelNode = new BoxModelNode()
        {
            Position = new Vector3(0, -5, 0),
            PositionType = PositionTypes.Center,
            Size = new Vector3(150, 10, 150),
            Material = StandardMaterials.Silver
        };

        scene.RootNode.Add(boxModelNode);


        var teapotNode = await base.GetCommonSceneAsync(scene, CommonScenes.TeapotLowResolution, 
                                                        position: new Vector3(0, 0, 0), 
                                                        positionType: PositionTypes.Center | PositionTypes.Bottom, 
                                                        finalSize: new Vector3(100, 100, 100));

        ModelUtils.ChangeMaterial(teapotNode, StandardMaterials.Gold.SetSpecular(Colors.White, 32));

        scene.RootNode.Add(teapotNode);
    }

    protected override void OnDisposed()
    {
        // Stop all animations (this will prevent automatically updating animations when the objects are removed from scene)
        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        base.OnDisposed();
    }
    
    private void AnimateFreeCamera(Vector3? cameraPosition = null, Vector3? targetPosition = null, Vector3? upDirection = null)
    {
        if (_freeCamera == null)
            return;

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        // We can use RotateTo or create an animation manually (see below)
        if (cameraPosition.HasValue && upDirection.HasValue)
        {
            if (targetPosition.HasValue)
            {
                // animate all CameraPosition, TargetPosition and UpDirection
                _freeCamera.RotateTo(cameraPosition.Value,
                                     targetPosition.Value, 
                                     upDirection.Value, 
                                     animationDurationInMilliseconds: 1000, 
                                     easingFunction: EasingFunctions.QuadraticEaseInOutFunction, 
                                     isSphericalInterpolation: _isSphericalInterpolation);
            }
            else
            {
                // animate only CameraPosition and UpDirection
                _freeCamera.RotateTo(cameraPosition.Value,
                                     upDirection.Value, 
                                     animationDurationInMilliseconds: 1000, 
                                     easingFunction: EasingFunctions.QuadraticEaseInOutFunction, 
                                     isSphericalInterpolation: _isSphericalInterpolation);
            }

            _currentCameraAnimation = _freeCamera.CameraAnimation;

            return;
        }

        // Manually create the FreeCamera animation.

        // NOTE:
        // When animating FreeCamera's CameraPosition, TargetPosition and UpDirection,
        // then you need to add keyframe info for all specified key frames, e.g.
        // you cannot set CameraPosition for time 0 and 1000, and TargetPosition for time 200 and 500.
        // In this case keyframes for CameraPosition and TargetPosition must be specified for time 0, 200, 500 and 1000.

        var cameraAnimation = AnimationBuilder.CreateCameraAnimation(_freeCamera);

        cameraAnimation.FreeCameraAnimationType = _isSphericalInterpolation ? CameraAnimation.FreeCameraAnimationTypes.SphericalInterpolation 
                                                                            : CameraAnimation.FreeCameraAnimationTypes.LinearInterpolation;

        if (cameraPosition != null)
        {
            cameraAnimation.AddKeyframe(CameraAnimatedProperties.CameraPosition, time: 0, _freeCamera.CameraPosition);
            cameraAnimation.AddKeyframe(CameraAnimatedProperties.CameraPosition, time: 1000, cameraPosition.Value);
        }
        
        if (targetPosition != null)
        {
            cameraAnimation.AddKeyframe(CameraAnimatedProperties.TargetPosition, time: 0, _freeCamera.TargetPosition);
            cameraAnimation.AddKeyframe(CameraAnimatedProperties.TargetPosition, time: 1000, targetPosition.Value);
        }

        if (upDirection != null)
        {
            cameraAnimation.AddKeyframe(CameraAnimatedProperties.UpDirection, time: 0, _freeCamera.UpDirection);
            cameraAnimation.AddKeyframe(CameraAnimatedProperties.UpDirection, time: 1000, upDirection.Value);
        }

        cameraAnimation.SetEasingFunctionToAllKeyframes(EasingFunctions.QuadraticEaseInOutFunction);

        cameraAnimation.Start();
        _currentCameraAnimation = cameraAnimation;
    }

    private void RotateFreeCamera(float headingChange = 0, float attitudeChange = 0)
    {
        if (_freeCamera == null)
            return;

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        _freeCamera.RotateCamera(headingChange, attitudeChange, bankChange: 0, 
                                 animationDurationInMilliseconds: 5000, 
                                 easingFunction: EasingFunctions.QuadraticEaseInOutFunction, 
                                 isSphericalInterpolation: _isSphericalInterpolation);

        _currentCameraAnimation = _freeCamera.CameraAnimation;
    }

    private void AnimateCameraPosition(float distanceFactor)
    {
        if (_freeCamera == null)
            return;

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        // NOTE:
        // For FreeCamera we can also animate Distance by using CameraAnimatedProperties.Distance.
        // Instead of that we could also animate the CameraPosition

        var cameraAnimation = AnimationBuilder.CreateCameraAnimation(_freeCamera);

        //cameraAnimation.SetDuration(500); // we will set duration with Set method
        cameraAnimation.EasingFunction = EasingFunctions.QuadraticEaseInOutFunction;

        var lookDirection = _freeCamera.TargetPosition - _freeCamera.CameraPosition;
        float currentDistance = lookDirection.Length();

        lookDirection /= currentDistance; // Normalize
        float newDistance = currentDistance * distanceFactor;

        var newCameraPosition = _freeCamera.TargetPosition - lookDirection * newDistance;

        cameraAnimation.Set(CameraAnimatedProperties.CameraPosition, newCameraPosition, duration: 500); // duration in ms

        cameraAnimation.Start();
        _currentCameraAnimation = cameraAnimation;
    }
    
    private void AnimateCameraDistanceFor(float distanceFactor)
    {
        if (_freeCamera == null)
            return;

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        // NOTE:
        // For FreeCamera we can also animate Distance by using CameraAnimatedProperties.Distance.
        // Instead of that we could also animate the CameraPosition

        var cameraAnimation = AnimationBuilder.CreateCameraAnimation(_freeCamera);

        //cameraAnimation.SetDuration(500); // we will set duration with Set method
        cameraAnimation.EasingFunction = EasingFunctions.QuadraticEaseInOutFunction;

        var lookDirection = _freeCamera.TargetPosition - _freeCamera.CameraPosition;
        float currentDistance = lookDirection.Length();

        float newDistance = currentDistance * distanceFactor;
        cameraAnimation.Set(CameraAnimatedProperties.Distance, newDistance, duration: 500); // duration in ms

        cameraAnimation.Start();
        _currentCameraAnimation = cameraAnimation;
    }
    
    private void AnimateCameraDistanceTo(float newDistance)
    {
        if (_freeCamera == null)
            return;

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        var cameraAnimation = AnimationBuilder.CreateCameraAnimation(_freeCamera);

        //cameraAnimation.SetDuration(500); // we will set duration with Set method
        cameraAnimation.EasingFunction = EasingFunctions.QuadraticEaseInOutFunction;

        cameraAnimation.Set(CameraAnimatedProperties.Distance, newDistance, duration: 500); // duration in ms

        cameraAnimation.Start();
        _currentCameraAnimation = cameraAnimation;
    }

    private void PlayAnimation()
    {
        if (_freeCamera == null)
            return;

        if (_currentCameraAnimation != null)
            _currentCameraAnimation.Stop();

        var cameraAnimation = AnimationBuilder.CreateCameraAnimation(_freeCamera);

        cameraAnimation.FreeCameraAnimationType = _isSphericalInterpolation ? CameraAnimation.FreeCameraAnimationTypes.SphericalInterpolation 
                                                                            : CameraAnimation.FreeCameraAnimationTypes.LinearInterpolation;


        // NOTE:
        // When animating FreeCamera's CameraPosition, TargetPosition and UpDirection,
        // then you need to add keyframe info for all specified key frames, e.g.
        // you cannot set CameraPosition for time 0 and 1000, and TargetPosition for time 200 and 500.
        // In this case keyframes for CameraPosition and TargetPosition must be specified for time 0, 200, 500 and 1000.

        cameraAnimation.Set(CameraAnimatedProperties.CameraPosition, new Vector3(200, 0, 0), duration: 1000);
        cameraAnimation.Set(CameraAnimatedProperties.TargetPosition, new Vector3(0, 0, 0),   duration: 1000);
        cameraAnimation.Set(CameraAnimatedProperties.UpDirection,    new Vector3(0, 1, 0),   duration: 1000); 
        
        cameraAnimation.Set(CameraAnimatedProperties.CameraPosition, new Vector3(225, 0, 0), duration: 250);
        cameraAnimation.Set(CameraAnimatedProperties.TargetPosition, new Vector3(0, 0, 0),   duration: 250);
        cameraAnimation.Set(CameraAnimatedProperties.UpDirection,    new Vector3(0, 0, 1),   duration: 250);
                                                                                                       
        cameraAnimation.Set(CameraAnimatedProperties.CameraPosition, new Vector3(250, 0, 0), duration: 250);
        cameraAnimation.Set(CameraAnimatedProperties.TargetPosition, new Vector3(0, 0, 0),   duration: 250);
        cameraAnimation.Set(CameraAnimatedProperties.UpDirection,    new Vector3(0, -1, 0),  duration: 250);
                                                                                                       
        cameraAnimation.Set(CameraAnimatedProperties.CameraPosition, new Vector3(275, 0, 0), duration: 250);
        cameraAnimation.Set(CameraAnimatedProperties.TargetPosition, new Vector3(0, 0, 0),   duration: 250);
        cameraAnimation.Set(CameraAnimatedProperties.UpDirection,    new Vector3(0, 0, -1),  duration: 250);
                                                                                                       
        cameraAnimation.Set(CameraAnimatedProperties.CameraPosition, new Vector3(300, 0, 0), duration: 250);
        cameraAnimation.Set(CameraAnimatedProperties.TargetPosition, new Vector3(0, 0, 0),   duration: 250);
        cameraAnimation.Set(CameraAnimatedProperties.UpDirection,    new Vector3(0, 1, 0),   duration: 250);
        
        cameraAnimation.Set(CameraAnimatedProperties.CameraPosition, new Vector3(-106, 212, 184),       duration: 1000);
        cameraAnimation.Set(CameraAnimatedProperties.TargetPosition, new Vector3(0, 0, 0),              duration: 1000);
        cameraAnimation.Set(CameraAnimatedProperties.UpDirection,    new Vector3(0.35f, 0.71f, -0.61f), duration: 1000);
                                                                                                        
        cameraAnimation.Set(CameraAnimatedProperties.CameraPosition, new Vector3(-106, 212, 184),       duration: 500);
        cameraAnimation.Set(CameraAnimatedProperties.TargetPosition, new Vector3(100, 0, 0),            duration: 500);
        cameraAnimation.Set(CameraAnimatedProperties.UpDirection,    new Vector3(0.35f, 0.71f, -0.61f), duration: 500);
                                                                                                        
        cameraAnimation.Set(CameraAnimatedProperties.CameraPosition, new Vector3(-106, 212, 184),       duration: 500);
        cameraAnimation.Set(CameraAnimatedProperties.TargetPosition, new Vector3(0, 0, 0),              duration: 500);
        cameraAnimation.Set(CameraAnimatedProperties.UpDirection,    new Vector3(0.35f, 0.71f, -0.61f), duration: 500);

        // Set easing function to all keyframe (we could also set that in each Set method call):
        cameraAnimation.SetEasingFunctionToAllKeyframes(EasingFunctions.QuadraticEaseInOutFunction);

        cameraAnimation.Start();
        _currentCameraAnimation = cameraAnimation;
    }
    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Right | PositionTypes.Bottom);


        ui.CreateCheckBox("SphericalInterpolation (?):When checked then FreeCameraAnimationType is set to SphericalInterpolation instead of LinearInterpolation.\nThis mode rotates the LookDirection and UpDirection using spherical interpolation instead of linear interpolation.",
                          _isSphericalInterpolation, isChecked => _isSphericalInterpolation = isChecked);

        ui.AddSeparator();
        ui.CreateButton("Rotate horizontally", () => RotateFreeCamera(headingChange: 20));
        ui.CreateButton("Rotate vertically", () => RotateFreeCamera(attitudeChange: 20));

        ui.AddSeparator();
        ui.CreateButton("Rotate to top down view",  () => AnimateFreeCamera(cameraPosition: new Vector3(0, 300, 0),  upDirection: new Vector3(0, 0, -1)));
        ui.CreateButton("Rotate to bottom up view", () => AnimateFreeCamera(cameraPosition: new Vector3(0, -300, 0), upDirection: new Vector3(0, 0, 1)));
        
        ui.AddSeparator();
        ui.CreateButton("Rotate to front view", () => AnimateFreeCamera(cameraPosition: new Vector3(0, 0, 300),  upDirection: new Vector3(0, 1, 0)));
        ui.CreateButton("Rotate to left view",  () => AnimateFreeCamera(cameraPosition: new Vector3(-300, 0, 0), upDirection: new Vector3(0, 1, 0)));
        ui.CreateButton("Rotate to back view",  () => AnimateFreeCamera(cameraPosition: new Vector3(0, 0, -300), upDirection: new Vector3(0, 1, 0)));
        ui.CreateButton("Rotate to side view",  () => AnimateFreeCamera(cameraPosition: new Vector3(300, 0, 0),  upDirection: new Vector3(0, 1, 0)));
        
        ui.AddSeparator();
        ui.CreateButton("Zoom out with CameraPosition", () => AnimateCameraPosition(distanceFactor: 1.5f));
        ui.CreateButton("Zoom out with Distance", () => AnimateCameraDistanceFor(distanceFactor: 1.5f));
        ui.CreateButton("Zoom to standard distance", () => AnimateCameraDistanceTo(newDistance: 300));

        ui.AddSeparator();
        ui.CreateButton("Play animation", PlayAnimation);
        
        base.OnCreateUI(ui);
    }
}