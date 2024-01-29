using System.Numerics;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class ArrowModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "ArrowModelNode";

    private Vector3 _startPosition = new Vector3(0, -40, 0);
    private Vector3 _endPosition = new Vector3(0, 40, 0);
    private float _radius = 10;
    private float _arrowRadius = 20;
    private int _segmentsCount = 15;

    private ArrowModelNode? _arrowModelNode;

    public ArrowModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _arrowModelNode = new ArrowModelNode("SampleArrow");
        UpdateModelNode();

        // Use MeshFactory.CreateArrowMesh to create an arrow mesh, for example:
        //StandardMesh arrowMesh = MeshFactory.CreateArrowMesh(_startPosition, _endPosition, _radius, _arrowRadius, arrowAngle: 30, maxArrowLength: 0.3f, _segmentsCount, generateTextureCoordinates: false, name: "ArrowMesh");

        return _arrowModelNode;
    }

    protected override void UpdateModelNode()
    {
        if (_arrowModelNode == null)
            return;

        _arrowModelNode.StartPosition = _startPosition;
        _arrowModelNode.EndPosition   = _endPosition;
        _arrowModelNode.Radius        = _radius;
        _arrowModelNode.ArrowRadius   = _arrowRadius;
        _arrowModelNode.Segments      = _segmentsCount;
        
        base.UpdateModelNode();
    }

    private void OnStartPositionChanged(int itemIndex, Vector3 selectedVector)
    {
        _startPosition = selectedVector;
        UpdateModelNode();
    }
    
    private void OnEndPositionChanged(int itemIndex, Vector3 selectedVector)
    {
        _endPosition = selectedVector;
        UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        CreateComboBoxWithVectors(ui: ui, vectors: new Vector3[] { new Vector3(0, -40, 0), new Vector3(0, -20, 0), new Vector3(-20, -20, 0), new Vector3(-40, 0, 0) },
            itemChangedAction: OnStartPositionChanged,
            selectedItemIndex: 0,
            width: 125,
            keyText: "StartPosition: ",
            keyTextWidth: 90);
        
        CreateComboBoxWithVectors(ui: ui, vectors: new Vector3[] { new Vector3(0, 20, 0), new Vector3(0, 40, 0), new Vector3(20, -40, 0), new Vector3(40, 0, 0) },
            itemChangedAction: OnEndPositionChanged,
            selectedItemIndex: 1,
            width: 125,
            keyText: "EndPosition: ",
            keyTextWidth: 90);


        ui.AddSeparator();
        ui.CreateSlider(1, 40,
            () => _radius,
            newValue =>
            {
                _radius = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "Radius: ",
            keyTextWidth: 90,
            formatShownValueFunc: newValue => newValue.ToString("F0"));
        
        ui.CreateSlider(1, 40,
            () => _arrowRadius,
            newValue =>
            {
                _arrowRadius = newValue;
                UpdateModelNode();
            },
            120,
            keyText: "ArrowRadius: ",
            keyTextWidth: 90,
            formatShownValueFunc: newValue => newValue.ToString("F0"));

        ui.CreateSlider(3, 40,
            () => _segmentsCount,
            newValue =>
            {
                if (_segmentsCount == (int)newValue)
                    return; // Do no update when only decimal part of the value is changed
                
                _segmentsCount = (int)newValue;
                UpdateModelNode();
            },
            120,
            keyText: "Segments: ",
            keyTextWidth: 90,
            formatShownValueFunc: newValue => ((int)newValue).ToString());

        ui.CreateLabel("(Default value for Segments is 30)").SetStyle("italic");
    }
}