using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Lights;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Utilities;
using System.Globalization;
using System.Numerics;

namespace Ab4d.SharpEngine.Samples.Common.Lights;

public class PCSShadowsSample : CommonSample
{
    public override string Title => "Percentage Closer Soft Shadows (PCSS)";
    public override string Subtitle => "See also 'Advanced 3D models / Planar shadows' sample.";
    
    private DirectionalLight? _directionalLight;
    private SpotLight? _spotLight;
    private SoftShadowRenderingProvider? _softShadowRenderingProvider;

    private WireCrossNode? _spotLightWireCross;
        
    private float _lightHorizontalAngle = 30;
    private float _lightVerticalAngle = 15;
    private float _spotLightXPosition = 0;
    private float _spotLightYPosition = 150;
    private float _lightDistance = 500;
    
    private ICommonSampleUIElement? _spotlightPositionLabel;
    private ICommonSampleUIElement? _spotlightXPositionSlider;
    private ICommonSampleUIElement? _spotlightYPositionSlider;

    public PCSShadowsSample(ICommonSamplesContext context)
        : base(context)
    {
    }

    private void SetupShadowRendering(Scene scene)
    {
        if (_softShadowRenderingProvider != null)
            return;
        
        if (_directionalLight != null)
            _softShadowRenderingProvider = scene.AddShadowLight(_directionalLight);
        else if (_spotLight != null)
            _softShadowRenderingProvider = scene.AddShadowLight(_spotLight);
        else
            return;
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
        _softShadowRenderingProvider?.Dispose();
        
        // We could also call:
        //Scene.RemoveShadowLight(_directionalLight);
        
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
        scene.SetAmbientLight(intensity: 0.3f);
        
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

    //private void UpdateShadowSettings()
    //{
    //    if (_softShadowRenderingProvider == null)
    //        return;

    //    _softShadowRenderingProvider.ShadowMapSize = GetComboBoxIntValue(ShadowMapSizeComboBox, defaultValue: 1024);
    //    _softShadowRenderingProvider.ShadowSamplesCount = GetComboBoxIntValue(ShadowSamplesCountComboBox, defaultValue: 1);

    //    _softShadowRenderingProvider.ShadowNormalBias   = GetComboBoxFloatValue(NormalDepthBiasComboBox);
    //    _softShadowRenderingProvider.ShadowConstantBias = GetComboBoxFloatValue(ConstantBiasComboBox);
    //    _softShadowRenderingProvider.ShadowSlopeBias    = GetComboBoxFloatValue(SlopeBiasComboBox);

    //    _softShadowRenderingProvider.ShadowBlur = (float)BlurSlider.Value;
    //    _softShadowRenderingProvider.ShadowLightSize = (float)LightSizeSlider.Value;
    //    _softShadowRenderingProvider.ShadowBlockerSearchRadius = (float)ShadowBlockerSearchRadiusSlider.Value;
    //}

    //private void ShadowMapSizeComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    //{
    //    if (_softShadowRenderingProvider == null)
    //        return;

    //    _softShadowRenderingProvider.ShadowMapSize = GetComboBoxIntValue(ShadowMapSizeComboBox, defaultValue: 1024);
    //}

    //private void ShadowSamplesCount_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    //{
    //    if (_softShadowRenderingProvider == null)
    //        return;

    //    _softShadowRenderingProvider.ShadowSamplesCount = GetComboBoxIntValue(ShadowSamplesCountComboBox, defaultValue: 1);
    //}

    //private void DepthBiasComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    //{
    //    if (_softShadowRenderingProvider == null)
    //        return;

    //    _softShadowRenderingProvider.ShadowNormalBias   = GetComboBoxFloatValue(NormalDepthBiasComboBox);
    //    _softShadowRenderingProvider.ShadowConstantBias = GetComboBoxFloatValue(ConstantBiasComboBox);
    //    _softShadowRenderingProvider.ShadowSlopeBias    = GetComboBoxFloatValue(SlopeBiasComboBox);
    //}

    //private int GetComboBoxIntValue(ComboBox comboBox, int defaultValue = 0)
    //{
    //    if (comboBox.SelectedItem is ComboBoxItem comboBoxItem && comboBoxItem.Content is string stringContent)
    //        return int.Parse(stringContent);

    //    return defaultValue;
    //}

    //private float GetComboBoxFloatValue(ComboBox comboBox, float defaultValue = 0)
    //{
    //    if (comboBox.SelectedItem is ComboBoxItem comboBoxItem && comboBoxItem.Content is string stringContent)
    //        return float.Parse(stringContent, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

    //    return defaultValue;
    //}

    //private void LightDirectionSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    //{
    //    if (!this.IsLoaded)
    //        return;

    //    _lightHorizontalAngle = (float)HorizontalDirectionSlider.Value;
    //    _lightVerticalAngle = (float)VerticalDirectionSlider.Value;

    //    UpdateLights();
    //}

    //private void SpotLightPositionSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    //{
    //    if (_spotLight == null)
    //        return;

    //    _spotLight.Position = new Vector3((float)SpotLightXPositionSlider.Value, 
    //                                      (float)SpotLightYPositionSlider.Value,
    //                                      400);

    //    if (_spotLightWireCross != null)
    //        _spotLightWireCross.Position = _spotLight.Position;

    //    UpdateLights();
    //}

    //private void BlurSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    //{
    //    if (_softShadowRenderingProvider == null)
    //        return;

    //    _softShadowRenderingProvider.ShadowBlur = (float)BlurSlider.Value;
    //    BlurTextBlock.Text = _softShadowRenderingProvider.ShadowBlur.ToString("F1");
    //}

    //private void LightSizeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    //{
    //    if (_softShadowRenderingProvider == null)
    //        return;

    //    _softShadowRenderingProvider.ShadowLightSize = (float)LightSizeSlider.Value;
    //    LightSizeTextBlock.Text = _softShadowRenderingProvider.ShadowLightSize.ToString("F1");
    //}

    //private void AmbientLightSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    //{
    //    if (_softShadowRenderingProvider == null)
    //        return;

    //    var ambientLight = 0.01f * (float)AmbientLightSlider.Value;
    //    _sharpEngineSceneView.Scene.SetAmbientLight(ambientLight);
    //    AmbientLightTextBlock.Text = AmbientLightSlider.Value.ToString("N0");
    //}

    //private void ShadowBlockerSearchRadiusSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    //{
    //    if (_softShadowRenderingProvider == null)
    //        return;

    //    _softShadowRenderingProvider.ShadowBlockerSearchRadius = (float)ShadowBlockerSearchRadiusSlider.Value;
    //    RadiusValueTextBlock.Text = _softShadowRenderingProvider.ShadowBlockerSearchRadius.ToString("F0");
    //}

    //private void EnableShadowCheckBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    //{
    //    if (!this.IsLoaded)
    //        return;

    //    bool isEnabled = EnableShadowCheckBox.IsChecked ?? false;

    //    if (_softShadowRenderingProvider != null && !isEnabled)
    //    {
    //        //_sharpEngineSceneView.Scene.RemoveShadowLight(_directionalLight);

    //        _softShadowRenderingProvider.Dispose();
    //        _softShadowRenderingProvider = null;
    //    }
    //    else if (_softShadowRenderingProvider == null && isEnabled)
    //    {
    //        if (_directionalLight != null)
    //            _softShadowRenderingProvider = _sharpEngineSceneView.Scene.AddShadowLight(_directionalLight);
    //        else if (_spotLight != null)
    //            _softShadowRenderingProvider = _sharpEngineSceneView.Scene.AddShadowLight(_spotLight);

    //        UpdateShadowSettings();
    //    }
    //}

    private void ChangeLightType(bool isSpotLight)
    {
        if (Scene == null)
            return;
        
        _softShadowRenderingProvider?.Dispose();

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
            _softShadowRenderingProvider = Scene.AddShadowLight(_spotLight);


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
            _softShadowRenderingProvider = Scene.AddShadowLight(_directionalLight);

            if (_spotLightWireCross != null)
            {
                Scene.RootNode.Remove(_spotLightWireCross);
                _spotLightWireCross = null;
            }
        }

        _spotlightPositionLabel?.SetIsVisible(_spotLight != null);
        _spotlightXPositionSlider?.SetIsVisible(_spotLight != null);
        _spotlightYPositionSlider?.SetIsVisible(_spotLight != null);
    }

    protected override void OnCreateUI(ICommonSampleUIProvider ui)
    {
        ui.CreateStackPanel(PositionTypes.Bottom | PositionTypes.Right);

        ui.CreateRadioButtons(new string[] { "DirectionalLight", "SpotLight" }, (selectedIndex, selectedText) =>
        {
            ChangeLightType(isSpotLight: selectedIndex == 1);
        }, selectedItemIndex: 0);
        
        ui.AddSeparator();
        
        ui.CreateLabel("Light direction:");
        ui.CreateSlider(-180, 180, () => _lightHorizontalAngle, newValue =>
        {
            _lightHorizontalAngle = newValue;
            UpdateLights();
        }, width: 120);
        
        ui.CreateSlider(0, 90, () => _lightVerticalAngle, newValue =>
        {
            _lightVerticalAngle = newValue;
            UpdateLights();
        }, width: 120);
        
        
        _spotlightPositionLabel = ui.CreateLabel("\nSpotLight position:").SetIsVisible(false);
        _spotlightXPositionSlider = ui.CreateSlider(-200, 200, () => _lightHorizontalAngle, newValue =>
        {
            _spotLightXPosition = newValue;
            UpdateLights();
        }, width: 120).SetIsVisible(false);
        
        _spotlightYPositionSlider = ui.CreateSlider(0, 300, () => _lightVerticalAngle, newValue =>
        {
            _spotLightYPosition = newValue;
            UpdateLights();
        }, width: 120).SetIsVisible(false);
    }    
}