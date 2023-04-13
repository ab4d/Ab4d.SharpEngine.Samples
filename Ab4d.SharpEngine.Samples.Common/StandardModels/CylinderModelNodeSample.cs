using System.Numerics;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class CylinderModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "CylinderModelNode";

    private int _sphereSegmentsCount = 15;
    private bool _isSmooth = false;
    private float _radius = 50;
    private float _height = 50;

    private CylinderModelNode? _cylinderModelNode;

    public CylinderModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _cylinderModelNode = new CylinderModelNode("SampleCylinder")
        {
            BottomCenterPosition = new Vector3(0, 0, 0),
            Radius = 50,
            Height = 50,
        };

        UpdateModelNode();

        return _cylinderModelNode;
    }

    protected override void UpdateModelNode()
    {
        if (_cylinderModelNode == null)
            return;

        _cylinderModelNode.Segments = _sphereSegmentsCount;
        _cylinderModelNode.IsSmooth = _isSmooth;
        _cylinderModelNode.Radius = _radius;
        _cylinderModelNode.Height = _height;
        
        base.UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateKeyValueLabel("BottomCenterPosition:", () => "(0, -25, 0)", keyTextWidth: 110);

        ui.AddSeparator();

        ui.CreateCheckBox("IsSmooth", false, OnIsSmoothChanged);

        ui.AddSeparator();
        ui.CreateSlider(0, 80,
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
            () => _radius,
            newValue =>
            {
                _radius = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "Radius: ",
            keyTextWidth: 100,
            formatShownValueFunc: newValue => newValue.ToString("F0"));

        ui.AddSeparator();
        ui.CreateSlider(3, 40,
                        () => _sphereSegmentsCount,
                        newValue =>
                        {
                            _sphereSegmentsCount = (int)newValue;
                            UpdateModelNode();
                        },
                        120,
                        keyText: "Segments: ",
                        keyTextWidth: 100,
                        formatShownValueFunc: newValue => ((int)newValue).ToString());

        ui.CreateLabel("(Default value for Segments is 30)").SetStyle("italic");

        ui.CreateLabel(
@"Cylinder is always oriented along Y axis (up).
If you want to define cylinder in some other 
direction use TubeLine").SetStyle("italic");
    }

    private void OnIsSmoothChanged(bool isChecked)
    {
        _isSmooth = isChecked;
        UpdateModelNode();
    }
}