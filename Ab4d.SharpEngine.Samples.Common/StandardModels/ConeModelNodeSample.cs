using System.Numerics;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class ConeModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "ConeModelNode";

    private float _topRadius = 20;
    private float _bottomRadius = 50;
    private float _height = 40;
    private int _segmentsCount = 15;
    private bool _isSmooth = false;

    private ConeModelNode? _coneModelNode;

    public ConeModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _coneModelNode = new ConeModelNode("SampleCone")
        {
            BottomCenterPosition = new Vector3(0, -20, 0),
            TopRadius    = _topRadius,
            BottomRadius = _bottomRadius,
            Height       = _height,
            Segments     = _segmentsCount,
            IsSmooth     = _isSmooth
        };

        UpdateModelNode();

        return _coneModelNode;
    }

    protected override void UpdateModelNode()
    {
        if (_coneModelNode == null)
            return;

        
        
        _coneModelNode.TopRadius    = _topRadius;
        _coneModelNode.BottomRadius = _bottomRadius;
        _coneModelNode.Height       = _height;
        _coneModelNode.Segments     = _segmentsCount;
        _coneModelNode.IsSmooth     = _isSmooth;

        base.UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateLabel("BottomCenterPosition: (0, -20, 0)");

        ui.AddSeparator();

        ui.CreateCheckBox("IsSmooth", false, OnIsSmoothChanged);

        ui.AddSeparator();
        ui.CreateSlider(1, 80,
            () => _height,
            newValue =>
            {
                _height = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "Height: ",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => newValue.ToString("F0"));
        
        ui.AddSeparator();
        ui.CreateSlider(0, 60,
            () => _topRadius,
            newValue =>
            {
                _topRadius = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "TopRadius: ",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => newValue.ToString("F0"));
        
        ui.AddSeparator();
        ui.CreateSlider(0, 60,
            () => _bottomRadius,
            newValue =>
            {
                _bottomRadius = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "BottomRadius: ",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => newValue.ToString("F0"));

        ui.AddSeparator();
        ui.CreateSlider(3, 40,
                        () => _segmentsCount,
                        newValue =>
                        {
                            _segmentsCount = (int)newValue;
                            UpdateModelNode();
                        },
                        120,
                        keyText: "Segments: ",
                        keyTextWidth: 100,
                        formatShownValueFunc: newValue => ((int)newValue).ToString());

        ui.CreateLabel("(Default value for Segments is 30)").SetStyle("italic");
    }

    private void OnIsSmoothChanged(bool isChecked)
    {
        _isSmooth = isChecked;
        UpdateModelNode();
    }
}