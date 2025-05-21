using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class CircleModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "CircleModelNode";

    private int _segmentsCount = 30;
    
    private float _innerRadius = 0;
    private float _radius = 50;

    private Vector3 _normal = new Vector3(0, 1, 0);
    private Vector3 _upDirection = new Vector3(0, 0, -1);

    private CircleModelNode? _circleModelNode;

    private LineNode? _normalLine;
    private LineNode? _upDirectionLine;
    private float _directionLinesLength = 60;

    public CircleModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _circleModelNode = new CircleModelNode("SampleCircle")
        {
            CenterPosition = new Vector3(0, 0, 0),
            Radius = _radius,
            InnerRadius = _innerRadius // When InnerRadius is bigger than zero, then the center of the circle is hollow until the InnerRadius.
        };

        // Use MeshFactory.CreateCircleMesh to create a circle mesh, for example:
        //StandardMesh circleMesh = MeshFactory.CreateCircleMesh(centerPosition: new Vector3(0, 0, 0), _normal, _upDirection, radius: 50, _segmentsCount, name: "CircleMesh");

        UpdateModelNode();

        return _circleModelNode;
    }

    protected override void OnCreateScene(Scene scene)
    {
        base.OnCreateScene(scene);

        _normalLine = new LineNode("NormalLine")
        {
            LineColor = Colors.Red,
            LineThickness = 2,
            StartPosition = new Vector3(0, 0, 0),
            EndPosition = new Vector3(0, _directionLinesLength, 0),
            EndLineCap = LineCap.ArrowAnchor
        };

        scene.RootNode.Add(_normalLine);

        _upDirectionLine = new LineNode("UpDirectionLine")
        {
            LineColor = Colors.Green,
            LineThickness = 2,
            StartPosition = new Vector3(0, 0, 0),
            EndPosition = new Vector3(0, 0, -_directionLinesLength),
            EndLineCap = LineCap.ArrowAnchor
        };

        scene.RootNode.Add(_upDirectionLine);
    }

    protected override void UpdateModelNode()
    {
        if (_circleModelNode == null)
            return;

        _circleModelNode.InnerRadius = _innerRadius;
        _circleModelNode.Radius = _radius;
        
        _circleModelNode.Segments = _segmentsCount;

        _circleModelNode.Normal = _normal;
        _circleModelNode.UpDirection = _upDirection;

        if (_normalLine != null)
            _normalLine.EndPosition = _normalLine.StartPosition + _normal * _directionLinesLength;

        if (_upDirectionLine != null)
            _upDirectionLine.EndPosition = _upDirectionLine.StartPosition + _upDirection * _directionLinesLength;

        base.UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        ui.CreateKeyValueLabel("CenterPosition:", () => "(0, 0, 0)", keyTextWidth: 110);
        
        ui.AddSeparator();
        
        ui.CreateSlider(10, 70,
            () => _radius,
            newValue =>
            {
                _radius = newValue;
                UpdateModelNode();
            },
            100,
            keyText: "Radius:",
            keyTextWidth: 110,
            formatShownValueFunc: newValue => newValue.ToString("F1"));
        
        ui.CreateSlider(0, 50,
            () => _innerRadius,
            newValue =>
            {
                _innerRadius = newValue;
                UpdateModelNode();
            },
            100,
            keyText: "InnerRadius:",
            keyTextWidth: 110,
            formatShownValueFunc: newValue => newValue.ToString("F1"));
        
        ui.AddSeparator();

        CreateComboBoxWithVectors(ui: ui, vectors: new Vector3[] { new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1) },
                                  itemChangedAction: OnNormalChanged,
                                  selectedItemIndex: 0,
                                  width: 120,
                                  keyText: "Normal: ",
                                  keyTextWidth: 110).SetColor(Colors.Red);

        CreateComboBoxWithVectors(ui: ui, vectors: new Vector3[] { new Vector3(0, 0, -1), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, -1) },
                                  itemChangedAction: OnUpDirectionChanged,
                                  selectedItemIndex: 0,
                                  width: 120,
                                  keyText: "UpDirection: ",
                                  keyTextWidth: 110).SetColor(Colors.Green);

        ui.AddSeparator();

        ui.CreateSlider(3, 40,
            () => _segmentsCount,
            newValue =>
            {
                if (_segmentsCount == (int)newValue)
                    return; // Do no update when only decimal part of the value is changed
                
                _segmentsCount = (int)newValue;
                UpdateModelNode();
            },
            100,
            keyText: "Segments:",
            keyTextWidth: 110,
            formatShownValueFunc: newValue => ((int)newValue).ToString());

        ui.CreateLabel("(Default value for Segments is 30)").SetStyle("italic");

        AddMeshStatisticsControls(ui);
    }

    private void OnNormalChanged(int itemIndex, Vector3 selectedVector)
    {
        _normal = Vector3.Normalize(selectedVector);
        UpdateModelNode();
    }
    
    private void OnUpDirectionChanged(int itemIndex, Vector3 selectedVector)
    {
        _upDirection = Vector3.Normalize(selectedVector);
        UpdateModelNode();
    }
}