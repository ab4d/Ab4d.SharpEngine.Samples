using System;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Animation;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class SharpEngineLogoAnimation : IDisposable
{
    public float AnimationDurationIsSeconds = 1.2f;
    
    public string LogoImageResourceName = @"Resources\Textures\sharp-engine-logo.png";

    public float HashModelSize = 100;
    public float HashModelBarThickness = 16;
    public float HashModelBarOffset = 20;

    public MeshModelNode HashModel => _hashModel;
    public PlaneModelNode LogoPlaneModel => _logoPlaneModel;

    public bool IsStarted => _transformationAnimation != null && _transformationAnimation.IsInitialized;

    public event EventHandler? AnimationCompleted;

    #if DEBUG
    public TransformationAnimation? TransformationAnimation => _transformationAnimation;
    public MaterialAnimation? MaterialAnimation => _materialAnimation;
    #endif

    private MeshModelNode _hashModel;
    
    private StandardMaterial _logoTextureMaterial;
    private StandardMaterial _hashModelMaterial;
    private PlaneModelNode _logoPlaneModel;

    private TransformationAnimation? _transformationAnimation;
    private MaterialAnimation? _materialAnimation;
    

    public SharpEngineLogoAnimation(Scene scene, IBitmapIO? bitmapIO = null)
    {
        // Create PlaneModelNode that will show the logo bitmap
        _logoTextureMaterial = new StandardMaterial(LogoImageResourceName, bitmapIO);
        _logoTextureMaterial.Opacity = 0; // hidden at start

        _logoPlaneModel = new PlaneModelNode()
        {
            Position = new Vector3(0, 0, -100),
            Normal = new Vector3(0, 0, 1),
            HeightDirection = new Vector3(0, 1, 0),
            Size = new Vector2(220, 220), // ration: w : h = 0.85 : 1.00,
            Material = _logoTextureMaterial,
            BackMaterial = StandardMaterials.Black,
        };


        // Create hash symbol
        var hashSymbolMesh = MeshFactory.CreateHashSymbolMesh(centerPosition: new Vector3(0, HashModelBarThickness * -0.5f, 0),
                                                              shapeYVector: new Vector3(0, 0, 1),
                                                              extrudeVector: new Vector3(0, HashModelBarThickness, 0),
                                                              size: HashModelSize,
                                                              barThickness: HashModelBarThickness,
                                                              barOffset: HashModelBarOffset,
                                                              name: "HashSymbolMesh");

        _hashModelMaterial = new StandardMaterial(Color3.FromByteRgb(255, 197, 0));

        _hashModel = new Ab4d.SharpEngine.SceneNodes.MeshModelNode(hashSymbolMesh, "HashSymbolModel")
        {
            Material = _hashModelMaterial,
            Transform = new StandardTransform()
        };


        // SetupAnimation
        float endInitialRotationFrameNumber = 0.30f;
        float lastRotateFrameNumber = 0.65f;
        float startColorAnimationFrameNumber = 0.80f;
        float lastColorFrameNumber = 1;


        _transformationAnimation = AnimationBuilder.CreateTransformationAnimation(scene, "SharpEngineLogo-TransformationAnimation");

        _transformationAnimation.AddTarget(_hashModel);

        _transformationAnimation.AddRotateKeyframe(0,                               new Vector3(0, 101, 0));
        _transformationAnimation.AddRotateKeyframe(endInitialRotationFrameNumber,   new Vector3(0, 65, 0));
        _transformationAnimation.AddRotateKeyframe(lastRotateFrameNumber,          new Vector3(73, 24, 55));
        _transformationAnimation.AddRotateKeyframe(startColorAnimationFrameNumber, new Vector3(73, 24, 55));
        _transformationAnimation.AddRotateKeyframe(lastColorFrameNumber,           new Vector3(73, 24, 55));

        _transformationAnimation.AddTranslateKeyframe(0,                               new Vector3(0, 0, 350));
        _transformationAnimation.AddTranslateKeyframe(endInitialRotationFrameNumber,   new Vector3(0, 0, 300));
        _transformationAnimation.AddTranslateKeyframe(lastRotateFrameNumber,          new Vector3(-6, 31, -6));
        _transformationAnimation.AddTranslateKeyframe(startColorAnimationFrameNumber, new Vector3(-6, 31, -6));
        _transformationAnimation.AddTranslateKeyframe(lastColorFrameNumber,           new Vector3(-6, 31, 300));

        _transformationAnimation.Completed += (_, _) => AnimationCompleted?.Invoke(this, EventArgs.Empty);

        _transformationAnimation.SetDuration(AnimationDurationIsSeconds * 1000); // Duration of this animation is only 65% (_lastRotateFrameNumber) of the whole animation length


        _materialAnimation = AnimationBuilder.CreateMaterialsAnimation(scene, "SharpEngineLogo-MaterialAnimation");
        _materialAnimation.AddTarget(_hashModelMaterial);

        _materialAnimation.SetOpacity(0, duration: lastColorFrameNumber - startColorAnimationFrameNumber, delay: startColorAnimationFrameNumber);

        _materialAnimation.SetDuration(AnimationDurationIsSeconds * 1000);
    }

    public void StartAnimation()
    {
        _transformationAnimation?.Start();
        _materialAnimation?.Start();
    }

    public void StopAnimation()
    {
        _transformationAnimation?.Stop();
        _materialAnimation?.Stop();
    }

    public void GotoFirstFrame()
    {
        _transformationAnimation?.Seek(0);
        _materialAnimation?.Seek(0);
    }

    public void GotoLastFrame()
    {
        _transformationAnimation?.Seek(_transformationAnimation.Duration);
        _materialAnimation?.Seek(_materialAnimation.Duration);
    }

    public void SetAnimationDuration(float durationInSeconds)
    {
        _transformationAnimation?.SetDuration(durationInSeconds * 1000);
        _materialAnimation?.SetDuration(durationInSeconds * 1000);
    }

    public void SetHashModelVisibility(bool isVisible)
    {
        _hashModel.Visibility = isVisible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
    }

    public void SetLogoPlaneVisibility(bool isVisible)
    {
        _logoPlaneModel.Visibility = isVisible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
    }

    public void ResetCamera(TargetPositionCamera targetPositionCamera)
    {
        targetPositionCamera.TargetPosition = new Vector3(0, 0, 0);
        targetPositionCamera.Heading = 0;
        targetPositionCamera.Attitude = 0;
        targetPositionCamera.Distance = 400;
    }

    public void SetupLights(Scene scene)
    {
        scene.Lights.Clear();

        scene.SetAmbientLight(intensity: 0.25f);                           // 25% ambient light
        scene.Lights.Add(new DirectionalLight(new Vector3(0, -0.2f, -1))); // light into the screen
    }

    public void UpdateAnimation()
    {
        _transformationAnimation?.Update();
        _materialAnimation?.Update();

        // Alpha of the background logo image is inverted alpha of the 3D hash model
        var showLogoModelAmount = _hashModelMaterial.Opacity;
        var showLogoImageAmount = 1 - showLogoModelAmount;

        _logoTextureMaterial.Opacity = showLogoImageAmount;
        
        // When increasing transparency of the logo model, we also increase the EmissiveColor amount to prevent color bleeding
        _hashModelMaterial.EmissiveColor = new Color3(showLogoImageAmount, showLogoImageAmount, showLogoImageAmount);
    }

    public void Dispose()
    {
        if (!_hashModel.IsDisposed)
        {
            if (_hashModel.Parent != null)
                _hashModel.Parent.Remove(_hashModel);

            _hashModel.Dispose(); // This will also dispose the mesh
        }
        
        if (!_logoPlaneModel.IsDisposed)
        {
            if (_logoPlaneModel.Parent != null)
                _logoPlaneModel.Parent.Remove(_logoPlaneModel);

            _logoPlaneModel.Dispose(); // This will also dispose the mesh
        }

        if (!_logoTextureMaterial.IsDisposed)
            _logoTextureMaterial.Dispose();
    }
}