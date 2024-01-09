using System.Numerics;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class PyramidModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "PyramidModelNode";

    private float _sizeX = 80;
    private float _sizeY = 60;
    private float _sizeZ = 80;

    private PyramidModelNode? _pyramidModelNode;

    public PyramidModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _pyramidModelNode = new PyramidModelNode("SamplePyramid")
        {
            BottomCenterPosition = new Vector3(0, -20, 0),
            Size = new Vector3(_sizeX, _sizeY, _sizeZ),
        };

        UpdateModelNode();

        return _pyramidModelNode;
    }

    protected override void UpdateModelNode()
    {
        if (_pyramidModelNode == null)
            return;
        
        _pyramidModelNode.Size = new Vector3(_sizeX, _sizeY, _sizeZ);

        base.UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateLabel("BottomCenterPosition: (0, -20, 0)");

        ui.AddSeparator();

        ui.AddSeparator();
        ui.CreateSlider(0, 100,
            () => _sizeX,
            newValue =>
            {
                _sizeX = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "SizeX: ",
            keyTextWidth: 50,
            formatShownValueFunc: newValue => newValue.ToString("F0"));

        ui.AddSeparator();
        ui.CreateSlider(0, 100,
            () => _sizeY,
            newValue =>
            {
                _sizeY = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "SizeY: ",
            keyTextWidth: 50,
            formatShownValueFunc: newValue => newValue.ToString("F0"));

        ui.AddSeparator();
        ui.CreateSlider(0, 100,
            () => _sizeZ,
            newValue =>
            {
                _sizeZ = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "SizeZ: ",
            keyTextWidth: 50,
            formatShownValueFunc: newValue => newValue.ToString("F0"));

    }
}