using Ab4d.SharpEngine.Animation;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Animations;

public class MaterialAnimationSample : CommonSample
{
    public override string Title => "MaterialAnimation sample";
    public override string? Subtitle => null;

    private StandardMaterial? _teapotMaterial;

    private MaterialAnimation? _currentAnimation;

    public MaterialAnimationSample(ICommonSamplesContext context)
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

        var objImporter = new ObjImporter();
        var teapotNode = objImporter.Import(teapotFileName);

        _teapotMaterial = StandardMaterials.Gold.SetSpecular(Colors.White, 32);

        Ab4d.SharpEngine.Utilities.ModelUtils.ChangeMaterial(teapotNode, _teapotMaterial);
        Ab4d.SharpEngine.Utilities.ModelUtils.PositionAndScaleSceneNode(teapotNode, position: new Vector3(0, 0, 0), positionType: PositionTypes.Center | PositionTypes.Bottom, finalSize: new Vector3(100, 100, 100));

        scene.RootNode.Add(teapotNode);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 30;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 300;
        }
    }

    protected override void OnDisposed()
    {
        // Stop all animations (this will prevent automatically updating animations when the objects are removed from scene)
        if (_currentAnimation != null)
            _currentAnimation.Stop();

        base.OnDisposed();
    }

    private void AnimateOpacity(float targetOpacity)
    {
        if (_teapotMaterial == null || Scene == null)
            return;

        if (_currentAnimation != null)
            _currentAnimation.Stop();

        var materialAnimation = AnimationBuilder.CreateMaterialsAnimation(Scene);
        materialAnimation.AddTarget(_teapotMaterial);
        materialAnimation.SetDuration(TimeSpan.FromSeconds(1));
        materialAnimation.EasingFunction = EasingFunctions.QuadraticEaseInOutFunction;

        materialAnimation.SetOpacity(targetOpacity);

        materialAnimation.Start();
        _currentAnimation = materialAnimation;
    }
    
    private void AnimateColor(Color3 targetColor, bool isHsvColorSpace)
    {
        if (_teapotMaterial == null || Scene == null)
            return;

        if (_currentAnimation != null)
            _currentAnimation.Stop();

        var materialAnimation = AnimationBuilder.CreateMaterialsAnimation(Scene);
        materialAnimation.AddTarget(_teapotMaterial);
        materialAnimation.SetDuration(TimeSpan.FromSeconds(1));
        materialAnimation.EasingFunction = EasingFunctions.QuadraticEaseInOutFunction;

        materialAnimation.ColorInterpolationMode = isHsvColorSpace ? MaterialAnimation.ColorInterpolationModes.HsvColorSpace : MaterialAnimation.ColorInterpolationModes.RgbColorSpace;
        materialAnimation.SetDiffuseColor(targetColor);

        materialAnimation.Start();
        _currentAnimation = materialAnimation;
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Right | PositionTypes.Bottom);

        ui.CreateButton("Hide", () => AnimateOpacity(targetOpacity: 0));
        ui.CreateButton("Show", () => AnimateOpacity(targetOpacity: 1));

        ui.CreateButton("Animate color to green (RGB)", () => AnimateColor(Colors.Green, isHsvColorSpace: false));
        ui.CreateButton("Animate color to red (RGB)", () => AnimateColor(Colors.Red, isHsvColorSpace: false));
        ui.CreateButton("Animate color to green (HSV)", () => AnimateColor(Colors.Green, isHsvColorSpace: true));
        ui.CreateButton("Animate color to red (HSV)", () => AnimateColor(Colors.Red, isHsvColorSpace: true));

        base.OnCreateUI(ui);
    }
}