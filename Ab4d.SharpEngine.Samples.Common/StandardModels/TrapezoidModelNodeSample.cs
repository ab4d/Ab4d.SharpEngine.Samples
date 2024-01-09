using System.Numerics;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Meshes;
using Ab4d.SharpEngine.SceneNodes;

namespace Ab4d.SharpEngine.Samples.Common.StandardModels;

public class TrapezoidModelNodeSample : StandardModelsSampleBase
{
    public override string Title => "TrapezoidModelNode";

    private Vector3 _topCenterPosition = new Vector3(0, 40, 0);
    private Vector3 _bottomCenterPosition = new Vector3(0, -20, 0);
    private Vector2 _topSize = new Vector2(50, 20);
    private Vector2 _bottomSize = new Vector2(100, 50);

    private TrapezoidModelNode? _trapezoidModelNode;
    private WireCrossNode? _topWireCrossNode;
    private WireCrossNode? _bottomWireCrossNode;

    public TrapezoidModelNodeSample(ICommonSamplesContext context) : base(context)
    {
    }

    protected override ModelNode CreateModelNode()
    {
        _trapezoidModelNode = new TrapezoidModelNode("SampleTrapezoid");
        UpdateModelNode();

        // Use MeshFactory.CreateTrapezoidMesh to create a trapezoid mesh, for example:
        //StandardMesh trapezoidMesh = MeshFactory.CreateTrapezoidMesh(_bottomCenterPosition, _bottomSize, _topCenterPosition, _topSize, sizeXVector: new Vector3(1, 0, 0), sizeYVector: new Vector3(0, 0, 1), name: "TrapezoidMesh");

        return _trapezoidModelNode;
    }

    protected override void OnCreateScene(Scene scene)
    {
        base.OnCreateScene(scene);


        _topWireCrossNode = new WireCrossNode("TopWireCrossNode")
        {
            LineColor = Colors.Red,
            LineThickness = 2,
            LinesLength = 60,
            Position = _topCenterPosition,
        };

        scene.RootNode.Add(_topWireCrossNode);
        

        _bottomWireCrossNode = new WireCrossNode("BottomWireCrossNode")
        {
            LineColor = Colors.Blue,
            LineThickness = 2,
            LinesLength = 60,
            Position = _bottomCenterPosition,
        };

        scene.RootNode.Add(_bottomWireCrossNode);
    }

    protected override void UpdateModelNode()
    {
        if (_trapezoidModelNode == null)
            return;
        
        _trapezoidModelNode.TopCenterPosition = _topCenterPosition;
        _trapezoidModelNode.BottomCenterPosition = _bottomCenterPosition;
        _trapezoidModelNode.TopSize = _topSize;
        _trapezoidModelNode.BottomSize = _bottomSize;

        if (_topWireCrossNode != null)
            _topWireCrossNode.Position = _topCenterPosition;
                
        if (_bottomWireCrossNode != null)
            _bottomWireCrossNode.Position = _bottomCenterPosition;

        base.UpdateModelNode();
    }

    private void OnTopCenterPositionChanged(int itemIndex, Vector3 selectedVector)
    {
        _topCenterPosition = selectedVector;
        UpdateModelNode();
    }

    private void OnBottomCenterPositionChanged(int itemIndex, Vector3 selectedVector)
    {
        _bottomCenterPosition = selectedVector;
        UpdateModelNode();
    }

    
    private void OnTopSizeChanged(int itemIndex, Vector2 selectedVector)
    {
        _topSize = selectedVector;
        UpdateModelNode();
    }
    
    private void OnBottomSizeChanged(int itemIndex, Vector2 selectedVector)
    {
        _bottomSize = selectedVector;
        UpdateModelNode();
    }

    protected override void OnCreatePropertiesUI(ICommonSampleUIProvider ui)
    {
        CreateComboBoxWithVectors(ui: ui, vectors: new Vector3[] { new Vector3(0, 60, 0), new Vector3(0, 40, 0), new Vector3(0, 20, 0), new Vector3(40, 40, 0), new Vector3(-20, 40, 0) },
            itemChangedAction: OnTopCenterPositionChanged,
            selectedItemIndex: 1,
            width: 125,
            keyText: "TopCenterPosition: ",
            keyTextWidth: 140).SetColor(Colors.Red);
        
        CreateComboBoxWithVectors(ui: ui, vectors: new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, -20, 0), new Vector3(0, -40, 0), new Vector3(40, -20, 0), new Vector3(-20, -20, 0) },
            itemChangedAction: OnBottomCenterPositionChanged,
            selectedItemIndex: 1,
            width: 125,
            keyText: "BottomCenterPosition: ",
            keyTextWidth: 140).SetColor(Colors.Blue);


        ui.AddSeparator();
        CreateComboBoxWithVectors(ui: ui, vectors: new Vector2[] { new Vector2(0, 0), new Vector2(20, 20), new Vector2(50, 20), new Vector2(50, 50), new Vector2(70, 30), new Vector2(100, 50), new Vector2(50, 100) },
            itemChangedAction: OnTopSizeChanged,
            selectedItemIndex: 2,
            width: 125,
            keyText: "TopSize: ",
            keyTextWidth: 140);
        
        CreateComboBoxWithVectors(ui: ui, vectors: new Vector2[] { new Vector2(0, 0), new Vector2(20, 20), new Vector2(50, 20), new Vector2(50, 50), new Vector2(70, 30), new Vector2(100, 50), new Vector2(50, 100) },
            itemChangedAction: OnBottomSizeChanged,
            selectedItemIndex: 5,
            width: 125,
            keyText: "BottomSize: ",
            keyTextWidth: 140);
    }
}