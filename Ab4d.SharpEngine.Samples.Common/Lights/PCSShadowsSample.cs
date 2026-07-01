using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Numerics;
using Ab4d.SharpEngine.Cameras;

namespace Ab4d.SharpEngine.Samples.Common.Lights;

public class PCSShadowsSample : CommonSample
{
    public override string Title => "Percentage Closer Soft Shadows (PCSS)";
    public override string Subtitle => "See also 'Advanced 3D models / Planar shadows' sample for simple planar shadows.";
    
    private DirectionalLight? _directionalLight;
    private SpotLight? _spotLight;
    private SoftShadowRenderingProvider? _softShadowRenderingProvider;

    private WireCrossNode? _spotLightWireCross;
        
    private float _lightHorizontalAngle = 30;
    private float _lightVerticalAngle = 15;
    private float _spotLightXPosition = 0;
    private float _spotLightYPosition = 150;
    private float _lightDistance = 500;
    private float _ambientLight = 0.3f;
    
    private int _shadowMapSize = 1024;
    private float _shadowNormalBias = 1.0f;
    private float _shadowConstantBias = 0.001f;
    private float _shadowSlopeBias = 1.0f;
    private int _shadowSamplesCount = 16;
    private float _shadowBlur = 1.5f;
    private float _shadowLightSize = 6;
    private float _shadowBlockerSearchRadius = 30;
    
    private float _savedShadowLightSize;
    private float _savedShadowBlockerSearchRadius;
    
    private int[]? _shadowSamplesCountOptions;
    private ICommonSampleUIElement? _spotlightPositionLabel;
    private ICommonSampleUIElement? _spotlightXPositionSlider;
    private ICommonSampleUIElement? _spotlightYPositionSlider;
    private ICommonSampleUIElement? _shadowSamplesCountComboBox;
    private ICommonSampleUIElement? _lightSizeSlider;
    private ICommonSampleUIElement? _blockerSearchRadiusSlider;

    public PCSShadowsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    private void SetupShadowRendering(Scene scene)
    {
        if (_softShadowRenderingProvider != null)
            return; // already created

        var shadowLight = (ILight?)_directionalLight ?? (ILight?)_spotLight;
        if (shadowLight != null)
        {
            _softShadowRenderingProvider = scene.CreateSoftShadowRenderingProvider(shadowLight);
            UpdateShadowSettings();
        }
    }
    
    protected override void OnCreateScene(Scene scene)
    {
        var greenPlane = new PlaneModelNode(new Vector3(0, 0, 0), new Vector2(1000, 1000), new Vector3(0, 1, 0), new Vector3(0, 0, 1))
        {
            Material = StandardMaterials.LightGreen,
            BackMaterial = StandardMaterials.DarkGreen
        };
        
        scene.RootNode.Add(greenPlane);

        
        for (int i = 0; i < 7; i ++)
        {
            float x = -300 + i * 100;
            
            var yellowBox1 = new BoxModelNode(new Vector3(x, 35, 0), new Vector3(20, 70, 20), StandardMaterials.Yellow);
            scene.RootNode.Add(yellowBox1);
            
            var yellowBox2 = new BoxModelNode(new Vector3(x, 110, 0), new Vector3(20, 20, 20), StandardMaterials.Yellow);
            scene.RootNode.Add(yellowBox2);


            float y = 20 + i * 5;
            
            var sphereModelNode2 = new SphereModelNode(new Vector3(x, y, 250), 20, StandardMaterials.Blue);
            scene.RootNode.Add(sphereModelNode2);                
        }


        if (targetPositionCamera != null)
        {
            targetPositionCamera.Heading = 60;
            targetPositionCamera.Attitude = -20;
            targetPositionCamera.Distance = 1200;
        }

        SetupShadowRendering(scene);
    }

    protected override void OnDisposed()
    {
        // This will dispose all shadow resources and remove the shadow light from the Scene
        if (_softShadowRenderingProvider != null && !_softShadowRenderingProvider.IsDisposed)
        {
            _softShadowRenderingProvider.Dispose();
            _softShadowRenderingProvider = null;
        }

        base.OnDisposed();
    }

    protected override void OnCreateLights(Scene scene)
    {
        scene.Lights.Clear();

        // Add lights
        var direction = new Vector3(-1, -0.3f, 0.5f);
        direction = Vector3.Normalize(direction);
        _directionalLight = new DirectionalLight(direction);
        
        scene.Lights.Add(_directionalLight);


        // Set ambient light (illuminates the objects from all directions)
        scene.SetAmbientLight(intensity: _ambientLight);
        
        UpdateLights();
        
        
        SetupShadowRendering(scene);
    }

    private Vector3 CalculateLightPosition()
    {
        // Handle vertical angle close to 90 degrees
        if (MathUtils.IsSame(_lightVerticalAngle, 90, 0.01f))
            return new Vector3(0, _lightDistance, 0);
        
        float xRad = _lightHorizontalAngle * MathF.PI / 180.0f;
        float yRad = _lightVerticalAngle * MathF.PI / 180.0f;

        float x = (MathF.Sin(xRad) * MathF.Cos(yRad)) * _lightDistance;
        float y = MathF.Sin(yRad) * _lightDistance;
        float z = (MathF.Cos(xRad) * MathF.Cos(yRad)) * _lightDistance;

        return new Vector3(x, y, z);
    }
    
    private void UpdateLights()
    {
        if (_directionalLight == null && _spotLight == null)
            return;
        
        var position = CalculateLightPosition();

        // Create direction from position - target position = (0,0,0)
        var lightDirection = new Vector3(-position.X, -position.Y, -position.Z);
        lightDirection = Vector3.Normalize(lightDirection);

        if (_directionalLight != null)
            _directionalLight.Direction = lightDirection;

        if (_spotLight != null)
        {
            _spotLight.Direction = lightDirection;
            _spotLight.Position = new Vector3(_spotLightXPosition, _spotLightYPosition, 400);
            
            if (_spotLightWireCross != null)
                _spotLightWireCross.Position = _spotLight.Position;
        }
    }
    
    
    private void UpdateShadowSettings()
    {
        if (_softShadowRenderingProvider == null)
            return;

        _softShadowRenderingProvider.ShadowMapSize = _shadowMapSize;
        _softShadowRenderingProvider.ShadowSamplesCount = _shadowSamplesCount;

        _softShadowRenderingProvider.ShadowNormalBias = _shadowNormalBias;
        _softShadowRenderingProvider.ShadowConstantBias = _shadowConstantBias;
        _softShadowRenderingProvider.ShadowSlopeBias = _shadowSlopeBias;

        _softShadowRenderingProvider.ShadowBlur = _shadowBlur;
        _softShadowRenderingProvider.ShadowLightSize = _shadowLightSize;
        _softShadowRenderingProvider.ShadowBlockerSearchRadius = _shadowBlockerSearchRadius;
    }

    private void ChangeLightType(bool isSpotLight)
    {
        if (Scene == null)
            return;
        
        if (_softShadowRenderingProvider != null && !_softShadowRenderingProvider.IsDisposed)
        {
            _softShadowRenderingProvider.Dispose();
            _softShadowRenderingProvider = null;
        }

        if (isSpotLight)
        {
            if (_directionalLight != null)
                Scene.Lights.Remove(_directionalLight);

            _spotLight = new SpotLight()
            {
                InnerConeAngle = 30,
                OuterConeAngle = 40
            };
            
            UpdateLights();

            _directionalLight = null;

            Scene.Lights.Add(_spotLight);

            _spotLightWireCross = new WireCrossNode(_spotLight.Position, Colors.Yellow, 20, 2, "SpotLightWireCross");
            Scene.RootNode.Add(_spotLightWireCross);
        }
        else
        {
            if (_spotLight != null)
                Scene.Lights.Remove(_spotLight);

            _directionalLight = new DirectionalLight();
            UpdateLights();

            _spotLight = null;

            Scene.Lights.Add(_directionalLight);

            if (_spotLightWireCross != null)
            {
                Scene.RootNode.Remove(_spotLightWireCross);
                _spotLightWireCross = null;
            }
        }
        
        // Create a new SoftShadowRenderingProvider based on the current _directionalLight / _spotLight
        SetupShadowRendering(Scene);

        _spotlightPositionLabel?.SetIsVisible(_spotLight != null);
        _spotlightXPositionSlider?.SetIsVisible(_spotLight != null);
        _spotlightYPositionSlider?.SetIsVisible(_spotLight != null);
    }

    private void EnableDisableShadows(bool isEnabled)
    {
        if (Scene == null)
            return;
        
        if (_softShadowRenderingProvider != null && !isEnabled)
        {
            Scene.RemoveShadowRenderingProvider(disposeResources: true);
            
            // Instead of Scene.RemoveShadowRenderingProvider we could also call Dispose:
            //_softShadowRenderingProvider.Dispose();
            
            _softShadowRenderingProvider = null;
        }
        else if (_softShadowRenderingProvider == null && isEnabled)
        {
            // Create a new SoftShadowRenderingProvider and add it to the Scene  
            SetupShadowRendering(Scene);
        }
    }
    
    private void UpdatePercentageCloserBlur(bool isEnabled)
    {
        if (isEnabled)
        {
            _shadowLightSize = _savedShadowLightSize;
            _shadowBlockerSearchRadius = _savedShadowBlockerSearchRadius;
            
            _lightSizeSlider?.UpdateValue();
            _blockerSearchRadiusSlider?.UpdateValue();
        }
        else
        {
            _savedShadowLightSize = _shadowLightSize;
            _savedShadowBlockerSearchRadius = _shadowBlockerSearchRadius;
         
            // Setting ShadowLightSize or ShadowBlockerSearchRadius to 0 disables the Percentage Closer Soft Shadows (PCSS) effect
            // and renders hard shadows (note that we can still use ShadowBlur that applies constant blur)
            _shadowLightSize = 0;
            _shadowBlockerSearchRadius = 0;
        }
        
        _shadowSamplesCountComboBox?.SetIsVisible(isVisible: isEnabled);
        _lightSizeSlider?.SetIsVisible(isVisible: isEnabled);
        _blockerSearchRadiusSlider?.SetIsVisible(isVisible: isEnabled);

        UpdateShadowSettings(); // SetValue will call UpdateShadowSettings
    }

    
    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        
        ui.CreateLabel("Shadow settings:", isHeader: true);

        ui.CreateCheckBox("Shadow rendering", true, isChecked => EnableDisableShadows(isChecked));

        ui.CreateCheckBox(
@"Percentage Closer Soft Shadow (PCSS) (?):By default the shadow generates soft shadows with a penumbra 
(increased blur as the distance from the shadow casting object increases).
This is controlled by the LightSize and BlockerSearchRadius. If those two values are 0,
then we get hard shadows that are uniformly blurred based on the ShadowBlur value.

Unchecking this value will set LightSize and BlockerSearchRadius to 0.", 
            true, isChecked => UpdatePercentageCloserBlur(isChecked));
        
        
        ui.AddSeparator();
        
        ui.CreateSlider(0, 10, () => _shadowBlur, newValue =>
            {
                _shadowBlur = newValue;
                UpdateShadowSettings();
            }, width: 100,
            keyText: 
@"ShadowBlur: (?):ShadowBlur defines the blur amount for the shadows.
This value uniformly blurs the shadow regardless of its distance from the shadow casting object.", 
            keyTextWidth: 150, 
            formatShownValueFunc: sliderValue => sliderValue.ToString("N1"));
        
        _lightSizeSlider = ui.CreateSlider(0, 20, () => _shadowLightSize, newValue =>
            {
                _shadowLightSize = newValue;
                UpdateShadowSettings();
            }, width: 100,
            keyText: 
@"ShadowLightSize: (?):ShadowLightSize controls the size of the light source used for generating soft shadows. 

A larger value results in softer shadows with a more pronounced penumbra, 
while a smaller value produces harder shadows.", 
            keyTextWidth: 150, 
            formatShownValueFunc: sliderValue => sliderValue.ToString("N1"));
        
        _blockerSearchRadiusSlider = ui.CreateSlider(0, 200, () => _shadowBlockerSearchRadius, newValue =>
            {
                _shadowBlockerSearchRadius = newValue;
                UpdateShadowSettings();
            }, width: 100,
            keyText: 
@"BlockerSearchRadius: (?):BlockerSearchRadius defines a radius used to search for shadow blockers 
in the Percentage Closer Soft Shadows (PCSS) algorithm. 

A larger value increases the area used to determine the shadow blockers, which can result in softer shadows.
The number of checked sample is defined by the ShadowSamplesCount value.", 
            keyTextWidth: 150,
            formatShownValueFunc: sliderValue => sliderValue.ToString("N0"));

        _shadowSamplesCountOptions = new int[] { 8, 16, 32, 64 };
        _shadowSamplesCountComboBox = ui.CreateComboBox(_shadowSamplesCountOptions.Select(f => f.ToString()).ToArray(),
            (selectedIndex, selectedText) =>
            {
                _shadowSamplesCount = _shadowSamplesCountOptions[selectedIndex];
                UpdateShadowSettings();
            }, selectedItemIndex: Array.IndexOf(_shadowSamplesCountOptions, _shadowSamplesCount), 
            width: 80, 
            keyText: 
@"ShadowSamplesCount: (?):ShadowSamplesCount defines the number of samples used to generate the soft shadows.

A larger value results in smoother shadows but requires more computational resources. 
The search radius that is used by the samples is defined by the BlockerSearchRadius value.", 
            keyTextWidth: 160);


        ui.AddSeparator();
        
        var shadowMapSizes = new int[] { 512, 1024, 2048, 4096 };
        ui.CreateComboBox(shadowMapSizes.Select(f => f.ToString()).ToArray(),
            (selectedIndex, selectedText) =>
            {
                _shadowMapSize = shadowMapSizes[selectedIndex];
                UpdateShadowSettings();
            }, selectedItemIndex: Array.IndexOf(shadowMapSizes, _shadowMapSize), 
            width: 80, 
            keyText: 
@"ShadowMapSize: (?):ShadowMapSize sets the size of a shadow depth map texture. 
For example, 1024 means that a 1024 x 1024 texture is used. 
Bigger texture will produce more detailed shadows but will be slower to render.", 
            keyTextWidth: 160);
        
        var shadowBiases = new float[] { 0f, 0.0001f, 0.001f, 0.005f, 0.01f, 0.02f, 0.05f, 0.1f, 0.5f, 1f, 2f, 5f, 10f };
        ui.CreateComboBox(shadowBiases.Select(f => f.ToString()).ToArray(),
            (selectedIndex, selectedText) =>
            {
                _shadowNormalBias = shadowBiases[selectedIndex];
                UpdateShadowSettings();
            }, selectedItemIndex: Array.IndexOf(shadowBiases, _shadowNormalBias), 
            width: 80, 
            keyText: 
@"NormalDepthBias: (?):This value offsets the position of the shadow receiver along its normal vector. 
Bias is used to reduce shadow artifacts, such as shadow acne, caused by precision issues in shadow mapping.", 
            keyTextWidth: 160);
        
        ui.CreateComboBox(shadowBiases.Select(f => f.ToString()).ToArray(),
            (selectedIndex, selectedText) =>
            {
                _shadowConstantBias = shadowBiases[selectedIndex];
                UpdateShadowSettings();
            }, selectedItemIndex: Array.IndexOf(shadowBiases, _shadowConstantBias), 
            width: 80, 
            keyText: 
@"ConstantDepthBias: (?):This value adjusts the bias based on the slope of the surface relative to the light direction.
Bias is used to reduce shadow artifacts, such as shadow acne, caused by precision issues in shadow mapping.", 
            keyTextWidth: 160);
        
        ui.CreateComboBox(shadowBiases.Select(f => f.ToString()).ToArray(),
            (selectedIndex, selectedText) =>
            {
                _shadowSlopeBias = shadowBiases[selectedIndex];
                UpdateShadowSettings();
            }, selectedItemIndex: Array.IndexOf(shadowBiases, _shadowSlopeBias), 
            width: 80, 
            keyText: 
@"SlopeDepthBias: (?):This value offsets the position of the shadow receiver along its normal vector.
Bias is used to reduce shadow artifacts, such as shadow acne, caused by precision issues in shadow mapping.", 
            keyTextWidth: 160);
        
        
        ui.CreateLabel("Light settings:", isHeader: true);
        
        ui.CreateRadioButtons(new string[] { "DirectionalLight", "SpotLight" }, (selectedIndex, selectedText) =>
        {
            ChangeLightType(isSpotLight: selectedIndex == 1);
        }, selectedItemIndex: 0);
        
        ui.AddSeparator();
        
        ui.CreateSlider(-180, 180, () => _lightHorizontalAngle, newValue =>
            {
                _lightHorizontalAngle = newValue;
                UpdateLights();
            }, width: 150,
            keyText: "Light direction:", keyTextWidth: 120);
        
        ui.CreateSlider(0, 90, () => _lightVerticalAngle, newValue =>
            {
                _lightVerticalAngle = newValue;
                UpdateLights();
            }, width: 150,
            keyText: " ", keyTextWidth: 120);
        
        
        ui.AddSeparator();
        
        _spotlightXPositionSlider = ui.CreateSlider(-200, 200, () => _lightHorizontalAngle, newValue =>
            {
                _spotLightXPosition = newValue;
                UpdateLights();
            }, width: 150,
            keyText: "SpotLight position:", keyTextWidth: 120).SetIsVisible(false);
        
        _spotlightYPositionSlider = ui.CreateSlider(0, 300, () => _lightVerticalAngle, newValue =>
            {
                _spotLightYPosition = newValue;
                UpdateLights();
            }, width: 150,
            keyText: " ", keyTextWidth: 120).SetIsVisible(false);
        
        
        ui.AddSeparator();
        
        ui.CreateSlider(0, 100, () => _ambientLight * 100, newValue =>
            {
                _ambientLight = newValue / 100f;
                Scene?.SetAmbientLight(_ambientLight);
            }, width: 150,
            keyText: "Ambient light:", keyTextWidth: 120);
    }    
}