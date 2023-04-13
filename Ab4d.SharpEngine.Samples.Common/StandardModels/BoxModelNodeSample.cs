using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class BoxModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "BoxModelNode";

    private int _xSegmentsCount = 5;
    private int _ySegmentsCount = 2;
    private int _zSegmentsCount = 4;

    private BoxModelNode? _boxModelNode;

    public BoxModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _boxModelNode = new BoxModelNode("SampleBox")
        {
            Position = new Vector3(0, 0, 0),
            Size = new Vector3(100, 40, 80),
        };

        UpdateModelNode();

        return _boxModelNode;
    }

    protected override void OnCreateScene(Scene scene)
    {
        base.OnCreateScene(scene);

        var wireCrossNode = new WireCrossNode()
        {
            Position = new Vector3(0, 0, 0),
            LineColor = Colors.Red,
            LineThickness = 2,
            LinesLength = 50
        };

        scene.RootNode.Add(wireCrossNode);
    }

    protected override void UpdateModelNode()
    {
        if (_boxModelNode == null)
            return;

        _boxModelNode.XSegmentsCount = _xSegmentsCount;
        _boxModelNode.YSegmentsCount = _ySegmentsCount;
        _boxModelNode.ZSegmentsCount = _zSegmentsCount;

        base.UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateKeyValueLabel("Size:", () => "(100, 20, 80)", keyTextWidth: 80);

        ui.CreateKeyValueLabel("Position:", () => "(0, 0, 0)", keyTextWidth: 80).SetColor(Colors.Red);

        var enumNames = Enum.GetNames<PositionTypes>();
        ui.CreateComboBox(items: enumNames,
                          itemChangedAction: OnPositionTypeChanged, 
                          selectedItemIndex: 0,
                          width: 120,
                          keyText: "PositionType: ",
                          keyTextWidth: 80);

        ui.AddSeparator();

        ui.CreateSlider(1, 10,
            () => _xSegmentsCount,
            newValue =>
            {
                _xSegmentsCount = (int)newValue;
                UpdateModelNode();
            },
            100,
            keyText: "X Segments:",
            formatShownValueFunc: newValue => ((int)newValue).ToString());
        
        ui.CreateSlider(1, 10,
            () => _ySegmentsCount,
            newValue =>
            {
                _ySegmentsCount = (int)newValue;
                UpdateModelNode();
            },
            100,
            keyText: "Y Segments:",
            formatShownValueFunc: newValue => ((int)newValue).ToString());

        ui.CreateSlider(1, 10,
            () => _zSegmentsCount,
            newValue =>
            {
                _zSegmentsCount = (int)newValue;
                UpdateModelNode();
            },
            100,
            keyText: "Z Segments:",
            formatShownValueFunc: newValue => ((int)newValue).ToString());


        ui.CreateLabel("(Default value for Segments is 1)").SetStyle("italic");
    }

    private void OnPositionTypeChanged(int index, string? itemText)
    {
        if (_boxModelNode == null)
            return;

        PositionTypes positionType;

        if (itemText == null)
            positionType = PositionTypes.Center;
        else
            positionType = Enum.Parse<PositionTypes>(itemText);

        _boxModelNode.PositionType = positionType;

        UpdateModelNode();
    }
}