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
    private bool _isGammaCorrectionPostProcess = false;
    private bool _isColorOverlayPostProcess = false;
    private bool _isGaussianBlurPostProcess = true;

    private int _blurFilterSize = 7; // MAX value is 15 (=SeparableKernelPostProcess.MaxFilterSize)

    private BlackAndWhitePostProcess _blackAndWhitePostProcess;
    private ToonShadingPostProcess _toonShadingPostProcess;
    private GammaCorrectionPostProcess _gammaCorrectionPostProcess;
    private ColorOverlayPostProcess _colorOverlayPostProcess;
    private GaussianBlurPostProcess _gaussianBlurPostProcess1;
    private GaussianBlurPostProcess _gaussianBlurPostProcess2;

    public PostProcessingSample(ICommonSamplesContext context)
        : base(context)
    {
        _blackAndWhitePostProcess = new BlackAndWhitePostProcess();
        _toonShadingPostProcess = new ToonShadingPostProcess();
        _gammaCorrectionPostProcess = new GammaCorrectionPostProcess() { Gamma = 2.2f}; // 2.2 is also a default value
        _colorOverlayPostProcess = new ColorOverlayPostProcess() { AddedColor = Color4.Black, ColorMultiplier = Colors.Red }; // Default color for AddedColor is Black; default color for ColorMultiplier is White (those settings do not change the rendered image)

        // GaussianBlur requires two passes: horizontal and vertical
        _gaussianBlurPostProcess1 = new GaussianBlurPostProcess(isVerticalBlur: false, filterSize: _blurFilterSize) { BlurRangeScale = 3 };
        _gaussianBlurPostProcess2 = new GaussianBlurPostProcess(isVerticalBlur: true, filterSize: _blurFilterSize) { BlurRangeScale = 3 };
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
        UpdatePostProcesses();

        base.OnSceneViewInitialized(sceneView);
    }

    private void UpdatePostProcesses()
    {
        if (SceneView == null)
            return;
        
        var sceneView = SceneView;

        // We clear all post processes and add only the selected ones.
        // This preserves the order of post processes so that they are executed as they are shown in the UI.
        sceneView.PostProcesses.Clear();
        
        if (_isBlackAndWhitePostProcess)
            SceneView!.PostProcesses.Add(_blackAndWhitePostProcess);
        
        if (_isToonShadingPostProcess)
            SceneView!.PostProcesses.Add(_toonShadingPostProcess);
        
        if (_isGammaCorrectionPostProcess)
            SceneView!.PostProcesses.Add(_gammaCorrectionPostProcess);
        
        if (_isColorOverlayPostProcess)
            SceneView!.PostProcesses.Add(_colorOverlayPostProcess);
        
        if (_isGaussianBlurPostProcess)
        {
            SceneView!.PostProcesses.Add(_gaussianBlurPostProcess1);
            SceneView!.PostProcesses.Add(_gaussianBlurPostProcess2);
        }
    }

    /// <inheritdoc />
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateLabel("Post processes:", isHeader: true);

        ui.CreateCheckBox("Black and white", _isBlackAndWhitePostProcess, isChecked =>
        {
            _isBlackAndWhitePostProcess = isChecked;
            UpdatePostProcesses();
        });
        
        ui.AddSeparator();
        
        
        ui.CreateCheckBox("Toon Shading", _isToonShadingPostProcess, isChecked =>
        {
            _isToonShadingPostProcess = isChecked;
            UpdatePostProcesses();
        });
        
        ui.AddSeparator();
        
        
        ui.CreateCheckBox("Gamma Correction", _isGammaCorrectionPostProcess, isChecked =>
        {
            _isGammaCorrectionPostProcess = isChecked;
            UpdatePostProcesses();
        });
        
        ui.CreateSlider(0.25f, 3f, () => _gammaCorrectionPostProcess.Gamma,
            newValue => _gammaCorrectionPostProcess.Gamma = newValue,
            width: 100,
            keyText: "      Gamma:",
            keyTextWidth: 140,
            formatShownValueFunc: sliderValue => $"{sliderValue:F2}");
        
        ui.AddSeparator();
        
        
        ui.CreateCheckBox("Color Overlay", _isColorOverlayPostProcess, isChecked =>
        {
            _isColorOverlayPostProcess = isChecked;
            UpdatePostProcesses();
        });

        ui.CreateComboBox(new string[] { "Black", "Red", "Green", "Blue", "#555555" },
            (selectedIndex, selectedText) =>
            {
                if (Color4.TryParse(selectedText, out var addedColor))
                    _colorOverlayPostProcess.AddedColor = addedColor;
            },
            selectedItemIndex: 0,
            width: 100,
            keyText: "      Added Color:",
            keyTextWidth: 140);
        
        ui.CreateComboBox(new string[] { "White", "Red", "Green", "Blue", "#888888FF", "#444444FF", "#44444444" }, // the last value sets alpha to #44 - note that all other values also need to be multiplied by alpha because we are using alpha-premultiplied colors
            (selectedIndex, selectedText) =>
            {
                if (Color4.TryParse(selectedText, out var colorMultiplier))
                    _colorOverlayPostProcess.ColorMultiplier = colorMultiplier;
            },
            selectedItemIndex: 1,
            width: 100,
            keyText: "      Color Multiplier:",
            keyTextWidth: 140);

        ui.AddSeparator();
        

        ui.CreateCheckBox("Gaussian Blur", _isGaussianBlurPostProcess, isChecked =>
        {
            _isGaussianBlurPostProcess = isChecked;
            UpdatePostProcesses();
        });
        
        var blurFilterSizes = new[] { 3, 5, 7, 9, 11, 13, 15 };
        var blurFilterSizeTexts = blurFilterSizes.Select(s => s.ToString()).ToArray();
        var selectedItemIndex = Array.IndexOf(blurFilterSizes, _blurFilterSize);
        
        ui.CreateComboBox(blurFilterSizeTexts, (selectedIndex, selectedText) =>
        {
            _gaussianBlurPostProcess1.Dispose(); // This will also remove the post process from the SceneView.PostProcesses
            _gaussianBlurPostProcess2.Dispose();
            
            _blurFilterSize = blurFilterSizes[selectedIndex];
            
            _gaussianBlurPostProcess1 = new GaussianBlurPostProcess(isVerticalBlur: false, filterSize: _blurFilterSize) { BlurRangeScale = _gaussianBlurPostProcess1.BlurRangeScale, BlurStandardDeviation = _gaussianBlurPostProcess1.BlurStandardDeviation };
            _gaussianBlurPostProcess2 = new GaussianBlurPostProcess(isVerticalBlur: true,  filterSize: _blurFilterSize) { BlurRangeScale = _gaussianBlurPostProcess1.BlurRangeScale, BlurStandardDeviation = _gaussianBlurPostProcess1.BlurStandardDeviation };
            
            if (_isGaussianBlurPostProcess)
            {
                SceneView!.PostProcesses.Add(_gaussianBlurPostProcess1);
                SceneView!.PostProcesses.Add(_gaussianBlurPostProcess2);
            }
        }, selectedItemIndex, keyText: "      Filter size:", keyTextWidth: 140);
        
        ui.CreateSlider(0, 5, () => _gaussianBlurPostProcess1.BlurStandardDeviation,
            newValue =>
            {
                _gaussianBlurPostProcess1.BlurStandardDeviation = newValue;
                _gaussianBlurPostProcess2.BlurStandardDeviation = newValue;
            },
            width: 100,
            keyText: "      StandardDeviation:",
            keyTextWidth: 140,
            formatShownValueFunc: sliderValue => $"{sliderValue:F2}");
        
        ui.CreateSlider(0, 5, () => _gaussianBlurPostProcess1.BlurRangeScale,
            newValue =>
            {
                _gaussianBlurPostProcess1.BlurRangeScale = newValue;
                _gaussianBlurPostProcess2.BlurRangeScale = newValue;
            },
            width: 100,
            keyText: "      BlurRangeScale:",
            keyTextWidth: 140,
            formatShownValueFunc: sliderValue => $"{sliderValue:F2}");
    }
}