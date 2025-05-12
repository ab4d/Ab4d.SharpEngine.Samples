using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.PostProcessing;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.Advanced;

public class PostProcessingSample : CommonSample
{
    public override string Title => "Post-processing";

    private bool _isBlackAndWhitePostProcess = true;
    private bool _isToonShadingPostProcess = false;
    private bool _isGaussianBlurPostProcess = true;

    private BlackAndWhitePostProcess _blackAndWhitePostProcess;
    private ToonShadingPostProcess _toonShadingPostProcess;
    private GaussianBlurPostProcess _gaussianBlurPostProcess1;
    private GaussianBlurPostProcess _gaussianBlurPostProcess2;

    public PostProcessingSample(ICommonSamplesContext context)
        : base(context)
    {
        _blackAndWhitePostProcess = new BlackAndWhitePostProcess();
        _toonShadingPostProcess = new ToonShadingPostProcess();

        // GaussianBlur requires two passes: horizontal and vertical
        _gaussianBlurPostProcess1 = new GaussianBlurPostProcess(isVerticalPass: false);
        _gaussianBlurPostProcess2 = new GaussianBlurPostProcess(isVerticalPass: true);
    }

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        if (SceneView != null)
            SceneView.PostProcesses.Clear();

        base.OnDisposed();
    }

    protected override void OnCreateScene(Scene scene)
    {
        var dragonMesh = TestScenes.GetTestMesh(TestScenes.StandardTestScenes.Dragon, 
                                                position: new Vector3(0, 0, 0), 
                                                positionType: PositionTypes.Bottom, 
                                                finalSize: new Vector3(50, 50, 50));

        var dragonModelNode = new MeshModelNode(dragonMesh, StandardMaterials.Silver, "DragonModel");
        scene.RootNode.Add(dragonModelNode);


        var wireGridNode = new WireGridNode()
        {
            CenterPosition = new Vector3(15, 0, 0),
            Size = new Vector2(130, 100),
            WidthCellsCount = 13,
            HeightCellsCount = 10,
        };
        scene.RootNode.Add(wireGridNode);

        var sphereModelNode1 = new SphereModelNode(centerPosition: new Vector3(-10, 10, 30), radius: 10, StandardMaterials.Gold.SetSpecular(specularPower: 32), "SphereModel1");
        scene.RootNode.Add(sphereModelNode1);

        var sphereModelNode2 = new SphereModelNode(centerPosition: new Vector3(-10, 10, -30), radius: 10, StandardMaterials.Gold.SetSpecular(specularPower: 32), "SphereModel2");
        scene.RootNode.Add(sphereModelNode2);


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 120;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 150;
        }
    }

    /// <inheritdoc />
    protected override void OnSceneViewInitialized(SceneView sceneView)
    {
        sceneView.BackgroundColor = Colors.Aqua;

        if (_isBlackAndWhitePostProcess)
            sceneView.PostProcesses.Add(_blackAndWhitePostProcess);

        if (_isGaussianBlurPostProcess)
        {
            sceneView.PostProcesses.Add(_gaussianBlurPostProcess1);
            sceneView.PostProcesses.Add(_gaussianBlurPostProcess2);
        }

        base.OnSceneViewInitialized(sceneView);
    }


    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Post processes:", isHeader: true);

        ui.CreateCheckBox("Black and white", _isBlackAndWhitePostProcess, isChecked =>
        {
            if (isChecked)
                SceneView!.PostProcesses.Add(_blackAndWhitePostProcess);
            else
                SceneView!.PostProcesses.Remove(_blackAndWhitePostProcess);

            _isBlackAndWhitePostProcess = isChecked;
        });
        
        ui.AddSeparator();
        
        
        ui.CreateCheckBox("Toon Shading", _isToonShadingPostProcess, isChecked =>
        {
            if (isChecked)
                SceneView!.PostProcesses.Add(_toonShadingPostProcess);
            else
                SceneView!.PostProcesses.Remove(_toonShadingPostProcess);

            _isToonShadingPostProcess = isChecked;
        });
        
        ui.AddSeparator();

        ui.CreateCheckBox("Gaussian Blur", _isGaussianBlurPostProcess, isChecked =>
        {
            if (isChecked)
            {
                SceneView!.PostProcesses.Add(_gaussianBlurPostProcess1);
                SceneView!.PostProcesses.Add(_gaussianBlurPostProcess2);
            }
            else
            {
                SceneView!.PostProcesses.Remove(_gaussianBlurPostProcess1);
                SceneView!.PostProcesses.Remove(_gaussianBlurPostProcess2);
            }

            _isGaussianBlurPostProcess = isChecked;
        });

        ui.CreateSlider(0, 5, () => _gaussianBlurPostProcess1.BlurScale,
            newValue => _gaussianBlurPostProcess1.BlurScale = newValue,
            width: 100,
            keyText: "      BlurScale",
            keyTextWidth: 100,
            formatShownValueFunc: sliderValue => $"{sliderValue:F2}");
        
        ui.CreateSlider(0, 5, () => _gaussianBlurPostProcess1.BlurStrength,
            newValue => _gaussianBlurPostProcess1.BlurStrength = newValue,
            width: 100,
            keyText: "      BlurStrength",
            keyTextWidth: 100,
            formatShownValueFunc: sliderValue => $"{sliderValue:F2}");

        ui.CreateButton("BG", () => SceneView.BackgroundColor = Color4.Transparent);
    }
}