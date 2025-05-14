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
    private bool _isSoberEdgeDetectionPostProcess = false;
    private bool _isExpandPostProcess = false;
    private bool _isGaussianBlurPostProcess = true;

    private int _blurFilterSize = 7; // MAX value is 15 (=SeparableKernelPostProcess.MaxFilterSize)

    private BlackAndWhitePostProcess _blackAndWhitePostProcess;
    private ToonShadingPostProcess _toonShadingPostProcess;
    private GammaCorrectionPostProcess _gammaCorrectionPostProcess;
    private ColorOverlayPostProcess _colorOverlayPostProcess;
    private SoberEdgeDetectionPostProcess _soberEdgeDetectionPostProcess;
    private ExpandPostProcess _expandPostProcess1;
    private ExpandPostProcess _expandPostProcess2;
    private GaussianBlurPostProcess _gaussianBlurPostProcess1;
    private GaussianBlurPostProcess _gaussianBlurPostProcess2;

    public PostProcessingSample(ICommonSamplesContext context)
        : base(context)
    {
        _blackAndWhitePostProcess = new BlackAndWhitePostProcess();
        _toonShadingPostProcess = new ToonShadingPostProcess();
        _gammaCorrectionPostProcess = new GammaCorrectionPostProcess() { Gamma = 2.2f};                                              // 2.2 is also a default value
        _colorOverlayPostProcess = new ColorOverlayPostProcess() { AddedColor = Color4.Black, ColorMultiplier = Colors.Red };        // Default color for AddedColor is Black; default color for ColorMultiplier is White (those settings do not change the rendered image)
        _soberEdgeDetectionPostProcess = new SoberEdgeDetectionPostProcess() { EdgeThreshold = 0.05f, AddEdgeToCurrentColor = true}; // Use default settings
        
        // ExpandPostProcess requires two passes: horizontal and vertical
        _expandPostProcess1 = new ExpandPostProcess(isVerticalRenderingPass: false, expansionWidth: 3, backgroundColor: Color4.Transparent); // Expand for 2 pixels
        _expandPostProcess2 = new ExpandPostProcess(isVerticalRenderingPass: true, expansionWidth: 3, backgroundColor: Color4.Transparent);  

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
        
        if (_isSoberEdgeDetectionPostProcess)
            SceneView!.PostProcesses.Add(_soberEdgeDetectionPostProcess);
        
        if (_isColorOverlayPostProcess)
            SceneView!.PostProcesses.Add(_colorOverlayPostProcess);
        
        if (_isExpandPostProcess)
        {
            SceneView!.PostProcesses.Add(_expandPostProcess1);
            SceneView!.PostProcesses.Add(_expandPostProcess2);
        }
        
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
        
        
        ui.CreateCheckBox("Sober Edge Detection", _isSoberEdgeDetectionPostProcess, isChecked =>
        {
            _isSoberEdgeDetectionPostProcess = isChecked;
            UpdatePostProcesses();
        });

        var edgeThresholds = new float[] { 0.01f, 0.02f, 0.05f, 0.1f, 0.2f, 0.3f, 0.5f, 0.8f, 0.9f, 1f}; 
        var edgeThresholdTexts = edgeThresholds.Select(v => v.ToString()).ToArray(); 
        ui.CreateComboBox(edgeThresholdTexts,
            (selectedIndex, selectedText) => _soberEdgeDetectionPostProcess.EdgeThreshold = edgeThresholds[selectedIndex],
            selectedItemIndex: 2,
            width: 100,
            keyText: "      Edge Threshold:",
            keyTextWidth: 140);
        
        // CheckBox cannot have left margin
        //ui.CreateCheckBox("      AddEdgeToCurrentColor", true, isChecked => _soberEdgeDetectionPostProcess.AddEdgeToCurrentColor = isChecked);
        
        ui.CreateComboBox(new string[] { "false", "true" },
            (selectedIndex, selectedText) => _soberEdgeDetectionPostProcess.AddEdgeToCurrentColor = selectedIndex > 0,
            selectedItemIndex: 1,
            width: 60,
            keyText: "      AddEdgeToCurrentColor:",
            keyTextWidth: 180);
        
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
        
        
        ui.CreateCheckBox("Expand", _isExpandPostProcess, isChecked =>
        {
            _isExpandPostProcess = isChecked;
            UpdatePostProcesses();
        });
        
        ui.CreateSlider(1, 16, () => _expandPostProcess1.ExpansionWidth,
            newValue =>
            {
                _expandPostProcess1.ExpansionWidth = (int)newValue;
                _expandPostProcess2.ExpansionWidth = (int)newValue;
            },
            width: 100,
            keyText: "      ExpansionWidth:",
            keyTextWidth: 140,
            formatShownValueFunc: sliderValue => $"{sliderValue:F0}");

        var expandColors = new Color4[] { Colors.Red, Colors.Blue, Colors.Black };
        ui.CreateComboBox(new string[] { "unchanged", "Red", "Blue", "Black" },
            (selectedIndex, selectedText) =>
            {
                Vector4 colorOffsets, colorFactors;
                if (selectedIndex == 0)
                {
                    // Preserve the object colors
                    colorOffsets = ExpandPostProcess.DefaultOffsets; // = new Vector4(0, 0, 0, 0);
                    colorFactors = ExpandPostProcess.DefaultFactors; // = new Vector4(1, 1, 1, 1);
                }
                else
                {
                    // With Offsets and Factors we can adjust the colors of the effect.
                    // Offsets are added to each color and then the color is multiplied by Factors.
                    //
                    // The following values render expansion in the specified color,
                    // for example for red (1, 0, 0, 1) the red color is added to the original color
                    // and then the color is multiplied by (1, 0, 0, 1) to clear the green and blue color components.

                    colorOffsets = expandColors[selectedIndex - 1].ToVector4();
                    colorFactors = colorOffsets;
                }
                
                _expandPostProcess1.Offsets = colorOffsets;
                _expandPostProcess1.Factors = colorFactors;

                _expandPostProcess2.Offsets = colorOffsets;
                _expandPostProcess2.Factors = colorFactors;
            },
            selectedItemIndex: 0,
            width: 100,
            keyText: "      Expand color:",
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