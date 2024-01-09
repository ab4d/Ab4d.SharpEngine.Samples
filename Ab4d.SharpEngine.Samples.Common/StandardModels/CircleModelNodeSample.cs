using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class CircleModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "CircleModelNode";

    private int _segmentsCount = 30;

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
            Radius = 50,
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
        ui.CreateKeyValueLabel("Radius:", () => "50", keyTextWidth: 110);

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
                _segmentsCount = (int)newValue;
                UpdateModelNode();
            },
            100,
            keyText: "Segments:",
            keyTextWidth: 110,
            formatShownValueFunc: newValue => ((int)newValue).ToString());

        ui.CreateLabel("(Default value for Segments is 30)").SetStyle("italic");
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