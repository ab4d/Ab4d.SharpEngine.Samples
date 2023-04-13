using System.Numerics;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class SphereModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "SphereModelNode";

    private int _sphereSegmentsCount = 15;

    private ICommonSampleUIElement? _sphereSegmentsKeyValue;
    private SphereModelNode? _sphereModelNode;

    public SphereModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _sphereModelNode = new SphereModelNode("SampleSphere")
        {
            CenterPosition = new Vector3(0, 0, 0),
            Radius = 50
        };

        UpdateModelNode();

        return _sphereModelNode;
    }

    protected override void UpdateModelNode()
    {
        if (_sphereModelNode == null)
            return;

        _sphereModelNode.Segments = _sphereSegmentsCount;
        
        _sphereSegmentsKeyValue?.UpdateValue();

        base.UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateKeyValueLabel("CenterPosition:", () => "(0, 0, 0)", keyTextWidth: 110);
        ui.CreateKeyValueLabel("Radius:", () => "50", keyTextWidth: 110);

        ui.AddSeparator();

        //ui.CreateSlider(3, 40,
        //    () => _sphereSegmentsCount,
        //    newValue =>
        //    {
        //        _sphereSegmentsCount = (int)newValue;
        //        UpdateModelNode();
        //    },
        //    100,
        //    keyText: "Segments:",
        //    formatShownValueFunc: newValue => newValue.ToString("F0"));

        _sphereSegmentsKeyValue = ui.CreateKeyValueLabel("Segments:", () => _sphereSegmentsCount.ToString(), keyTextWidth: 110);

        ui.CreateSlider(3, 40,
                        () => _sphereSegmentsCount,
                        newValue =>
                        {
                            _sphereSegmentsCount = (int)newValue;
                            UpdateModelNode();
                        },
                        200);

        ui.CreateLabel("(Default value for Segments is 30)").SetStyle("italic");
    }
}